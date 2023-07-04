using CSCore.CoreAudioAPI;
using CSCore.Win32;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wale.Configs;
//using NAudio.CoreAudioApi;
//using NAudio.CoreAudioApi.Interfaces;

namespace Wale.CoreAudio
{
    public enum DeviceState { Active = 1, Disabled = 2, NotPresent = 4, UnPlugged = 8, All = Active | Disabled | NotPresent | UnPlugged }
    internal class Manager : JPack.NotifyPropertyChanged, IDisposable
    {
        private readonly General gl = new General();
        public Manager(General gl)
        {
            this.gl = gl;
            gl.PropertyChanged += Gl_PropertyChanged;
            Polling = new TimedWorker(gl.ForceMMT);
            Polling.Start(Poll, (int)gl.UIUpdateInterval);
        }
        private void Gl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(gl.UIUpdateInterval):
                    Polling.Interval = (int)gl.UIUpdateInterval;
                    break;
            }
        }

        #region Background Tasks
        private double LastMMDPeakValue = 0;
        protected readonly TimedWorker Polling;
        private void Poll()
        {
            if (CheckAccess<object>())
            {
                try { _MMDPeakValue = MMDPeak?.PeakValue ?? 0; }
                catch (NullReferenceException) { }
                catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); }
                catch (Exception e) { M.F(e); }
            }
            if (_MMDPeakValue != LastMMDPeakValue) { OnPropertyChanged("MMDPeakValue"); LastMMDPeakValue = _MMDPeakValue; }
        }

        //protected readonly TimedWorker KeepAlive = new TimedWorker();
        //private void Alive()
        //{
        //    if (CheckAccess<object>())
        //    {
        //        try { ASM.IsDisposed}
        //        catch (NullReferenceException) { }
        //        catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); }
        //        catch (Exception e) { M.F(e); }
        //    }
        //}
        #endregion

        #region Device Reset
        //public event EventHandler RestartRequested;
        private readonly object resetLock = new object();
        private volatile bool _reseting = false;
        public object AccessLock => resetLock;
        public bool CanAccess => !_reseting;
        protected virtual async void Reset(object reason)
        {
            bool work = false;
            lock (resetLock)
            {
                if (!_reseting) { work = true; _reseting = true; }
                else return;
            }
            await Task.Delay(15);
            if (work)
            {
                M.F($"Reset CoreManager. Reason: {reason}", verbose: gl.VerboseLog);

                //RestartRequested?.Invoke(this, new EventArgs());
                //ResetType1();

                // unregister notify events
                if (_MMDVolume != null)
                {
                    try { _MMDVolume.UnregisterControlChangeNotify(MMDVolumeCallback); }
                    catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); }
                    catch (Exception e) { M.F(e); }
                }
                else M.F("Manager: MMDVolume was null");
                if (_ASM != null)
                {
                    try { _ASM.UnregisterSessionNotification(SessionNotification); }
                    catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); }
                    catch (Exception e) { M.F(e); }
                }
                else M.F("Manager: ASM was null");

                // reset all audio objects except mmde
                Nullize(ref _MMD);
                _DeviceID = string.Empty;
                _MMDName = null;
                Nullize(ref _MMDPeak);
                Nullize(ref _MMDVolume);
                _MMDMuted = null;
                _MMDVolumeValue = null;
                Nullize(ref _ASM);
                Nullize(ref _Sessions);

                await Task.Delay(2000);
                // set flag finish
                lock (resetLock) _reseting = false;
            }
        }
        protected void ResetType1()
        {
            // unregister notify events
            if (_MMDVolume != null)
            {
                try { _MMDVolume.UnregisterControlChangeNotify(MMDVolumeCallback); }
                catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); }
                catch (Exception e) { M.F(e); }
            }
            else M.F("Manager: MMDVolume was null");
            if (_ASM != null)
            {
                try { _ASM.UnregisterSessionNotification(SessionNotification); }
                catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); }
                catch (Exception e) { M.F(e); }
            }
            else M.F("Manager: ASM was null");

            // reset all audio objects except mmde
            Nullize(ref _Sessions);
            Nullize(ref _ASM);
            Nullize(ref _MMDPeak);
            Nullize(ref _MMDVolume);
            _MMDMuted = null;
            _MMDVolumeValue = null;
            Nullize(ref _MMD);
            _DeviceID = string.Empty;
            _MMDName = null;
        }
        protected void ResetType2()
        {
            // unregister notify events
            _MMDVolume?.UnregisterControlChangeNotify(MMDVolumeCallback);
            _ASM?.UnregisterSessionNotification(SessionNotification);

            // reset all audio objects except mmde
            _MMD = null;
            _DeviceID = string.Empty;
            _MMDName = null;
            _MMDPeak = null;
            _MMDVolume = null;
            _ASM = null;
            _Sessions = null;
        }
        private void Nullize<T>(ref T o)
        {
            try { if (o is IDisposable d) d?.Dispose(); }
            catch (NullReferenceException) { return; }
            catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); }
            catch (Exception e) { M.F(e); }
            finally { o = default; }
        }

        #endregion
        #region Device Management
        private MMDeviceEnumerator _MMDE;
        protected MMDeviceEnumerator MMDE
        {
            get
            {
                if (CheckAccessNull(_MMDE))
                {
                    try
                    {
                        _MMDE = new MMDeviceEnumerator();
                        _MMDE.RegisterEndpointNotificationCallback(MMNC);
                    }
                    catch (Exception e) { M.F(e.Message); }
                }
                return _MMDE ?? throw new NullReferenceException("CoreAudioAPI Manager: Failed to get MMDE object");
            }
        }

        private MMNotificationCallback _MMNC;
        protected MMNotificationCallback MMNC
        {
            get
            {
                if (_MMNC == null)
                {
                    _MMNC = new MMNotificationCallback(gl, DeviceID, string.Empty);
                    _MMNC.RestartRequested += (s, e) => Reset(e);
                }
                return _MMNC ?? throw new Exception("CoreAudioAPI Manager: Failed to get MMNC");
            }
        }

        private MMDevice _MMD;
        protected MMDevice MMD
        {
            get
            {
                if (CheckAccessNull(_MMD))// || _MMD.DeviceState != CSCore.CoreAudioAPI.DeviceState.Active || _MMD.IsDisposed
                {
                    try
                    {
                        _MMD = MMDE?.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                        if (_MMD == null) throw new Exception("CoreAudioAPI Manager: Failed to get MMD object");
                        MMNC.DeviceID = _DeviceID = _MMD.DeviceID;
                        MMNC.DeviceName = _MMD.FriendlyName;
                        //string DeviceName = _MMD.PropertyStore.First(p => p.Key.PropertyID == CSCore.Win32.PropertyStore.DeviceDesc.PropertyID).Value.GetValue() as string;
                        //DeviceNameTpl = new Tuple<string, string>(DeviceName, _MMD.FriendlyName);
                    }
                    catch (Exception e) { M.F(e.Message); }
                }
                return _MMD;// ?? throw new NullReferenceException("CoreAudioAPI Manager: Failed to get MMD object");
            }
            set { _MMD = value; OnPropertyChanged(); }
        }
        private string _DeviceID = string.Empty;
        public string DeviceID => _DeviceID;

        private Tuple<string, string> _MMDName;
        /// <summary>
        /// DeviceDesc, FriendlyName
        /// </summary>
        public Tuple<string, string> MMDName
        {
            get
            {
                if (CheckAccessNull(_MMDName))
                {
                    try
                    {
                        //string buffer = MMD?.PropertyStore.First(p => p.Key.PropertyID == PropertyStore.DeviceDesc.PropertyID).Value.GetValue() as string;
                        string buffer = MMD?.PropertyStore[PropertyStore.DeviceDesc].GetValue() as string;
                        //string buffer = MMD?.Properties[PropertyKeys.PKEY_Device_DeviceDesc].Value as string;
                        _MMDName = new Tuple<string, string>(buffer, MMD?.FriendlyName);
                    }
                    catch (Exception e) { M.F(e.Message); }
                }
                return _MMDName;
            }
        }

        #endregion
        #region Device Volume-Peak
        private AudioMeterInformation _MMDPeak;
        protected AudioMeterInformation MMDPeak
        {
            get
            {
                if (CheckAccessNull(_MMDPeak))
                {
                    try
                    {
                        if (MMD != null) _MMDPeak = new AudioMeterInformation(MMD.Activate(typeof(AudioMeterInformation).GUID, 0, IntPtr.Zero));
                        //_MMDPeak = MMD?.AudioMeterInformation;
                    }
                    catch (Exception e) { if (e.HResult.IsUnknown() || gl.VerboseLog) M.F(e.Message); }
                }
                return _MMDPeak;
            }
        }
        private double _MMDPeakValue = 0;
        /// <summary>
        /// NO need to be polled, background task is polling until disposed
        /// </summary>
        public double MMDPeakValue => _MMDPeakValue;

        private AudioEndpointVolume _MMDVolume;
        protected AudioEndpointVolume MMDVolume
        {
            get
            {
                if (CheckAccessNull(_MMDVolume))
                {
                    try
                    {
                        _MMDVolume = AudioEndpointVolume.FromDevice(MMD);
                        _MMDVolume.RegisterControlChangeNotify(MMDVolumeCallback);
                        //_MMDVolume = MMD?.AudioEndpointVolume;
                        //_MMDVolume.OnVolumeNotification += MMDVolumeCallback_NotifyRecived;
                        if (_MMDMuted != _MMDVolume.IsMuted) { _MMDMuted = _MMDVolume.IsMuted; OnPropertyChanged("MMDMuted"); }
                        if (_MMDVolumeValue != _MMDVolume.MasterVolumeLevelScalar) { _MMDVolumeValue = _MMDVolume.MasterVolumeLevelScalar; OnPropertyChanged("MMDVolumeValue"); }
                    }
                    catch (Exception e) { if (e.HResult.IsUnknown() || gl.VerboseLog) M.F(e.Message); }
                }
                return _MMDVolume;
            }
        }
        private AudioEndpointVolumeCallback _MMDVolumeCallback;
        private AudioEndpointVolumeCallback MMDVolumeCallback
        {
            get
            {
                if (_MMDVolumeCallback == null)
                {
                    try
                    {
                        _MMDVolumeCallback = new AudioEndpointVolumeCallback();
                        _MMDVolumeCallback.NotifyRecived += MMDVolumeCallback_NotifyRecived;
                    }
                    catch (Exception e) { M.F(e.Message); }
                }
                return _MMDVolumeCallback;
            }
        }
        private void MMDVolumeCallback_NotifyRecived(object sender, AudioEndpointVolumeCallbackEventArgs e)
        {
            if (_MMDMuted != e.IsMuted) { _MMDMuted = e.IsMuted; OnPropertyChanged("MMDMuted"); }
            if (_MMDVolumeValue != e.MasterVolume) { _MMDVolumeValue = e.MasterVolume; OnPropertyChanged("MMDVolumeValue"); }
        }
        //private void MMDVolumeCallback_NotifyRecived(AudioVolumeNotificationData e)
        //{
        //    if (_MMDMuted != e.Muted) { _MMDMuted = e.Muted; OnPropertyChanged("MMDMuted"); }
        //    if (_MMDVolumeValue != e.MasterVolume) { _MMDVolumeValue = e.MasterVolume; OnPropertyChanged("MMDVolumeValue"); }
        //}
        private bool? _MMDMuted;
        /// <summary>
        /// NO need to be polled
        /// </summary>
        public bool MMDMuted
        {
            get
            {
                lock (AccessLock)
                {
                    if (_MMDMuted == null)
                    {
                        if (CanAccess) return MMDVolume?.IsMuted ?? false;
                        else return false;
                    }
                    return (bool)_MMDMuted;
                }
            }
            set { lock (AccessLock) { if (CanAccess && MMDVolume != null) MMDVolume.IsMuted = value; } }
        }
        private double? _MMDVolumeValue;
        /// <summary>
        /// NO need to be polled
        /// </summary>
        public double MMDVolumeValue
        {
            get
            {
                lock (AccessLock)
                {
                    if (_MMDVolumeValue == null)
                    {
                        if (CanAccess) return MMDVolume?.MasterVolumeLevelScalar ?? 0;
                        else return 0;
                    }
                    return (double)_MMDVolumeValue;
                }
            }
            set { lock (AccessLock) { if (CanAccess && MMDVolume != null && !double.IsNaN(value)) MMDVolume.MasterVolumeLevelScalar = (float)value; } }
        }

        #endregion

        #region Session Management
        private AudioSessionManager2 _ASM;
        protected AudioSessionManager2 ASM
        {
            get
            {
                if (CheckAccessNull(_ASM))
                {
                    try
                    {
                        InvokeThreadApartment(GetASM);
                    }
                    catch (Exception e) { M.F(e.Message); }
                }
                return _ASM;
            }
        }
        private void GetASM()
        {
            _ASM = MMD != null ? AudioSessionManager2.FromMMDevice(MMD) : null;
            //_ASM.SessionCreated += Audio_SessionCreated;
            _ASM.RegisterSessionNotification(SessionNotification);
            //_ASM = MMD?.AudioSessionManager;
            //_ASM.OnSessionCreated += Audio_SessionCreated;
        }

        private AudioSessionNotification _SessionNotification;
        private AudioSessionNotification SessionNotification
        {
            get
            {
                if (_SessionNotification == null)
                {
                    try
                    {
                        _SessionNotification = new AudioSessionNotification();
                        _SessionNotification.SessionCreated += Audio_SessionCreated;
                    }
                    catch (Exception e) { M.F(e.Message); }
                }
                return _SessionNotification;
            }
        }

        private void Audio_SessionCreated(object sender, SessionCreatedEventArgs e)
        {
            //Console.WriteLine("Session Create raised");
            using (var asc2 = e.NewSession.QueryInterface<AudioSessionControl2>())
            {
                lock (_Sessions.Locker)
                {
                    if (!_Sessions.Exists(asc2.ProcessID))
                    {
                        _Sessions.Add(new Session(e.NewSession, gl, DeviceID));
                        _Sessions.Sort();
                    }
                }
                asc2.Dispose();
            }
        }
        //private void Audio_SessionCreated(object sender, IAudioSessionControl e)
        //{
        //    //Console.WriteLine("Session Create raised");
        //    var asc = e as AudioSessionControl;
        //    if (asc == null) return;
        //    lock (_Sessions.Locker)
        //    {
        //        if (!_Sessions.Exists((int)asc.GetProcessID))
        //        {
        //            _Sessions.Add(new Session(asc, gl, DeviceID));
        //            _Sessions.Sort();
        //        }
        //    }
        //}

        private SessionList _Sessions;
        public SessionList Sessions
        {
            get
            {
                if (CheckAccessNull(_Sessions))
                {
                    try
                    {
                        InvokeThreadApartment(GetSessions);
                    }
                    catch (Exception e) { M.F(e.Message); }
                }
                return _Sessions;
            }
        }
        private void GetSessions()
        {
            if (_Sessions == null) _Sessions = new SessionList();
            else
            {
                lock (_Sessions.Locker) { _Sessions.DisposeAll(); }
            }
            //enumerate all sessions
            foreach (var asc in ASM.GetSessionEnumerator())
            //for (int i = 0; i < ASM.Sessions.Count; i++)
            {
                //var asc = ASM.Sessions[i];
                lock (_Sessions.Locker) { _Sessions.Add(new Session(asc, gl, DeviceID)); }
            }
            lock (_Sessions.Locker) { _Sessions.Sort(); }
        }
        #endregion

        #region System Management
        private bool CheckAccessNull<T>(T o) => CheckAccess(o, x => x == null);
        private bool CheckAccess<T>(T o = default, Predicate<T> p = null)
        {
            bool access;
            lock (AccessLock) access = CanAccess;
            return p == null ? access : access && p.Invoke(o);
        }
        private void InvokeThreadApartment(Action a)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA) a.Invoke();
            else
            {
                using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                {
                    _ = ThreadPool.QueueUserWorkItem(o =>
                    {
                        try { a.Invoke(); }
                        finally { waitHandle.Set(); }
                    });
                    waitHandle.WaitOne();
                }
            }
        }

        #endregion
        #region Dispose
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                Polling.Dispose();
                Reset("Disposing");
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Manager()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
    internal static class AudioEventManager
    {

        public static MMDevice MMD { get; set; }

        public static MMNotificationCallback MMDEcallback { get; private set; }
        public static AudioEndpointVolumeCallback AEVcallback { get; private set; }
        public static void Set(MMNotificationCallback mmnc, EventHandler<string> e)
        {
            MMDEcallback = mmnc;
            MMDEcallback.RestartRequested += e;
        }
        public static void Set(AudioEndpointVolumeCallback aevc, EventHandler<AudioEndpointVolumeCallbackEventArgs> e)
        {
            AEVcallback = aevc;
            AEVcallback.NotifyRecived += e;
        }
    }
    internal class MMNotificationCallback : IMMNotificationClient
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public General gl { get; private set; }
        public string DeviceID { get; set; }
        public string DeviceName { get; set; }
        public MMNotificationCallback(General gl, string id, string name)
        {
            this.gl = gl;
            DeviceID = id;
            DeviceName = name;
        }
        public event EventHandler<string> RestartRequested;

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (gl.VerboseLog) M.F($"Default [{role} {flow}] Device is changed. [{defaultDeviceId}]");
            //if ((e.DataFlow == DataFlow.Render || e.DataFlow == DataFlow.All) && e.Role == Role.Multimedia)
            //{
            //GetDefaultDevice(true);
            RestartRequested?.Invoke(this, $"MMDE_DefaultDeviceChanged [{role} {flow}] [{defaultDeviceId}]");
            //}
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            if (gl.VerboseLog) M.F($"Audio Device is added. [{pwstrDeviceId}]");
        }

        public void OnDeviceRemoved(string deviceId)
        {
            if (gl.VerboseLog) M.F($"Audio Device is removed. [{deviceId}]");
            if (deviceId == DeviceID) RestartRequested?.Invoke(this, $"MMDE_DeviceRemoved [{deviceId}]");
        }

        public void OnDeviceStateChanged(string deviceId, CSCore.CoreAudioAPI.DeviceState newState)
        {
            if (gl.VerboseLog) M.F($"Audio Device State is changed to [{newState}]. [{deviceId}]");
            if (deviceId == DeviceID) RestartRequested?.Invoke(this, $"MMDE_DeviceStateChanged [{newState}] [{deviceId}]");
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            //System.Diagnostics.Debug.WriteLine($"PrCh {ResetKeys.Contains(e.PropertyKey)} {e.PropertyKey} {e.DeviceId == MMD?.DeviceID} {e.DeviceId}");
            //System.Diagnostics.Debug.WriteLine($"PrCh {key} {pwstrDeviceId == MMD?.DeviceID} {pwstrDeviceId}");
            //if (ResetKeys.Contains(e.PropertyKey))
            if (true)
            {
                if (gl.VerboseLog) M.F($"Audio Device Property [{key}] is changed. [{pwstrDeviceId}]");
                // Request to restart wale if detected device is current device.
                if (pwstrDeviceId == DeviceID)
                {
                    RestartRequested?.Invoke(this, $"MMDE_DevicePropertyChanged [{key}] [{DeviceName}{pwstrDeviceId}]");
                }
            }
        }
    }
}

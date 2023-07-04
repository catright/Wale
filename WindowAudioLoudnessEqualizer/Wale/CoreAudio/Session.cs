using CSCore.CoreAudioAPI;
using System;
using System.Collections.Generic;
using Wale.Configs;
using Wale.Controller;
using Wale.Extensions;

namespace Wale.CoreAudio
{
    public enum SessionState { AudioSessionStateInactive, AudioSessionStateActive, AudioSessionStateExpired }
    /// <summary>
    /// Data container for audio session
    /// </summary>
    public class Session : JPack.NotifyPropertyChanged, IDisposable, IComparable<Session>
    {
        public readonly AudioSessionControl2 _asc;
        public readonly General gl;
        public readonly string DeviceID;
        public Session(AudioSessionControl asc, General gl, string DeviceId)
        {
            ID = Guid.NewGuid();
            DeviceID = DeviceId;
            this.gl = gl;
            this.gl.PropertyChanged += Gl_PropertyChanged; ;
            aCheck = new ActiveChecker(this.gl);
            _asc = asc.QueryInterface<AudioSessionControl2>();
            asc.Dispose();
            ASE.VolumeChanged += (s, n) => { if (_Volume != n) { _Volume = n; OnPropertyChanged("Volume"); } };
            ASE.MuteChanged += (s, n) => { if (_Muted != n) { _Muted = n; OnPropertyChanged("Muted"); OnPropertyChanged("SoundOn"); } };
            ASE.StateChanged += (s, n) =>
            {
                if (n == AudioSessionState.AudioSessionStateExpired) Dispose();
                else if (_State != n.GetSessionState())
                {
                    _State = n.GetSessionState();
                    OnPropertyChanged("State");
                }
            };
            ASE.Disconnected += (s, e) => Dispose();
            _asc.RegisterAudioSessionNotification(ASE);

            // check exclusion and set auto
            Auto = gl.ExcList == null || !gl.ExcList.Contains(Name);
            if (gl.ExcList == null) M.F($"Error: ExcList is null. {ID}");

            // set volume object
            try
            {
                sav = _asc?.QueryInterface<SimpleAudioVolume>();
                _Muted = sav.IsMuted;
                _Volume = sav.MasterVolume;
            }
            catch (Exception e) { M.F($"Session: SimpleAudioVolume: {e}"); Dispose(); }
            // set meter object
            try
            {
                ami = _asc?.QueryInterface<AudioMeterInformation>();
                _Peak = ami.PeakValue;
            }
            catch (Exception e) { M.F($"Session: AudioMeterInformation: {e}"); Dispose(); }

            // set average
            avr = new SoundAverageNormal(gl);
            avr.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
            //SetAvTimeAR(1000, 100, AvgTime, 100, AcInterval);

            // start background tasks
            //_ = Polling();
            //_ = UIPolling();
            UIPolling = new TimedWorker(gl.ForceMMT);
            UIPolling.Start(UIPoll, (int)gl.UIUpdateInterval);
            Polling = new TimedWorker(gl.ForceMMT);
            Polling.Start(Poll, (int)(gl.StaticMode ? gl.UIUpdateInterval : gl.AutoControlInterval));
            //DebugPolling.Start(DebugPoll, 10000);
        }

        private void Gl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
            switch (e.PropertyName)
            {
                case nameof(gl.UIUpdateInterval):
                    UIPolling.Interval = (int)gl.UIUpdateInterval;
                    Polling.Interval = (int)(gl.StaticMode ? gl.UIUpdateInterval : gl.AutoControlInterval);
                    break;
                case nameof(gl.StaticMode):
                case nameof(gl.AutoControlInterval):
                    Polling.Interval = (int)(gl.StaticMode ? gl.UIUpdateInterval : gl.AutoControlInterval);
                    break;
            }
        }
        public event EventHandler<Guid> SessionExpired;

        #region API Data
        private string _Name;
        /// <summary>
        /// Human readable process name
        /// </summary>
        public string Name => _Name ?? (_Name = Namer.Make(_asc));
        //protected void MakeName(bool accessProcess = false)
        //{
        //    try { _Name = NameMaker.Make(asc2, accessProcess); }
        //    catch (Exception e) { M.F($"Error: failed to re-make SessionName of '{Name}'\n{e.Message}"); }
        //}

        private SessionState _State;
        public SessionState State => _State;
        public string DisplayName
        {
            get
            {
                try { return _asc?.DisplayName ?? string.Empty; }
                catch { return string.Empty; }
            }
        }
        private int _ProcessID = -1;
        public int ProcessID
        {
            get
            {
                try { if (_ProcessID == -1) _ProcessID = _asc?.ProcessID ?? -1; }
                catch (NullReferenceException) { }
                catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e.Message); }
                catch (Exception e) { M.F(e.Message); }
                return _ProcessID;
            }
        }
        private string _SessionIdentifier = string.Empty;
        public string SessionIdentifier
        {
            get
            {
                if (string.IsNullOrEmpty(_SessionIdentifier))
                {
                    try { _SessionIdentifier = _asc?.SessionIdentifier ?? string.Empty; }
                    catch { _SessionIdentifier = string.Empty; }
                }
                return _SessionIdentifier;
            }
        }
        public bool IsSystemSoundSession
        {
            get
            {
                try { return (bool)_asc?.IsSystemSoundSession; }
                catch { return false; }
            }
        }
        public Guid GroupParam
        {
            get
            {
                try { return _asc?.GroupingParam ?? Guid.Empty; }
                catch { return Guid.Empty; }
            }
        }

        private string _ProcessName;
        /// <summary>
        /// It COULD take VERY LONG TIME when read this property. Because it needs to access another process if a cache is empty.
        /// </summary>
        public string ProcessName
        {
            get
            {
                try { if (string.IsNullOrEmpty(_ProcessName)) _ProcessName = _asc?.Process?.ProcessName ?? string.Empty; }
                catch (NullReferenceException) { }
                catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e.Message); }
                catch (Exception e) { M.F(e.Message); }
                return _ProcessName;
            }
        }
        private string _MainWindowTitle;
        /// <summary>
        /// It COULD take VERY LONG TIME when read this property. Because it needs to access another process if a cache is empty.
        /// </summary>
        public string MainWindowTitle
        {
            get
            {
                try { if (string.IsNullOrEmpty(_MainWindowTitle)) _MainWindowTitle = _asc?.Process?.MainWindowTitle ?? string.Empty; }
                catch (NullReferenceException) { }
                catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e.Message); }
                catch (Exception e) { M.F(e.Message); }
                return _MainWindowTitle;
            }
        }
        public int UpdateProcessInfo()
        {
            if (_asc == null) return 1;
            System.Diagnostics.Process p = _asc?.Process;
            if (p != null)
            {
                try
                {
                    _ProcessName = p.ProcessName;
                    _MainWindowTitle = p.MainWindowTitle;
                }
                catch (NullReferenceException) { return 2; }
                catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e.Message); return 4; }
                catch (Exception e) { M.F(e.Message); return 8; }
            }
            else Dispose();
            return 0;
        }
        private string _Icon;
        /// <summary>
        /// It COULD take VERY LONG TIME when read this property. Because it needs to access to another process if IconPath is not set in session or a cache is empty.
        /// </summary>
        public string Icon
        {
            get
            {
                if (_Icon != null) return _Icon;

                string path = string.Empty;
                try { path = _asc?.IconPath; }
                catch (Exception e) { M.F($"PID({ProcessID}):FAILED to get iconPath from session control\n{e.Message}"); }
                if (string.IsNullOrWhiteSpace(path))
                {
                    //#if DEBUG
                    //                    Console.WriteLine($"PID({ProcessID}):there is no icon information. try to get it from process");
                    //#endif
                    if (gl.VerboseLog) M.F($"PID({ProcessID}):there is no icon information. try to get it from process");
                    try
                    {
                        if (_asc?.Process?.MainModule != null) path = _asc?.Process?.MainModule.FileName;
                    }
                    catch
                    {
                        //#if DEBUG
                        //                        Console.WriteLine($"PID({ProcessID}):FAILED to get icon from process");
                        //#endif
                        M.F($"PID({ProcessID}):FAILED to get icon from process");
                    }
                    //#if DEBUG
                    //                    Console.WriteLine(string.IsNullOrWhiteSpace(path)
                    //                        ? $"PID({ProcessID}):FAILED to get icon from process"
                    //                        : $"PID({ProcessID}):Succeed to get icon from process");
                    //#endif
                    if (gl.VerboseLog && !string.IsNullOrWhiteSpace(path)) M.F($"PID({ProcessID}):Succeed to get icon from process");
                }
                else { if (gl.VerboseLog) M.F($"PID({ProcessID}):Succeed to get icon from session control"); }
                return _Icon = path;
            }
        }

        private readonly AudioMeterInformation ami;
        private float _Peak;
        public float Peak => _Peak;//ami?.PeakValue ?? 0;
        //private void UpdatePeak(bool check)
        //{
        //    if (check)
        //    {
        //        _Peak = ami?.PeakValue ?? 0;
        //        if (_Peak != lastPeak) { lastPeak = _Peak; }
        //    }
        //}

        private readonly SimpleAudioVolume sav;
        private bool _Muted;
        public bool Muted
        {
            get => _Muted;
            set { if (sav != null) sav.IsMuted = value; }
        }
        public bool SoundOn
        {
            get => !_Muted;
            set { if (sav != null) sav.IsMuted = !value; }
        }
        private float _Volume;
        /// <summary>
        /// Read or write volume of audio session. Always clipped in 0~1
        /// </summary>
        public float Volume
        {
            get => _Volume;
            set { if (sav != null && !float.IsNaN(value)) { sav.MasterVolume = value.Clip(); } }
        }

        private readonly AudioSessionEvent ASE = new AudioSessionEvent();
        #endregion
        #region Customized Data
        public Guid ID { get; private set; }
        public object Locker { get; } = new object();
        public bool NewlyAdded { get; set; } = true;
        private float _Relative = 0f;

        /// <summary>
        /// Final volume would be multiplied by 2^Relative. This value is kept in -1~1.
        /// </summary>
        public float Relative
        {
            get => _Relative;
            set
            {
                _Relative = Math.Abs(value) < 0.00001 ? 0 : value.Clip(Audio.RelativeEnd, Audio.RelativeEndInv);
                Relfactor = _Relative == 0 ? 1 : Math.Pow(Audio.RelativeBase, _Relative);
                OnPropertyChanged();
                OnPropertyChanged("Relfactor");
            }
        }
        // math 2^0=1 so let's skip calculation and set relFactor to 1 for an efficiency when relative is 0 that is in most cases
        public double Relfactor { get; private set; } = 1;
        /// <summary>
        /// The session is included to Auto controller when this flag is True. Default is True.
        /// </summary>
        public bool Auto { get => Get<bool>(); set => Set(value); }

        private readonly ActiveChecker aCheck;
        public bool Active => aCheck.Active;

        private readonly ISoundAverage avr;
        public double AveragePeak => avr?.AveragePeak ?? 0;
        //public void SetAverage(double peak) => avr.Average_Chunk(peak);
        public void ResetAverage() => avr?.ResetAverage();
        #endregion


        #region Background Tasks
        private float lastPeak;
        private readonly TimedWorker UIPolling, Polling;
        //private readonly TimedWorker DebugPolling = new TimedWorker();
        private void UIPoll()
        {
            if (_asc.IsDisposed) Dispose();
            if (!Auto)
            {
                try
                {
                    _Peak = ami?.PeakValue ?? 0;
                    _ = aCheck.Push(_Peak, Auto);
                }
                catch (NullReferenceException) { }
                catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); else Dispose(); }
                catch (Exception e) { M.F(e); }
            }
            if (_Peak != lastPeak) { lastPeak = _Peak; OnPropertyChanged("Peak"); }
        }
        private void Poll()
        {
            if (Auto)
            {
                try
                {
                    _Peak = ami?.PeakValue ?? 0;
                    if (aCheck.Push(_Peak, Auto) && gl.Averaging) avr?.SetAverage(_Peak);
                    //Console.WriteLine($"{Name} {AveragePeak:n4} {_Peak:n4}/{gl.MinPeak:n4} {_Peak > gl.MinPeak} {aCheck.Active}");
                }
                catch (NullReferenceException) { }
                catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); else Dispose(); }
                catch (Exception e) { M.F(e); }
            }
        }
        //private void DebugPoll()
        //{
        //    M.F($"Session UI Polling: {Name} {ProcessID} {SessionIdentifier} {_asc.SessionInstanceIdentifier} [{ID}]. dispose={_asc.IsDisposed}");
        //}
        //private readonly CancellationTokenSource cts = new CancellationTokenSource();
        //private async Task UIPolling()
        //{
        //    List<Task> t = new List<Task>();
        //    while (!cts.IsCancellationRequested)
        //    {
        //        t.Clear();
        //        t.Add(HPT.Delay((int)gl.UIUpdateInterval, HPT.Select.MMT, gl.ForceMMT, cts.Token));
        //        t.Add(Task.Run(UIPoll, cts.Token));

        //        try { await Task.WhenAll(t); }
        //        catch (TaskCanceledException) { }
        //    }
        //}
        //private async Task Polling()
        //{
        //    List<Task> t = new List<Task>();
        //    while (!cts.IsCancellationRequested)
        //    {
        //        t.Clear();
        //        t.Add(HPT.Delay((int)(gl.StaticMode ? gl.UIUpdateInterval : gl.AutoControlInterval), HPT.Select.MMT, gl.ForceMMT, cts.Token));
        //        t.Add(Task.Run(Poll, cts.Token));

        //        try { await Task.WhenAll(t); }
        //        catch (TaskCanceledException) { }
        //    }
        //}
        #endregion
        #region SessionAutoControl
        #endregion


        public int CompareTo(Session other)
        {
            // A null value means that this object is greater.
            if (other == null) return 1;
            else return ProcessID.CompareTo(other.ProcessID);
        }
        #region IDisposable Support
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
                //cts.Cancel();
                //DebugPolling.Dispose();
                UIPolling.Dispose();
                Polling.Dispose();
                try
                {
                    if (_asc != null)
                    {
                        int result = _asc?.UnregisterAudioSessionNotificationNative(ASE) ?? 0;
                        if (result != 0) M.F($"Session: Dispose: asc unregister has a problem. hresult 0x{result:x8}");
                    }
                    else if (gl.VerboseLog) M.F("Session: Dispose: asc is null");
                    _asc?.Dispose();
                }
                catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); }
                catch (Exception e) { M.F(e); }
                //try
                //{
                //    ami?.Dispose();
                //    //if (ami != null) { ami.Dispose(); }
                //    //else if (gl.VerboseLog) JPack.FileLog.Log("Session: Dispose: ami is null");
                //}
                //catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); }
                //catch (Exception e) { M.F(e); }
                //try
                //{
                //    sav?.Dispose();
                //    //if (sav != null) { sav.Dispose(); }
                //    //else if (gl.VerboseLog) JPack.FileLog.Log("Session: Dispose: sav is null");
                //}
                //catch (CoreAudioAPIException e) { if (e.HResult.IsUnknown()) M.F(e); }
                //catch (Exception e) { M.F(e); }
                SessionExpired?.Invoke(this, ID);
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Session()
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

    internal class AudioSessionEvent : IAudioSessionEvents
    {
        public EventHandler<float> VolumeChanged;
        public EventHandler<bool> MuteChanged;
        public EventHandler<AudioSessionState> StateChanged;
        public EventHandler Disconnected;

        public void OnDisplayNameChanged(string newDisplayName, ref Guid eventContext) { }

        public void OnIconPathChanged(string newIconPath, ref Guid eventContext) { }

        public void OnSimpleVolumeChanged(float newVolume, bool newMute, ref Guid eventContext)
        {
            VolumeChanged?.Invoke(this, newVolume);
            MuteChanged?.Invoke(this, newMute);
        }

        public void OnChannelVolumeChanged(int channelCount, float[] newChannelVolumeArray, int changedChannel, ref Guid eventContext) { }

        public void OnGroupingParamChanged(ref Guid newGroupingParam, ref Guid eventContext) { }

        public void OnStateChanged(AudioSessionState newState) => StateChanged?.Invoke(this, newState);

        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason) => Disconnected?.Invoke(this, null);
    }
    internal class ActiveChecker
    {
        public readonly General gl;
        public ActiveChecker(General gl)
        {
            this.gl = gl;
            this.gl.PropertyChanged += Gl_PropertyChanged;
        }
        private void Gl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(gl.StaticMode):
                    mode = gl.StaticMode;
                    break;
            }
        }

        private int index = 0;
        private readonly float[] list = new float[10];
        private readonly double wait = 100;//have to divided by list.capacity, c.g. 100 for 1000ms when list.capacity is 10
        private double now;
        private bool mode;
        private double GetInterval(bool auto) => auto && !mode ? gl.AutoControlInterval : gl.UIUpdateInterval;
        private bool GetActive(double std)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] > std) return true;
            }
            return false;
        }

        public bool Active { get; private set; }

        public bool Push(float peak, bool auto)
        {
            now -= GetInterval(auto);
            if (now < 0)
            {
                now = wait;
                if (index >= list.Length) index = 0;
                list[index] = peak;
                Active = GetActive(gl.MinPeak);
            }
            return Active;
        }
        public bool Reset()
        {
            for (int i = 0; i < list.Length; i++) list[i] = 0;
            return Active = GetActive(gl.MinPeak);
        }
    }
}

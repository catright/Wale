using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
//using CSCore;
//using CSCore.SoundOut;
using CSCore.CoreAudioAPI;
//using NAudio.CoreAudioApi;

namespace Wale.CoreAudio
{
    public enum DeviceState { Active, Disabled, UnPlugged, NotPresent, Unknown }
    public enum SessionState { Active, Inactive, Expired }
    public class Audio : IDisposable
    {
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }
                //if (defaultDevice != null) Marshal.ReleaseComObject(defaultDevice);
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                sessionList = null;
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        //~Audio() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //Dispose(false);
        //}

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            //GC.SuppressFinalize(this);
        }
        #endregion

        #region Public Items
        public float MasterVolume { get => masterVolume; set => masterVolume = value; }
        public float MasterPeak { get => masterPeak; }
        public SessionDatas Sessions { get => sessionList; }

        public Audio() { }
        /// <summary>
        /// Instantiate new instance of Audio class.
        /// Automatically do UpdateDevice when <paramref name="autoStart"/> is true.
        /// </summary>
        /// <param name="autoStart"></param>
        public Audio(bool autoStart)
        {
            if (autoStart)
            {
                UpdateDevice();
                UpdateSession();
            }
        }
        
        /// <summary>
        /// Get new default MMDevice, VolumeSource, and PeakMeter.
        /// </summary>
        public void UpdateDevice()
        {
            GetDefaultDevice();
            GetMasterVolume();
            GetMasterPeak();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<DeviceData> GetDeviceList() { return EnumerateWasapiDevices(); }

        /// <summary>
        /// 
        /// </summary>
        public void UpdateSession() { GetSession(); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="volume"></param>
        public void SetSessionVolume(SessionData session, float volume) { setSessionVolume(session, volume); }
        #endregion


        #region Private Master Items
        //Master Volume Items
        private float masterVolume { get => GetMasterVolume(); set => SetMasterVolume(value); }
        private float GetMasterVolume()
        {
            using (var defaultDevice = GetDefaultDevice())
            {
                if (defaultDevice == null) { JDPack.FileLog.Log("GetMasterVolume: Fail to get master device."); return -1; }
                Guid IID_IAudioEndpointVolume = typeof(AudioEndpointVolume).GUID;
                IntPtr i = defaultDevice.Activate(IID_IAudioEndpointVolume, 0, IntPtr.Zero);
                using (var aev = new AudioEndpointVolume(i))
                {
                    return aev.MasterVolumeLevelScalar;
                }
            }
            //return defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
        }
        private void SetMasterVolume(float volume)
        {
            using (var defaultDevice = GetDefaultDevice())
            {
                if (defaultDevice == null) { JDPack.FileLog.Log("SetMasterVolume: Fail to get master device."); return; }
                Guid IID_IAudioEndpointVolume = typeof(AudioEndpointVolume).GUID;
                IntPtr i = defaultDevice.Activate(IID_IAudioEndpointVolume, 0, IntPtr.Zero);
                using (var aev = new AudioEndpointVolume(i))
                {
                    aev.MasterVolumeLevelScalar = volume;
                }
            }
            //defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
        }

        //Master Peak Items
        private float masterPeak { get => GetMasterPeak(); }
        private float GetMasterPeak()
        {
            using (var defaultDevice = GetDefaultDevice())
            {
                if (defaultDevice == null) { JDPack.FileLog.Log("GetMasterPeak: Fail to get master device."); return -1; }
                Guid IID_IAudioMeterInformation = typeof(AudioMeterInformation).GUID;
                IntPtr ip = defaultDevice.Activate(IID_IAudioMeterInformation, 0, IntPtr.Zero);
                using (var ami = new AudioMeterInformation(ip))
                {
                    return ami.PeakValue;
                }
            }
            

            //return defaultDevice.AudioMeterInformation.MasterPeakValue;
        }
        
        //Master Device Items
        private MMDevice GetDefaultDevice()
        {
            try
            {
                using (var obj = new CSCore.CoreAudioAPI.MMDeviceEnumerator())
                {
                    MMDevice device = obj.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    if(device == null) JDPack.FileLog.Log("GetSessionData: Fail to get master device.");
                    return device;
                }
            }
            catch (Exception e){ JDPack.FileLog.Log(e.ToString()); return null; }
        }
        //private IEnumerable<MMDevice> deviceList;
        private List<DeviceData> EnumerateWasapiDevices()
        {
            try
            {
                using (var obj = new CSCore.CoreAudioAPI.MMDeviceEnumerator())
                {
                    IEnumerable<MMDevice> devices = obj.EnumAudioEndpoints(DataFlow.Render, CSCore.CoreAudioAPI.DeviceState.Active | CSCore.CoreAudioAPI.DeviceState.Disabled | CSCore.CoreAudioAPI.DeviceState.UnPlugged);
                    if (devices == null) { JDPack.FileLog.Log("EnumerateWasapiDevices: Fail to get device collection."); return null; }

                    List<DeviceData> list = new List<DeviceData>();
                    foreach (MMDevice d in devices)
                    {
                        try
                        {
                            //Console.WriteLine($"{d.FriendlyName}, {d.DeviceState}, {d.DeviceID}");
                            DeviceState state;
                            switch (d.DeviceState)
                            {
                                case CSCore.CoreAudioAPI.DeviceState.Active:
                                    state = DeviceState.Active;
                                    break;
                                case CSCore.CoreAudioAPI.DeviceState.Disabled:
                                    state = DeviceState.Disabled;
                                    break;
                                case CSCore.CoreAudioAPI.DeviceState.UnPlugged:
                                    state = DeviceState.UnPlugged;
                                    break;
                                default:
                                    state = DeviceState.Unknown;
                                    break;
                            }

                            List<SessionData> slist = null;
                            try
                            {
                                using (var asm = (d != null ? AudioSessionManager2.FromMMDevice(d) : null))
                                using (var ase = (asm?.GetSessionEnumerator()))
                                {
                                    if (ase != null)
                                    {
                                        slist = new List<SessionData>();
                                        foreach (AudioSessionControl asc in ase)
                                        {
                                            using (var session2 = asc.QueryInterface<AudioSessionControl2>())
                                            using (var simpleAudioVolume = asc.QueryInterface<SimpleAudioVolume>())
                                            using (var audioMeterInformation = asc.QueryInterface<AudioMeterInformation>())
                                            {
                                                SessionState sstate = SessionState.Expired;
                                                switch (asc.SessionState)
                                                {
                                                    case AudioSessionState.AudioSessionStateActive:
                                                        sstate = SessionState.Active;
                                                        break;
                                                    case AudioSessionState.AudioSessionStateInactive:
                                                        sstate = SessionState.Inactive;
                                                        break;
                                                    case AudioSessionState.AudioSessionStateExpired:
                                                        sstate = SessionState.Expired;
                                                        break;
                                                }
                                                slist.Add(new SessionData(session2.ProcessID, session2.SessionIdentifier) { State = sstate });
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }

                            list.Add(new DeviceData()
                            {
                                DeviceId = d.DeviceID,
                                Name = d.FriendlyName,
                                State = state,
                                Sessions = slist
                            });
                        }
                        catch (NullReferenceException) { }
                    }
                    return list;
                }
            }
            catch (Exception e) { JDPack.FileLog.Log(e.ToString()); return null; }
        }
        #endregion


        #region Private Session Items
        //Session Control Items
        private SessionDatas sessionList;
        private void GetSession()
        {
            if (sessionList == null) { sessionList = new SessionDatas(); }
            GetSessionData();
        }
        private void GetSessionData()
        {
            try
            {
                using (var defaultDevice = GetDefaultDevice())
                using (var asm = (defaultDevice != null ? AudioSessionManager2.FromMMDevice(defaultDevice) : null))
                using (var ase = (asm?.GetSessionEnumerator()))
                {
                    if (defaultDevice == null) { JDPack.FileLog.Log("GetSessionData: Fail to get master device."); return; }

                    sessionList.Clear();
                    foreach (AudioSessionControl asc in ase)
                    {
                        using (var session2 = asc.QueryInterface<AudioSessionControl2>())
                        using (var simpleAudioVolume = asc.QueryInterface<SimpleAudioVolume>())
                        using (var audioMeterInformation = asc.QueryInterface<AudioMeterInformation>())
                        {
                            SessionState state = SessionState.Expired;
                            switch (asc.SessionState)
                            {
                                case AudioSessionState.AudioSessionStateActive:
                                    state = SessionState.Active;
                                    break;
                                case AudioSessionState.AudioSessionStateInactive:
                                    state = SessionState.Inactive;
                                    break;
                                case AudioSessionState.AudioSessionStateExpired:
                                    state = SessionState.Expired;
                                    break;
                            }
                            sessionList.Add(new SessionData(session2.ProcessID, session2.SessionIdentifier)
                            {
                                Volume = simpleAudioVolume.MasterVolume,
                                Peak = audioMeterInformation.PeakValue,
                                State = state
                            });
                        }
                    }

                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(GetSessionData): {e.ToString()}"); }
            //sessionList.ForEach(s => Console.WriteLine($"{s.PID}"));
        }
        private void GetSession2()
        {/*
            using (var defaultDevice = GetDefaultDevice())
            using (var asm = AudioSessionManager2.FromMMDevice(defaultDevice))
            {
                if (asm == null) return;
                using (var ase = asm.GetSessionEnumerator())
                {
                    sessionList.Clear();
                    foreach (AudioSessionControl asc in ase)
                    {
                        sessionList.Add(new Session2(asc.BasePtr));
                    }
                }
            }/**/
            //sessionList.ForEach(s => Console.WriteLine($"{s.PID}"));
        }
        private void GetSession3()
        {
            /*if (defaultDevice != null)
            {
                defaultDevice.AudioSessionManager.RefreshSessions();
                sessionList.Clear();
                for (int i = 0; i < defaultDevice.AudioSessionManager.Sessions.Count; i++)
                {
                    sessionList.Add(new Session3(defaultDevice.AudioSessionManager.Sessions[i]));
                }
            }*/
        }

        private void GetState(SessionData session)
        {
            using (var defaultDevice = GetDefaultDevice())
            using (var asm = (defaultDevice != null ? AudioSessionManager2.FromMMDevice(defaultDevice) : null))
            using (var ase = (asm?.GetSessionEnumerator()))
            {
                if (defaultDevice == null) { JDPack.FileLog.Log("GetState: Fail to get master device."); return; }
                foreach (var asc in ase)
                {
                    using (var session2 = asc.QueryInterface<AudioSessionControl2>())
                    using (var simpleAudioVolume = asc.QueryInterface<SimpleAudioVolume>())
                    {
                        if (session2.ProcessID == session.PID)
                        {
                            switch (asc.SessionState)
                            {
                                case AudioSessionState.AudioSessionStateActive:
                                    session.State = SessionState.Active;
                                    break;
                                case AudioSessionState.AudioSessionStateInactive:
                                    session.State = SessionState.Inactive;
                                    break;
                                case AudioSessionState.AudioSessionStateExpired:
                                    session.State = SessionState.Expired;
                                    break;
                                default:
                                    session.State = SessionState.Expired;
                                    break;
                            }
                        }
                    }
                }
            }
        }
        private void GetSessionVolume(SessionData session)
        {
            using (var defaultDevice = GetDefaultDevice())
            using (var asm = (defaultDevice != null ? AudioSessionManager2.FromMMDevice(defaultDevice) : null))
            using (var ase = (asm?.GetSessionEnumerator()))
            {
                if (defaultDevice == null) { JDPack.FileLog.Log("GetSessionVolume: Fail to get master device."); return; }
                foreach (var asc in ase)
                {
                    using (var session2 = asc.QueryInterface<AudioSessionControl2>())
                    using (var simpleAudioVolume = asc.QueryInterface<SimpleAudioVolume>())
                    {
                        if (session2.ProcessID == session.PID) session.Volume = simpleAudioVolume.MasterVolume;
                    }
                }
            }
        }
        private void setSessionVolume(SessionData session, float volume)
        {
            try
            {
                using (var defaultDevice = GetDefaultDevice())
                using (var asm = (defaultDevice != null ? AudioSessionManager2.FromMMDevice(defaultDevice) : null))
                using (var ase = (asm?.GetSessionEnumerator()))
                {
                    if (defaultDevice == null) { JDPack.FileLog.Log("SetSessionVolume: Fail to get master device."); return; }
                    foreach (var asc in ase)
                    {
                        using (var session2 = asc.QueryInterface<AudioSessionControl2>())
                        using (var simpleAudioVolume = asc.QueryInterface<SimpleAudioVolume>())
                        {
                            if (session2.ProcessID == session.PID) simpleAudioVolume.MasterVolume = volume;
                        }
                    }
                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(setSessionVolume): {e.ToString()}"); }
        }
        private void GetSessionPeak(SessionData session)
        {
            using (var defaultDevice = GetDefaultDevice())
            using (var asm = (defaultDevice != null ? AudioSessionManager2.FromMMDevice(defaultDevice) : null))
            using (var ase = (asm?.GetSessionEnumerator()))
            {
                if (defaultDevice == null) { JDPack.FileLog.Log("GetSessionPeak: Fail to get master device."); return; }
                foreach (var asc in ase)
                {
                    using (var session2 = asc.QueryInterface<AudioSessionControl2>())
                    using (var audioMeterInformation = asc.QueryInterface<AudioMeterInformation>())
                    {
                        if (session2.ProcessID == session.PID) session.Peak = audioMeterInformation.PeakValue;
                    }
                }
            }
        }
        #endregion


    }//End class Audio
}
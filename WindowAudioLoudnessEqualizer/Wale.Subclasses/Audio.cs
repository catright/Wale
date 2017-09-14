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

        public void UpdateSession()
        {
            GetSession();
        }
        #endregion


        #region Private Master Items
        //Master Volume Items
        private float masterVolume { get => GetMasterVolume(); set => SetMasterVolume(value); }
        private float GetMasterVolume()
        {
            using (var defaultDevice = GetDefaultDevice())
            {
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
            using (var obj = new CSCore.CoreAudioAPI.MMDeviceEnumerator())
            //using (var obj = new NAudio.CoreAudioApi.MMDeviceEnumerator())
            {
                return obj.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
        }
        //private IEnumerable<MMDevice> deviceList;
        private void EnumerateWasapiDevices()
        {
            //using (CSCore.CoreAudioAPI.MMDeviceEnumerator enumerator = new CSCore.CoreAudioAPI.MMDeviceEnumerator())
            //{
            //    deviceList = enumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
            //}
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
                using (var asm = AudioSessionManager2.FromMMDevice(defaultDevice))
                {
                    if (asm == null) return;
                    using (var ase = asm.GetSessionEnumerator())
                    {
                        sessionList.Clear();
                        foreach (AudioSessionControl asc in ase)
                        {
                            using (var session2 = asc.QueryInterface<AudioSessionControl2>())
                            using (var simpleAudioVolume = asc.QueryInterface<SimpleAudioVolume>())
                            using (var audioMeterInformation = asc.QueryInterface<AudioMeterInformation>())
                            {
                                SessionData session = new SessionData(session2.ProcessID, session2.SessionIdentifier);
                                session.Volume = simpleAudioVolume.MasterVolume;
                                session.Peak = audioMeterInformation.PeakValue;
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
                                sessionList.Add(session);
                            }
                        }
                    }
                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Getting session data Error: {e.Message}"); }
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
        
        public void GetState(SessionData session)
        {
            using (var defaultDevice = GetDefaultDevice())
            using (var asm = AudioSessionManager2.FromMMDevice(defaultDevice))
            using (var ase = asm.GetSessionEnumerator())
            {
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
        public void GetSessionVolume(SessionData session)
        {
            using (var defaultDevice = GetDefaultDevice())
            using (var asm = AudioSessionManager2.FromMMDevice(defaultDevice))
            using (var ase = asm.GetSessionEnumerator())
            {
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
        public void SetSessionVolume(SessionData session, float volume)
        {
            using (var defaultDevice = GetDefaultDevice())
            using (var asm = AudioSessionManager2.FromMMDevice(defaultDevice))
            using (var ase = asm.GetSessionEnumerator())
            {
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
        public void GetSessionPeak(SessionData session)
        {
            using (var defaultDevice = GetDefaultDevice())
            using (var asm = AudioSessionManager2.FromMMDevice(defaultDevice))
            using (var ase = asm.GetSessionEnumerator())
            {
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
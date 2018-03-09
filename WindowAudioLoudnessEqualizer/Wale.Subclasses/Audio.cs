﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using CSCore.CoreAudioAPI;

namespace Wale.CoreAudio
{
    /// <summary>
    /// Custom device state equivalent of DeviceState of CoreAudioAPI.
    /// </summary>
    public enum DeviceState { Active, Disabled, UnPlugged, NotPresent, Unknown }
    /// <summary>
    /// Custom session state equivalent of SessionState of CoreAudioAPI.
    /// </summary>
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
        /// <summary>
        /// True when there is no device that can use.
        /// </summary>
        public bool NoDevice = false;
        /// <summary>
        /// Volume of default device.
        /// </summary>
        public float MasterVolume { get => GetMasterVolume(); set => SetMasterVolume(value); }
        /// <summary>
        /// Peak level of default device.
        /// </summary>
        public float MasterPeak { get => GetMasterPeak(); }
        /// <summary>
        /// Session data list of sessions in default device.
        /// </summary>
        public SessionDataList Sessions { get => sessionList; }
        /// <summary>
        /// A list of excluded sessions for automatic control
        /// </summary>
        public List<string> ExcludeList = new List<string> { "amddvr", "ShellExperienceHost", "Windows Shell Experience Host" };
        
        /// <summary>
        /// Instantiate new instance of Audio class.
        /// </summary>
        public Audio() { }
        /// <summary>
        /// Instantiate new instance of Audio class.
        /// Automatically do UpdateDevice and UpdateSession when <paramref name="autoStart"/> is true.
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
        /// Get a list of all devices and their sessions.
        /// </summary>
        /// <returns></returns>
        public List<DeviceData> GetDeviceList() { return EnumerateWasapiDevices(); }

        public void EnableAudioDevice(string deviceId) { EnableDevice(deviceId); }
        public void DisableAudioDevice(string deviceId) { DisableDevice(deviceId); }

        /// <summary>
        /// Update all sessions in default device and session list.
        /// </summary>
        public void UpdateSession() { GetSession(); }
        /// <summary>
        /// Set volume level of the session that has <paramref name="pid"/> for ProcessId.
        /// <para>Log($"Error(SetSessionVolume): {e.ToString()}") when Exception.</para>
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <param name="volume">Volume level you want to set</param>
        public void SetSessionVolume(uint pid, float volume)
        {
            try
            {
                using (var s = sessionList.GetSession(pid))
                {
                    SetSessionVolume(s, volume);
                    s.Volume = volume;
                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(SetSessionVolume): {e.ToString()}"); }
        }
        /// <summary>
        /// Set all sessions' volume.
        /// <para>Log($"Error(SetAllSessions): {e.ToString()}") when Exception.</para>
        /// </summary>
        /// <param name="volume">Volume level you want to set</param>
        public void SetAllSessions(float volume)
        {
            try
            {
                sessionList.ForEach(s =>
                {
                    SetSessionVolume(s, volume);
                    s.Volume = volume;
                });
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(SetAllSessions): {e.ToString()}"); }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="peak"></param>
        public void SetSessionAverage(uint pid, double peak)
        {
            try
            {
                using (var s = sessionList.GetSession(pid))
                {
                    SetSessionAverage(s, peak);
                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(SetSessionAverage): {e.ToString()}"); }
        }
        public void UpdateAvTime(uint pid, double AVTime, double ACInterval)
        {
            try
            {
                using (var s = sessionList.GetSession(pid))
                {
                    SetSessionAvTime(s, AVTime, ACInterval);
                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(UpdateAvData): {e.ToString()}"); }
        }
        /// <summary>
        /// Update averaging time for all sessions.
        /// </summary>
        /// <param name="AVTime">Total time to stack peaks for average</param>
        /// <param name="ACInterval">Internal of automatic control task</param>
        public void UpdateAvTimeAll(double AVTime, double ACInterval)
        {
            try
            {
                sessionList.ForEach(s =>
                {
                    SetSessionAvTime(s, AVTime, ACInterval);
                });
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(UpdateAvDataAll): {e.ToString()}"); }
        }
        #endregion

        #region Private Common Variables
        private object sessionLocker = new object();
        #endregion

        #region Private Common Methods
        /// <summary>
        /// Return converted sesstion state from audio session state <paramref name="sessionState"/> of CoreAPI
        /// </summary>
        /// <param name="sessionState">audio session state of CoreAPI</param>
        /// <returns>Session state is converted for Wale from CoreAPI</returns>
        private SessionState State(AudioSessionState sessionState)
        {
            SessionState state = SessionState.Expired;
            switch (sessionState)
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
            return state;
        }
        /// <summary>
        /// Get process has <paramref name="processId"/> as its id
        /// </summary>
        /// <param name="processId"></param>
        /// <returns>null or process object has <paramref name="processId"/> as its id if the process is present</returns>
        private System.Diagnostics.Process Process(int processId)
        {
            System.Diagnostics.Process p = null;
            try { p = System.Diagnostics.Process.GetProcessById(processId); } catch { }
            return p;
        }
        #endregion


        #region Private Audio Device Items
        //Master Volume Items
        private float GetMasterVolume()
        {
            try
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
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(GetMasterVolume): {e.ToString()}"); return -2; }
            //return defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
        }
        private void SetMasterVolume(float volume)
        {
            try
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
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(SetMasterVolume): {e.ToString()}"); return; }
            //defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
        }

        //Master Peak Items
        private float GetMasterPeak()
        {
            try
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
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(GetMasterPeak): {e.ToString()}"); return -2; }

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
                    NoDevice = false;
                    return device;
                }
            }
            catch (Exception e){ JDPack.FileLog.Log(e.ToString()); NoDevice = true; return null; }
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
                                            using (var asc2 = asc.QueryInterface<AudioSessionControl2>())
                                            using (var simpleAudioVolume = asc.QueryInterface<SimpleAudioVolume>())
                                            using (var audioMeterInformation = asc.QueryInterface<AudioMeterInformation>())
                                            {
                                                NameSet nameSet = null;
                                                SessionState sstate = State(asc.SessionState);
                                                if (sstate != SessionState.Expired)
                                                {
                                                    try
                                                    {
                                                        nameSet = new NameSet(
                                                            asc2.ProcessID,
                                                            asc2.IsSystemSoundSession,
                                                            asc2.Process.ProcessName,
                                                            asc2.Process.MainWindowTitle,
                                                            asc.DisplayName,
                                                            asc2.SessionIdentifier
                                                        );
                                                    }
                                                    catch { }
                                                    if (nameSet != null) { slist.Add(new SessionData(nameSet) { State = sstate }); }
                                                }
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
        
        //Device Items
        private MMDevice GetDevice(string deviceId)
        {
            try
            {
                using (var obj = new CSCore.CoreAudioAPI.MMDeviceEnumerator())
                {
                    MMDevice device = obj.GetDevice(deviceId);
                    if (device == null) JDPack.FileLog.Log("GetSessionData: Fail to get master device.");
                    NoDevice = false;
                    return device;
                }
            }
            catch (Exception e) { JDPack.FileLog.Log(e.ToString()); NoDevice = true; return null; }
        }
        private void EnableDevice(string deviceId)
        {
            try
            {
                using (var device = GetDevice(deviceId))
                {
                    if (device == null) { JDPack.FileLog.Log("EnableDevice: Fail to get specific device."); return; }
                    
                    Guid IID_IAudioEndpointVolume = typeof(AudioClient).GUID;
                    IntPtr i = device.Activate(IID_IAudioEndpointVolume, 0, IntPtr.Zero);
                    using (var aev = new AudioClient(i))
                    {
                        aev.Start();
                    }
                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(EnableDevice): {e.ToString()}"); return; }
        }
        private void DisableDevice(string deviceId)
        {
            try
            {
                using (var device = GetDevice(deviceId))
                {
                    if (device == null) { JDPack.FileLog.Log("EnableDevice: Fail to get specific device."); return; }

                    Guid IID_IAudioEndpointVolume = typeof(AudioClient).GUID;
                    IntPtr i = device.Activate(IID_IAudioEndpointVolume, 0, IntPtr.Zero);
                    using (var aev = new AudioClient(i))
                    {
                        aev.Stop();
                    }
                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(EnableDevice): {e.ToString()}"); return; }
        }
        #endregion


        #region Private Session Items
        //Session Control Items
        private SessionDataList sessionList;
        private void GetSession()
        {
            if (sessionList == null) { sessionList = new SessionDataList(); }
            GetSessionData();
        }
        private void GetSessionData()
        {
            try
            {
                lock (sessionLocker)
                {
                    using (var defaultDevice = GetDefaultDevice())
                    using (var asm = (defaultDevice != null ? AudioSessionManager2.FromMMDevice(defaultDevice) : null))
                    using (var ase = (asm?.GetSessionEnumerator()))
                    {
                        if (defaultDevice == null) { JDPack.FileLog.Log("GetSessionData: Fail to get master device."); return; }

                        foreach (AudioSessionControl asc in ase)
                        {
                            //bool delete = false;
                            using (var asc2 = asc.QueryInterface<AudioSessionControl2>())
                            using (var simpleAudioVolume = asc.QueryInterface<SimpleAudioVolume>())
                            using (var audioMeterInformation = asc.QueryInterface<AudioMeterInformation>())
                            {
                                //check expired
                                SessionState state = State(asc.SessionState);
                                //Console.WriteLine($"{asc.SessionState}");


                                //System.Diagnostics.Process process = Process(asc2.ProcessID);
                                //System.Diagnostics.Process.GetProcessById(asc2.ProcessID);


                                //process check and make nameset, new session check and update
                                if (!asc.IsDisposed && state != SessionState.Expired)//asc2.Process != null && 
                                {
                                    bool exists = false;
                                    sessionList.ForEach(s => { if (s.PID == asc2.ProcessID) exists = true; });

                                    //Console.WriteLine($"{asc2}");
                                    //Console.WriteLine($"DNAME:{asc.DisplayName}({asc2.ProcessID})\r\nSID:{asc2.SessionIdentifier}\r\nSIID:{asc2.SessionInstanceIdentifier}");
                                    //Console.WriteLine($"PNAME:{asc2.Process.ProcessName}\r\nPWTITLE:{asc2.Process.MainWindowTitle}\r\nSID:{asc2.Process.SessionId}");

                                    NameSet nameSet = new NameSet(
                                        asc2.ProcessID,
                                        asc2.IsSystemSoundSession,
                                        "",//asc2.Process.ProcessName
                                        "",//asc2.Process.MainWindowTitle
                                        asc.DisplayName,
                                        asc2.SessionIdentifier
                                        );
                                    //Console.WriteLine($"NAME:{nameSet.Name}({asc2.ProcessID}), STATE:{state}");
                                    //Console.WriteLine($"1:{nameSet.IsSystemSoundSession},2:{nameSet.ProcessName},3:{nameSet.MainWindowTitle},4:{nameSet.DisplayName},5:{nameSet.SessionIdentifier}");

                                    float peak = 0;
                                    if (audioMeterInformation.MeteringChannelCount > 1)
                                    {
                                        float[] peaks = audioMeterInformation.GetChannelsPeakValues();
                                        //Console.Write($"{asc2.ProcessID}({audioMeterInformation.MeteringChannelCount}):[{audioMeterInformation.PeakValue},{peaks.Average()}]");
                                        //foreach (float p in peaks) { Console.Write($",{p.ToString()}"); }
                                        //Console.WriteLine();
                                        peak = peaks.Average();
                                    }
                                    else if (audioMeterInformation.MeteringChannelCount == 0 || audioMeterInformation.MeteringChannelCount == 1)
                                    {
                                        peak = audioMeterInformation.PeakValue;
                                    }

                                    if (!exists)
                                    {
                                        if (!ExcludeList.Contains(nameSet.Name)) { simpleAudioVolume.MasterVolume = 0.01f; }
                                        sessionList.Add(new SessionData()
                                        {
                                            State = state,
                                            nameSet = nameSet,
                                            Volume = simpleAudioVolume.MasterVolume,
                                            Peak = peak
                                        });
                                    }
                                    else
                                    {
                                        using (var s = sessionList.GetSession((uint)asc2.ProcessID))
                                        {
                                            s.State = state;
                                            s.nameSet = nameSet;
                                            s.Volume = simpleAudioVolume.MasterVolume;
                                            s.Peak = peak;
                                        }
                                    }
                                }
                                //else if(process == null) { delete = true; }
                                //null process check, expired check
                            }
                            //if (delete) { asc.Dispose(); }
                        }

                        //expired check and remove session data
                        List<SessionData> expired = new List<SessionData>();
                        sessionList.ForEach(s =>
                        {
                            bool exists = false;
                            foreach (AudioSessionControl asc in ase)
                            {
                                using (var asc2 = asc.QueryInterface<AudioSessionControl2>())
                                {
                                    if (s.PID == asc2.ProcessID && asc.SessionState != AudioSessionState.AudioSessionStateExpired) exists = true;
                                }
                            }
                            if (!exists) expired.Add(s);
                        });
                        expired.ForEach(s => sessionList.Remove(s));
                        expired.Clear();
                        
                    }
                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(GetSessionData): {e.StackTrace}"); }
            //sessionList.ForEach(s => Console.WriteLine($"{s.PID}"));
        }

        private void GetState(SessionData session)
        {
            try
            {
                lock (sessionLocker)
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
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(GetState): {e.ToString()}"); }
        }
        private void GetSessionVolume(SessionData session)
        {
            try
            {
                lock (sessionLocker)
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
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(GetSessionVolume): {e.ToString()}"); }
        }
        private void SetSessionVolume(SessionData session, float volume)
        {
            try
            {
                lock (sessionLocker)
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
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(SetSessionVolume-pri): {e.ToString()}"); }
        }
        private void GetSessionPeak(SessionData session)
        {
            try
            {
                lock (sessionLocker)
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
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(GetSessionPeak): {e.ToString()}"); }
        }
        private void GetSessionPeakTotal(SessionData session)
        {
            try
            {
                lock (sessionLocker)
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
                                if (session2.ProcessID == session.PID)
                                {
                                    float[] peaks = audioMeterInformation.GetChannelsPeakValues();
                                    Console.Write($"{session.PID}({audioMeterInformation.MeteringChannelCount}):[{peaks.Average()}]");
                                    foreach (float p in peaks) { Console.Write($",{p.ToString()}"); }
                                    Console.WriteLine();
                                    session.Peak = peaks.Average();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(GetSessionPeak): {e.ToString()}"); }
        }

        private void SetSessionAvTime(SessionData session, double AVTime, double ACInterval)
        {
            try
            {
                lock (sessionLocker)
                {
                    session.SetAvTime(AVTime, ACInterval);
                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(SetSessionAvData): {e.ToString()}"); }
        }
        private void SetSessionAverage(SessionData session, double peak)
        {
            try
            {
                lock (sessionLocker)
                {
                    session.SetAverage(peak);
                }
            }
            catch (Exception e) { JDPack.FileLog.Log($"Error(SetSessionAverage): {e.ToString()}"); }
        }
        #endregion


    }//End class Audio
}
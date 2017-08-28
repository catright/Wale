using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Vannatech.CoreAudio.Interfaces;
using Vannatech.CoreAudio.Enumerations;

namespace Wale.Subclasses
{
    public static class Audio
    {
        #region Master Volume Manipulation

        public static float GetMasterPeak()
        {
            IAudioMeterInformation masterPeak = null;
            try
            {
                masterPeak = GetMasterPeakObject();
                if (masterPeak == null)
                    return -1;

                uint peakCount;
                masterPeak.GetMeteringChannelCount(out peakCount);
                if (peakCount <= 0) return -2;

                float peakLevel;
                masterPeak.GetPeakValue(out peakLevel);
                return peakLevel;
            }
            finally
            {
                if (masterPeak != null)
                    Marshal.ReleaseComObject(masterPeak);
            }
        }
        public static float GetMasterVolume()
        {
            IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return -1;

                float volumeLevel;
                masterVol.GetMasterVolumeLevelScalar(out volumeLevel);
                return volumeLevel;
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }
        public static bool GetMasterVolumeMute()
        {
            IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return false;

                bool isMuted;
                masterVol.GetMute(out isMuted);
                return isMuted;
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }
        
        public static void SetMasterVolume(float newLevel)
        {
            IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return;

                masterVol.SetMasterVolumeLevelScalar(newLevel, Guid.Empty);
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }
        public static float StepMasterVolume(float stepAmount)
        {
            IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return -1;

                float stepAmountScaled = stepAmount;

                // Get the level
                float volumeLevel;
                masterVol.GetMasterVolumeLevelScalar(out volumeLevel);

                // Calculate the new level
                float newLevel = volumeLevel + stepAmountScaled;
                newLevel = Math.Min(1, newLevel);
                newLevel = Math.Max(0, newLevel);

                masterVol.SetMasterVolumeLevelScalar(newLevel, Guid.Empty);

                // Return the new volume level that was set
                return newLevel;
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }
        public static void SetMasterVolumeMute(bool isMuted)
        {
            IAudioEndpointVolume masterVol = null;
            try
            {
                masterVol = GetMasterVolumeObject();
                if (masterVol == null)
                    return;

                masterVol.SetMute(isMuted, Guid.Empty);
            }
            finally
            {
                if (masterVol != null)
                    Marshal.ReleaseComObject(masterVol);
            }
        }

        public static IMMDevice GetDevice()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IMMDevice speakers = null;
            try
            {
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                //Guid IID_IAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;
                //object o;
                //speakers.Activate(IID_IAudioEndpointVolume, 0, IntPtr.Zero, out o);
                //IAudioEndpointVolume masterVol = (IAudioEndpointVolume)o;

                //return masterVol;
                return speakers;
            }
            finally
            {
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }
        private static IAudioPeakMeter GetMasterLoudnessObject()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IMMDevice speakers = null;
            try
            {
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                Guid IID_IAudioEndpointPeak = typeof(IAudioPeakMeter).GUID;
                object o;
                speakers.Activate(IID_IAudioEndpointPeak, 0, IntPtr.Zero, out o);
                IAudioPeakMeter masterPeak = (IAudioPeakMeter)o;

                return masterPeak;
            }
            finally
            {
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }
        private static IAudioMeterInformation GetMasterPeakObject()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IMMDevice speakers = null;
            try
            {
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                Guid IID_IAudioEndpointPeak = typeof(IAudioMeterInformation).GUID;
                object o;
                speakers.Activate(IID_IAudioEndpointPeak, 0, IntPtr.Zero, out o);
                IAudioMeterInformation masterPeak = (IAudioMeterInformation)o;

                return masterPeak;
            }
            finally
            {
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }
        private static IAudioEndpointVolume GetMasterVolumeObject()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IMMDevice speakers = null;
            try
            {
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                Guid IID_IAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;
                object o;
                speakers.Activate(IID_IAudioEndpointVolume, 0, IntPtr.Zero, out o);
                IAudioEndpointVolume masterVol = (IAudioEndpointVolume)o;

                return masterVol;
            }
            finally
            {
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }

        #endregion

        #region Individual Application Volume Manipulation

        public static uint[] GetApplicationIDs()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            try
            {
                // get the speakers (1st render + multimedia) device
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);
                
                // search for an audio session with the required process-id
                uint[] ids = new uint[count];
                for (int i = 0; i < count; ++i)
                {
                    IAudioSessionControl ctl = null;
                    IAudioSessionControl2 ctl2 = null;
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);

                        ctl2 = (IAudioSessionControl2)ctl;
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                        uint cpid;
                        ctl2.GetProcessId(out cpid);
                        ids[i] = cpid;
                    }
                    finally
                    {
                        if (ctl != null) Marshal.ReleaseComObject(ctl);
                        if (ctl2 != null) Marshal.ReleaseComObject(ctl2);
                    }
                }
                
                return ids;
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }
        public static string GetApplicationName(uint pid)
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            try
            {
                // get the speakers (1st render + multimedia) device
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                // search for an audio session with the required process-id
                string name = "";
                for (int i = 0; i < count; ++i)
                {
                    IAudioSessionControl ctl = null;
                    IAudioSessionControl2 ctl2 = null;
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);

                        ctl2 = (IAudioSessionControl2)ctl;
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                        uint cpid;
                        ctl2.GetProcessId(out cpid);

                        if (cpid == pid)
                        {
                            ctl2.GetSessionIdentifier(out name);
                            break;
                        }
                    }
                    finally
                    {
                        if (ctl != null) Marshal.ReleaseComObject(ctl);
                        if (ctl2 != null) Marshal.ReleaseComObject(ctl2);
                    }
                }

                return name;
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }
        public static SessionState GetApplicationState(uint pid)
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            try
            {
                // get the speakers (1st render + multimedia) device
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                // search for an audio session with the required process-id
                AudioSessionState state = AudioSessionState.AudioSessionStateExpired;
                for (int i = 0; i < count; ++i)
                {
                    IAudioSessionControl ctl = null;
                    IAudioSessionControl2 ctl2 = null;
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);

                        ctl2 = (IAudioSessionControl2)ctl;
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                        uint cpid;
                        ctl2.GetProcessId(out cpid);

                        if (cpid == pid)
                        {
                            ctl2.GetState(out state);
                            break;
                        }
                    }
                    finally
                    {
                        if (ctl != null) Marshal.ReleaseComObject(ctl);
                        if (ctl2 != null) Marshal.ReleaseComObject(ctl2);
                    }
                }

                switch (state)
                {
                    case AudioSessionState.AudioSessionStateActive:
                        return SessionState.Active;
                    case AudioSessionState.AudioSessionStateInactive:
                        return SessionState.Inactive;
                    case AudioSessionState.AudioSessionStateExpired:
                        return SessionState.Expired;
                    default:
                        return SessionState.Expired;
                }
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }
        public static float GetApplicationPeak(uint pid)
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            try
            {
                // get the speakers (1st render + multimedia) device
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                // search for an audio session with the required process-id
                float peak = -1;
                for (int i = 0; i < count; ++i)
                {
                    IAudioSessionControl ctl = null;
                    IAudioSessionControl2 ctl2 = null;
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);

                        ctl2 = (IAudioSessionControl2)ctl;
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                        uint cpid;
                        ctl2.GetProcessId(out cpid);

                        if (cpid == pid)
                        {
                            (ctl2 as IAudioMeterInformation).GetPeakValue(out peak);
                            break;
                        }
                    }
                    finally
                    {
                        if (ctl != null) Marshal.ReleaseComObject(ctl);
                        if (ctl2 != null) Marshal.ReleaseComObject(ctl2);
                    }
                }

                return peak;
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }
        public static float GetApplicationVolume(uint pid)
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            try
            {
                // get the speakers (1st render + multimedia) device
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                // search for an audio session with the required process-id
                float volume= -1;
                for (int i = 0; i < count; ++i)
                {
                    IAudioSessionControl ctl = null;
                    IAudioSessionControl2 ctl2 = null;
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);

                        ctl2 = (IAudioSessionControl2)ctl;
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                        uint cpid;
                        ctl2.GetProcessId(out cpid);

                        if (cpid == pid)
                        {
                            (ctl2 as ISimpleAudioVolume).GetMasterVolume(out volume);
                            break;
                        }
                    }
                    finally
                    {
                        if (ctl != null) Marshal.ReleaseComObject(ctl);
                        if (ctl2 != null) Marshal.ReleaseComObject(ctl2);
                    }
                }

                return volume;
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }
        public static void SetApplicationVolume(uint pid, float volume)
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            try
            {
                // get the speakers (1st render + multimedia) device
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                // search for an audio session with the required process-id
                for (int i = 0; i < count; ++i)
                {
                    IAudioSessionControl ctl = null;
                    IAudioSessionControl2 ctl2 = null;
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);

                        ctl2 = (IAudioSessionControl2)ctl;
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                        uint cpid;
                        ctl2.GetProcessId(out cpid);

                        if (cpid == pid)
                        {
                            (ctl2 as ISimpleAudioVolume).SetMasterVolume(volume, Guid.Empty);
                            break;
                        }
                    }
                    finally
                    {
                        if (ctl != null) Marshal.ReleaseComObject(ctl);
                        if (ctl2 != null) Marshal.ReleaseComObject(ctl2);
                    }
                }
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }
        
        public static bool? GetApplicationMute(uint pid)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return null;

            bool mute;
            volume.GetMute(out mute);
            Marshal.ReleaseComObject(volume);
            return mute;
        }
        public static void SetApplicationMute(uint pid, bool mute)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return;

            Guid guid = Guid.Empty;
            volume.SetMute(mute, guid);
            Marshal.ReleaseComObject(volume);
        }

        private static ISimpleAudioVolume GetVolumeObject(uint pid)
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            try
            {
                // get the speakers (1st render + multimedia) device
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);
                
                // search for an audio session with the required process-id
                ISimpleAudioVolume volumeControl = null;
                for (int i = 0; i < count; ++i)
                {
                    IAudioSessionControl ctl = null;
                    IAudioSessionControl2 ctl2 = null;
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);

                        ctl2 = (IAudioSessionControl2)ctl;
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                        uint cpid;
                        ctl2.GetProcessId(out cpid);

                        if (cpid == pid)
                        {
                            volumeControl = ctl2 as ISimpleAudioVolume;
                            break;
                        }
                    }
                    finally
                    {
                        if (ctl != null) Marshal.ReleaseComObject(ctl);
                        if(ctl2 != null) Marshal.ReleaseComObject(ctl2);
                    }
                }

                return volumeControl;
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
                if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
            }
        }

        #endregion
        
    }
    

    #region Abstracted COM interfaces from Windows CoreAudio API
    
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    public enum SessionState { Active, Inactive, Expired }
    internal static class Locker
    {
        public static object MMDeviceEnumerator = new object();
    }
    #endregion
}
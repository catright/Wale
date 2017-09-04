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
    public class Session : IDisposable
    {
        public float Relative = 0;
        public double AveragePeak { get; private set; }
        public bool AutoIncluded = true, Averaging = true;

        public uint ProcessId { get; private set; }
        public string ProcessName { get; private set; }
        public SessionState SessionState { get; private set; }
        public float SessionPeak { get; private set; }
        public float SessionVolume { get; private set; }

        public void Refresh()
        {
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //sw.Start();
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            //lock (Locker.MMDeviceEnumerator)
            //{
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);
                }
                finally
                {
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            //}
            try
            {
                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                IAudioSessionControl2 SCObject = null;
                // search for an audio session with the required process-id
                for (int i = 0; i < count; ++i)
                {
                    IAudioSessionControl ctl = null;
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);

                        SCObject = (IAudioSessionControl2)ctl;
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                        uint cpid;
                        SCObject.GetProcessId(out cpid);

                        if (cpid == ProcessId)
                        {
                            AudioSessionState state;
                            SCObject.GetState(out state);
                            switch (state)
                            {
                                case AudioSessionState.AudioSessionStateActive:
                                    SessionState = SessionState.Active;
                                    break;
                                case AudioSessionState.AudioSessionStateInactive:
                                    SessionState = SessionState.Inactive;
                                    break;
                                case AudioSessionState.AudioSessionStateExpired:
                                    SessionState = SessionState.Expired;
                                    break;
                                default:
                                    SessionState = SessionState.Expired;
                                    break;
                            }
                            float peak, volume;
                            (SCObject as IAudioMeterInformation).GetPeakValue(out peak);
                            SessionPeak = peak;
                            (SCObject as ISimpleAudioVolume).GetMasterVolume(out volume);
                            SessionVolume = volume;
                        }
                        //rcwHandle = GCHandle.Alloc(SessionControlObject, GCHandleType.Normal);
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                    }
                    finally
                    {
                        //if (ctl != null) Marshal.ReleaseComObject(ctl);
                    }
                }

            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
            }
            //sw.Stop();
            //Console.WriteLine($"session {this.ProcessName}({this.ProcessId}) refresh time={sw.ElapsedMilliseconds}");
        }
        public void SetVolume(float volume) { SetApplicationVolume(volume); }
        public void GetSession() { /*MakeControlObject(); MakePeakObject(); MakeVolumeObject();*/ }

        public Session(uint pid)
        {
            ProcessId = pid;
            if (GetApplicationName()) GetSession();
            else throw new NullReferenceException($"There is no device that has ProcessId:{pid}");
        }
        //Dispose
        private GCHandle rcwHandle;
        private bool disposed = false;
        ~Session() { Dispose(false); }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                }
                if (SessionPeakObject != null) Marshal.ReleaseComObject(SessionPeakObject);
                if (SessionVolumeObject != null) Marshal.ReleaseComObject(SessionVolumeObject);
                if (SessionControlObject != null)
                {
                    if (rcwHandle.IsAllocated) Marshal.ReleaseComObject(SessionControlObject);
                }
                disposed = true;
            }
        }

        private bool GetApplicationName()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            lock (Locker.MMDeviceEnumerator)
            {
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);
                }
                finally
                {
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            }
            try
            {
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
                string name = "null";
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

                        if (cpid == ProcessId)
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
                if (name == "null") return false;
                else { ProcessName = MakeName(name); return true; }
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
            }
        }
        private string MakeName(string name)
        {
            int startidx = name.IndexOf("|"), endidx = name.IndexOf("%b");
            name = name.Substring(startidx, endidx - startidx + 2);
            if (name == "|#%b") name = "System";
            else
            {
                startidx = name.LastIndexOf("\\") + 1; endidx = name.IndexOf("%b");
                name = name.Substring(startidx, endidx - startidx);
                if (name.EndsWith(".exe")) name = name.Substring(0, name.LastIndexOf(".exe"));
            }
            return name;
        }

        private SessionState GetApplicationState()
        {
            IAudioSessionControl2 SCObject = null;
            try
            {
                SCObject = GetControlObject();
                if (SCObject == null) return SessionState.Expired;

                AudioSessionState state;
                SCObject.GetState(out state);
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
            finally { if (SCObject != null) Marshal.ReleaseComObject(SCObject); }
        }
        private float GetApplicationPeak()
        {
            IAudioMeterInformation SPObject = null;
            try
            {
                SPObject = GetPeakObject();
                if (SPObject == null) return -1;

                //uint peakCount;
                //SPObject.GetMeteringChannelCount(out peakCount);
                //if (peakCount <= 0) return -2;

                float peakLevel;
                SPObject.GetPeakValue(out peakLevel);
                return peakLevel;
            }
            finally { if (SPObject != null) Marshal.ReleaseComObject(SPObject); }
        }
        private float GetApplicationVolume()
        {
            ISimpleAudioVolume SVObject = null;
            try
            {
                SVObject = GetVolumeObject();
                if (SVObject == null) return -1;

                float volume;
                SVObject.GetMasterVolume(out volume);
                return volume;
            }
            finally { if (SVObject != null) Marshal.ReleaseComObject(SVObject); }
        }
        private void SetApplicationVolume(float volume)
        {
            ISimpleAudioVolume SVObject = null;
            try
            {
                SVObject = GetVolumeObject();
                if (SVObject == null) return;

                SVObject.SetMasterVolume(volume, Guid.Empty);
            }
            finally { if (SVObject != null) Marshal.ReleaseComObject(SVObject); }
        }

        private IAudioSessionControl2 SessionControlObject = null;
        private IAudioMeterInformation SessionPeakObject = null;
        private ISimpleAudioVolume SessionVolumeObject = null;

        private IAudioSessionControl2 GetControlObject()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            lock (Locker.MMDeviceEnumerator)
            {
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);
                }
                finally
                {
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            }
            try
            {
                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                IAudioSessionControl2 SCObject = null;
                // search for an audio session with the required process-id
                for (int i = 0; i < count; ++i)
                {
                    IAudioSessionControl ctl = null;
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);

                        SCObject = (IAudioSessionControl2)ctl;
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                        uint cpid;
                        SCObject.GetProcessId(out cpid);

                        if (cpid == ProcessId)
                        {
                            break;
                        }
                        //rcwHandle = GCHandle.Alloc(SessionControlObject, GCHandleType.Normal);
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                    }
                    finally
                    {
                        //if (ctl != null) Marshal.ReleaseComObject(ctl);
                    }
                }
                return SCObject;
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
            }
        }
        private IAudioMeterInformation GetPeakObject()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            lock (Locker.MMDeviceEnumerator)
            {
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);
                }
                finally
                {
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            }
            try
            {
                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                IAudioMeterInformation SPObject = null;
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

                        if (cpid == ProcessId)
                        {
                            SPObject = ctl2 as IAudioMeterInformation;
                            break;
                        }
                    }
                    finally
                    {
                        //if (ctl != null) Marshal.ReleaseComObject(ctl);
                        //if (ctl2 != null) Marshal.ReleaseComObject(ctl2);
                    }
                }
                return SPObject;
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
            }
        }
        private ISimpleAudioVolume GetVolumeObject()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            //lock (Locker.MMDeviceEnumerator)
            //{
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);
                }
                finally
                {
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            //}
            try
            {
                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                speakers.Activate(IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                mgr.GetSessionEnumerator(out sessionEnumerator);
                int count;
                sessionEnumerator.GetCount(out count);

                ISimpleAudioVolume SVObject = null;
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

                        if (cpid == ProcessId)
                        {
                            SVObject = ctl2 as ISimpleAudioVolume;
                            break;
                        }
                    }
                    finally
                    {
                        //if (ctl != null) Marshal.ReleaseComObject(ctl);
                        //if (ctl2 != null) Marshal.ReleaseComObject(ctl2);
                    }
                }
                return SVObject;
            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
            }
        }
        private void MakeControlObject()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            lock (Locker.MMDeviceEnumerator)
            {
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);
                }
                finally
                {
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            }
            try
            {
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
                    try
                    {
                        sessionEnumerator.GetSession(i, out ctl);

                        SessionControlObject = (IAudioSessionControl2)ctl;
                        rcwHandle = GCHandle.Alloc(SessionControlObject, GCHandleType.Normal);
                        // NOTE: we could also use the app name from ctl.GetDisplayName()
                    }
                    finally
                    {
                        if (ctl != null) Marshal.ReleaseComObject(ctl);
                    }
                }

            }
            finally
            {
                if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                if (mgr != null) Marshal.ReleaseComObject(mgr);
                if (speakers != null) Marshal.ReleaseComObject(speakers);
            }
        }
        private void MakePeakObject()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            lock (Locker.MMDeviceEnumerator)
            {
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);
                }
                finally
                {
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            }
            try
            {
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

                        if (cpid == ProcessId)
                        {
                            SessionPeakObject = ctl2 as IAudioMeterInformation;
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
            }
        }
        private void MakeVolumeObject()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IAudioSessionEnumerator sessionEnumerator = null;
            IAudioSessionManager2 mgr = null;
            IMMDevice speakers = null;
            lock (Locker.MMDeviceEnumerator)
            {
                try
                {
                    // get the speakers (1st render + multimedia) device
                    deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);
                }
                finally
                {
                    if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
                }
            }
            try
            {
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

                        if (cpid == ProcessId)
                        {
                            SessionVolumeObject = ctl2 as ISimpleAudioVolume;
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
            }
        }



        private List<double> Peaks = new List<double>();
        private int AvCount = 0;
        private double AvTime;

        public void SetAvTime(double critTime, double unitTime) { AvCount = (int)(critTime / unitTime); AvTime = unitTime * (double)AvCount; }
        public void ResetAverage() { Peaks.Clear(); AveragePeak = 1; }
        public void SetAverage(double peak)
        {
            if (Peaks.Count > AvCount) Peaks.RemoveAt(0);
            Peaks.Add(peak);
            AveragePeak = Peaks.Average();
            //Console.WriteLine($"Av={AveragePeak}, PC={Peaks.Count}, AvT={AvTime}");
        }
    }
}

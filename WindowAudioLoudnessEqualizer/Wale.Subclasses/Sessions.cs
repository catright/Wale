using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
//using Vannatech.CoreAudio.Interfaces;
//using Vannatech.CoreAudio.Enumerations;

namespace Wale.CoreAudio
{
    /*public class Sessions : List<Session>
    {
        public void RefreshSessions()
        {
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //sw.Start();
            uint[] ids = GetApplicationIDs();
            for (uint i = 0; i < ids.Count(); i++)
            {
                bool exists = false;
                this.ForEach(s =>
                {
                    if (s.ProcessId == ids[i]) exists = true;
                });
                if (!exists) this.Add(new Session(ids[i]));
            }
            List<Session> expired = new List<Session>();
            this.ForEach(s =>
            {
                bool exists = false;
                for (uint i = 0; i < ids.Count(); i++)
                {
                    if (s.ProcessId == ids[i]) exists = true;
                }
                if (!exists) expired.Add(s);
            });
            expired.ForEach(s => this.Remove(s));
            expired.Clear();
            //sw.Stop();
            //Console.WriteLine($"session list refresh time={sw.ElapsedMilliseconds}");
        }
        public Sessions() { this.Clear(); RefreshSessions(); }
        public Session GetSession(uint id) { try { return this.Find(sc => sc.ProcessId == id); } catch (ArgumentNullException) { return null; } }
        public double GetRelative(uint id) { return GetSession(id).Relative; }

        private uint[] GetApplicationIDs()
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
    }//End class Sessions*/
}//End namespace Wale.CoreAudio
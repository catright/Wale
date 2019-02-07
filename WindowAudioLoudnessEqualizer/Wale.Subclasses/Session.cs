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
    /// <summary>
    /// Data container for audio session
    /// </summary>
    public class Session : IDisposable//, CSCore.CoreAudioAPI.AudioSessionControl2
    {
        private CSCore.CoreAudioAPI.AudioSessionControl2 asc2;
        public Session(CSCore.CoreAudioAPI.AudioSessionControl2 asc2, List<string> ExcList, double AvTime, double AvInterval)
        {
            this.asc2 = asc2;
            nameSet = new NameSet(
                asc2.ProcessID,
                asc2.IsSystemSoundSession,
                "",//asc2.Process.ProcessName
                "",//asc2.Process.MainWindowTitle
                asc2.DisplayName,
                asc2.SessionIdentifier
            );
            AutoIncluded = ExcList.Contains(nameSet.Name) ? false : true;
            SetAvTime(AvTime, AvInterval);
        }
        /*public Session(CSCore.CoreAudioAPI.AudioSessionControl2 asc2, NameSet nameset) { this.asc2 = asc2; this.nameSet = nameset; }
        public Session(CSCore.CoreAudioAPI.AudioSessionControl2 asc2, int pid, bool issystem, string pname, string mwtitle, string dispname, string sessider)
        {
            this.asc2 = asc2;
            //ProcessID = pid;
            //IsSystemSoundSession = issystem;
            //ProcessName = pname;
            //MainWindowTitle = mwtitle;
            //DisplayName = dispname;
            //SessionIdentifier = sessider;
            //name = MakeName();
        }*/

        #region API Default Datas
        public NameSet nameSet { get; set; }
        /// <summary>
        /// Converted from CoreAudioApi
        /// </summary>
        public SessionState State {
            get
            {
                switch (asc2.SessionState)
                {
                    case CSCore.CoreAudioAPI.AudioSessionState.AudioSessionStateActive: return SessionState.Active;
                    case CSCore.CoreAudioAPI.AudioSessionState.AudioSessionStateInactive: return SessionState.Inactive;
                    default: return SessionState.Expired;
                }
            }
        }
        private bool IsSystemSoundSession => asc2.IsSystemSoundSession;

        private string name;
        /// <summary>
        /// Human readable process name
        /// </summary>
        public string Name { get { if (nameSet != null) return nameSet.Name; return name; } set => name = value; }
        public int ProcessID => asc2.ProcessID;
        public uint PID { get => (uint)ProcessID; }
        /// <summary>
        /// Process Id
        /// </summary>
        //public uint PID { get { if (nameSet != null) return (uint)nameSet.ProcessID; return (uint)ProcessID; } set => ProcessID = (int)value; }
        //private readonly string DisplayName, ProcessName, MainWindowTitle;
        private string DisplayName => asc2.DisplayName;
        private string ProcessName => asc2.Process.ProcessName;
        private string MainWindowTitle => asc2.Process.MainWindowTitle;
        //public string SessionIdentifier { get; private set; }
        public string SessionIdentifier => asc2.SessionIdentifier;
        public float Volume
        {
            get { using (var v = asc2.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>()) { return v.MasterVolume; } }
            set { using (var v = asc2.QueryInterface<CSCore.CoreAudioAPI.SimpleAudioVolume>()) { v.MasterVolume = RV(value); } }
        }
        public float Peak
        {
            get
            {
                using (var p = asc2.QueryInterface<CSCore.CoreAudioAPI.AudioMeterInformation>())
                {
                    return p.GetMeteringChannelCount() > 1 ? p.GetChannelsPeakValues().Average() : p.PeakValue;
                }
            }
        }
        
        #endregion

        #region Customized Datas
        /// <summary>
        /// This value would be added arithmetically when setting the volume for the session.
        /// </summary>
        public float Relative { get; set; } = 0f;
        public float MinVol { get; set; } = 0.01f;
        private float RV(float vol)
        {
            float v = vol + Relative;
            return (v > 1) ? 1 : ((v < MinVol) ? MinVol : v);
        }
        /// <summary>
        /// The session is included to Auto controller when this flag is True. Default is True.
        /// </summary>
        public bool AutoIncluded { get; set; } = true;
        /// <summary>
        /// Average Calculation is enabled when this flag is True. Default is True.
        /// </summary>
        public bool Averaging { get; set; } = true;
        #endregion


        #region Averaging
        /// <summary>
        /// Average peak value within AvTime.
        /// </summary>
        public double AveragePeak { get; private set; }
        /// <summary>
        /// Total stacking time for averaging.
        /// </summary>
        public double AvTime { get; private set; }//ms
        private int AvCount = 0;
        private List<double> Peaks = new List<double>();

        /// <summary>
        /// Set stacking time for average calculation.
        /// </summary>
        /// <param name="averagingTime">Total stacking time.[ms](=AvTime)</param>
        /// <param name="unitTime">Passing time when calculate the average once.[ms]</param>
        public void SetAvTime(double averagingTime, double unitTime)
        {
            AvTime = averagingTime;
            AvCount = (int)(averagingTime / unitTime);
            //Console.WriteLine($"Average Time Updated Cnt:{AvCount}");
            JDPack.FileLog.Log($"Average Time Updated Cnt:{AvCount}");
            ResetAverage();
        }
        /// <summary>
        /// Clear all stacked peak values and set average to 0.
        /// </summary>
        public void ResetAverage() { Peaks.Clear(); AveragePeak = 0; JDPack.FileLog.Log("Average Reset"); }//Console.WriteLine("Average Reset");
        /// <summary>
        /// Add new peak value to peaks container and re-calculate AveragePeak value.
        /// </summary>
        /// <param name="peak"></param>
        public void SetAverage(double peak)
        {
            if (Peaks.Count > AvCount) Peaks.RemoveAt(0);
            Peaks.Add(peak);
            AveragePeak = Peaks.Average();
            //Console.WriteLine($"Av={AveragePeak}, PC={Peaks.Count}, AvT={AvTime}");
        }
        #endregion


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

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SessionData() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }//End class Session2
    /// <summary>
    /// List object of SessionData.
    /// </summary>
    public class SessionList : List<Session>
    {
        public SessionList() { this.Clear(); }
        /// <summary>
        /// Find session by its process id.
        /// <para>Log($"Error(GetSession): ArgumentNullException") when ArgumentNullException. 
        /// Log($"Error(GetSession): NullReferenceException") when NullReferenceException. 
        /// Log($"Error(GetSession): {(Exception)e.ToString()}") when Exception</para>
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <returns>SessionData when find SessionData successfully or null.</returns>
        public Session GetSession(uint pid)
        {
            try { return this.Find(sc => sc.PID == pid); }
            catch (ArgumentNullException)
            {
                JDPack.FileLog.Log($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JDPack.FileLog.Log($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JDPack.FileLog.Log($"Error(GetSession): {e.ToString()}");
            }
            return null;
        }
        /// <summary>
        /// Find session by its process id.
        /// <para>Log($"Error(GetSession): ArgumentNullException") when ArgumentNullException. 
        /// Log($"Error(GetSession): NullReferenceException") when NullReferenceException. 
        /// Log($"Error(GetSession): {(Exception)e.ToString()}") when Exception</para>
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <returns>SessionData when find SessionData successfully or null.</returns>
        public Session GetSession(int pid)
        {
            try { return this.Find(sc => sc.ProcessID == pid); }
            catch (ArgumentNullException)
            {
                JDPack.FileLog.Log($"Error(GetSession): ArgumentNullException");
            }
            catch (NullReferenceException)
            {
                JDPack.FileLog.Log($"Error(GetSession): NullReferenceException");
            }
            catch (Exception e)
            {
                JDPack.FileLog.Log($"Error(GetSession): {e.ToString()}");
            }
            return null;
        }
        /// <summary>
        /// Get Relative value with <code>GetSession</code> by its process id.
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <returns>Relative value when find Relative value successfully or 0.0.</returns>
        public double GetRelative(uint pid)
        {
            try { return GetSession(pid).Relative; }
            catch (NullReferenceException)
            {
                JDPack.FileLog.Log($"Error(GetRelative): NullReferenceException");
            }
            catch (Exception e)
            {
                JDPack.FileLog.Log($"Error(GetRelative): {e.ToString()}");
            }
            return 0.0;
        }

    }
}//End namespace Wale.Subclasses

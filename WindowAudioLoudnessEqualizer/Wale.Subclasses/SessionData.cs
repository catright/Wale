using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.CoreAudio
{
    /// <summary>
    /// Data container for audio session
    /// </summary>
    public class SessionData : IDisposable
    {
        public SessionData() { }
        public SessionData(NameSet nameset) { this.nameSet = nameset; }
        public SessionData(int pid, bool issystem, string pname, string mwtitle, string dispname, string sessider)
        {
            ProcessID = pid;
            IsSystemSoundSession = issystem;
            ProcessName = pname;
            MainWindowTitle = mwtitle;
            DisplayName = dispname;
            SessionIdentifier = sessider;
            //name = MakeName();
        }

        #region API Default Datas
        public NameSet nameSet;
        /// <summary>
        /// Converted from CoreAudioApi
        /// </summary>
        public SessionState State { get; set; }
        private bool IsSystemSoundSession;

        private string name;
        /// <summary>
        /// Human readable process name
        /// </summary>
        public string Name { get { if (nameSet != null) return nameSet.Name; return name; } set => name = value; }
        private int ProcessID;
        /// <summary>
        /// Process Id
        /// </summary>
        public uint PID { get { if (nameSet != null) return (uint)nameSet.ProcessID; return (uint)ProcessID; } set => ProcessID = (int)value; }
        private string DisplayName, ProcessName, MainWindowTitle;
        public string SessionIdentifier { get; private set; }
        public float Volume { get; set; }
        public float Peak { get; set; }

        private string MakeName()
        {
            if (IsSystemSoundSession) { return "System Sound"; }
            else if (SessionIdentifier.EndsWith("{5E081B13-649D-48BC-9F67-4DBF51759BD8}")) { return "Windows Shell Experience Host"; }
            else if (SessionIdentifier.EndsWith("{ABC33D23-135D-4C00-B1BF-A9FA4C7916D4}")) { return "Microsoft Edge"; }
            else if (!string.IsNullOrWhiteSpace(DisplayName)) { return DisplayName; }
            else if (!string.IsNullOrWhiteSpace(ProcessName)) { return ProcessName; }
            else
            {
                string name = SessionIdentifier;
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
        }
        #endregion

        #region Customized Datas
        /// <summary>
        /// This value would be added arithmetically when setting the volume for the session.
        /// </summary>
        public float Relative = 0;
        /// <summary>
        /// The session is included to Auto controller when this flag is True. Default is True.
        /// </summary>
        public bool AutoIncluded = true;
        /// <summary>
        /// Average Calculation is enabled when this flag is True. Default is True.
        /// </summary>
        public bool Averaging = true;
        #endregion


        #region Averaging
        /// <summary>
        /// Average peak value within AvTime.
        /// </summary>
        public double AveragePeak { get; private set; }
        /// <summary>
        /// Total stacking time for averaging.
        /// </summary>
        public double AvTime { get; private set; }
        private int AvCount = 0;
        private List<double> Peaks = new List<double>();

        /// <summary>
        /// Set stacking time for average calculation.
        /// </summary>
        /// <param name="averagingTime">Total stacking time.[ms](=AvTime)</param>
        /// <param name="unitTime">Passing time when calculate the average once.[ms]</param>
        public void SetAvTime(double averagingTime, double unitTime) { AvCount = (int)(averagingTime / unitTime); AvTime = unitTime * (double)AvCount; }
        /// <summary>
        /// Clear all stacked peak values and set average to 0.
        /// </summary>
        public void ResetAverage() { Peaks.Clear(); AveragePeak = 0; }
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
}//End namespace Wale.CoreAudio
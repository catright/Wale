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
        /// <summary>
        /// Automatically generate human readable Name property
        /// </summary>
        /// <param name="pid">ProcessId</param>
        /// <param name="ident">ProcessIdentifier</param>
        public SessionData(int pid, string ident)
        {
            this.pid = (int)pid;
            this.Identifier = ident;
            this.Name = MakeName(ident);
        }

        #region API Default Datas
        /// <summary>
        /// Conveted from CoreAudioApi
        /// </summary>
        public SessionState State { get; set; }

        /// <summary>
        /// Human readable process name
        /// </summary>
        public string Name { get; private set; }
        private int pid;
        /// <summary>
        /// Process Id
        /// </summary>
        public uint PID { get => (uint)pid; set => pid = (int)value; }
        public string Identifier { get; private set; }
        public float Volume { get; set; }
        public float Peak { get; set; }
        
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.CoreAudio
{
    public class SessionData : IDisposable
    {
        private object locker = new object();
        //private AudioSessionControl2 asc;
        //private SimpleAudioVolume volumeObject;
        //private AudioMeterInformation peakObject;

        public IntPtr BasePtr { get; private set; }

        public float Relative = 0;
        public double AveragePeak { get; private set; }
        public bool AutoIncluded = true, Averaging = true;

        public SessionState State { get; set; }

        public string Name { get; private set; }
        private int pid;
        public uint PID { get => (uint)pid; set => pid = (int)value; }
        public string Identifier { get; private set; }
        public float Volume { get; set; }
        public float Peak { get; set; }

        public SessionData(int pid, string ident)
        {
            this.pid = (int)pid;
            this.Identifier = ident;
            this.Name = MakeName(ident);
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

        private List<double> Peaks = new List<double>();
        private int AvCount = 0;
        private double AvTime;

        public void SetAvTime(double critTime, double unitTime) { AvCount = (int)(critTime / unitTime); AvTime = unitTime * (double)AvCount; }
        public void ResetAverage() { Peaks.Clear(); AveragePeak = 0; }
        public void SetAverage(double peak)
        {
            if (Peaks.Count > AvCount) Peaks.RemoveAt(0);
            Peaks.Add(peak);
            AveragePeak = Peaks.Average();
            //Console.WriteLine($"Av={AveragePeak}, PC={Peaks.Count}, AvT={AvTime}");
        }
        
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
                //if (asc != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(asc);
                //if (volumeObject != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(volumeObject);
                //if (peakObject != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(peakObject);
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        //~Session2() {
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
    }//End class Session2
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.Subclasses
{
    public class SessionData
    {
        //public delegate void StatusUpdateHandler(object sender, EventArgs e);
        //public event StatusUpdateHandler OnVolumeUpdate;
        //public event StatusUpdateHandler OnPeakUpdate;
        private List<double> Peaks = new List<double>();
        private int AvCount = 0;
        private double AvTime;

        public int Index { get; private set; }
        public uint ID { get; private set; }
        public string Name { get; private set; }
        public double Peak { get; private set; }
        public double Volume { get; private set; }
        public double AveragePeak { get; private set; }
        public double Relative = 0;
        public bool AutoIncluded = true, Expired = false, Active = true, Averaging = true;

        public SessionData(int idx, uint id, string name) { SetFirst(idx, id, name); }
        public void SetFirst(int idx, uint id, string name)
        {
            Index = idx;
            ID = id;
            int startidx = name.IndexOf("|"), endidx = name.IndexOf("%b");
            name = name.Substring(startidx, endidx - startidx + 2);
            if (name == "|#%b") name = "System";
            else
            {
                startidx = name.LastIndexOf("\\") + 1; endidx = name.IndexOf("%b");
                name = name.Substring(startidx, endidx - startidx);
                if (name.EndsWith(".exe")) name = name.Substring(0, name.LastIndexOf(".exe"));
            }
            Name = name;
        }

        public void SetIndex(int idx) { Index = idx; }
        public void SetVol(double vol) { Volume = vol; }//OnVolumeUpdate?.Invoke(this, new EventArgs()); }
        public void SetPeak(double peak) { Peak = peak; }//OnPeakUpdate?.Invoke(this, new EventArgs()); }
        public void SetAvTime(double critTime, double unitTime) { AvCount = (int)(critTime / unitTime); AvTime = unitTime * (double)AvCount; }
        public void ResetAverage() { Peaks.Clear(); AveragePeak = 0; }
        public void SetAverage(double peak)
        {
            if (Peaks.Count > AvCount) Peaks.RemoveAt(0);
            Peaks.Add(peak);
            AveragePeak = Peaks.Average();
        }
        //Class Ends
    }//Class session data
}

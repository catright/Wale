using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore.CoreAudioAPI;

namespace Wale.CoreAudio
{
    public class Session2
    {
        public AudioSessionControl2 ASC;
        public SimpleAudioVolume VolumeObject;
        public AudioMeterInformation PeakObject;

        public float Relative = 0;
        public double AveragePeak { get; private set; }
        public bool AutoIncluded = true, Averaging = true;

        public SessionState State { get => GetState(); }

        public string Name;
        public int PID { get => ASC.ProcessID; }
        public float Volume { get => VolumeObject.MasterVolume; set => VolumeObject.MasterVolume = value; }
        public float Peak { get => PeakObject.PeakValue; }

        public Session2(AudioSessionControl2 asc)
        {
            ASC = asc;
            Name = MakeName(asc.SessionIdentifier);
            
            VolumeObject = new SimpleAudioVolume(asc.BasePtr);
            PeakObject = new AudioMeterInformation(asc.BasePtr);
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

        private SessionState GetState()
        {
            if(ASC == null) { throw new NullReferenceException("AudioSessionControl2 is not exists."); }
            switch(ASC.SessionState)
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
    }//End class Session2
}

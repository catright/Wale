using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using NAudio.CoreAudioApi;

namespace Wale.CoreAudio
{
    public class Session3 : IDisposable
    {/*
        private AudioSessionControl ASC;
        public float Relative = 0;
        public double AveragePeak { get; private set; }
        public bool AutoIncluded = true, Averaging = true;

        public SessionState State { get => GetState(); }

        public uint PID { get; private set; }
        public string Identifier { get; private set; }
        public string Name { get; private set; }
        public float Volume { get => GetVolume(); set => SetVolume(value); }
        public float Peak { get => GetPeak(); }

        public Session3(AudioSessionControl asc)
        {
            this.ASC = asc;
            this.PID = asc.GetProcessID;
            this.Identifier = asc.GetSessionIdentifier;
            this.Name = MakeName(asc.GetSessionIdentifier);
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
            switch (ASC.State)
            {
                case NAudio.CoreAudioApi.Interfaces.AudioSessionState.AudioSessionStateActive:
                    return SessionState.Active;
                case NAudio.CoreAudioApi.Interfaces.AudioSessionState.AudioSessionStateInactive:
                    return SessionState.Inactive;
                case NAudio.CoreAudioApi.Interfaces.AudioSessionState.AudioSessionStateExpired:
                    return SessionState.Expired;
                default:
                    return SessionState.Expired;
            }
        }
        private float GetVolume()
        {
            //using (var volumeObject = new SimpleAudioVolume(BasePtr))
            //{
            //return volumeObject.MasterVolume;
            //}
            return ASC.SimpleAudioVolume.Volume;
        }
        private void SetVolume(float value)
        {
            //using (var volumeObject = new SimpleAudioVolume(BasePtr))
            //{
            //volumeObject.MasterVolume = value;
            //}
            ASC.SimpleAudioVolume.Volume = value;
        }
        private float GetPeak()
        {
            //using (var peakObject = new AudioMeterInformation(BasePtr))
            //{
            //return peakObject.PeakValue;
            //}
            return ASC.AudioMeterInformation.MasterPeakValue;
        }
        */
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
                //if (ASC != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(ASC);
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        //~Session3() {
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
    }
}

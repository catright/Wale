using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreTest
{
    public class AudioData : JPack.NotifyPropertyChanged
    {
        public AudioData() { Update(false, 0, 0); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="b">Muted</param>
        /// <param name="v">Volume</param>
        public AudioData(bool b, double v) { Update(b, v, 0); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="b">Muted</param>
        /// <param name="v">Volume</param>
        /// <param name="p">Peak</param>
        public AudioData(bool b, double v, double p) { Update(b, v, p); }

        public bool Muted { get => Get<bool>(); private set => Set(value); }
        public double Volume { get => Get<double>(); private set => Set(value); }
        public double VolumedB { get => DB(Get<double>()); }
        public double Peak { get => Get<double>(); private set => Set(value); }
        public double PeakdB { get => DB(Get<double>()); }

        public void SetMute(bool b) => Muted = b;
        public void SetVolume(double v) { Volume = v; }
        public void SetPeak(double p) { Peak = p; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b">Muted</param>
        /// <param name="v">Volume</param>
        public void Update(bool b, double v) { SetMute(b); SetVolume(v); }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p">Peak</param>
        public void Update(double p) => SetPeak(p);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="b">Muted</param>
        /// <param name="v">Volume</param>
        /// <param name="p">Peak</param>
        public void Update(bool b, double v, double p) { SetMute(b); SetVolume(v); SetPeak(p); }

        private double DB(double val) => 20 * Math.Log10(val / 1);
    }
}

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
        public AudioData(bool b, double v) { Update(b, v, 0); }
        public AudioData(bool b, double v, double p) { Update(b, v, p); }

        public bool Muted { get => Get<bool>(); set => Set(value); }
        public double Volume { get => Get<double>(); set => Set(value); }
        public double VolumedB { get => Get<double>(); set => Set(value); }
        public double Peak { get => Get<double>(); set => Set(value); }
        public double PeakdB { get => Get<double>(); set => Set(value); }

        public void Mute(bool b) => Muted = b;
        public void NewVolume(double v) { Volume = v; VolumedB = DB(v); }
        public void NewPeak(double p) { Peak = p; PeakdB = DB(p); }
        public void Update(bool b, double v) { Mute(b); NewVolume(v); }
        public void Update(double p) => NewPeak(p);
        public void Update(bool b, double v, double p) { Mute(b); NewVolume(v); NewPeak(p); }

        private double DB(double val) => 20 * Math.Log10(val / 1);
    }
}

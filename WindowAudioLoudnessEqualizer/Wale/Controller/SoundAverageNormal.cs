using System;
using System.Collections.Generic;
using System.Linq;
using Wale.Configs;

namespace Wale.Controller
{
    internal class SoundAverageNormal : JPack.NotifyPropertyChanged, ISoundAverage
    {
        private readonly General gl;
        public SoundAverageNormal(General gl)
        {
            this.gl = gl;
            gl.PropertyChanged += Gl_PropertyChanged;
            SetAvTime();
        }
        private void Gl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AverageTime" || e.PropertyName == "AutoControlInterval") SetAvTime();
        }

        public double AveragePeak { get => Get<double>(); private set => Set(value); }
        private uint AvCount = 0, ChunkSize = 5;
        private readonly List<double> Peaks = new List<double>();
        private readonly List<double> PeaksBuffer = new List<double>();

        /// <summary>
        /// Set stacking time for average calculation.
        /// </summary>
        /// <param name="chunkTime">[ms]</param>
        /// <param name="averageTime">[ms]Total stacking time. Use <see cref="Configs.General.AverageTime"/> when <c>null</c></param>
        /// <param name="unitTime">[ms]Time span passing by in an iteration of average calculation. Use <see cref="Configs.General.AutoControlInterval"/> when <c>null</c></param>
        /// <exception cref="ArgumentException"></exception>
        protected void SetAvTime(double chunkTime = 100, double averageTime = -1, double unitTime = -1)
        {
            if (averageTime < 0) averageTime = gl.AverageTime;
            if (unitTime < 0) unitTime = gl.AutoControlInterval;
            if (averageTime < 0) { throw new ArgumentException("Invalid averageTime"); }
            if (unitTime < 0) { throw new ArgumentException("Invalid unitTime"); }
            if (chunkTime < 0) { throw new ArgumentException("Invalid chunkTime"); }
            //if (chunkTime < unitTime) { throw new Exception("chunkTime is smaller than unitTime"); }

            ChunkSize = Chunk(chunkTime, (double)unitTime);
            AvCount = Count((double)averageTime, (double)unitTime, ChunkSize);
            ResetAverage();
        }
        /// <summary>
        /// Clear all stacked peak values and set average to 0.
        /// </summary>
        public void ResetAverage()
        {
            AveragePeak = 0;
            Peaks.Clear();
            PeaksBuffer.Clear();
        }
        public void SetAverage(double peak) => Average_Chunk(peak);

        private uint Chunk(double chunkTime, double unitTime) => Math.Max(1u, Convert.ToUInt32(chunkTime / unitTime));
        private uint Count(double countTime, double unitTime, uint chunkSize) => Convert.ToUInt32(countTime / unitTime / Convert.ToDouble(chunkSize));

        private void Average_Chunk(double peak)
        {
            PeaksBuffer.Add(peak);
            if (PeaksBuffer.Count >= ChunkSize)
            {
                Peaks.Add(PeaksBuffer.Average());
                PeaksBuffer.Clear();
                if (Peaks.Count > AvCount) Peaks.RemoveAt(0);
                AveragePeak = Peaks.Average();
            }
            //Console.WriteLine($"Av={AveragePeak}, PC={Peaks.Count}, AvT={gl.AverageTime}");
        }
    }
}

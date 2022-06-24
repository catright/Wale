using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.Controller
{
    internal class SoundAverageAttackRelease : JPack.NotifyPropertyChanged, ISoundAverage
    {
        private readonly Configs.General gl;
        public SoundAverageAttackRelease(Configs.General gl)
        {
            this.gl = gl;
            gl.PropertyChanged += Gl_PropertyChanged;
            SetAvTime();
        }
        private void Gl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AverageTime" || e.PropertyName == "AutoControlInterval") SetAvTime();
        }

        /// <summary>
        /// Average peak value within AvTime.
        /// </summary>
        public double AveragePeak { get => Get<double>(); private set => Set(value); }
        private uint AvCount = 0, ChunkSize = 5;
        private readonly List<double> Peaks = new List<double>();
        private readonly List<double> PeaksBuffer = new List<double>();
        public double AveragePeakAttack { get; private set; }
        public double AveragePeakRelease { get; private set; }
        private uint AttackCount = 0, AttackChunk = 5, ReleaseCount = 0, ReleaseChunk = 5;
        private readonly List<double> PeaksAttack = new List<double>(), PeaksAttackBuffer = new List<double>();
        private readonly List<double> PeaksRelease = new List<double>(), PeaksReleaseBuffer = new List<double>();

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
        /// Set stacking time for average calculation.
        /// </summary>
        /// <param name="attackTime">Total stacking time for attack.[ms]</param>
        /// <param name="releaseTime">Total stacking time for release.[ms]</param>
        /// <param name="unitTime">Passing time when calculate the average once.[ms]</param>
        protected void SetAvTimeAR(double attackTime, double atChunkTime, double releaseTime, double relChunkTime, double unitTime)
        {
            if (attackTime < 0) { throw new Exception("Invalid attackTime"); }
            if (releaseTime < 0) { throw new Exception("Invalid releaseTime"); }
            if (unitTime < 0) { throw new Exception("Invalid unitTime"); }

            AttackChunk = Chunk(atChunkTime, unitTime);
            ReleaseChunk = Chunk(relChunkTime, unitTime);

            AttackCount = Count(attackTime, unitTime, AttackChunk);
            ReleaseCount = Count(releaseTime, unitTime, ReleaseChunk);

            ResetAverage();
        }
        private uint Chunk(double chunkTime, double unitTime)
        {
            int chunk = Convert.ToInt32(chunkTime / unitTime);
            return (chunk > 1 ? (uint)chunk : 1);
        }
        private uint Count(double countTime, double unitTime, uint chunkSize)
        {
            return Convert.ToUInt32((countTime / unitTime) / Convert.ToDouble(chunkSize));
        }

        /// <summary>
        /// Clear all stacked peak values and set average to 0.
        /// </summary>
        public void ResetAverage()
        {
            AveragePeak = 0; Peaks.Clear(); PeaksBuffer.Clear();
            // JPack.FileLog.Log("Average Reset"); }//Console.WriteLine("Average Reset");
            AveragePeakAttack = 0; PeaksAttack.Clear(); PeaksAttackBuffer.Clear();
            AveragePeakRelease = 0; PeaksRelease.Clear(); PeaksReleaseBuffer.Clear();
        }
        /// <summary>
        /// Add new peak value to peaks container and re-calculate AveragePeak value.
        /// </summary>
        /// <param name="peak"></param>
        //public void SetAverage(double peak) => Average_AR(peak);
        //public void SetAverage(double peak, double sigma = 4) => Average_Sigma(peak, sigma);
        public void SetAverage(double peak) => Average_Chunk(peak);

        private void Average_AR(double peak)
        {
            PeaksAttackBuffer.Add(peak);
            if (PeaksAttackBuffer.Count > AttackChunk)
            {
                PeaksAttack.AddRange(PeaksAttackBuffer);
                PeaksAttackBuffer.Clear();
                AveragePeakAttack = PeaksAttack.Average();
            }
            PeaksReleaseBuffer.Add(peak);
            if (PeaksReleaseBuffer.Count > ReleaseChunk)
            {
                PeaksRelease.AddRange(PeaksReleaseBuffer);
                PeaksReleaseBuffer.Clear();
                AveragePeakRelease = PeaksRelease.Average();
            }
        }
        private double low, high;
        private void Average_Sigma(double peak, double sigma)
        {
            if (sigma < 1) sigma = 1;
            //Peaks.Add(peak);
            //if (Peaks.Count > AvCount) Peaks.RemoveAt(0);
            if (AveragePeak == 0) PeaksBuffer.Add(peak);
            else
            {
                if (peak >= low && peak <= high) PeaksBuffer.Add(peak);
            }
            if (PeaksBuffer.Count > ChunkSize)
            {
                Peaks.AddRange(PeaksBuffer);
                PeaksBuffer.Clear();
                AveragePeak = Peaks.Average();
                low = AveragePeak / sigma;
                high = AveragePeak * sigma;
            }
        }
        public void Average_Chunk(double peak)
        {
            if (PeaksBuffer.Count >= ChunkSize)
            {
                if (Peaks.Count() > AvCount) Peaks.RemoveAt(0);
                Peaks.Add(PeaksBuffer.Average());
                PeaksBuffer.Clear();
                AveragePeak = Peaks.Average();
            }
            PeaksBuffer.Add(peak);
            //Console.WriteLine($"Av={AveragePeak}, PC={Peaks.Count}, AvT={AvTime}");
        }
        //public void SetAverage2()
        //{
        //    if (PeaksBuffer.Count >= ChunkSize)
        //    {
        //        if (Peaks.Count() > AvCount) Peaks.RemoveAt(0);
        //        Peaks.Add(PeaksBuffer.Average());
        //        PeaksBuffer.Clear();
        //        AveragePeak = Peaks.Average();
        //    }
        //    else PeaksBuffer.Add(Peak);
        //    //Console.WriteLine($"Av={AveragePeak}, PC={Peaks.Count}, AvT={AvTime}");
        //}
    }
}

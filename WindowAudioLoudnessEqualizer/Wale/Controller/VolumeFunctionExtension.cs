using System;
using Wale.Extensions;

namespace Wale.Controller
{
    /// <summary>
    /// Volume function type
    /// </summary>
    public enum VType { Simple, Compress, CompressDB, CompAR }

    internal class CompFactor
    {
        public CompFactor(double attack, double release, double before)
        {
            Attack = attack;
            Release = release;
            Before = before;
        }

        public double Attack { get; set; }
        public double Release { get; set; }
        public double Before { get; set; }
    }

    internal static class VolumeFunctionExtension
    {
        internal static Configs.General gl = null;

        /// <summary>
        /// Calc next volume.
        /// Lower volume at once if current <paramref name="peak"/> with next volume exceeds the limit.
        /// Or lower volume at once if <paramref name="average"/> with next volume exceeds the limit.
        /// Then set volume.
        /// <para><see cref="gl"/> must be set before use.</para>
        /// </summary>
        /// <param name="f"></param>
        /// <param name="peak"></param>
        /// <param name="average"></param>
        /// <returns></returns>
        public static double Next(this VType f, double peak, double average = 0, CompFactor cf = null)
            => gl != null && gl.Averaging ? f.Calc(average, cf).Limiter(peak).Limiter(average) : f.Calc(peak, cf).Limiter(peak);
        public static double Calc(this VType f, double peak, CompFactor cf = null)
        {
            if (gl == null) throw new ArgumentNullException(nameof(gl));
            switch (f)
            {
                case VType.Simple: return peak.Simple();
                case VType.Compress: return peak.Simple().Compress();
                case VType.CompressDB: return peak.CompressDB();
                case VType.CompAR: return peak.CompAR(cf);
                default: return peak.Simple();
            }
        }

        private static double Simple(this double peak) => gl.TargetLevel / peak;
        private static double Compress(this double simple) => simple + (1 - simple) / gl.CompRate;
        private static double CompressDB(this double peak)
        {
            double p = peak.TodB();
            return (gl.CompRatioDB == 0 ? 0 : gl.TargetLevelDB + (p - gl.TargetLevelDB) / gl.CompRatioDB - p).ToLinear();
        }
        private static double CompAR(this double peak, CompFactor cf)
        {
            if (cf == null) throw new ArgumentNullException(nameof(cf));
            double tVol = gl.TargetLevel / cf.Attack;
            if (tVol > cf.Before) tVol = gl.TargetLevel / cf.Release;
            if (tVol * peak > gl.LimitLevel) tVol = gl.LimitLevel / peak;
            return tVol;
        }

        private static double Limiter(this double nextVol, double peak) => peak * nextVol > gl.LimitLevel ? gl.LimitLevel / peak : nextVol;

    }
}

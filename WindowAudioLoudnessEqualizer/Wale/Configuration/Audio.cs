using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.Configuration
{
    public static class Audio
    {
        #region Readonly Values
        public static double RelativeBase => 3;
        public static float RelativeEnd => 2;
        public static float RelativeEndInv => -2;

        #endregion

        private static double _AutoControlInterval;
        public static double AutoControlInterval { get => _AutoControlInterval; set { _AutoControlInterval = value; ACInterval = (int)(value * 1000); } }
        public static int ACInterval { get; private set; }

        public static double MinPeak { get; set; }
        public static double TargetLevel { get; set; }
        public static double UpRate { get => upRate; set { upRate = (value * AutoControlInterval / 1000); } }
        private static double upRate = 0.02;
        public static double Kurtosis { get; set; } = 0.5;
        public static VFunction.Func VFunc { get; set; }
        public static VFunction.FactorsForSlicedLinear SliceFactors { get; set; }

        public static bool Averaging { get; set; }
        public static double AverageTime { get; set; }

        public static void ResetToDefault()
        {
            upRate = AudioSettingsDefault.upRate;
            Kurtosis = AudioSettingsDefault.kurtosis;
        }
    }
    internal static class AudioSettingsDefault
    {
        internal static double upRate = 0.02;
        internal static double kurtosis = 0.5;
    }
}

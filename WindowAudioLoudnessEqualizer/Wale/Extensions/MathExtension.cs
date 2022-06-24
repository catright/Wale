using System;

namespace Wale.Extensions
{
    public static class MathExtension
    {
        public static double Clip(this double value, double high = 1, double low = 0)
            => Math.Max(Math.Min(value, high), low);
        public static float Clip(this float value, float high = 1, float low = 0)
            => Math.Max(Math.Min(value, high), low);

        public static (double, string) UnitPrefix(this double value, bool binary = false)
        {
            double standard = binary ? 1024 : 1000;
            int count = 0;
            while (value > standard) { value /= (double)standard; count++; }
            string prefix;
            switch (count)
            {
                case 8: prefix = "Y"; break;
                case 7: prefix = "Z"; break;
                case 6: prefix = "E"; break;
                case 5: prefix = "P"; break;
                case 4: prefix = "T"; break;
                case 3: prefix = "G"; break;
                case 2: prefix = "M"; break;
                case 1: prefix = "k"; break;
                case 0:
                default:
                    prefix = string.Empty;
                    break;
            }
            if (prefix != string.Empty) prefix = binary ? prefix + "i" : prefix;
            return (value, prefix);
        }
        public static string Digit(this double value, int digit = 0)
        {
            switch (digit)
            {
                case var d when d >= 5: return $"{value:n5}";
                case 4: return $"{value:n4}";
                case 3: return $"{value:n3}";
                case 2: return $"{value:n2}";
                case 1: return $"{value:n1}";
                default: return $"{value:n0}";
            }
        }
        public static string AsBinary(this double value, int digit = 0)
        {
            (double v, string p) = value.UnitPrefix(true);
            return $"{v.Digit(digit)}{p}B";
        }
        public static string AsBinary(this long value, int digit = 0)
            => AsBinary((double)value, digit);
        public static string AsTime(this double value, int digit = 0)
        {
            if (value < 1000) return $"{value}ms";
            double sec = value / 1000;
            if (sec < 60) return $"{sec.Digit(digit)}s";
            double min = Math.Floor(sec / 60);
            sec -= (min * 60);
            if (min < 60) return $"{min}m:{sec.Digit(digit)}s";
            double hour = Math.Floor(min / 60);
            min -= (hour * 60);
            return $"{hour}h:{min}m:{sec}s";
        }
        public static string AsTime(this long value, int digit = 0)
            => AsTime((double)value, digit);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale
{
    public static class VFunction
    {
        public enum Func { None, Linear, SlicedLinear, Reciprocal, FixedReciprocal }

        public struct FactorsForSlicedLinear
        {
            public double A, B;
            public FactorsForSlicedLinear(double A, double B)
            {
                this.A = A;
                this.B = B;
            }
        }
        public static FactorsForSlicedLinear GetFactorsForSlicedLinear(double upRate, double baseLevel)
        {
            double acA = upRate - upRate / baseLevel;
            double acB = upRate * baseLevel / (baseLevel - 1);
            return new FactorsForSlicedLinear(acA, acB);
        }

        public static double Linear(double input, double upRate)
        {
            return (upRate - upRate * input);
        }/**/
        public static double SlicedLinear(double input, double upRate, double baseLevel, double a, double b)
        {
            return (input < baseLevel) ? a * input + upRate : b * (input - 1);
        }/**/
        public static double Reciprocal(double input, double upRate, double kurtosis)
        {
            if (kurtosis == 1 && input == 0) return 99;
            return upRate * (1 / ((kurtosis) + (1 - kurtosis) * input) - 1);
        }/**/
        public static double FixedReciprocal(double input, double upRate, double kurtosis)
        {
            if (kurtosis == 0) return input;
            else if (kurtosis == 1) return input + upRate;
            return upRate * (Math.Pow((kurtosis + (1 - kurtosis) * input), (1 / (Math.Log(kurtosis, 2)))) - 1);
        }/**/
        //Class ends
    }//class VolumeFunction
}

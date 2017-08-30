using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.Subclasses
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
        public static FactorsForSlicedLinear GetFactorsForSlicedLinear(double upRate, double baseVol)
        {
            double acA = upRate - upRate / baseVol;
            double acB = upRate * baseVol / (baseVol - 1);
            return new FactorsForSlicedLinear(acA, acB);
        }

        public static double Linear(double input, double upRate)
        {
            return (upRate - upRate * input);
        }/**/
        public static double SlicedLinear(double input, double upRate, double baseVol, double a, double b)
        {
            return (input < baseVol) ? a * input + upRate : b * (input - 1);
        }/**/
        public static double Reciprocal(double input, double upRate, double skewness)
        {
            if (skewness == 1 && input == 0) return 99;
            return upRate * (1 / ((skewness) + (1 - skewness) * input) - 1);
        }/**/
        public static double FixedReciprocal(double input, double upRate, double skewness)
        {
            if (skewness == 0) return input;
            else if (skewness == 1) return input + upRate;
            return upRate * (Math.Pow((skewness + (1 - skewness) * input), (1 / (Math.Log(skewness, 2)))) - 1);
        }/**/
        //Class ends
    }//class VolumeFunction
}

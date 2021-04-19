using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale
{
    public static class VFunction
    {
        /// <summary>
        /// Decrement function
        /// </summary>
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

        /// <summary>
        /// Get output level for UI. 0=Linear, 1=dB.
        /// Output would be rounded to 3digit when 0, 1digit when 1.
        /// Output would be -∞ when it's below than -99.9 and unit is 1.
        /// </summary>
        /// <param name="input">audio level, such as volume or peak</param>
        /// <param name="unit">0=Linear(Windows default), 1=dB, else=Linear</param>
        /// <returns></returns>
        public static double Level(double input, int unit = 0)
        {
            double output = 0;
            switch (unit)
            {
                case 1:
                    output = Math.Round(20 * Math.Log10(input), 1);
                    if (output < -99.9) { output = double.NegativeInfinity; }
                    else if (output > 99.9) { output = double.PositiveInfinity; }
                    break;
                case 0:
                default:
                    output = Math.Round(input, 3);
                    break;
            }
            return output;
        }
        /// <summary>
        /// Get output level difference (subtraction) for UI. 0=Linear, 1=dB.
        /// Output would be rounded to 3digit when 0, 1digit when 1.
        /// Output would be -∞ when it's below than -99.9 and unit is 1.
        /// </summary>
        /// <param name="input1">audio level, such as volume or peak</param>
        /// <param name="input2">audio level, such as volume or peak</param>
        /// <param name="unit">0=Linear(Windows default), 1=dB, else=Linear</param>
        /// <returns></returns>
        public static double LevelDiff(double input1, double input2, int unit = 0)
        {
            double output = 0;
            switch (unit)
            {
                case 1:
                    output = Math.Round(20 * (Math.Log10(input1) - Math.Log10(input2)), 1);
                    if (output < -99.9) { output = double.NegativeInfinity; }
                    else if (output > 99.9) { output = double.PositiveInfinity; }
                    break;
                case 0:
                default:
                    output = Math.Round(input1 - input2, 3);
                    break;
            }
            return output;
        }
        /// <summary>
        /// Get output level difference (multiplication) for UI. 0=Linear, 1=dB.
        /// Output would be rounded to 3digit when 0, 1digit when 1.
        /// Output would be -∞ when it's below than -99.9 and unit is 1.
        /// </summary>
        /// <param name="input">audio level, such as volume or peak</param>
        /// <param name="unit">0=Linear(Windows default), 1=dB, else=Linear</param>
        /// <returns></returns>
        public static double LevelMult(double input, int unit = 0)
        {
            double output;
            switch (unit)
            {
                case 1:
                    output = Math.Round(20 * Math.Log10(1 + input), 1);
                    if (output < -99.9) { output = double.NegativeInfinity; }
                    else if (output > 99.9) { output = double.PositiveInfinity; }
                    break;
                case 0:
                default:
                    output = Math.Round(1 + input, 3);
                    break;
            }
            return output;
        }
        /// <summary>
        /// Get output level of Relative factor for UI. 0=Linear, 1=dB.
        /// Output would be rounded to 3digit when 0, 1digit when 1.
        /// Output would be ∞ when absolute value of output is above than 99.9 and unit is 1.
        /// </summary>
        /// <param name="input">audio level, such as volume or peak</param>
        /// <param name="unit">0=Linear(Windows default), 1=dB, else=Linear</param>
        /// <returns></returns>
        public static double RelLv(double input, int unit = 0)
        {
            double output = 0;
            input = Math.Pow(4, input);
            switch (unit)
            {
                case 1:
                    output = Math.Round(20 * Math.Log10(input), 1);
                    if (output < -99.9) { output = double.NegativeInfinity; }
                    else if (output > 99.9) { output = double.PositiveInfinity; }
                    break;
                case 0:
                default:
                    output = Math.Round(input, 3);
                    break;
            }
            return output;
        }
        //Class ends
    }//class VolumeFunction
}

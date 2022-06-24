using System;

namespace Wale.Controller
{
    /// <summary>
    /// Delay function type
    /// </summary>
    public enum DType { None, Linear, SlicedLinear, Reciprocal, FixedReciprocal }

    /// <summary>
    /// Delay function factors
    /// </summary>
    public class DFactors
    {

        public readonly Configs.General gl;
        public DFactors(Configs.General gl)
        {
            this.gl = gl;
            this.gl.PropertyChanged += Gl_PropertyChanged;
            Init();
        }

        protected virtual void Gl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "UpRate":
                    GetUpRate();
                    GetFactorsForSlicedLinear();
                    break;
                case "TargetLevel":
                    GetFactorsForSlicedLinear();
                    break;
                case "AutoControlInterval":
                    GetURratio();
                    GetUpRate();
                    break;
                case "Kurtosis":
                    GetKurtosis();
                    break;
            }
        }
        protected virtual void Init()
        {
            GetURratio();
            GetUpRate();
            GetKurtosis();
            GetFactorsForSlicedLinear();
        }

        private double URratio;
        protected double GetURratio() => URratio = gl.AutoControlInterval / 1000;

        public double UpRate;
        protected double GetUpRate() => UpRate = gl?.UpRate * URratio ?? URratio;

        public double Kurtosis;
        protected double GetKurtosis() => Kurtosis = gl?.Kurtosis ?? 1.0;

        public (double A, double B) SliceFactors { get; set; }
        protected (double A, double B) GetFactorsForSlicedLinear() => SliceFactors = (UpRate - UpRate / gl.TargetLevel, UpRate * gl.TargetLevel / (gl.TargetLevel - 1));

    }

    public static class DelayFunctionExtension
    {
        public static double Segment(this DType f, double nextVol, DFactors df) => f.Calc(nextVol, df) + nextVol;
        public static double Calc(this DType f, double nextVol, DFactors df)
        {
            switch (f)
            {
                case DType.Linear: return Linear(nextVol, df);
                case DType.SlicedLinear: return SlicedLinear(nextVol, df);
                case DType.Reciprocal: return Reciprocal(nextVol, df);
                case DType.FixedReciprocal: return FixedReciprocal(nextVol, df);
                default: return df.UpRate;
            }
        }

        private static double Linear(double nextVol, DFactors df) => df.UpRate - df.UpRate * nextVol;
        private static double SlicedLinear(double nextVol, DFactors df) => (nextVol < df.gl.TargetLevel) ? df.SliceFactors.A * nextVol + df.UpRate : df.SliceFactors.B * (nextVol - 1);
        private static double Reciprocal(double nextVol, DFactors df)
        {
            if (df.Kurtosis == 1 && nextVol == 0) return 99;
            return df.UpRate * ((1 / (df.Kurtosis + ((1 - df.Kurtosis) * nextVol))) - 1);
        }
        private static double FixedReciprocal(double nextVol, DFactors df)
        {
            if (df.Kurtosis == 0) return nextVol;
            else if (df.Kurtosis == 1) return nextVol + df.UpRate;
            return df.UpRate * (Math.Pow(df.Kurtosis + ((1 - df.Kurtosis) * nextVol), 1 / Math.Log(df.Kurtosis, 2)) - 1);
        }

    }
}

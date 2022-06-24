using System;
using System.Globalization;
using System.Windows.Data;
using Wale.Extensions;

namespace Wale.WPF.ValueConverters
{
    internal class LevelConverter : IMultiValueConverter, IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //return (double)value * (double)parameter;
            try { return System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter); }
            catch { return 0; }
        }
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 && values.Length != 3) return -99;
            int unit, para = int.MaxValue;
            double value, mult = 1;
            try
            {
                unit = System.Convert.ToInt32(values[0]);
                value = System.Convert.ToDouble(values[1]);
                if (values.Length == 3) mult = System.Convert.ToDouble(values[2]);
                if (parameter != null)
                {
                    para = System.Convert.ToInt16(parameter);
                    if (para == 0 || para == 1) unit = para;
                }
            }
            catch { return -99; }
            double output = (value * mult).Level(unit);
            if (para == -1) output = (output == double.NegativeInfinity ? -99 : output);
            return output;
            //return VFunction.Level((double)values[1] * (values.Length == 3 ? (double)values[2] : 1), parameter != null ? (int)parameter : (int)values[0]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

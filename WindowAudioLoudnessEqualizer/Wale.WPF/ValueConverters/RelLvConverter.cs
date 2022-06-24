using System;
using System.Globalization;
using System.Windows.Data;
using Wale.Extensions;

namespace Wale.WPF.ValueConverters
{
    internal class RelLvConverter : IMultiValueConverter
    {
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
            double output = (value * mult).RelLv(unit);
            if (para == -1) output = (output == double.NegativeInfinity ? -99 : output);
            return output;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using System.Windows.Data;

namespace Wale.WPF.ValueConverters
{
    public class ACwaitedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return false;
            double value, criteria;
            try
            {
                value = System.Convert.ToDouble(values[0]);
                if (values[1] is Configs.General gl) criteria = gl.AutoControlInterval;
                else criteria = Properties.Settings.Default.AutoControlInterval;
            }
            catch { return false; }
            return value > criteria * 1.1 || value < 0;
            //return (double)values[0] > (values[1] is Configs.General gl ? gl.AutoControlInterval : Properties.Settings.Default.AutoControlInterval) * 1.1 || (double)values[0] < 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

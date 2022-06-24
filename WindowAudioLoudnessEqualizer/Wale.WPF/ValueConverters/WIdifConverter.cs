using System;
using System.Globalization;
using System.Windows.Data;

namespace Wale.WPF.ValueConverters
{
    public class WIdifConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return (double)value > System.Convert.ToDouble(parameter) || (double)value < -System.Convert.ToDouble(parameter);
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

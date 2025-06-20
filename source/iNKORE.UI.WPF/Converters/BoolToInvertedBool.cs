using System;
using System.Globalization;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BoolInversionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }
    }
}

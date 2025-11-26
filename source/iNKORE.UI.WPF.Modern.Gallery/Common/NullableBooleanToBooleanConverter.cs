using System;
using System.Globalization;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Modern.Gallery.Common
{
    public class NullableBooleanToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return (bool?)b;
            return false;
        }
    }
}
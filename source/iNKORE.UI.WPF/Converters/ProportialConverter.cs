using System;
using System.Globalization;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Converters
{
    class ProportialConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is double && values[1] is double && values[2] is double)
                return (double)values[0] * ((double)values[1] / (double)values[2]);
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

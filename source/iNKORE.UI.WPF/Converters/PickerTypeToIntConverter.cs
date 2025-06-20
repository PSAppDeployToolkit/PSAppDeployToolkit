using iNKORE.UI.WPF.ColorPicker.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Converters
{
    [ValueConversion(typeof(PickerType), typeof(int))]
    class PickerTypeToIntConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (PickerType)value;
        }
    }
}

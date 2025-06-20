using iNKORE.UI.WPF.ColorPicker.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Converters
{
    [ValueConversion(typeof(double), typeof(string))]
    internal class RangeConstrainedDoubleToDoubleConverter : DependencyObject, IValueConverter
    {
        public static DependencyProperty MinProperty =
            DependencyProperty.Register(nameof(Min), typeof(double), typeof(RangeConstrainedDoubleToDoubleConverter),
                new PropertyMetadata(0.0));

        public static DependencyProperty MaxProperty =
            DependencyProperty.Register(nameof(Max), typeof(double), typeof(RangeConstrainedDoubleToDoubleConverter),
                new PropertyMetadata(1.0));

        public double Min
        {
            get => (double)GetValue(MinProperty);
            set => SetValue(MinProperty, value);
        }

        public double Max
        {
            get => (double)GetValue(MaxProperty);
            set => SetValue(MaxProperty, value);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!double.TryParse(((string)value).Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return DependencyProperty.UnsetValue;
            return MathHelper.Clamp(result, Min, Max);
        }
    }
}

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Converters
{
    public class ThicknessFilterConverter : DependencyObject, IValueConverter
    {
        public ThicknessFilterKind Filter { get; set; }

        public double Scale { get; set; } = 1.0;

        public static Thickness Convert(Thickness thickness, ThicknessFilterKind filterKind)
        {
            Thickness result = thickness;

            if (!filterKind.HasFlag(ThicknessFilterKind.Top))
            {
                result.Top = 0;
            }
            if (!filterKind.HasFlag(ThicknessFilterKind.Left))
            {
                result.Left = 0;
            }
            if (!filterKind.HasFlag(ThicknessFilterKind.Right))
            {
                result.Right = 0;
            }
            if (!filterKind.HasFlag(ThicknessFilterKind.Bottom))
            {
                result.Bottom = 0;
            }


            return result;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var thickness = (Thickness)value;

            var scale = Scale;
            if (!double.IsNaN(scale))
            {
                thickness.Left *= scale;
                thickness.Right *= scale;
                thickness.Bottom *= scale;
                thickness.Top *= scale;
            }

            var filterType = Filter;

            return Convert(thickness, filterType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    [Flags]
    public enum ThicknessFilterKind
    {
        None,
        Top,
        Right,
        Bottom,
        Left,

        TopAndLeft = Top | Left,
        BottomAndRight = Bottom | Right,
        LeftAndRight = Left | Right,
        TopAndBottom = Top | Bottom,

        ExcludeTop = Left | Right | Bottom,
        ExcludeLeft = Top | Right | Bottom,
        ExcludeRight = Left | Bottom | Top,
        ExcludeBottom = Top | Left | Right,
    }
}

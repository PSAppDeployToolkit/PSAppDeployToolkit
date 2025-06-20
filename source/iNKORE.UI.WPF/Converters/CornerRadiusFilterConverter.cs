using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using iNKORE.UI.WPF.Common;

namespace iNKORE.UI.WPF.Converters
{
    public class CornerRadiusFilterConverter : DependencyObject, IValueConverter
    {
        public CornerRadiusFilterKind Filter { get; set; }

        public double Scale { get; set; } = 1.0;

        public static CornerRadiusEx Convert(CornerRadiusEx radius, CornerRadiusFilterKind filterKind, double scale = 1)
        {
            CornerRadiusEx result = new CornerRadiusEx(0);

            if (filterKind.HasFlag(CornerRadiusFilterKind.TopLeft))
            {
                result.TopLeftX = radius.TopLeftX;
                result.TopLeftY = radius.TopLeftY;
            }
            if (filterKind.HasFlag(CornerRadiusFilterKind.TopRight))
            {
                result.TopRightX = radius.TopRightX;
                result.TopRightY = radius.TopRightY;
            }
            if (filterKind.HasFlag(CornerRadiusFilterKind.BottomRight))
            {
                result.BottomRightX = radius.BottomRightX;
                result.BottomRightY = radius.BottomRightY;
            }
            if (filterKind.HasFlag(CornerRadiusFilterKind.BottomLeft))
            {
                result.BottomLeftX = radius.BottomLeftX;
                result.BottomLeftY = radius.BottomLeftY;
            }

            result.Scale(scale);
            return result;
        }

        public static CornerRadius Convert(CornerRadius radius, CornerRadiusFilterKind filterKind, double scale = 1)
        {
            return Convert(new CornerRadiusEx(radius), filterKind, scale).ToCornerRadius();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CornerRadius cornerRadius)
            {
                return Convert(cornerRadius, Filter, Scale);
            }

            // No way!
            return new CornerRadius(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Nice try!
            return new CornerRadius(0);
        }
    }

    [Flags]
    public enum CornerRadiusFilterKind
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 4,
        BottomLeft = 8,

        Top = TopLeft | TopRight,
        Bottom = BottomLeft | BottomRight,
        Left = TopLeft | BottomLeft,
        Right = TopRight | BottomRight,

        All = TopLeft | TopRight | BottomLeft | BottomRight,
    }
}

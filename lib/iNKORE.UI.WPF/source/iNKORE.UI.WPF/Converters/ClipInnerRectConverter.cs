using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace iNKORE.UI.WPF.Converters
{
    public class ClipInnerRectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(o => o == DependencyProperty.UnsetValue || o == null)) return null;

            var width = (double)values[0];
            var height = (double)values[1];
            var outerMargin = (double)values[2];

            var cornerRadius = new CornerRadius(0);
            if (values.Length >= 4 && values[3] is CornerRadius)
                cornerRadius = (CornerRadius)values[3];

            var region = DrawRoundedRectangle(new Rect(-outerMargin, -outerMargin, width + outerMargin * 2, height + outerMargin * 2), cornerRadius);
            var clip = DrawRoundedRectangle(new Rect(0, 0, width, height), cornerRadius);

            var group = new GeometryGroup();
            group.Children.Add(region);
            group.Children.Add(clip);

            return group;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }

        /// <summary>
        /// Draws a rounded rectangle with four individual corner radius
        /// </summary>
        public static StreamGeometry DrawRoundedRectangle(Rect rect, CornerRadius cornerRadius)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                bool isStroked = false;
                bool isSmoothJoin = true;

                context.BeginFigure(rect.TopLeft + new Vector(0, cornerRadius.TopLeft), true, true);
                context.ArcTo(new Point(rect.TopLeft.X + cornerRadius.TopLeft, rect.TopLeft.Y),
                    new Size(cornerRadius.TopLeft, cornerRadius.TopLeft),
                    90, false, SweepDirection.Clockwise, isStroked, isSmoothJoin);
                context.LineTo(rect.TopRight - new Vector(cornerRadius.TopRight, 0), isStroked, isSmoothJoin);
                context.ArcTo(new Point(rect.TopRight.X, rect.TopRight.Y + cornerRadius.TopRight),
                    new Size(cornerRadius.TopRight, cornerRadius.TopRight),
                    90, false, SweepDirection.Clockwise, isStroked, isSmoothJoin);
                context.LineTo(rect.BottomRight - new Vector(0, cornerRadius.BottomRight), isStroked, isSmoothJoin);
                context.ArcTo(new Point(rect.BottomRight.X - cornerRadius.BottomRight, rect.BottomRight.Y),
                    new Size(cornerRadius.BottomRight, cornerRadius.BottomRight),
                    90, false, SweepDirection.Clockwise, isStroked, isSmoothJoin);
                context.LineTo(rect.BottomLeft + new Vector(cornerRadius.BottomLeft, 0), isStroked, isSmoothJoin);
                context.ArcTo(new Point(rect.BottomLeft.X, rect.BottomLeft.Y - cornerRadius.BottomLeft),
                    new Size(cornerRadius.BottomLeft, cornerRadius.BottomLeft),
                    90, false, SweepDirection.Clockwise, isStroked, isSmoothJoin);

                context.Close();
            }

            return geometry;
        }
    }

    public class ClipOuterRectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(o => o == DependencyProperty.UnsetValue || o == null)) return null;

            var width = (double)values[0];
            var height = (double)values[1];
            var margin = (double)values[2];

            var cornerRadius = new CornerRadius(0);
            if (values.Length >= 4 && values[3] is CornerRadius)
                cornerRadius = (CornerRadius)values[3];


            return ClipInnerRectConverter.DrawRoundedRectangle(new Rect(margin, margin, width, height), cornerRadius);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NegativeMarginConverter : IValueConverter
    {
        public double Multiple { get; set; } = -1d;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var margin = (double)value;

            return new Thickness(margin * Multiple);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

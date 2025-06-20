using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Converters
{
    public class ColorToBrushConverter : AdvancedValueConverterBase<Color, SolidColorBrush>
    {
        public bool FreezeBrushes { get; set; } = false;


        public override SolidColorBrush DoConvert(Color from)
        {
            var brush = new SolidColorBrush(from);

            if (FreezeBrushes && brush.CanFreeze)
            {
                brush.Freeze();
            }

            return brush;
        }


        public override Color DoConvertBack(SolidColorBrush to)
        {
            return to.Color;
        }

        //public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    Color col = (Color)value;
        //    Color c = Color.FromArgb(col.A, col.R, col.G, col.B);
        //    return new SolidColorBrush(c);
        //}

        //public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    SolidColorBrush c = (SolidColorBrush)value;
        //    Color col = Color.FromArgb(c.Color.A, c.Color.R, c.Color.G, c.Color.B);
        //    return col;
        //}

    }
}

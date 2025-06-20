using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Converters
{
    /// <summary>
    /// Extracts a single member of a CornerRadius object.
    /// For example, if you have a CornerRadius of 5,5,5,5 and you want to extract the TopLeft value, you would use this converter with the TargetMember set to TopLeft.
    /// </summary>
    public class CornerRadiusExtractionConverter: IValueConverter
    {
        public CornerRadiusExtractMember TargetMember { get; set; }

        public double Scale { get; set; } = 1;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CornerRadius)
            {
                var result = 0d;
                CornerRadius cornerRadius = (CornerRadius)value;

                switch (TargetMember)
                {
                    case CornerRadiusExtractMember.TopLeft:
                        result = cornerRadius.TopLeft;
                        break;
                    case CornerRadiusExtractMember.TopRight:
                        result = cornerRadius.TopRight;
                        break;
                    case CornerRadiusExtractMember.BottomRight:
                        result = cornerRadius.BottomRight;
                        break;
                    case CornerRadiusExtractMember.BottomLeft:
                        result = cornerRadius.BottomLeft;
                        break;
                    default:
                        result = cornerRadius.TopLeft;
                        break;
                }

                return result * Scale;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                double doubleValue = (double)value / Scale;

                switch (TargetMember)
                {
                    case CornerRadiusExtractMember.TopLeft:
                        return new CornerRadius(doubleValue, 0, 0, 0);
                    case CornerRadiusExtractMember.TopRight:
                        return new CornerRadius(0, doubleValue, 0, 0);
                    case CornerRadiusExtractMember.BottomRight:
                        return new CornerRadius(0, 0, doubleValue, 0);
                    case CornerRadiusExtractMember.BottomLeft:
                        return new CornerRadius(0, 0, 0, doubleValue);
                    default:
                        return new CornerRadius(doubleValue);
                }
            }

            return new CornerRadius(0);
        }
    }

    public enum CornerRadiusExtractMember
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }
}

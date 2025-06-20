using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Converters
{
    public class ColorToHexConverter : AdvancedValueConverterBase<Color, string>
    {
        public static DependencyProperty ShowAlphaProperty =
            DependencyProperty.Register(nameof(ShowAlpha), typeof(bool), typeof(ColorToHexConverter),
                new PropertyMetadata(true, ShowAlphaChangedCallback));

        public bool ShowAlpha
        {
            get => (bool)GetValue(ShowAlphaProperty);
            set => SetValue(ShowAlphaProperty, value);
        }

        public static DependencyProperty DefaultResultProperty =
            DependencyProperty.Register(nameof(DefaultResult), typeof(Color), typeof(ColorToHexConverter),
                new PropertyMetadata(Colors.Transparent));

        public Color DefaultResult
        {
            get => (Color)GetValue(DefaultResultProperty);
            set => SetValue(DefaultResultProperty, value);
        }


        public event EventHandler OnShowAlphaChange;

        public void RaiseShowAlphaChange()
        {
            OnShowAlphaChange(this, EventArgs.Empty);
        }

        private static void ShowAlphaChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ColorToHexConverter)d).RaiseShowAlphaChange();
        }

        public override string DoConvert(Color from)
        {
            if (!ShowAlpha)
                return ConvertNoAlpha(from);
            return from.ToString();
        }

        public override Color DoConvertBack(string to)
        {
            if (!ShowAlpha)
                return ConvertBackNoAlpha(to);
            string text = to;
            text = Regex.Replace(text.ToUpperInvariant(), @"[^0-9A-F]", "");
            StringBuilder final = new StringBuilder();
            if (text.Length == 3) //short hex with no alpha
            {
                final.Append("#FF").Append(text[0]).Append(text[0]).Append(text[1]).Append(text[1]).Append(text[2]).Append(text[2]);
            }
            else if (text.Length == 4) //short hex with alpha
            {
                final.Append("#").Append(text[0]).Append(text[0]).Append(text[1]).Append(text[1]).Append(text[2]).Append(text[2]).Append(text[3]).Append(text[3]);
            }
            else if (text.Length == 6) //hex with no alpha
            {
                final.Append("#FF").Append(text);
            }
            else
            {
                final.Append("#").Append(text);
            }
            try
            {
                return (Color)ColorConverter.ConvertFromString(final.ToString());
            }
            catch (Exception)
            {
                return DefaultResult;
            }
        }


        public string ConvertNoAlpha(Color value)
        {
            return "#" + value.ToString().Substring(3, 6);
        }

        public Color ConvertBackNoAlpha(string text)
        {
            text = Regex.Replace(text.ToUpperInvariant(), @"[^0-9A-F]", "");
            StringBuilder final = new StringBuilder();
            if (text.Length == 3) //short hex
            {
                final.Append("#FF").Append(text[0]).Append(text[0]).Append(text[1]).Append(text[1]).Append(text[2]).Append(text[2]);
            }
            else if (text.Length == 4)
            {
                return DefaultResult;
            }
            else if (text.Length > 6)
            {
                return DefaultResult;
            }
            else //regular hex
            {
                final.Append("#").Append(text);
            }
            try
            {
                return (Color)ColorConverter.ConvertFromString(final.ToString());
            }
            catch (Exception)
            {
                return DefaultResult;
            }
        }
    }
}

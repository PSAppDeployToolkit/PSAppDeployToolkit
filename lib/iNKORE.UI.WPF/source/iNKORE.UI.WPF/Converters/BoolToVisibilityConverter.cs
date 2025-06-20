using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Converters
{
    class BoolToVisibilityConverter : AdvancedValueConverterBase<bool, Visibility>
    {
        public static DependencyProperty InvertProperty =
            DependencyProperty.Register(nameof(Invert), typeof(bool), typeof(BoolToVisibilityConverter),
                new PropertyMetadata(false));

        public bool Invert
        {
            get => (bool)GetValue(InvertProperty);
            set => SetValue(InvertProperty, value);
        }

        public static DependencyProperty VisibilityToTrueProperty =
            DependencyProperty.Register(nameof(VisibilityToTrue), typeof(Visibility), typeof(BoolToVisibilityConverter),
                new PropertyMetadata(Visibility.Visible));

        public Visibility VisibilityToTrue
        {
            get => (Visibility)GetValue(VisibilityToTrueProperty);
            set => SetValue(VisibilityToTrueProperty, value);
        }

        public static DependencyProperty VisibilityToFalseProperty =
            DependencyProperty.Register(nameof(VisibilityToFalse), typeof(Visibility), typeof(BoolToVisibilityConverter),
                new PropertyMetadata(Visibility.Hidden));

        public Visibility VisibilityToFalse
        {
            get => (Visibility)GetValue(VisibilityToFalseProperty);
            set => SetValue(VisibilityToFalseProperty, value);
        }


        public override Visibility DoConvert(bool from)
        {
            bool actualValue = from ^ Invert;
            return actualValue ? VisibilityToTrue : VisibilityToFalse;
        }

        public override bool DoConvertBack(Visibility to)
        {
            if (to == VisibilityToTrue)
            {
                return InvertBool(true, Invert);
            }
            else if (to == VisibilityToFalse)
            {
                return InvertBool(false, Invert);
            }

            return false;
        }


        public static bool InvertBool(bool value, bool invert)
        {
            if(invert)
            {
                return !value;
            }
            else
            {
                return value;
            }
        }
    }
}

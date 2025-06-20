using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Converters
{
   
    public abstract class AdvancedValueConverterBase<TFrom, TTo> : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty ConvertDirectionProperty = DependencyProperty.Register(nameof(ConvertDirection), typeof(AdvancedValueConverterBaseDirection), typeof(AdvancedValueConverterBase<TFrom, TTo>), new PropertyMetadata(AdvancedValueConverterBaseDirection.Auto));
        public AdvancedValueConverterBaseDirection ConvertDirection
        {
            get { return (AdvancedValueConverterBaseDirection)this.GetValue(ConvertDirectionProperty); }
            set {  this.SetValue(ConvertDirectionProperty, value); }
        }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is TFrom from && (ConvertDirection == AdvancedValueConverterBaseDirection.Auto || ConvertDirection == AdvancedValueConverterBaseDirection.Normal))
            {
                return DoConvert(from);
            }
            else if (value is TTo to && (ConvertDirection == AdvancedValueConverterBaseDirection.Auto || ConvertDirection == AdvancedValueConverterBaseDirection.Inverted))
            {
                return DoConvertBack(to);
            }

            return null;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
           if (value is TTo to && (ConvertDirection == AdvancedValueConverterBaseDirection.Auto || ConvertDirection == AdvancedValueConverterBaseDirection.Normal))
            {
                return DoConvertBack(to);
            }
            else if (value is TFrom from && (ConvertDirection == AdvancedValueConverterBaseDirection.Auto || ConvertDirection == AdvancedValueConverterBaseDirection.Inverted))
            {
                return DoConvert(from);
            }

            return null;

        }


        public abstract TTo DoConvert(TFrom from);

        public abstract TFrom DoConvertBack(TTo to);
    }

    public enum AdvancedValueConverterBaseDirection
    {
        Normal,
        Inverted,
        Auto
    }
}

using iNKORE.UI.WPF.Modern.Helpers.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Modern.Common.Converters
{
    public class BackdropUtilityConverter: IValueConverter
    {
        public BackdropUtilityConverterType ConverterType { get; set; } = BackdropUtilityConverterType.IsSupported;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is BackdropType val)
            {
                switch (ConverterType)
                {
                    case BackdropUtilityConverterType.IsSupported:
                        return BackdropHelper.IsSupported(val);
                    case BackdropUtilityConverterType.ManualBackgroundNeeded:
                        return BackdropHelper.IsManualBackgroundNeeded(val);
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public enum BackdropUtilityConverterType
    {
        IsSupported,
        ManualBackgroundNeeded
    }
}

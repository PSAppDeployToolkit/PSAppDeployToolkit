using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PSADT.UserInterface.Converters
{
    /// <summary>
    /// Converts an integer value to a Visibility enum value.
    /// If the integer is greater than 0 or greater than specified threshold, returns Visible, otherwise returns Collapsed.
    /// Can be reversed with the parameter 'True' to collapse when value > 0.
    /// Special cases can be handled with string parameters like 'ListView' for ListView scrollbar behavior.
    /// </summary>
    public sealed class IntToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts an integer value to a Visibility enum value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Converts back from Visibility to integer.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

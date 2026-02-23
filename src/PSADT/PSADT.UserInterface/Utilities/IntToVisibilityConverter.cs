using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PSADT.UserInterface.Utilities
{
    /// <summary>
    /// Converts an integer value to a Visibility enum value.
    /// If the integer is greater than 0 or greater than specified threshold, returns Visible, otherwise returns Collapsed.
    /// Can be reversed with the parameter 'True' to collapse when value > 0.
    /// Special cases can be handled with string parameters like 'ListView' for ListView scrollbar behavior.
    /// </summary>
    public sealed record IntToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts an integer value representing a count to a corresponding visibility state for UI elements.
        /// </summary>
        /// <remarks>This method is typically used in data binding scenarios to control the visibility of
        /// UI elements based on the presence or absence of items.</remarks>
        /// <param name="value">The value to convert. Expected to be an integer representing a count.</param>
        /// <param name="targetType">The type to convert to. This parameter is not used.</param>
        /// <param name="parameter">An optional parameter to influence the conversion. This parameter is not used.</param>
        /// <param name="culture">The culture to use in the converter. This parameter is not used.</param>
        /// <returns>Returns <see cref="Visibility.Visible"/> if the input value is an integer greater than zero; otherwise,
        /// returns <see cref="Visibility.Collapsed"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a value from the target type back to the source type.
        /// </summary>
        /// <remarks>Override this method in a derived class to provide custom conversion logic from the
        /// target type back to the source type.</remarks>
        /// <param name="value">The value to convert back to the source type.</param>
        /// <param name="targetType">The type to convert the value to.</param>
        /// <param name="parameter">An optional parameter to use in the conversion logic.</param>
        /// <param name="culture">The culture to use in the conversion.</param>
        /// <returns>The converted value, or throws an exception if the conversion is not implemented.</returns>
        /// <exception cref="NotImplementedException">Thrown in all cases, as this method is not implemented.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PSADT.UserInterface.Interfaces
{
    /// <summary>
    /// Converts a count to a <see cref="Visibility"/>, showing an element only when the count is above zero.
    /// </summary>
    /// <remarks>The conversion is one-way and takes no converter parameter. A value that is not an <see cref="int"/> - including a boxed <see cref="long"/> or <see cref="uint"/>, and null - converts to <see cref="Visibility.Collapsed"/> rather than throwing. The value and the parameter are annotated as nullable because WPF supplies null for both: the first for a binding with no source value, the second whenever no ConverterParameter was given.</remarks>
    public sealed class IntToVisibilityConverter : IValueConverter
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
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value as int? > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a value from the target type back to the source type.
        /// </summary>
        /// <remarks>Not supported. A count cannot be recovered from a <see cref="Visibility"/>, so the bindings using this converter are one-way.</remarks>
        /// <param name="value">The value to convert back to the source type. This parameter is not used.</param>
        /// <param name="targetType">The type to convert the value to. This parameter is not used.</param>
        /// <param name="parameter">An optional parameter to use in the conversion logic. This parameter is not used.</param>
        /// <param name="culture">The culture to use in the conversion. This parameter is not used.</param>
        /// <returns>This method does not return; it always throws.</returns>
        /// <exception cref="NotSupportedException">Thrown in all cases, as this method is not implemented.</exception>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

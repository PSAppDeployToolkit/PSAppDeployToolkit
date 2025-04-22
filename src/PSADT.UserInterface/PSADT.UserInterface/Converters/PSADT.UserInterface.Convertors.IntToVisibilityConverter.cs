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
    internal sealed class IntToVisibilityConverter : IValueConverter
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
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            // Handle parameter-specific thresholds and behaviors
            bool reverseLogic = false;
            int threshold = 0;
            if (parameter is string paramStr)
            {
                // Check for reversed logic
                if (paramStr.Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    reverseLogic = true;
                }
                // Special case for ListView scrollbar appearance
                else if (paramStr.Equals("ListView", StringComparison.OrdinalIgnoreCase))
                {
                    threshold = 4; // Show scrollbar only when more than 4 items
                }
                // Check if parameter is a number for custom threshold
                else if (int.TryParse(paramStr, out int parsedThreshold))
                {
                    threshold = parsedThreshold;
                }
            }

            int intValue = 0;
            if (value is int i)
            {
                intValue = i;
            }
            else if (int.TryParse(value?.ToString(), out int parsedInt))
            {
                intValue = parsedInt;
            }

            // When reversed, collapse when count > threshold, otherwise it's visible
            if (reverseLogic)
            {
                return intValue > threshold ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return intValue > threshold ? Visibility.Visible : Visibility.Collapsed;
            }
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

﻿using System;
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
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            bool reverseLogic = false;
            int threshold = 0;

            // Handle parameter-specific thresholds and behaviors
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
                intValue = i;
            else if (int.TryParse(value?.ToString(), out int parsedInt))
                intValue = parsedInt;

            if (reverseLogic)
            {
                // When reversed, collapse when count > threshold
                return intValue > threshold ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                // Default: visible when count > threshold
                return intValue > threshold ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not implemented as we don't need two-way binding for this use case
            throw new NotImplementedException();
        }
    }
}

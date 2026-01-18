using System;
using System.Globalization;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides utility methods for parsing and converting numerical values.
    /// </summary>
    internal static class NumericalUtilities
    {
        /// <summary>
        /// Attempts to convert the specified string representation of a number to its equivalent <see cref="IntPtr"/>
        /// value.
        /// </summary>
        /// <remarks>This method supports both decimal and hexadecimal string formats. Hexadecimal values
        /// must be prefixed with "0x". The conversion is performed using the invariant culture. If the parsed value
        /// exceeds the size of <see cref="IntPtr"/> on the current platform, the method returns false.</remarks>
        /// <param name="s">The string containing the number to convert. The string may be in decimal or hexadecimal format (with a "0x"
        /// prefix for hexadecimal).</param>
        /// <param name="value">When this method returns, contains the <see cref="IntPtr"/> value equivalent to the number contained in
        /// <paramref name="s"/>, if the conversion succeeded, or <see cref="IntPtr.Zero"/> if the conversion failed.
        /// This parameter is passed uninitialized.</param>
        /// <returns>true if <paramref name="s"/> was converted successfully; otherwise, false.</returns>
        internal static bool TryParseIntPtr(string s, out IntPtr value)
        {
            // Return early if the string is bad.
            value = IntPtr.Zero;
            if (string.IsNullOrWhiteSpace(s))
            {
                return false;
            }

            // Handle "0x..." too, just in case.
            NumberStyles style = NumberStyles.Integer;
            if ((s = s.Trim()).StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(2);
                style = NumberStyles.AllowHexSpecifier;
            }

            // Parse into 64-bit, then down-cast safely depending on platform.
            if (!long.TryParse(s, style, CultureInfo.InvariantCulture, out long raw))
            {
                return false;
            }
            try
            {
                value = new(IntPtr.Size == 8 ? raw : checked((int)raw));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        /// <summary>
        /// Converts the specified string representation of a pointer or handle to its equivalent IntPtr value.
        /// </summary>
        /// <param name="s">The string that contains the pointer or handle to convert.</param>
        /// <returns>An IntPtr value that is equivalent to the pointer or handle specified in s.</returns>
        /// <exception cref="FormatException">Thrown if s is not in a valid format to represent an IntPtr value.</exception>
        internal static IntPtr ParseIntPtr(string s)
        {
            return !TryParseIntPtr(s, out IntPtr value)
                ? throw new FormatException($"The string '{s}' is not in a correct format to convert to IntPtr.")
                : value;
        }
    }
}

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;

namespace PSADT.Extensions
{
    /// <summary>
    /// Provides extension methods for converting <see cref="FILETIME"/> structures to <see cref="DateTime"/> objects.
    /// </summary>
    /// <remarks>The <see cref="FILETIMEExtensions"/> class includes methods that facilitate the conversion of
    /// Windows file time representations to .NET <see cref="DateTime"/> objects. Windows file time is a 64-bit value
    /// representing the number of 100-nanosecond intervals since January 1, 1601 (UTC).</remarks>
    internal static class FILETIMEExtensions
    {
        /// <summary>
        /// Determines whether the specified FILETIME structure represents a zero date and time value.
        /// </summary>
        /// <param name="filetime">The FILETIME structure to evaluate for a zero date and time.</param>
        /// <returns>true if the specified FILETIME structure represents a zero date and time; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsZero(this FILETIME filetime)
        {
            return filetime.dwHighDateTime == 0 && filetime.dwLowDateTime == 0;
        }

        /// <summary>
        /// Converts a FILETIME structure to its equivalent 64-bit integer representation.
        /// </summary>
        /// <remarks>This method is typically used to perform arithmetic or comparisons on FILETIME values
        /// by representing them as a single integer.</remarks>
        /// <param name="filetime">The FILETIME structure to convert to a 64-bit integer value.</param>
        /// <returns>A 64-bit integer representing the combined high and low parts of the specified FILETIME structure.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ToLong(this FILETIME filetime)
        {
            return ((long)filetime.dwHighDateTime << 32) | (filetime.dwLowDateTime & 0xFFFFFFFFL);
        }

        /// <summary>
        /// Converts a <see cref="FILETIME"/> structure to a <see cref="DateTime"/> object.
        /// </summary>
        /// <remarks>The conversion is based on the Windows file time, which is a 64-bit value
        /// representing the number of 100-nanosecond intervals since January 1, 1601 (UTC).</remarks>
        /// <param name="filetime">The <see cref="FILETIME"/> structure to convert.</param>
        /// <returns>A <see cref="DateTime"/> object that represents the same point in time as the specified <see
        /// cref="FILETIME"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DateTime ToDateTime(this FILETIME filetime)
        {
            return DateTime.FromFileTime(filetime.ToLong());
        }

        /// <summary>
        /// Converts a <see cref="FILETIME"/> structure to a UTC <see cref="DateTime"/> object.
        /// </summary>
        /// <remarks>The conversion is based on the Windows file time, which is a 64-bit value
        /// representing the number of 100-nanosecond intervals since January 1, 1601 (UTC).</remarks>
        /// <param name="filetime">The <see cref="FILETIME"/> structure to convert.</param>
        /// <returns>A UTC <see cref="DateTime"/> object that represents the same point in time as the specified
        /// <see cref="FILETIME"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static DateTime ToDateTimeUtc(this FILETIME filetime)
        {
            return DateTime.FromFileTimeUtc(filetime.ToLong());
        }
    }
}

using System;
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
        /// Converts a <see cref="FILETIME"/> structure to a <see cref="DateTime"/> object.
        /// </summary>
        /// <remarks>The conversion is based on the Windows file time, which is a 64-bit value
        /// representing the number of 100-nanosecond intervals since January 1, 1601 (UTC).</remarks>
        /// <param name="filetime">The <see cref="FILETIME"/> structure to convert.</param>
        /// <returns>A <see cref="DateTime"/> object that represents the same point in time as the specified <see
        /// cref="FILETIME"/>.</returns>
        internal static DateTime ToDateTime(this FILETIME filetime)
        {
            return DateTime.FromFileTime((long)(filetime.dwHighDateTime << 32) | (filetime.dwLowDateTime & 0xFFFFFFFFL));
        }
    }
}

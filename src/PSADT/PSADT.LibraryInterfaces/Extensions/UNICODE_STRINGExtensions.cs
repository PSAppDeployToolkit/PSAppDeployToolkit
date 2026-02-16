using System;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces.Extensions
{
    /// <summary>
    /// Provides extension methods for working with the UNICODE_STRING structure.
    /// </summary>
    /// <remarks>This class contains methods intended to facilitate interoperability between managed code and
    /// native code that uses the UNICODE_STRING structure. These methods help convert and manipulate UNICODE_STRING
    /// instances in a manner suitable for .NET applications.</remarks>
    internal static class UNICODE_STRINGExtensions
    {
        /// <summary>
        /// Converts the specified UNICODE_STRING structure to a managed string.
        /// </summary>
        /// <param name="unicodeString">The UNICODE_STRING structure to convert to a managed string.</param>
        /// <returns>A managed string representation of the specified UNICODE_STRING.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the UNICODE_STRING does not contain a valid string.</exception>
        internal static string ToManagedString(this UNICODE_STRING unicodeString)
        {
            return unicodeString.Buffer.ToIntPtr().ToManagedString(unicodeString.Length / sizeof(char));
        }
    }
}

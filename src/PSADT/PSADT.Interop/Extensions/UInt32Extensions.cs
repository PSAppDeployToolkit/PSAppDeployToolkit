using System;
using System.Runtime.CompilerServices;

namespace PSADT.Interop.Extensions
{
    /// <summary>
    /// Provides extension methods for the UInt32 (unsigned 32-bit integer) data type to support additional validation
    /// and utility operations.
    /// </summary>
    /// <remarks>These extension methods are intended to simplify common validation patterns and enhance code
    /// readability when working with unsigned integer values. Methods in this class are designed for use within the
    /// assembly and are not intended for public API exposure.</remarks>
    internal static class UInt32Extensions
    {
        /// <summary>
        /// Throws an exception if the specified unsigned integer value is zero.
        /// </summary>
        /// <remarks>Use this method to enforce that a parameter or value is not zero, providing a clear
        /// exception message for debugging and validation purposes.</remarks>
        /// <param name="value">The unsigned integer value to validate. Must not be zero.</param>
        /// <param name="name">The name of the calling member. Used to identify the parameter in the exception message.</param>
        /// <returns>The original value if it is not zero.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is zero.</exception>
        internal static uint ThrowIfZero(this uint value, [CallerMemberName] string name = null!)
        {
            return value == 0 ? throw new ArgumentOutOfRangeException(name, value, "Value cannot be zero.") : value;
        }
    }
}

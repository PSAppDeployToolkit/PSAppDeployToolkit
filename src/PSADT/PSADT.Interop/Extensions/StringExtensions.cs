using System;
using System.Runtime.CompilerServices;

namespace PSADT.Interop.Extensions
{
    /// <summary>
    /// Provides extension methods for performing common string validation and manipulation tasks.
    /// </summary>
    /// <remarks>The methods in this class extend the functionality of string objects, enabling convenient
    /// validation and utility operations. These extensions are intended to simplify input checking and other
    /// string-related logic throughout an application.</remarks>
    internal static class StringExtensions
    {
        /// <summary>
        /// Throws an exception if the specified string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <remarks>Use this method to enforce that string parameters are not null, empty, or white-space
        /// in method calls. This is useful for validating input and ensuring that required string values are
        /// provided.</remarks>
        /// <param name="value">The string to validate. This value must not be null, empty, or contain only white-space characters.</param>
        /// <param name="name">The name of the parameter or member invoking this method. Used to identify the argument in the exception
        /// message.</param>
        /// <returns>The original string value if it is not null, empty, or white-space.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null, empty, or consists only of white-space characters.</exception>
        internal static string ThrowIfNullOrWhiteSpace(this string? value, [CallerMemberName] string name = null!)
        {
            return string.IsNullOrWhiteSpace(value) ? throw new ArgumentNullException(name) : value!;
        }
    }
}

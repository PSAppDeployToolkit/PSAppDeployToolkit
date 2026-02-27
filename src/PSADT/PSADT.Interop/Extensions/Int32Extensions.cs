using System;
using System.Runtime.CompilerServices;

namespace PSADT.Interop.Extensions
{
    internal static class Int32Extensions
    {
        /// <summary>
        /// Throws an exception if the specified integer value is negative.
        /// </summary>
        /// <remarks>Use this method to enforce non-negative constraints on integer parameters or values.
        /// This is typically used in guard clauses to ensure that arguments meet required preconditions.</remarks>
        /// <param name="value">The integer value to validate. Must be zero or positive.</param>
        /// <param name="name">The name of the parameter or member invoking this method. Used to provide context in the exception message.</param>
        /// <returns>The original integer value if it is zero or positive.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is less than zero.</exception>
        internal static int ThrowIfNegative(this int value, [CallerMemberName] string name = null!)
        {
            return value < 0 ? throw new ArgumentOutOfRangeException(name, value, $"{name} cannot be negative.") : value;
        }

        /// <summary>
        /// Throws an exception if the specified integer value is less than or equal to zero.
        /// </summary>
        /// <param name="value">The integer value to validate. Must be greater than zero.</param>
        /// <param name="name">The name of the parameter or member invoking this method. Used in exception reporting.</param>
        /// <returns>The original value if it is greater than zero.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is less than or equal to zero.</exception>
        internal static int ThrowIfZeroOrNegative(this int value, [CallerMemberName] string name = null!)
        {
            return value <= 0 ? throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than zero.") : value;
        }
    }
}

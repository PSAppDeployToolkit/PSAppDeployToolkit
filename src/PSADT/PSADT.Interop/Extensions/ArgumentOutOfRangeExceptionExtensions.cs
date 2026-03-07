using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.Win32.Foundation;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Additional ArgumentOutOfRangeException extensions for handle validation.
    /// These are not polyfills - they are always available on all target frameworks.
    /// </summary>
    internal static class ArgumentOutOfRangeExceptionExtensions
    {
        /// <summary>
        /// Provides extension methods for validating argument values to ensure they are non-zero and not invalid,
        /// throwing an ArgumentOutOfRangeException when validation fails.
        /// </summary>
        extension(ArgumentOutOfRangeException)
        {
            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is zero or invalid (-1).
            /// </summary>
            /// <param name="value">The argument to validate.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is zero or -1.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static void ThrowIfZeroOrInvalid(nint value, [CallerArgumentExpression(nameof(value))] string paramName = null!)
            {
                if (value is 0 or -1)
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-zero, valid value.");
                }
            }

            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is zero or invalid (max value, equivalent to -1 bit pattern).
            /// </summary>
            /// <param name="value">The argument to validate.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is zero or max value.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static void ThrowIfZeroOrInvalid(nuint value, [CallerArgumentExpression(nameof(value))] string paramName = null!)
            {
                if (value == 0 || value == (nuint)(nint)HANDLE.INVALID_HANDLE_VALUE)
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-zero, valid value.");
                }
            }

            /// <summary>
            /// Throws an exception if the specified value is invalid, specifically if it equals -1.
            /// </summary>
            /// <param name="value">The value to validate. Must not be -1, as this indicates an invalid state.</param>
            /// <param name="paramName">The name of the parameter being validated, used in the exception message.</param>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is -1, indicating an invalid value.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static void ThrowIfInvalid(nint value, [CallerArgumentExpression(nameof(value))] string paramName = null!)
            {
                if (value == -1)
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a valid value.");
                }
            }

            /// <summary>
            /// Throws an exception if the specified value is invalid, specifically if it equals -1.
            /// </summary>
            /// <param name="value">The value to validate. Must not be equal to -1, as this indicates an invalid state.</param>
            /// <param name="paramName">The name of the parameter being validated, used in the exception message if validation fails.</param>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is equal to -1, indicating an invalid value.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static void ThrowIfInvalid(nuint value, [CallerArgumentExpression(nameof(value))] string paramName = null!)
            {
                if (value == unchecked((nuint)(-1)))
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a valid value.");
                }
            }
        }
    }
}

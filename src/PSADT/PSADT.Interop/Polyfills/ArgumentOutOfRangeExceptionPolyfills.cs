#if !NET8_0_OR_GREATER
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfills for ArgumentOutOfRangeException.ThrowIf* methods on .NET Framework 4.7.2.
    /// Provides static methods to throw an ArgumentOutOfRangeException based on various conditions.
    /// </summary>
    internal static class ArgumentOutOfRangeExceptionPolyfills
    {
        extension(ArgumentOutOfRangeException)
        {
            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is equal to <paramref name="other"/>.
            /// </summary>
            /// <typeparam name="T">The type of the objects to compare.</typeparam>
            /// <param name="value">The argument to validate.</param>
            /// <param name="other">The value to compare with <paramref name="value"/>.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is equal to <paramref name="other"/>.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string paramName = null!) where T : IEquatable<T>?
            {
                if (EqualityComparer<T>.Default.Equals(value, other))
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must not be equal to '{other}'.");
                }
            }

            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is not equal to <paramref name="other"/>.
            /// </summary>
            /// <typeparam name="T">The type of the objects to compare.</typeparam>
            /// <param name="value">The argument to validate.</param>
            /// <param name="other">The value to compare with <paramref name="value"/>.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not equal to <paramref name="other"/>.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfNotEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string paramName = null!) where T : IEquatable<T>?
            {
                if (!EqualityComparer<T>.Default.Equals(value, other))
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be equal to '{other}'.");
                }
            }

            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than <paramref name="other"/>.
            /// </summary>
            /// <typeparam name="T">The type of the objects to compare.</typeparam>
            /// <param name="value">The argument to validate.</param>
            /// <param name="other">The value to compare with <paramref name="value"/>.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is greater than <paramref name="other"/>.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfGreaterThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string paramName = null!) where T : IComparable<T>
            {
                if (value.CompareTo(other) > 0)
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be less than or equal to '{other}'.");
                }
            }

            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than or equal to <paramref name="other"/>.
            /// </summary>
            /// <typeparam name="T">The type of the objects to compare.</typeparam>
            /// <param name="value">The argument to validate.</param>
            /// <param name="other">The value to compare with <paramref name="value"/>.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is greater than or equal to <paramref name="other"/>.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfGreaterThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string paramName = null!) where T : IComparable<T>
            {
                if (value.CompareTo(other) >= 0)
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be less than '{other}'.");
                }
            }

            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is less than <paramref name="other"/>.
            /// </summary>
            /// <typeparam name="T">The type of the objects to compare.</typeparam>
            /// <param name="value">The argument to validate.</param>
            /// <param name="other">The value to compare with <paramref name="value"/>.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than <paramref name="other"/>.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfLessThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string paramName = null!) where T : IComparable<T>
            {
                if (value.CompareTo(other) < 0)
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be greater than or equal to '{other}'.");
                }
            }

            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is less than or equal to <paramref name="other"/>.
            /// </summary>
            /// <typeparam name="T">The type of the objects to compare.</typeparam>
            /// <param name="value">The argument to validate.</param>
            /// <param name="other">The value to compare with <paramref name="value"/>.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than or equal to <paramref name="other"/>.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfLessThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string paramName = null!) where T : IComparable<T>
            {
                if (value.CompareTo(other) <= 0)
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be greater than '{other}'.");
                }
            }

            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative.
            /// </summary>
            /// <typeparam name="T">The type of the value to compare.</typeparam>
            /// <param name="value">The argument to validate.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfNegative<T>(T value, [CallerArgumentExpression(nameof(value))] string paramName = null!) where T : IComparable<T>
            {
                if (value.CompareTo(default!) < 0)
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-negative value.");
                }
            }

            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative or zero.
            /// </summary>
            /// <typeparam name="T">The type of the value to compare.</typeparam>
            /// <param name="value">The argument to validate.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative or zero.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfNegativeOrZero<T>(T value, [CallerArgumentExpression(nameof(value))] string paramName = null!) where T : IComparable<T>
            {
                if (value.CompareTo(default!) <= 0)
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a positive value.");
                }
            }

            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is zero.
            /// </summary>
            /// <typeparam name="T">The type of the value to compare.</typeparam>
            /// <param name="value">The argument to validate.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is zero.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfZero<T>(T value, [CallerArgumentExpression(nameof(value))] string paramName = null!) where T : IEquatable<T>?
            {
                if (EqualityComparer<T>.Default.Equals(value, default!))
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-zero value.");
                }
            }

            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is zero.
            /// </summary>
            /// <param name="value">The argument to validate.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is zero.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfZero(nint value, [CallerArgumentExpression(nameof(value))] string paramName = null!)
            {
                if (value == 0)
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-zero value.");
                }
            }

            /// <summary>
            /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is zero.
            /// </summary>
            /// <param name="value">The argument to validate.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is zero.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfZero(nuint value, [CallerArgumentExpression(nameof(value))] string paramName = null!)
            {
                if (value == 0)
                {
                    throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-zero value.");
                }
            }
        }
    }
}
#endif

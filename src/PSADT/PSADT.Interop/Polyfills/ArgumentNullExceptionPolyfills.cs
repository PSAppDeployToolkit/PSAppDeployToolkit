#if !NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfills for ArgumentNullException.ThrowIfNull on .NET Framework 4.7.2.
    /// Provides a static method to throw an ArgumentNullException if an argument is null.
    /// </summary>
    internal static class ArgumentNullExceptionPolyfills
    {
        /// <summary>
        /// Provides extension methods for validating that arguments are not null, throwing an <see
        /// cref="ArgumentNullException"/> if validation fails.
        /// </summary>
        /// <remarks>This extension is intended for use in scenarios where null arguments are not
        /// acceptable, providing clear exception messages to aid in debugging.</remarks>
        extension(ArgumentNullException)
        {
            /// <summary>
            /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.
            /// </summary>
            /// <param name="argument">The reference type argument to validate as non-null.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
            /// <exception cref="ArgumentNullException"><paramref name="argument"/> is null.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string paramName = null!)
            {
                if (argument is null)
                {
                    throw new ArgumentNullException(paramName);
                }
            }

            /// <summary>
            /// Throws an ArgumentNullException if the specified pointer argument is null.
            /// </summary>
            /// <remarks>This method is intended for use in scenarios where null pointer arguments are
            /// not acceptable. It provides a clear exception message indicating which parameter was null, aiding in
            /// debugging and validation.</remarks>
            /// <param name="argument">The pointer to the argument to validate. This parameter cannot be null.</param>
            /// <param name="paramName">The name of the parameter being validated. Used in the exception message if the argument is null.</param>
            /// <exception cref="ArgumentNullException">Thrown if <paramref name="argument"/> is null.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void ThrowIfNull([NotNull] void* argument, [CallerArgumentExpression(nameof(argument))] string paramName = null!)
            {
                if (argument is null)
                {
                    throw new ArgumentNullException(paramName);
                }
            }
        }
    }
}
#endif

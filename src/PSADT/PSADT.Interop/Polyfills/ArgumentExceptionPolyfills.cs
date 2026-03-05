#if !NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfills for ArgumentException.ThrowIfNullOrWhiteSpace on .NET Framework 4.7.2.
    /// Provides a static method to throw an ArgumentException if a string argument is null, empty, or whitespace.
    /// </summary>
    internal static class ArgumentExceptionPolyfills
    {
        /// <summary>
        /// Provides extension methods for validating that string arguments are not null, empty, or whitespace,
        /// throwing an <see cref="ArgumentException"/> if the validation fails.
        /// </summary>
        extension(ArgumentException)
        {
            /// <summary>
            /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null,
            /// or an <see cref="ArgumentException"/> if it is empty or consists only of white-space characters.
            /// </summary>
            /// <param name="argument">The string argument to validate.</param>
            /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
            /// <exception cref="ArgumentNullException"><paramref name="argument"/> is null.</exception>
            /// <exception cref="ArgumentException"><paramref name="argument"/> is empty or consists only of white-space characters.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string paramName = null!)
            {
                if (argument is null)
                {
                    throw new ArgumentNullException(paramName);
                }
                if (string.IsNullOrWhiteSpace(argument))
                {
                    throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);
                }
            }
        }
    }
}
#endif

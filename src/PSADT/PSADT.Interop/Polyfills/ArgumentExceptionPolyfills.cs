using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;

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

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Additional ArgumentException extensions for handle validation.
    /// These are not polyfills - they are always available on all target frameworks.
    /// </summary>
    internal static class ArgumentExceptionExtensions
    {
        extension(ArgumentException)
        {
            /// <summary>
            /// Validates that the specified SafeHandle is not null and is not closed.
            /// </summary>
            /// <remarks>Use this method to ensure that a SafeHandle is in a valid state before performing
            /// operations that require an open handle.</remarks>
            /// <param name="handle">The SafeHandle instance to validate. This parameter must not be null or closed.</param>
            /// <param name="name">The name of the parameter. Used in exception messages to identify the source of the error.</param>
            /// <exception cref="ArgumentNullException">Thrown if <paramref name="handle"/> is null.</exception>
            /// <exception cref="ObjectDisposedException">Thrown if <paramref name="handle"/> is closed.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfNullOrClosed(SafeHandle handle, [CallerArgumentExpression(nameof(handle))] string name = null!)
            {
                if (handle is null)
                {
                    throw new ArgumentNullException(name, "SafeHandle cannot be null.");
                }
                if (handle.IsClosed)
                {
                    throw new ObjectDisposedException(name, "SafeHandle is already closed.");
                }
            }

            /// <summary>
            /// Validates that the specified SafeHandle is neither null, closed, nor invalid, and throws an exception if the
            /// handle is not usable.
            /// </summary>
            /// <remarks>This method is intended to simplify SafeHandle validation. It ensures that the handle 
            /// is ready for use and provides clear exception information if validation fails.</remarks>
            /// <param name="handle">The SafeHandle instance to validate. Must not be null, closed, or invalid.</param>
            /// <param name="name">The name of the parameter. Used in exception messages to identify the source of the error.</param>
            /// <exception cref="ArgumentException">Thrown if the SafeHandle is invalid, indicating that the handle cannot be used.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ThrowIfNullOrInvalid(SafeHandle handle, [CallerArgumentExpression(nameof(handle))] string name = null!)
            {
                ThrowIfNullOrClosed(handle, name);
                if (handle.IsInvalid)
                {
                    throw new ArgumentException("SafeHandle is invalid.", name);
                }
            }
        }
    }
}

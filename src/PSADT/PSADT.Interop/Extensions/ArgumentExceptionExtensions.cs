using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        /// <summary>
        /// Provides extension methods for validating SafeHandle instances and throwing appropriate exceptions when
        /// handles are null, closed, or invalid.
        /// </summary>
        /// <remarks>These extension methods help ensure that SafeHandle parameters are in a valid state
        /// before use, simplifying error handling and improving code clarity. Use these methods to enforce
        /// preconditions for APIs that require open and valid handles.</remarks>
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
                    throw new ArgumentNullException(name, "The specified SafeHandle cannot be null.");
                }
                if (handle.IsClosed)
                {
                    throw new ObjectDisposedException(name, "The specified SafeHandle is already closed.");
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
                    throw new ArgumentOutOfRangeException(name, "The specified SafeHandle is invalid.");
                }
            }
        }
    }
}

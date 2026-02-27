using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PSADT.Interop.Extensions
{
    /// <summary>
    /// Provides extension methods for validating SafeHandle instances.
    /// </summary>
    /// <remarks>These methods ensure that a SafeHandle is in a valid state before performing operations that
    /// require an open or valid handle.</remarks>
    internal static class SafeHandleExtensions
    {
        /// <summary>
        /// Validates that the specified SafeHandle is not null and is not closed.
        /// </summary>
        /// <remarks>Use this method to ensure that a SafeHandle is in a valid state before performing
        /// operations that require an open handle.</remarks>
        /// <param name="handle">The SafeHandle instance to validate. This parameter must not be null or closed.</param>
        /// <param name="name">The name of the calling member. Used in exception messages to identify the source of the error.</param>
        /// <returns>The validated SafeHandle instance if it is not null and not closed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handle"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if <paramref name="handle"/> is closed.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Implementing this here will just make for worse code.")]
        internal static SafeHandle ThrowIfNullOrClosed(this SafeHandle handle, [CallerMemberName] string name = null!)
        {
            if (handle is null)
            {
                throw new ArgumentNullException(name, "SafeHandle cannot be null.");
            }
            if (handle.IsClosed)
            {
                throw new ObjectDisposedException(name, "SafeHandle is already closed.");
            }
            return handle;
        }

        /// <summary>
        /// Validates that the specified SafeHandle is neither null, closed, nor invalid, and throws an exception if the
        /// handle is not usable.
        /// </summary>
        /// <remarks>This method is intended to be used as an extension method to simplify SafeHandle
        /// validation. It ensures that the handle is ready for use and provides clear exception information if
        /// validation fails.</remarks>
        /// <param name="handle">The SafeHandle instance to validate. Must not be null, closed, or invalid.</param>
        /// <param name="name">The name of the member invoking this method. Used in exception messages to identify the source of the error.</param>
        /// <returns>The validated SafeHandle instance if it is valid and open.</returns>
        /// <exception cref="ArgumentException">Thrown if the SafeHandle is invalid, indicating that the handle cannot be used.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Implementing this here will just make for worse code.")]
        internal static SafeHandle ThrowIfNullOrInvalid(this SafeHandle handle, [CallerMemberName] string name = null!)
        {
            if (handle.ThrowIfNullOrClosed(name).IsInvalid)
            {
                throw new ArgumentException("SafeHandle is invalid.", name);
            }
            return handle;
        }
    }
}

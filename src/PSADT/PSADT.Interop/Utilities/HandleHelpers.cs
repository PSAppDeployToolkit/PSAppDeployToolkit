using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PSADT.Interop.Utilities
{
    /// <summary>
    /// Provides static helper methods for validating native handles and pointer values to ensure they are not zero,
    /// null, closed, or invalid before performing operations.
    /// </summary>
    /// <remarks>Use the methods in this class to guard against invalid handle or pointer states, helping to
    /// prevent runtime errors when interacting with unmanaged resources. Each method throws an exception if the
    /// specified value does not meet the required condition, allowing callers to enforce preconditions for safe
    /// resource usage.</remarks>
    internal static class HandleHelpers
    {
        /// <summary>
        /// Throws an InvalidOperationException if the specified value is zero.
        /// </summary>
        /// <param name="value">The value to check. If this parameter is zero, an exception will be thrown.</param>
        /// <param name="message">The message that will be included in the exception if the value is zero.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="value"/> is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfZero(nint value, string message)
        {
            if (value == IntPtr.Zero)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Throws an InvalidOperationException if the specified value is zero.
        /// </summary>
        /// <param name="value">The value to check. If this parameter is zero, an exception will be thrown.</param>
        /// <param name="message">The message that will be included in the exception if the value is zero.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="value"/> is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfZero(nuint value, string message)
        {
            if (value == UIntPtr.Zero)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Throws an exception if the specified value is invalid.
        /// </summary>
        /// <param name="value">The value to validate. If this value is -1, an exception is thrown.</param>
        /// <param name="message">The error message to include with the exception if the value is invalid.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="value"/> is -1.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfInvalid(nint value, string message)
        {
            if (value == -1)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Throws an exception if the specified value represents an invalid state.
        /// </summary>
        /// <param name="value">The value to validate. If this value is equal to -1, it is considered invalid and an exception will be
        /// thrown.</param>
        /// <param name="message">The error message to include with the exception if the value is invalid.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="value"/> is equal to -1, indicating an invalid operation.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfInvalid(nuint value, string message)
        {
            if (value == unchecked((nuint)(-1)))
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Throws an exception if the specified native pointer value is null or invalid.
        /// </summary>
        /// <remarks>Use this method to ensure that a native pointer is valid before performing operations
        /// that require a non-null reference.</remarks>
        /// <param name="value">The native pointer value to validate. Must not be equal to <see cref="IntPtr.Zero"/>.</param>
        /// <param name="message">The message to include in the exception if the value is invalid.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="value"/> is equal to <see cref="IntPtr.Zero"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfNullOrInvalid(nint value, string message)
        {
            if (value == IntPtr.Zero || value == -1)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Throws an exception if the specified native pointer value is null or invalid.
        /// </summary>
        /// <remarks>Use this method to ensure that a native pointer is valid before performing operations
        /// that require a non-null reference.</remarks>
        /// <param name="value">The native pointer value to validate. Must not be equal to <see cref="IntPtr.Zero"/>.</param>
        /// <param name="message">The message to include in the exception if the value is invalid.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="value"/> is equal to <see cref="IntPtr.Zero"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfNullOrInvalid(nuint value, string message)
        {
            if (value == UIntPtr.Zero || value == unchecked((nuint)(-1)))
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Throws an exception if the specified handle is null, closed, or invalid.
        /// </summary>
        /// <remarks>Use this method to ensure that a SafeHandle is in a valid state before performing
        /// operations that require a valid handle.</remarks>
        /// <typeparam name="T">Specifies the type of the handle. Must derive from SafeHandle.</typeparam>
        /// <param name="handle">The handle to validate. The handle must not be null, closed, or invalid.</param>
        /// <param name="message">The message included in the exception if the handle is null, closed, or invalid.</param>
        /// <exception cref="InvalidOperationException">Thrown if the handle is null, closed, or invalid.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfNullOrInvalid<T>(T handle, string message) where T : SafeHandle
        {
            if (handle is null || handle.IsClosed || handle.IsInvalid)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

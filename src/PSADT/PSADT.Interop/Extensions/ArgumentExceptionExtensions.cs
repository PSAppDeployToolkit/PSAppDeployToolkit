using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

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
            /// Throws an exception if the provided span is empty or consists solely of whitespace characters.
            /// </summary>
            /// <param name="argument">The span of characters to validate. Must not be empty or contain only whitespace.</param>
            /// <param name="paramName">The name of the parameter being validated. Used in the exception message if validation fails.</param>
            /// <exception cref="ArgumentException">Thrown if <paramref name="argument"/> is empty or contains only whitespace characters.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static void ThrowIfEmptyOrWhiteSpace(ReadOnlySpan<char> argument, [CallerArgumentExpression(nameof(argument))] string paramName = null!)
            {
                if (argument.IsEmpty || argument.IsWhiteSpace())
                {
                    throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);
                }
            }

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
            [StackTraceHidden]
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
            [StackTraceHidden]
            public static void ThrowIfNullOrInvalid(SafeHandle handle, [CallerArgumentExpression(nameof(handle))] string name = null!)
            {
                ThrowIfNullOrClosed(handle, name);
                if (handle.IsInvalid)
                {
                    throw new ArgumentOutOfRangeException(name, "The specified SafeHandle is invalid.");
                }
            }

            /// <summary>
            /// Validates that the specified UNICODE_STRING is not null and contains a non-empty buffer. Throws an
            /// exception if the value is invalid.
            /// </summary>
            /// <remarks>Use this method to ensure that a UNICODE_STRING parameter is valid before
            /// further processing. This helps prevent errors caused by null or empty string buffers.</remarks>
            /// <param name="value">The UNICODE_STRING instance to validate. The buffer must not be null and the length must be greater than
            /// zero.</param>
            /// <param name="name">The name of the parameter being validated. Used in exception messages to identify the invalid argument.</param>
            /// <exception cref="ArgumentNullException">Thrown if the buffer of the specified UNICODE_STRING is null.</exception>
            /// <exception cref="ArgumentException">Thrown if the length of the specified UNICODE_STRING is zero.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static void ThrowIfNullOrInvalid(UNICODE_STRING value, [CallerArgumentExpression(nameof(value))] string name = null!)
            {
                unsafe
                {
                    if (value.Buffer.Value is null)
                    {
                        throw new ArgumentNullException(name, "The specified UNICODE_STRING cannot have a null buffer.");
                    }
                }
                if (value.Length == 0)
                {
                    throw new ArgumentException("The specified UNICODE_STRING cannot be empty.", name);
                }
            }

            /// <summary>
            /// Validates a specified UNICODE_STRING structure and throws an exception if its buffer and maximum length
            /// are not consistent with required constraints.
            /// </summary>
            /// <remarks>This method enforces structural integrity for UNICODE_STRING instances,
            /// ensuring that the buffer and maximum length are logically consistent. Use this method to guard against
            /// invalid states before processing or passing UNICODE_STRING values.</remarks>
            /// <param name="value">The UNICODE_STRING to validate. If MaximumLength is greater than zero, Buffer must not be null; if
            /// MaximumLength is zero, Buffer must be null.</param>
            /// <param name="name">The name of the parameter being validated. Used in exception messages to identify the invalid argument.</param>
            /// <exception cref="ArgumentNullException">Thrown if value.Buffer is null while value.MaximumLength is greater than zero.</exception>
            /// <exception cref="ArgumentException">Thrown if value.Buffer is not null while value.MaximumLength is zero.</exception>
            public static void ThrowIfInvalid(UNICODE_STRING value, [CallerArgumentExpression(nameof(value))] string name = null!)
            {
                unsafe
                {
                    if (value.Buffer.Value is null && value.MaximumLength > 0)
                    {
                        throw new ArgumentNullException(name, "The specified UNICODE_STRING cannot have a null buffer when MaximumLength is greater than zero.");
                    }
                    if (value.Buffer.Value is not null && value.MaximumLength == 0)
                    {
                        throw new ArgumentException("The specified UNICODE_STRING cannot have a non-null buffer when MaximumLength is zero.", name);
                    }
                }
            }
        }
    }
}

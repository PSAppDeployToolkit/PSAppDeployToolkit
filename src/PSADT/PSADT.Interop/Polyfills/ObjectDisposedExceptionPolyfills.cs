#if !NET7_0_OR_GREATER
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfills for ObjectDisposedException.ThrowIf on .NET Framework 4.7.2.
    /// Provides a static method to throw an ObjectDisposedException if a condition is true.
    /// </summary>
    internal static class ObjectDisposedExceptionPolyfills
    {
        /// <summary>
        /// Provides extension methods for validating that objects are not disposed, throwing an <see
        /// cref="ObjectDisposedException"/> if validation fails.
        /// </summary>
        extension(ObjectDisposedException)
        {
            /// <summary>
            /// Throws an <see cref="ObjectDisposedException"/> if <paramref name="condition"/> is true.
            /// </summary>
            /// <param name="condition">The condition to evaluate.</param>
            /// <param name="instance">The object whose type name should be included in the exception message.</param>
            /// <exception cref="ObjectDisposedException">The condition is true.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static void ThrowIf(bool condition, object instance)
            {
                if (condition)
                {
                    throw new ObjectDisposedException(instance?.GetType().FullName);
                }
            }

            /// <summary>
            /// Throws an <see cref="ObjectDisposedException"/> if <paramref name="condition"/> is true.
            /// </summary>
            /// <param name="condition">The condition to evaluate.</param>
            /// <param name="type">The type whose full name should be included in the exception message.</param>
            /// <exception cref="ObjectDisposedException">The condition is true.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static void ThrowIf(bool condition, Type type)
            {
                if (condition)
                {
                    throw new ObjectDisposedException(type?.FullName);
                }
            }
        }
    }
}
#endif

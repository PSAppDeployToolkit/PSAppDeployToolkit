using System;

namespace PSADT.Utilities
{
    /// <summary>
    /// A class containing utility methods for library operations.
    /// </summary>
    internal static class LibraryUtilities
    {
        /// <summary>
        /// Aligns the given value up to the nearest multiple of the system's pointer size.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static int AlignUp(int value)
        {
            return (value + IntPtr.Size - 1) & ~(IntPtr.Size - 1);
        }

        /// <summary>
        /// Creates a span from a pointer and length.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pointer"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal static unsafe Span<T> CreateSpanFromPointer<T>(IntPtr pointer, int length) where T : unmanaged
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative.");
            }
            return new Span<T>(pointer.ToPointer(), length);
        }
    }
}

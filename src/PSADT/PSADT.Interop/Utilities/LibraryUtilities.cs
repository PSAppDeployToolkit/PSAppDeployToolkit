using System;

namespace PSADT.Interop.Utilities
{
    /// <summary>
    /// A class containing utility methods for library operations.
    /// </summary>
    internal static class LibraryUtilities
    {
        /// <summary>
        /// Aligns the specified integer value to the next highest multiple of the platform's pointer size.
        /// </summary>
        /// <remarks>Use this method to ensure that memory sizes or offsets are properly aligned for
        /// native interop or performance-sensitive operations.</remarks>
        /// <param name="value">The integer value to align. Must be non-negative.</param>
        /// <returns>The smallest integer greater than or equal to <paramref name="value"/> that is a multiple of <see
        /// cref="IntPtr.Size"/>.</returns>
        internal static int AlignUp(int value)
        {
            return (value + IntPtr.Size - 1) & ~(IntPtr.Size - 1);
        }
    }
}

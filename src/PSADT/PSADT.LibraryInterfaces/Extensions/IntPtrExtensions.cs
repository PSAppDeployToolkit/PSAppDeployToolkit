using System;
using System.Runtime.CompilerServices;

namespace PSADT.LibraryInterfaces.Extensions
{
    /// <summary>
    /// Provides extension methods for working with memory addresses represented by <see cref="IntPtr"/>. These methods
    /// enable interpreting unmanaged memory as structures of a specified type.
    /// </summary>
    /// <remarks>These extension methods are intended for advanced scenarios involving direct memory
    /// manipulation. The caller is responsible for ensuring that memory addresses and offsets are valid and properly
    /// aligned for the target structure type. Improper use may result in undefined behavior or application
    /// instability.</remarks>
    internal static class IntPtrExtensions
    {
        /// <summary>
        /// Returns a reference to a structure of type <typeparamref name="T"/> located at the specified memory address
        /// and offset.
        /// </summary>
        /// <remarks>This method performs no safety checks on the provided memory address or offset. The
        /// caller is responsible for ensuring that the memory is valid and properly aligned for the type <typeparamref
        /// name="T"/>. Using an invalid address or offset may result in undefined behavior.</remarks>
        /// <typeparam name="T">The value type to interpret at the given memory location. Must be an unmanaged structure.</typeparam>
        /// <param name="handle">The base memory address that points to the start of the structure.</param>
        /// <param name="offset">The byte offset from <paramref name="handle"/> at which the structure of type <typeparamref name="T"/> is
        /// located. The offset must be zero or positive.</param>
        /// <returns>A reference to the structure of type <typeparamref name="T"/> at the specified memory address and offset.</returns>
        internal static ref readonly T AsReadOnlyStructure<T>(this IntPtr handle, int offset = 0) where T : unmanaged
        {
            unsafe
            {
                return ref Unsafe.AsRef<T>((void*)unchecked(handle + offset));
            }
        }
    }
}

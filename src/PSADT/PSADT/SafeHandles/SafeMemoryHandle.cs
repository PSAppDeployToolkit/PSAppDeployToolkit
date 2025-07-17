using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for a handle to a block of memory allocated from the Marshal class.
    /// </summary>
    internal abstract class SafeMemoryHandle : SafeBaseHandle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeMemoryHandle"/> class with a specified handle, length, and
        /// ownership flag.
        /// </summary>
        /// <param name="handle">The memory handle to be managed.</param>
        /// <param name="length">The length of the memory block. Must be greater than zero.</param>
        /// <param name="ownsHandle">A value indicating whether the <see cref="SafeMemoryHandle"/> should reliably release the handle during the
        /// finalization phase.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="length"/> is less than or equal to zero.</exception>
        protected SafeMemoryHandle(IntPtr handle, int length, bool ownsHandle) : base(handle, ownsHandle)
        {
            Length = length >= 0 ? length : throw new ArgumentOutOfRangeException(nameof(length));
        }

        /// <summary>
        /// Reallocates the memory block to the specified size.
        /// </summary>
        /// <param name="length"></param>
        /// <exception cref="OutOfMemoryException"></exception>
        internal virtual void ReAlloc(int length) => throw new NotImplementedException();

        /// <summary>
        /// Converts the handle to a string using the ANSI character set.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal string? ToStringUni(int offset = 0) => Marshal.PtrToStringUni(handle + offset);

        /// <summary>
        /// Converts the handle to a structure of type <typeparamref name="T"/>. The structure must be a value type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal T ToStructure<T>(int offset = 0) where T : struct => Marshal.PtrToStructure<T>(handle + offset);

        /// <summary>
        /// Converts the handle to a structure of type <typeparamref name="T"/>. The structure must be a value type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="structure"></param>
        /// <param name="fDeleteOld"></param>
        /// <param name="offset"></param>
        internal SafeMemoryHandle FromStructure<T>(T structure, bool fDeleteOld, int offset = 0) where T : struct
        {
            Marshal.StructureToPtr(structure, handle + offset, fDeleteOld);
            return this;
        }

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal long ReadInt64(int offset = 0) => Marshal.ReadInt64(handle, offset);

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal int ReadInt32(int offset = 0) => Marshal.ReadInt32(handle, offset);

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal short ReadInt16(int offset = 0) => Marshal.ReadInt16(handle, offset);

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal byte ReadByte(int offset = 0) => Marshal.ReadByte(handle, offset);

        /// <summary>
        /// Clears the memory block by setting all bytes to zero.
        /// </summary>
        internal unsafe void Clear() => new Span<byte>(handle.ToPointer(), Length).Clear();

        /// <summary>
        /// Converts the handle to a <see cref="PWSTR"/> structure.
        /// </summary>
        /// <returns></returns>
        internal PWSTR ToPWSTR() => new(handle);

        /// <summary>
        /// Releases the handle and frees the allocated memory.
        /// </summary>
        /// <returns></returns>
        protected abstract override bool ReleaseHandle();

        /// <summary>
        /// Gets the size of the allocated memory block.
        /// </summary>
        internal int Length { get; private protected set; }

        /// <summary>
        /// Represents a null safe handle for memory.
        /// This is a bit cheeky, but it works.
        /// </summary>
        internal static readonly SafeMemoryHandle Null = new SafeWtsHandle(IntPtr.Zero, 0, false);
    }
}

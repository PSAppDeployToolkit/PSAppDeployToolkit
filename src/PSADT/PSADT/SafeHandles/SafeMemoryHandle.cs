using System;
using System.Runtime.CompilerServices;
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
            // Pin unconditionally to be safe across both .NET Framework 4.7.2 and .NET 8.
            // Marshal.StructureToPtr can be unsafe when structs contain reference types.
            var gchandle = GCHandle.Alloc(structure, GCHandleType.Pinned);
            try
            {
                Marshal.StructureToPtr(structure, handle + offset, fDeleteOld);
            }
            finally
            {
                gchandle.Free();
            }
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
        /// Writes a 64-bit signed integer to the specified memory location.
        /// </summary>
        /// <remarks>This method uses the <see cref="System.Runtime.InteropServices.Marshal.WriteInt64"/>
        /// function to write the integer value to unmanaged memory. Ensure that the memory location is valid and
        /// accessible to avoid undefined behavior.</remarks>
        /// <param name="value">The 64-bit signed integer to write.</param>
        /// <param name="offset">The byte offset from the start of the memory location where the value will be written. Defaults to 0.</param>
        internal void WriteInt64(long value, int offset = 0) => Marshal.WriteInt64(handle, offset, value);

        /// <summary>
        /// Writes a 32-bit integer value to a specific location in unmanaged memory.
        /// </summary>
        /// <remarks>This method writes the specified integer value to the unmanaged memory block
        /// referenced by the handle. Ensure that the memory location is valid and that the offset does not exceed the
        /// bounds of the allocated memory.</remarks>
        /// <param name="value">The 32-bit integer value to write.</param>
        /// <param name="offset">The byte offset from the start of the memory location where the value will be written. Defaults to 0.</param>
        internal void WriteInt32(int value, int offset = 0) => Marshal.WriteInt32(handle, offset, value);

        /// <summary>
        /// Writes a 16-bit signed integer to the specified offset within the unmanaged memory block.
        /// </summary>
        /// <remarks>This method writes the specified <paramref name="value"/> to the unmanaged memory
        /// block associated with the current instance. Ensure that the offset is within the bounds of the allocated
        /// memory to avoid memory corruption.</remarks>
        /// <param name="value">The 16-bit signed integer to write.</param>
        /// <param name="offset">The byte offset within the unmanaged memory block where the value will be written. Defaults to 0.</param>
        internal void WriteInt16(short value, int offset = 0) => Marshal.WriteInt16(handle, offset, value);

        /// <summary>
        /// Writes a byte value to a specific location in unmanaged memory.
        /// </summary>
        /// <remarks>This method writes directly to unmanaged memory, which can lead to memory corruption
        /// if not used carefully. Ensure that the offset is within the bounds of the allocated memory.</remarks>
        /// <param name="value">The byte value to write.</param>
        /// <param name="offset">The byte offset from the start of the memory location where the value will be written. Defaults to 0.</param>
        internal void WriteByte(byte value, int offset = 0) => Marshal.WriteByte(handle, offset, value);

        /// <summary>
        /// Clears the memory block by setting all bytes to zero.
        /// </summary>
        internal unsafe void Clear() => new Span<byte>(handle.ToPointer(), Length).Clear();

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

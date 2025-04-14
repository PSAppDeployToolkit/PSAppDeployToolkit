using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.Foundation;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for a handle to a block of memory allocated from the Marshal class.
    /// </summary>
    internal abstract class SafeMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeMemoryHandle"/> class with the specified handle and size.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="length"></param>
        /// <param name="ownsHandle"></param>
        protected SafeMemoryHandle(IntPtr handle, int length, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
            Length = length;
        }

        /// <summary>
        /// Reallocates the memory block to the specified size.
        /// </summary>
        /// <param name="length"></param>
        /// <exception cref="OutOfMemoryException"></exception>
        internal abstract void Reallocate(int length);

        /// <summary>
        /// Converts the handle to a string using the ANSI character set.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal string? ToStringUni(int offset = 0)
        {
            return Marshal.PtrToStringUni(handle + offset);
        }   

        /// <summary>
        /// Converts the handle to a structure of type <typeparamref name="T"/>. The structure must be a value type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal T ToStructure<T>(int offset = 0) where T : struct
        {
            return Marshal.PtrToStructure<T>(handle + offset);
        }

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal long ReadInt64(int offset = 0)
        {
            return Marshal.ReadInt64(handle + offset);
        }

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal int ReadInt32(int offset = 0)
        {
            return Marshal.ReadInt32(handle + offset);
        }

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal short ReadInt16(int offset = 0)
        {
            return Marshal.ReadInt16(handle + offset);
        }

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal byte ReadByte(int offset = 0)
        {
            return Marshal.ReadByte(handle + offset);
        }   

        /// <summary>
        /// Clears the memory block by setting all bytes to zero.
        /// </summary>
        internal unsafe void Clear()
        {
            new Span<byte>(handle.ToPointer(), Length).Clear();
        }

        /// <summary>
        /// Converts the handle to a <see cref="PWSTR"/> structure.
        /// </summary>
        /// <returns></returns>
        internal PWSTR ToPWSTR()
        {
            return new PWSTR(handle);
        }

        /// <summary>
        /// Releases the handle and frees the allocated memory.
        /// </summary>
        /// <returns></returns>
        protected abstract override bool ReleaseHandle();

        /// <summary>
        /// Gets the size of the allocated memory block.
        /// </summary>
        public int Length { get; protected set; }
    }
}

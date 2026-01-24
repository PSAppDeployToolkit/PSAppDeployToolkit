using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.LibraryInterfaces.Extensions;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Provides a base class for managing and safely releasing unmanaged memory handles. Ensures that memory is
    /// properly released and offers utility methods for reading, writing, and interpreting the underlying memory
    /// region.
    /// </summary>
    /// <remarks>This class is intended for use as a base for safe handle implementations that manage
    /// unmanaged memory blocks. It provides methods for reading and writing primitive values, converting memory to and
    /// from structures, and exposing the memory as spans for efficient access. Inheritors must implement the
    /// ReleaseHandle method to define how the memory is freed. Instances of this class are not thread safe unless
    /// otherwise specified by derived types.</remarks>
    /// <typeparam name="TSelf">The derived type that inherits from this class.</typeparam>
    internal abstract class SafeMemoryHandle<TSelf> : SafeHandleZeroOrMinusOneIsInvalid where TSelf : SafeMemoryHandle<TSelf>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeMemoryHandle{TSelf}"/> class with a specified handle, length, and
        /// ownership flag.
        /// </summary>
        /// <param name="handle">The memory handle to be managed.</param>
        /// <param name="length">The length of the memory block. Must be greater than zero.</param>
        /// <param name="ownsHandle">A value indicating whether the <see cref="SafeMemoryHandle{TSelf}"/> should reliably release the handle during the
        /// finalization phase.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="length"/> is less than or equal to zero.</exception>
        protected SafeMemoryHandle(IntPtr handle, int length, bool ownsHandle) : base(ownsHandle)
        {
            Length = length >= 0 ? length : throw new ArgumentOutOfRangeException(nameof(length));
            SetHandle(handle);
        }

        /// <summary>
        /// Reallocates the memory block to the specified size.
        /// </summary>
        /// <param name="length"></param>
        /// <exception cref="OutOfMemoryException"></exception>
        internal virtual void ReAlloc(int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts the handle to a structure of type <typeparamref name="T"/>. The structure must be a value type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="structure"></param>
        /// <param name="fDeleteOld"></param>
        /// <param name="offset"></param>
        internal TSelf FromStructure<T>(T structure, bool fDeleteOld, int offset = 0) where T : struct
        {
            ConfirmStateValidity(offset);
            Marshal.StructureToPtr(structure, handle + offset, fDeleteOld);
            return (TSelf)this;
        }

        /// <summary>
        /// Converts the handle to a string using the ANSI character set.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal string? ToStringUni(int offset = 0)
        {
            ConfirmStateValidity(offset);
            return Marshal.PtrToStringUni(handle + offset)?.Trim() is string result && !string.IsNullOrWhiteSpace(result) ? result : null;
        }

        /// <summary>
        /// Returns a reference to a structure of type <typeparamref name="T"/> located at the specified offset from the
        /// underlying handle.
        /// </summary>
        /// <remarks>The caller is responsible for ensuring that the memory at the specified offset is
        /// valid and properly aligned for type <typeparamref name="T"/>. Accessing invalid or misaligned memory may
        /// result in undefined behavior.</remarks>
        /// <typeparam name="T">The value type to interpret the memory as. Must be an unmanaged structure.</typeparam>
        /// <param name="offset">The byte offset from the start of the handle at which to read the structure. Defaults to 0.</param>
        /// <returns>A reference to the structure of type <typeparamref name="T"/> at the specified offset.</returns>
        internal ref readonly T AsReadOnlyStructure<T>(int offset = 0) where T : unmanaged
        {
            ConfirmStateValidity(offset);
            return ref handle.AsReadOnlyStructure<T>(offset);
        }

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal long ReadInt64(int offset = 0)
        {
            ConfirmStateValidity(offset);
            return Marshal.ReadInt64(handle, offset);
        }

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal int ReadInt32(int offset = 0)
        {
            ConfirmStateValidity(offset);
            return Marshal.ReadInt32(handle, offset);
        }

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal short ReadInt16(int offset = 0)
        {
            ConfirmStateValidity(offset);
            return Marshal.ReadInt16(handle, offset);
        }

        /// <summary>
        /// Reads a byte from the memory block at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal byte ReadByte(int offset = 0)
        {
            ConfirmStateValidity(offset);
            return Marshal.ReadByte(handle, offset);
        }

        /// <summary>
        /// Writes a 64-bit signed integer to the specified memory location.
        /// </summary>
        /// <remarks>This method writes the specified <paramref name="value"/> to the unmanaged memory
        /// block associated with the current instance. Ensure that the offset is within the bounds of the allocated
        /// memory to avoid memory corruption.</remarks>
        /// <param name="value">The 64-bit signed integer to write.</param>
        /// <param name="offset">The byte offset from the start of the memory location where the value will be written. Defaults to 0.</param>
        internal TSelf WriteInt64(long value, int offset = 0)
        {
            ConfirmStateValidity(offset);
            Marshal.WriteInt64(handle, offset, value);
            return (TSelf)this;
        }

        /// <summary>
        /// Writes a 32-bit integer value to a specific location in unmanaged memory.
        /// </summary>
        /// <remarks>This method writes the specified <paramref name="value"/> to the unmanaged memory
        /// block associated with the current instance. Ensure that the offset is within the bounds of the allocated
        /// memory to avoid memory corruption.</remarks>
        /// <param name="value">The 32-bit integer value to write.</param>
        /// <param name="offset">The byte offset from the start of the memory location where the value will be written. Defaults to 0.</param>
        internal TSelf WriteInt32(int value, int offset = 0)
        {
            ConfirmStateValidity(offset);
            Marshal.WriteInt32(handle, offset, value);
            return (TSelf)this;
        }

        /// <summary>
        /// Writes a 16-bit signed integer to the specified offset within the unmanaged memory block.
        /// </summary>
        /// <remarks>This method writes the specified <paramref name="value"/> to the unmanaged memory
        /// block associated with the current instance. Ensure that the offset is within the bounds of the allocated
        /// memory to avoid memory corruption.</remarks>
        /// <param name="value">The 16-bit signed integer to write.</param>
        /// <param name="offset">The byte offset within the unmanaged memory block where the value will be written. Defaults to 0.</param>
        internal TSelf WriteInt16(short value, int offset = 0)
        {
            ConfirmStateValidity(offset);
            Marshal.WriteInt16(handle, offset, value);
            return (TSelf)this;
        }

        /// <summary>
        /// Writes a byte value to a specific location in unmanaged memory.
        /// </summary>
        /// <remarks>This method writes the specified <paramref name="value"/> to the unmanaged memory
        /// block associated with the current instance. Ensure that the offset is within the bounds of the allocated
        /// memory to avoid memory corruption.</remarks>
        /// <param name="value">The byte value to write.</param>
        /// <param name="offset">The byte offset from the start of the memory location where the value will be written. Defaults to 0.</param>
        internal TSelf WriteByte(byte value, int offset = 0)
        {
            ConfirmStateValidity(offset);
            Marshal.WriteByte(handle, offset, value);
            return (TSelf)this;
        }

        /// <summary>
        /// Writes the provided data to the allocated memory.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal TSelf Write(byte[] data, int startIndex = 0)
        {
            ConfirmStateValidity(startIndex);
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (data.Length == 0)
            {
                throw new ArgumentException("Code length cannot be zero.", nameof(data));
            }
            if (data.Length + startIndex > Length)
            {
                throw new ArgumentException($"Data length [{data.Length}] exceeds allocated memory length [{Length}].", nameof(data));
            }
            Marshal.Copy(data, startIndex, handle, data.Length - startIndex);
            return (TSelf)this;
        }

        /// <summary>
        /// Returns a read-only span of bytes representing the memory region, starting at the specified offset.
        /// </summary>
        /// <remarks>The returned span reflects the contents of the underlying memory. Modifying the
        /// memory through other means will be visible in the span. The caller is responsible for ensuring that the
        /// memory remains valid for the lifetime of the span.</remarks>
        /// <typeparam name="T">The unmanaged value type to interpret the memory as.</typeparam>
        /// <param name="offset">The zero-based byte offset at which to begin the span. Must be greater than or equal to 0 and less than or
        /// equal to the length of the memory region.</param>
        /// <returns>A read-only span of type T beginning at the specified offset and extending to the end of the memory region.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the handle has been disposed or is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified offset is less than 0 or greater than the length of the memory region.</exception>
        /// <exception cref="ArgumentException">Thrown if offset is not aligned to the size of T.</exception>
        internal ReadOnlySpan<T> AsReadOnlySpan<T>(int offset = 0) where T : unmanaged
        {
            ConfirmStateValidity(offset);
            int length = (Length - offset) / Marshal.SizeOf<T>();
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset is out of bounds of the allocated memory.");
            }
            if ((Length - offset) % Marshal.SizeOf<T>() != 0)
            {
                throw new ArgumentException("Offset must be aligned to the size of the type T.", nameof(offset));
            }
            unsafe
            {
                return new((void*)(handle + offset), length);
            }
        }

        /// <summary>
        /// Clears the memory block by setting all bytes to zero.
        /// </summary>
        internal void Clear()
        {
            unsafe
            {
                new Span<byte>((void*)handle, Length).Clear();
            }
        }

        /// <summary>
        /// Validates that the current object is in a usable state and that the specified offset is within the valid
        /// range.
        /// </summary>
        /// <param name="offset">The offset to validate. Must be zero or greater.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the object is in an invalid or closed state.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if offset is less than zero.</exception>
        private void ConfirmStateValidity(int offset)
        {
            if (IsInvalid || IsClosed)
            {
                throw new ObjectDisposedException(typeof(TSelf).Name);
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");
            }
        }

        /// <summary>
        /// Releases the handle and frees the allocated memory.
        /// </summary>
        /// <returns></returns>
        protected abstract override bool ReleaseHandle();

        /// <summary>
        /// Gets the size of the allocated memory block.
        /// </summary>
        internal readonly int Length;
    }
}

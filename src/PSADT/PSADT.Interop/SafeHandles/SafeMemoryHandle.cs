using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.Interop.Extensions;

namespace PSADT.Interop.SafeHandles
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
        private protected SafeMemoryHandle(nint handle, int length, bool ownsHandle) : base(ownsHandle)
        {
            Length = length.ThrowIfNegative();
            SetHandle(handle.ThrowIfZeroOrMinusOne());
        }

        /// <summary>
        /// Converts the handle to a string using the Unicode character set.
        /// </summary>
        /// <param name="offset">The byte offset from the start of the handle.</param>
        /// <returns>The string value at the specified offset.</returns>
        internal string ToStringUni(int offset = 0)
        {
            return (this.ThrowIfNullOrInvalid().handle + offset.ThrowIfNegative()).ToManagedString((Length - offset) / sizeof(char));
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
            return ref this.ThrowIfNullOrInvalid().handle.AsReadOnlyStructure<T>(offset.ThrowIfNegative());
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the memory location represented by the handle, starting at the specified
        /// byte offset.
        /// </summary>
        /// <remarks>The handle must be valid and initialized before calling this method. Reading from an
        /// invalid or uninitialized handle may result in an exception or undefined behavior.</remarks>
        /// <param name="offset">The zero-based byte offset, in bytes, from the start of the memory location at which to begin reading. Must
        /// be non-negative.</param>
        /// <returns>The 64-bit signed integer value read from the specified memory location.</returns>
        internal long ReadInt64(int offset = 0)
        {
            return Marshal.ReadInt64(this.ThrowIfNullOrInvalid().handle, offset.ThrowIfNegative());
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the memory location represented by the handle, starting at the specified
        /// byte offset.
        /// </summary>
        /// <remarks>The handle must be in a valid state before calling this method. An exception may be
        /// thrown if the handle is not properly initialized or if the offset is out of range.</remarks>
        /// <param name="offset">The zero-based byte offset, in bytes, from the start of the memory location at which to begin reading. Must
        /// be non-negative.</param>
        /// <returns>The 32-bit signed integer value read from the specified memory location.</returns>
        internal int ReadInt32(int offset = 0)
        {
            return Marshal.ReadInt32(this.ThrowIfNullOrInvalid().handle, offset.ThrowIfNegative());
        }

        /// <summary>
        /// Reads a 16-bit signed integer from the memory region at the specified byte offset.
        /// </summary>
        /// <remarks>The memory region must be in a valid state for reading. An exception may be thrown if
        /// the offset is out of bounds or if the handle is invalid.</remarks>
        /// <param name="offset">The zero-based byte offset from the start of the memory region at which to read the 16-bit integer. Must be
        /// non-negative.</param>
        /// <returns>The 16-bit signed integer value read from the specified offset.</returns>
        internal short ReadInt16(int offset = 0)
        {
            return Marshal.ReadInt16(this.ThrowIfNullOrInvalid().handle, offset.ThrowIfNegative());
        }

        /// <summary>
        /// Reads a byte value from the memory location at the specified offset within the handle.
        /// </summary>
        /// <remarks>The memory handle must be in a valid state before calling this method. An exception
        /// may be thrown if the handle is not properly initialized or if the offset is out of range.</remarks>
        /// <param name="offset">The zero-based byte offset from the start of the memory handle at which to read. Must be greater than or
        /// equal to zero.</param>
        /// <returns>The byte value read from the specified offset in the memory handle.</returns>
        internal byte ReadByte(int offset = 0)
        {
            return Marshal.ReadByte(this.ThrowIfNullOrInvalid().handle, offset.ThrowIfNegative());
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
            Marshal.WriteInt64(this.ThrowIfNullOrInvalid().handle, offset.ThrowIfNegative(), value);
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
            Marshal.WriteInt32(this.ThrowIfNullOrInvalid().handle, offset.ThrowIfNegative(), value);
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
            Marshal.WriteInt16(this.ThrowIfNullOrInvalid().handle, offset.ThrowIfNegative(), value);
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
            Marshal.WriteByte(this.ThrowIfNullOrInvalid().handle, offset.ThrowIfNegative(), value);
            return (TSelf)this;
        }

        /// <summary>
        /// Writes the specified byte array to the allocated memory starting at the given index.
        /// </summary>
        /// <param name="data">The byte array containing the data to write. This array must not be null or empty.</param>
        /// <param name="startIndex">The zero-based index in the byte array at which to begin writing data. The default value is 0.</param>
        /// <returns>The current instance of the class, enabling method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="data"/> parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="data"/> parameter is empty, or if the combined length of <paramref
        /// name="data"/> and <paramref name="startIndex"/> exceeds the allocated memory length.</exception>
        internal TSelf Write(byte[] data, int startIndex = 0)
        {
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
                throw new InvalidOperationException($"Data length [{data.Length}] exceeds allocated memory length [{Length}].");
            }
            Marshal.Copy(data, startIndex.ThrowIfNegative(), this.ThrowIfNullOrInvalid().handle, data.Length - startIndex);
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
        /// <exception cref="InvalidOperationException">Thrown if offset is not aligned to the size of T.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Implementing this here will just make a mess.")]
        internal ReadOnlySpan<T> AsReadOnlySpan<T>(int offset = 0) where T : unmanaged
        {
            int length = ((Length - offset.ThrowIfNegative()) / Marshal.SizeOf<T>()).ThrowIfNegative();
            if ((Length - offset) % Marshal.SizeOf<T>() != 0)
            {
                throw new InvalidOperationException("Offset must be aligned to the size of the type T.");
            }
            return (this.ThrowIfNullOrInvalid().handle + offset).AsReadOnlySpan<T>(length);
        }

        /// <summary>
        /// Clears the memory block by setting all bytes to zero.
        /// </summary>
        internal void Clear()
        {
            unsafe
            {
                new Span<byte>((void*)this.ThrowIfNullOrInvalid().handle, Length).Clear();
            }
        }

        /// <summary>
        /// Releases the handle associated with the resource, ensuring that unmanaged resources are properly freed.
        /// </summary>
        /// <remarks>This method must be implemented by derived classes to specify the logic for releasing
        /// the handle. It is called when the handle is no longer needed, and proper implementation is essential to
        /// prevent resource leaks.</remarks>
        /// <returns>true if the handle is released successfully; otherwise, false.</returns>
        protected abstract override bool ReleaseHandle();

        /// <summary>
        /// Gets the size of the allocated memory block.
        /// </summary>
        internal readonly int Length;
    }
}

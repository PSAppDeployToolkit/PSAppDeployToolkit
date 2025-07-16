using System;
using System.Runtime.InteropServices;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for a handle to a block of memory allocated with <see cref="Marshal.AllocHGlobal(int)"/>.
    /// </summary>
    internal sealed class SafeHGlobalHandle : SafeMemoryHandle
    {
        /// <summary>
        /// Allocates a block of memory of the specified size and returns a <see cref="SafeHGlobalHandle"/> that wraps the allocated memory.
        /// This is fully implemented here as static virtual members don't come until C# 11 (.NET 7).
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        /// <exception cref="OutOfMemoryException"></exception>
        internal static SafeHGlobalHandle Alloc(int length)
        {
            var handle = Marshal.AllocHGlobal(length);
            if (handle == IntPtr.Zero)
            {
                throw new OutOfMemoryException("Failed to allocate memory.");
            }
            return new(handle, length, true);
        }

        /// <summary>
        /// Allocates a block of memory for the specified string and returns a <see cref="SafeHGlobalHandle"/> that wraps the allocated memory.
        /// This is fully implemented here as static virtual members don't come until C# 11 (.NET 7).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static SafeHGlobalHandle StringToUni(string input) => new(Marshal.StringToHGlobalUni(input), (input.Length + 1) * 2, true);

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeHGlobalHandle"/> class with the specified handle and size.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="length"></param>
        /// <param name="ownsHandle"></param>
        private SafeHGlobalHandle(IntPtr handle, int length, bool ownsHandle) : base(handle, length, ownsHandle)
        {
        }

        /// <summary>
        /// Reallocates the memory block to the specified size.
        /// </summary>
        /// <param name="length"></param>
        /// <exception cref="OutOfMemoryException"></exception>
        internal override void ReAlloc(int length)
        {
            if (length == Length)
            {
                return;
            }
            SetHandle(Marshal.ReAllocHGlobal(handle, new IntPtr(length)));
            Length = length;
        }

        /// <summary>
        /// Releases the handle and frees the allocated memory.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}

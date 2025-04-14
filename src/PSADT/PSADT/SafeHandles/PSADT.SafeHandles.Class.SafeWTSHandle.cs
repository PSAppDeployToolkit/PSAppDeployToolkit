using System;
using PSADT.LibraryInterfaces;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for an environment block handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeWTSHandle : SafeMemoryHandle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeWTSHandle"/> class with the specified handle and ownership.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ownsHandle"></param>
        internal SafeWTSHandle(IntPtr handle, int length, bool ownsHandle) : base(handle, length, ownsHandle)
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Reallocates the memory block to the specified size.
        /// </summary>
        /// <param name="length"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal override void ReAlloc(int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            WtsApi32.WTSFreeMemory(ref handle);
            return true;
        }
    }
}

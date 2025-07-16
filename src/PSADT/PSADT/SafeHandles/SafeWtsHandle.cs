using System;
using Windows.Win32;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for an environment block handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeWtsHandle(IntPtr handle, int length, bool ownsHandle) : SafeMemoryHandle(handle, length, ownsHandle)
    {
        /// <summary>
        /// Reallocates the memory block to the specified size.
        /// </summary>
        /// <param name="length"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal override void ReAlloc(int length) => throw new NotImplementedException();

        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override unsafe bool ReleaseHandle()
        {
            if (handle != default && IntPtr.Zero != handle)
            {
                PInvoke.WTSFreeMemory(handle.ToPointer());
                handle = default;
            }
            return true;
        }
    }
}

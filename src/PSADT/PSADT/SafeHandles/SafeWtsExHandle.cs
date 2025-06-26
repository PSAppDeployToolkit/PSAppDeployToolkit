using System;
using PSADT.LibraryInterfaces;
using Windows.Win32.System.RemoteDesktop;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for an environment block handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeWtsExHandle(IntPtr handle, WTS_TYPE_CLASS type, int length, bool ownsHandle) : SafeMemoryHandle(handle, length, ownsHandle)
    {
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
            return WtsApi32.WTSFreeMemoryEx(type, ref handle, (uint)Length);
        }
    }
}

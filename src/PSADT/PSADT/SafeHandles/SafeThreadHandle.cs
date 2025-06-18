using System;
using PSADT.LibraryInterfaces;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for a thread handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeThreadHandle(IntPtr handle, bool ownsHandle) : SafeBaseHandle(handle, ownsHandle)
    {
        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            return Kernel32.CloseHandle(ref handle);
        }
    }
}

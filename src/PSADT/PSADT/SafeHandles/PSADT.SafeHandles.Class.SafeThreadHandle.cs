using System;
using Microsoft.Win32.SafeHandles;
using PSADT.LibraryInterfaces;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for a thread handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeThreadHandle"/> class with the specified handle and ownership.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ownsHandle"></param>
        internal SafeThreadHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
        }

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

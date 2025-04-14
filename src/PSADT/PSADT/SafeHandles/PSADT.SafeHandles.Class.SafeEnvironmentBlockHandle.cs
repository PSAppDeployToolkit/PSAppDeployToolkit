using System;
using Microsoft.Win32.SafeHandles;
using PSADT.LibraryInterfaces;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for an environment block handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeEnvironmentBlockHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeEnvironmentBlockHandle"/> class with the specified handle and ownership.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ownsHandle"></param>
        internal SafeEnvironmentBlockHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            return UserEnv.DestroyEnvironmentBlock(ref handle);
        }
    }
}

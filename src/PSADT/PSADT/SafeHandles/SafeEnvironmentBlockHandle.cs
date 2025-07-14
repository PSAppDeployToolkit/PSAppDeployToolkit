using System;
using PSADT.LibraryInterfaces;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for an environment block handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeEnvironmentBlockHandle(IntPtr handle, bool ownsHandle) : SafeBaseHandle(handle, ownsHandle)
    {
        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle() => UserEnv.DestroyEnvironmentBlock(ref handle);

        /// <summary>
        /// Represents a null safe handle for an environment block.
        /// </summary>
        internal static readonly SafeEnvironmentBlockHandle Null = new SafeEnvironmentBlockHandle(IntPtr.Zero, false);
    }
}

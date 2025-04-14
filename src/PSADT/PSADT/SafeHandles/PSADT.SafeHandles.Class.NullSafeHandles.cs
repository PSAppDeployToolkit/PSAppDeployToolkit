using System;
using System.Runtime.InteropServices;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a class that contains null safe handles.
    /// </summary>
    internal static class NullSafeHandles
    {
        /// <summary>
        /// Represents a null safe handle for an environment block.
        /// </summary>
        internal static readonly SafeEnvironmentBlockHandle NullSafeEnvironmentBlockHandle = new SafeEnvironmentBlockHandle(IntPtr.Zero, false);

        /// <summary>
        /// Represents a null safe handle for a thread.
        /// </summary>
        internal static readonly SafeHandle NullSafeHandle = (SafeHandle)NullSafeEnvironmentBlockHandle;
    }
}

using System;
using Microsoft.Win32.SafeHandles;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Provides a SafeHandle implementation that encapsulates a native handle without taking ownership or releasing it
    /// when disposed or finalized.
    /// </summary>
    /// <remarks>Use SafeNoReleaseHandle when you need to wrap a native handle for interop or API
    /// compatibility, but the handle's lifetime is managed externally. This type does not release or close the
    /// underlying handle; disposing or finalizing the SafeNoReleaseHandle instance has no effect on the native
    /// resource.</remarks>
    internal sealed class SafeNoReleaseHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the SafeNoReleaseHandle class with the specified handle value.
        /// </summary>
        /// <remarks>This constructor does not take ownership of the handle and will not release it when
        /// the SafeNoReleaseHandle is disposed or finalized. Use this when the handle's lifetime is managed
        /// elsewhere.</remarks>
        /// <param name="handle">The native handle to be encapsulated by the SafeNoReleaseHandle instance.</param>
        internal SafeNoReleaseHandle(IntPtr handle) : base(false)
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Releases the handle associated with the current instance.
        /// </summary>
        /// <returns><see langword="true"/> if the handle was successfully released; otherwise, <see langword="false"/>.</returns>
        protected override bool ReleaseHandle()
        {
            return true;
        }
    }
}

using System;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a safe handle that does not release the underlying handle resource.
    /// </summary>
    /// <remarks>This class is intended for scenarios where the handle should not be released or closed when
    /// the object is disposed or finalized. It overrides the release behavior to ensure the handle remains
    /// intact.</remarks>
    /// <param name="handle"></param>
    internal sealed class SafeNoReleaseHandle(IntPtr handle) : SafeBaseHandle(handle, false)
    {
        /// <summary>
        /// Releases the handle associated with the current instance.
        /// </summary>
        /// <returns><see langword="true"/> if the handle was successfully released; otherwise, <see langword="false"/>.</returns>
        protected override bool ReleaseHandle() => true;
    }
}

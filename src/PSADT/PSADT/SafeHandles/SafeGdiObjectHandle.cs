using System;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a safe handle for a GDI object, ensuring proper release of resources.
    /// </summary>
    /// <remarks>This class provides a managed wrapper for a GDI object handle, ensuring that the handle is
    /// released correctly when no longer needed. It inherits from <see cref="SafeBaseHandle"/> and overrides the <see
    /// cref="ReleaseHandle"/> method to implement the specific release logic for GDI objects.</remarks>
    /// <param name="handle"></param>
    /// <param name="ownsHandle"></param>
    internal sealed class SafeGdiObjectHandle(IntPtr handle, bool ownsHandle) : SafeBaseHandle(handle, ownsHandle)
    {
        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override unsafe bool ReleaseHandle()
        {
            if (handle == default || IntPtr.Zero == handle)
            {
                return true;
            }
            var res = PInvoke.DeleteObject((HGDIOBJ)handle);
            if (!res)
            {
                throw new InvalidOperationException("Failed to delete GDI object handle.");
            }
            handle = default;
            return res;
        }
    }
}

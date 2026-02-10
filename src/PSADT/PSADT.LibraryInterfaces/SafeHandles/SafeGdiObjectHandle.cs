using System;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for a Windows GDI object, ensuring that the underlying handle is released reliably.
    /// </summary>
    /// <remarks>This class is intended for internal use when working with unmanaged GDI resources. It ensures
    /// that the associated GDI object is properly deleted when the handle is no longer needed, helping to prevent
    /// resource leaks. The handle is considered invalid if it is zero or minus one.</remarks>
    internal sealed class SafeGdiObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the SafeGdiObjectHandle class with the specified handle and ownership value.
        /// </summary>
        /// <param name="handle">The native handle to a GDI object to be wrapped by this instance.</param>
        /// <param name="ownsHandle">true to reliably release the handle during finalization; false to prevent the handle from being released.</param>
        internal SafeGdiObjectHandle(nint handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            if (default == handle)
            {
                return true;
            }
            BOOL res;
            unsafe
            {
                res = PInvoke.DeleteObject((HGDIOBJ)handle);
            }
            if (!res)
            {
                throw new InvalidOperationException("Failed to delete GDI object handle.");
            }
            handle = default;
            return res;
        }
    }
}

using System;
using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for buffers allocated by Windows network management APIs that must be freed using
    /// NetApiBufferFree.
    /// </summary>
    /// <remarks>This handle ensures that the associated unmanaged memory is released using NetApiBufferFree
    /// when the handle is disposed or finalized. Use this class to safely manage buffers returned by Windows network
    /// management functions that require explicit deallocation.</remarks>
    /// <param name="handle">The pointer to the unmanaged buffer to be managed by the handle.</param>
    /// <param name="length">The length, in bytes, of the buffer referenced by the handle.</param>
    /// <param name="ownsHandle">true to reliably release the handle during finalization; otherwise, false.</param>
    internal sealed class SafeNetApiBufferFreeHandle(IntPtr handle, int length, bool ownsHandle) : SafeMemoryHandle<SafeNetApiBufferFreeHandle>(handle, length, ownsHandle)
    {
        /// <summary>
        /// Releases the handle associated with the unmanaged resource.
        /// </summary>
        /// <remarks>This method is called by the runtime to free the unmanaged memory buffer when the
        /// handle is no longer needed. If the handle is already invalid or zero, the method returns true without
        /// performing any operation. An exception is thrown if the underlying buffer cannot be freed
        /// successfully.</remarks>
        /// <returns>true if the handle is released successfully; otherwise, false.</returns>
        protected override unsafe bool ReleaseHandle()
        {
            if (handle == default || IntPtr.Zero == handle)
            {
                return true;
            }
            WIN32_ERROR res = (WIN32_ERROR)PInvoke.NetApiBufferFree(handle.ToPointer());
            if (res != PInvoke.NERR_Success)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            handle = default;
            return true;
        }
    }
}

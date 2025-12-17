using System;
using System.ComponentModel;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Represents a safe handle for memory allocated by the Local Security Authority (LSA) that requires deallocation
    /// using the <see cref="PInvoke.LsaFreeMemory"/> function.
    /// </summary>
    /// <remarks>This class ensures that memory allocated by the LSA is properly released when the handle is
    /// disposed or finalized. It is a specialized implementation of <see cref="SafeMemoryHandle"/> designed to work
    /// with LSA memory management functions.</remarks>
    /// <param name="handle"></param>
    /// <param name="ownsHandle"></param>
    internal sealed class SafeLsaFreeMemoryHandle(IntPtr handle, bool ownsHandle) : SafeMemoryHandle(handle, 0, ownsHandle)
    {
        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            if (handle == default || IntPtr.Zero == handle)
            {
                return true;
            }
            NTSTATUS res;
            unsafe
            {
                res = PInvoke.LsaFreeMemory((void*)handle);
            }
            if (res != NTSTATUS.STATUS_SUCCESS)
            {
                throw new Win32Exception((int)PInvoke.LsaNtStatusToWinError(res));
            }
            handle = default;
            return true;
        }
    }
}

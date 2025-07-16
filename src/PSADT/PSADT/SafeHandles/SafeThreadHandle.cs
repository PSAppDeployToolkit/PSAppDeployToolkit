using System;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for a thread handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeThreadHandle(IntPtr handle, bool ownsHandle) : SafeBaseHandle(handle, ownsHandle)
    {
        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            if (handle == default || IntPtr.Zero != handle)
            {
                return true;
            }
            var res = PInvoke.CloseHandle((HANDLE)handle);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            handle = default;
            return res;
        }
    }
}

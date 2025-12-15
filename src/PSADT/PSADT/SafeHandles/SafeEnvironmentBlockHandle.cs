using System;
using Microsoft.Win32.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for a native environment block, ensuring that the underlying handle is released reliably.
    /// </summary>
    /// <remarks>This class is intended for internal use when working with native environment blocks. It
    /// ensures that the environment block handle is released appropriately to prevent resource leaks. The handle is
    /// considered invalid if it is zero or minus one.</remarks>
    internal sealed class SafeEnvironmentBlockHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the SafeEnvironmentBlockHandle class with the specified handle and ownership
        /// value.
        /// </summary>
        /// <param name="handle">The native handle to the environment block to be managed.</param>
        /// <param name="ownsHandle">true to reliably release the handle during the finalization phase; false to prevent the handle from being
        /// released.</param>
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
            if (handle == default || IntPtr.Zero == handle)
            {
                return true;
            }
            BOOL res;
            unsafe
            {
                res = PInvoke.DestroyEnvironmentBlock((void*)handle);
            }
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            handle = default;
            return res;
        }
    }
}

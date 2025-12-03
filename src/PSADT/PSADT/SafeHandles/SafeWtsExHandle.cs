using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.RemoteDesktop;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for an environment block handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeWtsExHandle(IntPtr handle, WTS_TYPE_CLASS type, int length, bool ownsHandle) : SafeMemoryHandle(handle, length, ownsHandle)
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
            BOOL res;
            unsafe
            {
                res = PInvoke.WTSFreeMemoryEx(type, (void*)handle, (uint)(Length / WtsTypeClassSizes[(int)type]));
            }
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            handle = default;
            return res;
        }

        /// <summary>
        /// Represents a collection of sizes for various WTS (Windows Terminal Services) type classes.
        /// </summary>
        /// <remarks>This collection contains the sizes of the structures <see cref="WTS_PROCESS_INFOW"/>,
        /// <see cref="WTS_PROCESS_INFO_EXW"/>, and <see cref="WTS_SESSION_INFO_1W"/>. These sizes are
        /// used for operations involving Windows Terminal Services data structures.</remarks>
        private static readonly ReadOnlyCollection<int> WtsTypeClassSizes = new([Marshal.SizeOf<WTS_PROCESS_INFOW>(), Marshal.SizeOf<WTS_PROCESS_INFO_EXW>(), Marshal.SizeOf<WTS_SESSION_INFO_1W>()]);
    }
}

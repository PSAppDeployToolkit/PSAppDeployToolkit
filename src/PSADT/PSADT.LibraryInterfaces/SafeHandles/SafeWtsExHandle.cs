using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.RemoteDesktop;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for Windows Terminal Services (WTS) memory blocks allocated by native APIs, ensuring
    /// proper release of unmanaged resources.
    /// </summary>
    /// <remarks>This class is intended for internal use when working with WTS-related native memory
    /// allocations. It ensures that memory is released using the appropriate WTS API based on the type of data. The
    /// handle should not be used after it has been released.</remarks>
    /// <param name="handle">The native pointer to the WTS memory block to be managed by the handle.</param>
    /// <param name="type">The WTS type class that determines the structure of the memory block.</param>
    /// <param name="length">The length, in bytes, of the memory block referenced by the handle.</param>
    /// <param name="ownsHandle">true to indicate that the handle is responsible for releasing the memory block; otherwise, false.</param>
    internal sealed class SafeWtsExHandle(IntPtr handle, WTS_TYPE_CLASS type, int length, bool ownsHandle) : SafeMemoryHandle<SafeWtsExHandle>(handle, length, ownsHandle)
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

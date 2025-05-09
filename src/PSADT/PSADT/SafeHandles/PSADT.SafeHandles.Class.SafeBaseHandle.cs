using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.Foundation;

namespace PSADT.SafeHandles
{
    internal abstract class SafeBaseHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeBaseHandle"/> class with the specified handle and ownership.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ownsHandle"></param>
        protected SafeBaseHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
        }

        /// <summary>
        /// A null handle that can be used to represent an invalid or uninitialized handle.
        /// </summary>
        internal static readonly SafeHandle NullHandle = new SafeFileHandle(IntPtr.Zero, false);

        /// <summary>
        /// An invalid handle that can be used to represent an invalid or closed handle.
        /// </summary>
        internal static readonly SafeHandle InvalidHandle = new SafeFileHandle(HANDLE.INVALID_HANDLE_VALUE, false);
    }
}

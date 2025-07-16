using System;
using PSADT.Utilities;
using Windows.Win32;

namespace PSADT.SafeHandles
{
    /// <summary>
    /// Represents a wrapper for an environment block handle that ensures the handle is properly released.
    /// </summary>
    internal sealed class SafeEnvironmentBlockHandle(IntPtr handle, bool ownsHandle) : SafeBaseHandle(handle, ownsHandle)
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
            var res = PInvoke.DestroyEnvironmentBlock(handle.ToPointer());
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            handle = default;
            return res;
        }

        /// <summary>
        /// Represents a null safe handle for an environment block.
        /// </summary>
        internal static readonly SafeEnvironmentBlockHandle Null = new(IntPtr.Zero, false);
    }
}

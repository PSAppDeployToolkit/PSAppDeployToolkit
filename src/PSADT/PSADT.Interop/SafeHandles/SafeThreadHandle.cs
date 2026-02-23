using Microsoft.Win32.SafeHandles;
using PSADT.Interop.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.Interop.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for a native thread resource, ensuring that the underlying handle is released reliably.
    /// </summary>
    /// <remarks>This class is intended for internal use when working with native thread handles. It ensures
    /// that the thread handle is released using the appropriate native method when the object is disposed or finalized.
    /// SafeThreadHandle inherits from SafeHandleZeroOrMinusOneIsInvalid, which treats zero or minus one as invalid
    /// handle values.</remarks>
    internal sealed class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the SafeThreadHandle class using the specified handle and ownership value.
        /// </summary>
        /// <param name="handle">The native handle to the thread resource to be wrapped by the SafeThreadHandle instance.</param>
        /// <param name="ownsHandle">true to indicate that the SafeThreadHandle instance owns the handle and is responsible for releasing it;
        /// otherwise, false.</param>
        internal SafeThreadHandle(nint handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Releases the handle associated with the current instance, ensuring proper cleanup of unmanaged resources.
        /// </summary>
        /// <remarks>If the handle is already set to its default value, this method returns true without
        /// performing any action. If the handle is not valid, an exception is thrown to indicate the failure of the
        /// operation.</remarks>
        /// <returns>true if the handle was successfully released or was already set to its default value; otherwise, false.</returns>
        protected override bool ReleaseHandle()
        {
            if (default == handle)
            {
                return true;
            }
            BOOL res = PInvoke.CloseHandle((HANDLE)handle);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            handle = default;
            return res;
        }
    }
}

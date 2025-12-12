using System;
using PSADT.Interop.Extensions;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.Interop.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for memory buffers allocated by the Net API, ensuring that resources are properly
    /// released when no longer needed.
    /// </summary>
    /// <remarks>This class inherits from SafeMemoryHandle and is designed to manage the lifetime of unmanaged
    /// memory buffers allocated by the Net API. When the handle is released, the associated memory is freed using
    /// NetApiBufferFree to prevent resource leaks.</remarks>
    /// <param name="handle">The pointer to the memory buffer allocated by the Net API.</param>
    /// <param name="length">The size, in bytes, of the memory buffer referenced by the handle.</param>
    /// <param name="ownsHandle">A value indicating whether this instance is responsible for releasing the handle when it is no longer needed.</param>
    internal sealed class SafeNetApiBufferFreeHandle(IntPtr handle, int length, bool ownsHandle) : SafeMemoryHandle<SafeNetApiBufferFreeHandle>(handle, length, ownsHandle)
    {
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
            unsafe
            {
                _ = ((WIN32_ERROR)PInvoke.NetApiBufferFree((void*)handle)).ThrowOnFailure();
            }
            handle = default;
            return true;
        }
    }
}

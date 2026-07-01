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
    /// <param name="ptr">A PWSTR representing the memory block to be managed by the handle. The pointer is converted to an IntPtr for
    /// internal use.</param>
    /// <param name="ownsHandle">true to indicate that this instance owns the handle and will release it when disposed; otherwise, false.</param>
    internal sealed class SafeNetApiBufferFreeHandle(PWSTR ptr, bool ownsHandle) : SafeMemoryHandle<SafeNetApiBufferFreeHandle>(ptr.ToIntPtr(), ptr.Length * sizeof(char), ownsHandle)
    {
        /// <summary>
        /// Releases the handle.
        /// </summary>
        /// <returns>true if the handle was released successfully; otherwise, false.</returns>
        protected override bool ReleaseHandle()
        {
            if (handle == default)
            {
                return true;
            }
            try
            {
                unsafe
                {
                    _ = ((WIN32_ERROR)PInvoke.NetApiBufferFree((void*)handle)).ThrowOnFailure();
                }
            }
            finally
            {
                handle = default;
            }
            return true;
        }
    }
}

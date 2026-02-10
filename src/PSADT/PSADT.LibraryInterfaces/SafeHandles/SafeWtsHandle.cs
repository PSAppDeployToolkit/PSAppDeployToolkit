using Windows.Win32;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Represents a safe handle for memory allocated by Windows Terminal Services (WTS) APIs, ensuring that the memory
    /// is released reliably.
    /// </summary>
    /// <remarks>This handle automatically releases the associated WTS memory when disposed or finalized,
    /// helping to prevent memory leaks when working with unmanaged resources returned by WTS API calls.</remarks>
    /// <param name="handle">The native pointer to the WTS-allocated memory to be managed by the handle.</param>
    /// <param name="length">The length, in bytes, of the memory region referenced by the handle.</param>
    /// <param name="ownsHandle">true to indicate that the handle is responsible for releasing the memory; otherwise, false.</param>
    internal sealed class SafeWtsHandle(nint handle, int length, bool ownsHandle) : SafeMemoryHandle<SafeWtsHandle>(handle, length, ownsHandle)
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
                PInvoke.WTSFreeMemory((void*)handle);
            }
            handle = default;
            return true;
        }
    }
}

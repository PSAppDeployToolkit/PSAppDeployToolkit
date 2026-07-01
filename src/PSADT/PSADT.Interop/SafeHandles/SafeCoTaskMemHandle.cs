using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace PSADT.Interop.SafeHandles
{
    /// <summary>
    /// Provides a safe handle for unmanaged memory allocated with COM task memory allocation, ensuring proper memory
    /// management and disposal.
    /// </summary>
    /// <remarks>This class is designed to manage the lifecycle of unmanaged memory, automatically releasing
    /// it when the handle is disposed. It is essential for preventing memory leaks in applications that interact with
    /// unmanaged resources.</remarks>
    /// <param name="ptr">A PWSTR representing the memory block to be managed by the handle. The pointer is converted to an IntPtr for
    /// internal use.</param>
    /// <param name="ownsHandle">true to indicate that the SafeCoTaskMemHandle instance should release the memory when disposed; otherwise,
    /// false.</param>
    internal sealed class SafeCoTaskMemHandle(PWSTR ptr, bool ownsHandle) : SafeMemoryHandle<SafeCoTaskMemHandle>(ptr.ToIntPtr(), ptr.Length * sizeof(char), ownsHandle)
    {
        /// <summary>
        /// Releases the handle and frees the associated unmanaged memory.
        /// </summary>
        /// <remarks>Overrides the base class implementation to ensure that memory allocated with COM task
        /// memory allocation is properly freed. If the handle is already set to its default value, no action is
        /// taken.</remarks>
        /// <returns>Always returns <see langword="true"/>, indicating that the handle has been released.</returns>
        protected override bool ReleaseHandle()
        {
            if (handle == default)
            {
                return true;
            }
            try
            {
                Marshal.FreeCoTaskMem(handle);
            }
            finally
            {
                handle = default;
            }
            return true;
        }
    }
}

using PSADT.LibraryInterfaces.Extensions;
using Windows.Win32;

namespace PSADT.LibraryInterfaces.SafeHandles
{
    /// <summary>
    /// Represents a safe handle for memory allocated by the Local Security Authority (LSA) that requires deallocation
    /// using the <see cref="PInvoke.LsaFreeMemory"/> function.
    /// </summary>
    /// <remarks>This class ensures that memory allocated by the LSA is properly released when the handle is
    /// disposed or finalized. It is a specialized implementation of <see cref="SafeMemoryHandle{TSelf}"/> designed to work
    /// with LSA memory management functions.</remarks>
    /// <param name="handle"></param>
    /// <param name="ownsHandle"></param>
    internal sealed class SafeLsaFreeMemoryHandle(nint handle, bool ownsHandle) : SafeMemoryHandle<SafeLsaFreeMemoryHandle>(handle, 0, ownsHandle)
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
                _ = PInvoke.LsaFreeMemory((void*)handle).ThrowOnFailure();
            }
            handle = default;
            return true;
        }
    }
}

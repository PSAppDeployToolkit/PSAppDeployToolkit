using PSADT.Interop.Extensions;
using Windows.Win32;

namespace PSADT.Interop.SafeHandles
{
    /// <summary>
    /// Represents a safe handle for memory allocated by the Local Security Authority (LSA) that ensures the memory is
    /// properly released when no longer needed.
    /// </summary>
    /// <remarks>Use this class to safely encapsulate memory allocated by LSA functions, ensuring that
    /// resources are released reliably and preventing memory leaks. This handle should be used in conjunction with APIs
    /// that allocate memory through the LSA and require explicit deallocation.</remarks>
    /// <param name="handle">The handle to the memory allocated by the LSA. This value identifies the memory block to be managed and
    /// released.</param>
    /// <param name="ownsHandle">true to indicate that this instance is responsible for releasing the handle; otherwise, false.</param>
    internal sealed class SafeLsaFreeMemoryHandle(nint handle, bool ownsHandle) : SafeMemoryHandle<SafeLsaFreeMemoryHandle>(handle, 0, ownsHandle)
    {
        /// <summary>
        /// Releases the handle associated with unmanaged LSA memory resources.
        /// </summary>
        /// <remarks>This method is called by the runtime to free the unmanaged memory allocated by LSA
        /// functions. If the handle is already set to its default value, no action is taken. This override ensures that
        /// memory is properly released to prevent resource leaks.</remarks>
        /// <returns>Always returns <see langword="true"/> to indicate that the handle has been released.</returns>
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

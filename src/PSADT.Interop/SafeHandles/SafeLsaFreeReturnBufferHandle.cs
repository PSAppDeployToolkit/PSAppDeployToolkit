using Windows.Win32;

namespace PSADT.Interop.SafeHandles
{
    /// <summary>
    /// Owns a logon-information buffer allocated by LSA and released with LsaFreeReturnBuffer.
    /// </summary>
    /// <param name="handle">The allocated buffer.</param>
    /// <param name="length">The readable length in bytes.</param>
    /// <param name="ownsHandle">Whether this instance owns the buffer.</param>
    internal sealed class SafeLsaFreeReturnBufferHandle(nint handle, int length, bool ownsHandle) : SafeMemoryHandle<SafeLsaFreeReturnBufferHandle>(handle, length, ownsHandle)
    {
        /// <summary>
        /// Releases the LSA return buffer and checks the native result for failure.
        /// </summary>
        /// <returns><see langword="true"/> when the buffer has been released.</returns>
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
                    _ = PInvoke.LsaFreeReturnBuffer((void*)handle).ThrowOnFailure();
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

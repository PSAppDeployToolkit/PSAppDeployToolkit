using Microsoft.Win32.SafeHandles;
using Windows.Win32;

namespace PSADT.TokenDiagnostics
{
    /// <summary>
    /// Owns a buffer returned by LsaEnumerateLogonSessions or LsaGetLogonSessionData.
    /// </summary>
    internal sealed class SafeLsaReturnBufferHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Takes ownership of an LSA return buffer.
        /// </summary>
        /// <param name="buffer">The buffer to release with LsaFreeReturnBuffer, not LsaFreeMemory.</param>
        internal SafeLsaReturnBufferHandle(nint buffer) : base(ownsHandle: true)
        {
            SetHandle(buffer);
        }

        /// <inheritdoc />
        protected override bool ReleaseHandle()
        {
            unsafe
            {
                return PInvoke.LsaFreeReturnBuffer((void*)handle).Value >= 0;
            }
        }
    }
}

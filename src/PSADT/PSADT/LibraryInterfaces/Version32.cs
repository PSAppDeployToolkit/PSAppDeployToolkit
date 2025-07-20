using System;
using PSADT.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// P/Invoke wrappers for the version.dll library.
    /// </summary>
    internal static class Version32
    {
        /// <summary>
        /// Queries version information from the specified version-information resource.
        /// </summary>
        /// <param name="pBlock">A handle to the memory block containing the version-information resource. This handle must be valid and not
        /// null.</param>
        /// <param name="lpSubBlock">The version-information value to be retrieved. This string must specify a valid sub-block within the
        /// version-information resource.</param>
        /// <param name="lplpBuffer">When this method returns, contains a pointer to the requested version-information value. This parameter is
        /// passed uninitialized.</param>
        /// <param name="puLen">When this method returns, contains the length, in bytes, of the data pointed to by <paramref
        /// name="lplpBuffer"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the specified version-information value is successfully retrieved; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the version-information value cannot be queried.</exception>
        internal static unsafe BOOL VerQueryValue(SafeHGlobalHandle pBlock, string lpSubBlock, out IntPtr lplpBuffer, out uint puLen)
        {
            bool pBlockAddRef = false;
            try
            {
                pBlock.DangerousAddRef(ref pBlockAddRef);
                var res = PInvoke.VerQueryValue(pBlock.DangerousGetHandle().ToPointer(), lpSubBlock, out var lplpBufferLocal, out puLen);
                if (!res)
                {
                    throw new InvalidOperationException($"Failed to query [{lpSubBlock}] version value.");
                }
                lplpBuffer = (IntPtr)lplpBufferLocal;
                return res;
            }
            finally
            {
                if (pBlockAddRef)
                {
                    pBlock.DangerousRelease();
                }
            }
        }
    }
}

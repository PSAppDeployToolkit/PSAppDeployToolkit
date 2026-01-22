using PSADT.LibraryInterfaces.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides utility methods for working with PROPVARIANT structures and property system conversions.
    /// </summary>
    internal static class PropSys
    {
        /// <summary>
        /// Converts the value of a PROPVARIANT structure to a string and allocates memory for the resulting string.
        /// </summary>
        /// <remarks>The returned string is allocated with CoTaskMemAlloc and must be released by
        /// disposing the SafeCoTaskMemoryHandle. This method is typically used to obtain a string representation of a
        /// property value stored in a PROPVARIANT.</remarks>
        /// <param name="propvar">The PROPVARIANT structure containing the value to convert.</param>
        /// <param name="ppszOut">When this method returns, contains a SafeCoTaskMemoryHandle that holds the pointer to the allocated string.
        /// The caller is responsible for releasing the handle.</param>
        /// <returns>An HRESULT value indicating the success or failure of the operation.</returns>
        internal static HRESULT PropVariantToStringAlloc(SafePropVariantHandle propvar, out SafeCoTaskMemoryHandle ppszOut)
        {
            bool propvarAddRef = false;
            try
            {
                propvar.DangerousAddRef(ref propvarAddRef); ref PROPVARIANT pv = ref propvar.DangerousGetHandle();
                HRESULT res = PInvoke.PropVariantToStringAlloc(in pv, out PWSTR ppszOutLocal).ThrowOnFailure();
                unsafe
                {
                    ppszOut = new SafeCoTaskMemoryHandle(ppszOutLocal, 0, true);
                }
                return res;
            }
            finally
            {
                if (propvarAddRef)
                {
                    propvar.DangerousRelease();
                }
            }
        }
    }
}

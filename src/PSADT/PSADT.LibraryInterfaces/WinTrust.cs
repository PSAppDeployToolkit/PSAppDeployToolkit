using System;
using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides methods for verifying the trust of objects using the Windows Trust Verification API.
    /// </summary>
    /// <remarks>This class is intended for internal use and exposes functionality to perform trust
    /// verification operations on Windows objects. It is not thread-safe and should be used with care when handling
    /// exceptions resulting from trust verification failures.</remarks>
    internal static class WinTrust
    {
        /// <summary>
        /// Verifies the trustworthiness of a specified object using the provided action identifier and verification
        /// data.
        /// </summary>
        /// <remarks>If the trust verification fails or if the provided parameters are invalid, an
        /// exception is thrown. Ensure that the action identifier and verification data are valid and appropriate for
        /// the intended verification operation.</remarks>
        /// <param name="hwnd">A handle to the parent window to be used for any user interface that may be displayed during the trust
        /// verification process.</param>
        /// <param name="pgActionID">A reference to a GUID that specifies the action to be performed during the trust verification.</param>
        /// <param name="pWVTData">A pointer to a structure containing additional data required for the trust verification. The structure and
        /// its contents depend on the action specified by the action identifier.</param>
        /// <returns>An integer value indicating the result of the trust verification. A value of 0 indicates that the
        /// verification succeeded.</returns>
        internal static unsafe HRESULT WinVerifyTrust(HWND hwnd, ref Guid pgActionID, void* pWVTData)
        {
            HRESULT res = (HRESULT)PInvoke.WinVerifyTrust(hwnd, ref pgActionID, pWVTData);
            return res != HRESULT.S_OK ? throw ExceptionUtilities.GetException(res) : res;
        }
    }
}

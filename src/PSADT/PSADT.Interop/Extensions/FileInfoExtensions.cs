using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security.WinTrust;

namespace PSADT.Interop.Extensions
{
    /// <summary>
    /// Provides extension methods for the FileInfo type to support file trust verification and related operations.
    /// </summary>
    /// <remarks>This class contains static methods that extend the functionality of FileInfo, enabling trust
    /// checks and other file-related utilities. Methods in this class typically use Windows APIs for verification and
    /// do not perform network communications unless explicitly documented.</remarks>
    internal static class FileInfoExtensions
    {
        /// <summary>
        /// Determines whether the specified file is trusted based on its Authenticode signature.
        /// </summary>
        /// <remarks>This method performs a verification of the file's Authenticode signature using the
        /// WinVerifyTrust API. It does not perform any network communications during the verification
        /// process.</remarks>
        /// <param name="fileInfo">The FileInfo object representing the file to be verified. This parameter cannot be null, and the file must exist.</param>
        /// <returns>true if the file is trusted; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="fileInfo"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
        internal static bool IsAuthenticodeTrusted(this FileInfo fileInfo)
        {
            // Load up everything we need for WinVerifyTrust. The CsWin32 projects this
            // all using pointers, so we must follow suite as well or roll our own setup.
            unsafe
            {
                fixed (char* pFilePath = fileInfo.FullName.ThrowIfFileDoesNotExist())
                {
                    // Set up WINTRUST_DATA to not perform any network comms.
                    WINTRUST_FILE_INFO wtFileInfo = new()
                    {
                        cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                        pcwszFilePath = pFilePath,
                    };
                    WINTRUST_DATA wtData = new()
                    {
                        cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                        dwUIChoice = WINTRUST_DATA_UICHOICE.WTD_UI_NONE,
                        fdwRevocationChecks = WINTRUST_DATA_REVOCATION_CHECKS.WTD_REVOKE_NONE,
                        dwUnionChoice = WINTRUST_DATA_UNION_CHOICE.WTD_CHOICE_FILE,
                        Anonymous = new() { pFile = &wtFileInfo },
                        dwStateAction = WINTRUST_DATA_STATE_ACTION.WTD_STATEACTION_IGNORE,
                        dwProvFlags = WINTRUST_DATA_PROVIDER_FLAGS.WTD_CACHE_ONLY_URL_RETRIEVAL,
                        dwUIContext = WINTRUST_DATA_UICONTEXT.WTD_UICONTEXT_EXECUTE,
                    };
                    try
                    {
                        Guid guid = PInvoke.WINTRUST_ACTION_GENERIC_VERIFY_V2;
                        HWND handle = (HWND)(nint)HANDLE.INVALID_HANDLE_VALUE;
                        return NativeMethods.WinVerifyTrust(handle, ref guid, in wtData) == HRESULT.S_OK;
                    }
                    catch
                    {
                        return false;
                        throw;
                    }
                }
            }
        }
    }
}

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Public P/Invokes from the advapi32.dll library.
    /// </summary>
    public static class AdvApi32
    {
        internal static unsafe WIN32_ERROR RegOpenKeyEx(HKEY hKey, string lpSubKey, uint ulOptions, REG_SAM_FLAGS samDesired, out HKEY phkResult)
        {
            fixed (char* lpSubKeyPtr = lpSubKey)
            {
                HKEY phkResultInternal;
                var res = PInvoke.RegOpenKeyEx(hKey, lpSubKeyPtr, ulOptions, samDesired, &phkResultInternal);
                if (res != WIN32_ERROR.ERROR_SUCCESS)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
                }
                phkResult = phkResultInternal;
                return res;
            }
        }
        /// <summary>
        /// Retrieves information about the specified registry key.
        /// </summary>
        /// <param name="hKey"></param>
        /// <param name="lpClass"></param>
        /// <param name="lpcchClass"></param>
        /// <param name="lpcSubKeys"></param>
        /// <param name="lpcbMaxSubKeyLen"></param>
        /// <param name="lpcbMaxClassLen"></param>
        /// <param name="lpcValues"></param>
        /// <param name="lpcbMaxValueNameLen"></param>
        /// <param name="lpcbMaxValueLen"></param>
        /// <param name="lpcbSecurityDescriptor"></param>
        /// <param name="lpftLastWriteTime"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe WIN32_ERROR RegQueryInfoKey(HKEY hKey, PWSTR lpClass, [Optional] out uint lpcchClass, [Optional] out uint lpReserved, [Optional] out uint lpcSubKeys, [Optional] out uint lpcbMaxSubKeyLen, [Optional] out uint lpcbMaxClassLen, [Optional] out uint lpcValues, [Optional] out uint lpcbMaxValueNameLen, [Optional] out uint lpcbMaxValueLen, [Optional] out uint lpcbSecurityDescriptor, [Optional] out global::System.Runtime.InteropServices.ComTypes.FILETIME lpftLastWriteTime)
        {
            fixed (uint* lpcchClassPtr = &lpcchClass, lpReservedPtr = &lpReserved, lpcSubKeysPtr = &lpcSubKeys, lpcbMaxSubKeyLenPtr = &lpcbMaxSubKeyLen, lpcbMaxClassLenPtr = &lpcbMaxClassLen, lpcValuesPtr = &lpcValues, lpcbMaxValueNameLenPtr = &lpcbMaxValueNameLen, lpcbMaxValueLenPtr = &lpcbMaxValueLen, lpcbSecurityDescriptorPtr = &lpcbSecurityDescriptor)
            fixed (global::System.Runtime.InteropServices.ComTypes.FILETIME* lpftLastWriteTimePtr = &lpftLastWriteTime)
            {
                var res = PInvoke.RegQueryInfoKey(hKey, lpClass, lpcchClassPtr, lpReservedPtr, lpcSubKeysPtr, lpcbMaxSubKeyLenPtr, lpcbMaxClassLenPtr, lpcValuesPtr, lpcbMaxValueNameLenPtr, lpcbMaxValueLenPtr, lpcbSecurityDescriptorPtr, lpftLastWriteTimePtr);
                if (res != WIN32_ERROR.ERROR_SUCCESS)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
                }
                return res;
            }
        }

        /// <summary>
        /// Closes a handle to the specified registry key.
        /// </summary>
        /// <param name="hKey"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static WIN32_ERROR RegCloseKey(ref HKEY hKey)
        {
            if (null == hKey || hKey == default || hKey.IsNull)
            {
                return WIN32_ERROR.ERROR_SUCCESS;
            }
            var res = PInvoke.RegCloseKey(hKey);
            if (res != WIN32_ERROR.ERROR_SUCCESS)
            {
                
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            hKey = default;
            return res;
        }

        /// <summary>
        /// Duplicates an access token.
        /// </summary>
        /// <param name="hExistingToken"></param>
        /// <param name="dwDesiredAccess"></param>
        /// <param name="lpTokenAttributes"></param>
        /// <param name="ImpersonationLevel"></param>
        /// <param name="TokenType"></param>
        /// <param name="phNewToken"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL DuplicateTokenEx(HANDLE hExistingToken, TOKEN_ACCESS_MASK dwDesiredAccess, [Optional] SECURITY_ATTRIBUTES? lpTokenAttributes, SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, out HANDLE phNewToken)
        {
            fixed (HANDLE* phNewTokenPtr = &phNewToken)
            {
                SECURITY_ATTRIBUTES lpTokenAttributesLocal = lpTokenAttributes ?? default(SECURITY_ATTRIBUTES);
                var res = PInvoke.DuplicateTokenEx(hExistingToken, dwDesiredAccess, lpTokenAttributes.HasValue ? &lpTokenAttributesLocal : null, ImpersonationLevel, TokenType, phNewTokenPtr);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Opens the access token associated with a process.
        /// </summary>
        /// <param name="ProcessHandle"></param>
        /// <param name="DesiredAccess"></param>
        /// <param name="TokenHandle"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL OpenProcessToken(HANDLE ProcessHandle, TOKEN_ACCESS_MASK DesiredAccess, out HANDLE TokenHandle)
        {
            fixed (HANDLE* TokenHandlePtr = &TokenHandle)
            {
                var res = PInvoke.OpenProcessToken(ProcessHandle, DesiredAccess, TokenHandlePtr);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Enables or disables privileges in the specified access token.
        /// </summary>
        /// <param name="lpSystemName"></param>
        /// <param name="lpName"></param>
        /// <param name="lpLuid"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid)
        {
            var res = PInvoke.LookupPrivilegeValue(lpSystemName, lpName, out lpLuid);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Retrieves a specified type of information about an access token.
        /// </summary>
        /// <param name="TokenHandle"></param>
        /// <param name="TokenInformationClass"></param>
        /// <param name="TokenInformation"></param>
        /// <param name="TokenInformationLength"></param>
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL GetTokenInformation(HANDLE TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, [Optional] IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength)
        {
            fixed (uint* ReturnLengthPtr = &ReturnLength)
            {
                var res = PInvoke.GetTokenInformation(TokenHandle, TokenInformationClass, TokenInformation.ToPointer(), TokenInformationLength, ReturnLengthPtr);
                if (!res)
                {
                    var error = (WIN32_ERROR)Marshal.GetLastWin32Error();
                    if (error != WIN32_ERROR.NO_ERROR && (error != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER || TokenInformationLength != 0))
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error(error);
                    }
                }
                return res;
            }
        }

        /// <summary>
        /// Enables or disables privileges in the specified access token.
        /// </summary>
        /// <param name="TokenHandle"></param>
        /// <param name="DisableAllPrivileges"></param>
        /// <param name="NewState"></param>
        /// <param name="BufferLength"></param>
        /// <param name="PreviousState"></param>
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL AdjustTokenPrivileges(HANDLE TokenHandle, BOOL DisableAllPrivileges, [Optional] TOKEN_PRIVILEGES? NewState, uint BufferLength)
        {
            TOKEN_PRIVILEGES NewStateLocal = NewState ?? default(TOKEN_PRIVILEGES);
            var res = PInvoke.AdjustTokenPrivileges(TokenHandle, DisableAllPrivileges, NewState.HasValue ? &NewStateLocal : null, BufferLength, null, null);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }
    }
}

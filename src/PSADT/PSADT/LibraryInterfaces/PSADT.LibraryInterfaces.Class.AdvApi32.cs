using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
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
                    throw new Win32Exception((int)res);
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
            {
                global::System.Runtime.InteropServices.ComTypes.FILETIME lastWriteTime;
                var res = PInvoke.RegQueryInfoKey(hKey, lpClass, lpcchClassPtr, lpReservedPtr, lpcSubKeysPtr, lpcbMaxSubKeyLenPtr, lpcbMaxClassLenPtr, lpcValuesPtr, lpcbMaxValueNameLenPtr, lpcbMaxValueLenPtr, lpcbSecurityDescriptorPtr, &lastWriteTime);
                if (res != WIN32_ERROR.ERROR_SUCCESS)
                {
                    throw new Win32Exception((int)res);
                }
                lpftLastWriteTime = lastWriteTime;
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
                throw new Win32Exception((int)res);
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
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                return res;
            }
        }
    }
}

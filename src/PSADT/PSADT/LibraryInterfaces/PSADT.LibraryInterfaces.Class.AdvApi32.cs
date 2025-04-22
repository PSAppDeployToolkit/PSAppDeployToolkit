using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Security;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// CsWin32 P/Invoke wrappers for the advapi32.dll library.
    /// </summary>
    internal static class AdvApi32
    {
        /// <summary>
        /// Opens the requested registry subkey for the given key.
        /// </summary>
        /// <param name="hKey"></param>
        /// <param name="lpSubKey"></param>
        /// <param name="ulOptions"></param>
        /// <param name="samDesired"></param>
        /// <param name="phkResult"></param>
        /// <returns></returns>
        internal static WIN32_ERROR RegOpenKeyEx(SafeHandle hKey, string lpSubKey, uint ulOptions, REG_SAM_FLAGS samDesired, out SafeRegistryHandle phkResult)
        {
            var res = PInvoke.RegOpenKeyEx(hKey, lpSubKey, ulOptions, samDesired, out phkResult);
            if (res != WIN32_ERROR.ERROR_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            return res;
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
        internal static unsafe WIN32_ERROR RegQueryInfoKey(SafeHandle hKey, Span<char> lpClass, IntPtr lpcchClass, out uint lpcSubKeys, out uint lpcbMaxSubKeyLen, out uint lpcbMaxClassLen, out uint lpcValues, out uint lpcbMaxValueNameLen, out uint lpcbMaxValueLen, out uint lpcbSecurityDescriptor, out global::System.Runtime.InteropServices.ComTypes.FILETIME lpftLastWriteTime)
        {
            fixed (uint* lpcSubKeysPtr = &lpcSubKeys, lpcbMaxSubKeyLenPtr = &lpcbMaxSubKeyLen, lpcbMaxClassLenPtr = &lpcbMaxClassLen, lpcValuesPtr = &lpcValues, lpcbMaxValueNameLenPtr = &lpcbMaxValueNameLen, lpcbMaxValueLenPtr = &lpcbMaxValueLen, lpcbSecurityDescriptorPtr = &lpcbSecurityDescriptor)
            fixed (global::System.Runtime.InteropServices.ComTypes.FILETIME* lpftLastWriteTimePtr = &lpftLastWriteTime)
            {
                var res = PInvoke.RegQueryInfoKey(hKey, lpClass, (uint*)lpcchClass, lpcSubKeysPtr, lpcbMaxSubKeyLenPtr, lpcbMaxClassLenPtr, lpcValuesPtr, lpcbMaxValueNameLenPtr, lpcbMaxValueLenPtr, lpcbSecurityDescriptorPtr, lpftLastWriteTimePtr);
                if (res != WIN32_ERROR.ERROR_SUCCESS)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
                }
                return res;
            }
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
        internal static BOOL DuplicateTokenEx(SafeHandle hExistingToken, TOKEN_ACCESS_MASK dwDesiredAccess, SECURITY_ATTRIBUTES? lpTokenAttributes, SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, out SafeFileHandle phNewToken)
        {
            var res = PInvoke.DuplicateTokenEx(hExistingToken, dwDesiredAccess, lpTokenAttributes, ImpersonationLevel, TokenType, out phNewToken);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Opens the access token associated with a process.
        /// </summary>
        /// <param name="ProcessHandle"></param>
        /// <param name="DesiredAccess"></param>
        /// <param name="TokenHandle"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL OpenProcessToken(SafeHandle ProcessHandle, TOKEN_ACCESS_MASK DesiredAccess, out SafeFileHandle TokenHandle)
        {
            var res = PInvoke.OpenProcessToken(ProcessHandle, DesiredAccess, out TokenHandle);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }

        /// <summary>
        /// Enables or disables privileges in the specified access token.
        /// </summary>
        /// <param name="lpSystemName"></param>
        /// <param name="lpName"></param>
        /// <param name="lpLuid"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static BOOL LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid)
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
        /// <param name="ReturnLength"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal static unsafe BOOL GetTokenInformation(SafeHandle TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, SafeMemoryHandle TokenInformation, out uint ReturnLength)
        {
            var res = PInvoke.GetTokenInformation(TokenHandle, TokenInformationClass, TokenInformation.DangerousGetHandle().ToPointer(), (uint)TokenInformation.Length, out ReturnLength);
            if (!res && !TokenInformation.IsInvalid && 0 != TokenInformation.Length)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
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
        internal static unsafe BOOL AdjustTokenPrivileges(SafeHandle TokenHandle, BOOL DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength)
        {
            fixed (TOKEN_PRIVILEGES* newStatePtr = &NewState)
            {
                var res = PInvoke.AdjustTokenPrivileges(TokenHandle, DisableAllPrivileges, newStatePtr, BufferLength, (TOKEN_PRIVILEGES*)PreviousState, (uint*)ReturnLength);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
        }

        /// <summary>
        /// Retrieves the name of the specified privilege.
        /// </summary>
        /// <param name="lpSystemName"></param>
        /// <param name="lpLuid"></param>
        /// <param name="lpName"></param>
        /// <returns></returns>
        internal static BOOL LookupPrivilegeName(string? lpSystemName, in LUID lpLuid, Span<char> lpName, out uint cchName)
        {
            var len = (uint)lpName.Length;
            var res = PInvoke.LookupPrivilegeName(lpSystemName, in lpLuid, lpName, ref len);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            cchName = len;
            return res;
        }

        /// <summary>
        /// Retrieves the name of the specified account for a given security identifier (SID).
        /// </summary>
        /// <param name="lpSystemName"></param>
        /// <param name="Sid"></param>
        /// <param name="Name"></param>
        /// <param name="cchName"></param>
        /// <param name="ReferencedDomainName"></param>
        /// <param name="cchReferencedDomainName"></param>
        /// <param name="peUse"></param>
        /// <returns></returns>
        internal static BOOL LookupAccountSid(string? lpSystemName, SafeHandle Sid, Span<char> Name, out uint cchName, Span<char> ReferencedDomainName, out uint cchReferencedDomainName, out SID_NAME_USE peUse)
        {
            var nameLen = (uint)Name.Length;
            var refDomainNameLen = (uint)ReferencedDomainName.Length;
            var res = PInvoke.LookupAccountSid(lpSystemName, Sid, Name, ref nameLen, ReferencedDomainName, ref refDomainNameLen, out peUse);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            cchName = nameLen;
            cchReferencedDomainName = refDomainNameLen;
            return res;
        }

        /// <summary>
        /// Converts a string SID to a binary SID.
        /// </summary>
        /// <param name="StringSid"></param>
        /// <param name="Sid"></param>
        /// <returns></returns>
        internal static BOOL ConvertStringSidToSid(string StringSid, out FreeSidSafeHandle Sid)
        {
            var res = PInvoke.ConvertStringSidToSid(StringSid, out Sid);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }
    }
}

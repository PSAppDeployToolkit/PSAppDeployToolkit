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
using Windows.Win32.System.Services;
using Windows.Win32.System.Threading;

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
        internal unsafe static WIN32_ERROR RegQueryInfoKey(SafeHandle hKey, Span<char> lpClass, out uint lpcchClass, out uint lpcSubKeys, out uint lpcbMaxSubKeyLen, out uint lpcbMaxClassLen, out uint lpcValues, out uint lpcbMaxValueNameLen, out uint lpcbMaxValueLen, out uint lpcbSecurityDescriptor, out global::System.Runtime.InteropServices.ComTypes.FILETIME lpftLastWriteTime)
        {
            uint lpcchClassLocal = (uint)lpClass.Length;
            fixed (uint* lpcSubKeysPtr = &lpcSubKeys, lpcbMaxSubKeyLenPtr = &lpcbMaxSubKeyLen, lpcbMaxClassLenPtr = &lpcbMaxClassLen, lpcValuesPtr = &lpcValues, lpcbMaxValueNameLenPtr = &lpcbMaxValueNameLen, lpcbMaxValueLenPtr = &lpcbMaxValueLen, lpcbSecurityDescriptorPtr = &lpcbSecurityDescriptor)
            fixed (global::System.Runtime.InteropServices.ComTypes.FILETIME* lpftLastWriteTimePtr = &lpftLastWriteTime)
            {
                var res = PInvoke.RegQueryInfoKey(hKey, lpClass, null != lpClass ? &lpcchClassLocal : null, lpcSubKeysPtr, lpcbMaxSubKeyLenPtr, lpcbMaxClassLenPtr, lpcValuesPtr, lpcbMaxValueNameLenPtr, lpcbMaxValueLenPtr, lpcbSecurityDescriptorPtr, lpftLastWriteTimePtr);
                if (res != WIN32_ERROR.ERROR_SUCCESS)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
                }
                lpcchClass = lpcchClassLocal;
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
        internal unsafe static BOOL GetTokenInformation(SafeHandle TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, SafeMemoryHandle TokenInformation, out uint ReturnLength)
        {
            if (TokenInformation is null || TokenInformation.IsClosed)
            {
                throw new ArgumentNullException(nameof(TokenInformation));
            }

            bool TokenInformationAddRef = false;
            try
            {
                TokenInformation.DangerousAddRef(ref TokenInformationAddRef);
                var res = PInvoke.GetTokenInformation(TokenHandle, TokenInformationClass, TokenInformation.DangerousGetHandle().ToPointer(), (uint)TokenInformation.Length, out ReturnLength);
                if (!res && !TokenInformation.IsInvalid && 0 != TokenInformation.Length)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
            finally
            {
                if (TokenInformationAddRef)
                {
                    TokenInformation.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Enables or disables privileges in the specified access token.
        /// </summary>
        /// <param name="TokenHandle"></param>
        /// <param name="NewState"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal unsafe static BOOL AdjustTokenPrivileges(SafeHandle TokenHandle, in TOKEN_PRIVILEGES NewState)
        {
            fixed (TOKEN_PRIVILEGES* newStatePtr = &NewState)
            {
                var res = PInvoke.AdjustTokenPrivileges(TokenHandle, false, newStatePtr, 0, null, null);
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
        /// Creates a new process using the specified token, application name, command line, and other parameters.
        /// </summary>
        /// <remarks>This method wraps the native Windows API function <c>CreateProcessWithTokenW</c>,
        /// providing a managed interface for creating processes with a specified access token. The caller is
        /// responsible for ensuring that the provided token and environment block are valid.</remarks>
        /// <param name="hToken">A handle to the access token that will be used to create the process. This handle must be valid and have the
        /// necessary privileges.</param>
        /// <param name="dwLogonFlags">Flags that control the logon behavior of the process. These flags determine how the process is created and
        /// logged on.</param>
        /// <param name="lpApplicationName">The name of the application to execute. Can be null if the application name is included in <paramref
        /// name="lpCommandLine"/>.</param>
        /// <param name="lpCommandLine">The command line to be executed, including the application name if <paramref name="lpApplicationName"/> is
        /// null.</param>
        /// <param name="dwCreationFlags">Flags that control the creation of the process, such as whether it is created in a suspended state.</param>
        /// <param name="lpEnvironment">A handle to the environment block for the new process. This handle must not be null, closed, or invalid.</param>
        /// <param name="lpCurrentDirectory">The full path to the current directory for the new process. Can be null to use the current directory of the
        /// calling process.</param>
        /// <param name="lpStartupInfo">A structure containing startup information for the new process, such as window settings and standard
        /// handles.</param>
        /// <param name="lpProcessInformation">When the method returns, contains information about the newly created process and its primary thread.</param>
        /// <returns><see langword="true"/> if the process is successfully created; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="lpEnvironment"/> is null, closed, or invalid.</exception>
        internal unsafe static BOOL CreateProcessWithToken(SafeHandle hToken, CREATE_PROCESS_LOGON_FLAGS dwLogonFlags, string? lpApplicationName, ref Span<char> lpCommandLine, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
        {
            if (lpEnvironment is null || lpEnvironment.IsClosed)
            {
                throw new ArgumentNullException(nameof(lpEnvironment));
            }

            bool lpEnvironmentAddRef = false;
            try
            {
                lpEnvironment.DangerousAddRef(ref lpEnvironmentAddRef);
                var res = PInvoke.CreateProcessWithToken(hToken, dwLogonFlags, lpApplicationName, ref lpCommandLine, dwCreationFlags, lpEnvironment.DangerousGetHandle().ToPointer(), lpCurrentDirectory, lpStartupInfo, out lpProcessInformation);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Wrapper around CreateProcessAsUser to manage error handling.
        /// </summary>
        /// <param name="hToken"></param>
        /// <param name="lpApplicationName"></param>
        /// <param name="lpCommandLine"></param>
        /// <param name="lpProcessAttributes"></param>
        /// <param name="lpThreadAttributes"></param>
        /// <param name="bInheritHandles"></param>
        /// <param name="dwCreationFlags"></param>
        /// <param name="lpEnvironment"></param>
        /// <param name="lpCurrentDirectory"></param>
        /// <param name="lpStartupInfo"></param>
        /// <param name="lpProcessInformation"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal unsafe static BOOL CreateProcessAsUser(SafeHandle hToken, string? lpApplicationName, ref Span<char> lpCommandLine, SECURITY_ATTRIBUTES? lpProcessAttributes, SECURITY_ATTRIBUTES? lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
        {
            if (lpEnvironment is null || lpEnvironment.IsClosed)
            {
                throw new ArgumentNullException(nameof(lpEnvironment));
            }

            bool lpEnvironmentAddRef = false;
            try
            {
                lpEnvironment.DangerousAddRef(ref lpEnvironmentAddRef);
                var res = PInvoke.CreateProcessAsUser(hToken, lpApplicationName, ref lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment.DangerousGetHandle().ToPointer(), lpCurrentDirectory, lpStartupInfo, out lpProcessInformation);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                return res;
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Wrapper around CreateProcessAsUser to manage error handling.
        /// </summary>
        /// <param name="hToken"></param>
        /// <param name="lpApplicationName"></param>
        /// <param name="lpCommandLine"></param>
        /// <param name="lpProcessAttributes"></param>
        /// <param name="lpThreadAttributes"></param>
        /// <param name="bInheritHandles"></param>
        /// <param name="dwCreationFlags"></param>
        /// <param name="lpEnvironment"></param>
        /// <param name="lpCurrentDirectory"></param>
        /// <param name="lpStartupInfoEx"></param>
        /// <param name="lpProcessInformation"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        internal unsafe static BOOL CreateProcessAsUser(SafeHandle hToken, string? lpApplicationName, ref Span<char> lpCommandLine, SECURITY_ATTRIBUTES? lpProcessAttributes, SECURITY_ATTRIBUTES? lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, SafeEnvironmentBlockHandle lpEnvironment, string? lpCurrentDirectory, in STARTUPINFOEXW lpStartupInfoEx, out PROCESS_INFORMATION lpProcessInformation)
        {
            if (lpCommandLine != null && lpCommandLine.LastIndexOf('\0') == -1)
            {
                throw new ArgumentException("Required null terminator missing.", "lpCommandLine");
            }
            bool hTokenAddRef = false;
            bool lpEnvironmentAddRef = false;
            try
            {
                fixed (char* lpApplicationNameLocal = lpApplicationName, plpCommandLine = lpCommandLine, lpCurrentDirectoryLocal = lpCurrentDirectory)
                fixed (PROCESS_INFORMATION* lpProcessInformationLocal = &lpProcessInformation)
                fixed (STARTUPINFOEXW* lpStartupInfoExLocal = &lpStartupInfoEx)
                {
                    SECURITY_ATTRIBUTES lpProcessAttributesLocal = lpProcessAttributes ?? default(SECURITY_ATTRIBUTES);
                    SECURITY_ATTRIBUTES lpThreadAttributesLocal = lpThreadAttributes ?? default(SECURITY_ATTRIBUTES);
                    PWSTR wstrlpCommandLine = plpCommandLine;
                    hToken.DangerousAddRef(ref hTokenAddRef);
                    lpEnvironment.DangerousAddRef(ref lpEnvironmentAddRef);
                    var res = PInvoke.CreateProcessAsUser((HANDLE)hToken.DangerousGetHandle(), lpApplicationNameLocal, plpCommandLine, lpProcessAttributes.HasValue ? &lpProcessAttributesLocal : null, lpThreadAttributes.HasValue ? &lpThreadAttributesLocal : null, bInheritHandles, dwCreationFlags, lpEnvironment.DangerousGetHandle().ToPointer(), lpCurrentDirectoryLocal, (STARTUPINFOW*)lpStartupInfoExLocal, lpProcessInformationLocal);
                    if (!res)
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error();
                    }
                    lpCommandLine = lpCommandLine.Slice(0, wstrlpCommandLine.Length);
                    return res;
                }
            }
            finally
            {
                if (lpEnvironmentAddRef)
                {
                    lpEnvironment.DangerousRelease();
                }
                if (hTokenAddRef)
                {
                    hToken.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Opens a handle to the specified service control manager database.
        /// </summary>
        /// <param name="lpMachineName">The name of the target computer. If <see langword="null"/>, the local computer is used.</param>
        /// <param name="lpDatabaseName">The name of the service control manager database. If <see langword="null"/>, the default database is used.</param>
        /// <param name="dwDesiredAccess">The access rights to the service control manager. This parameter must be a combination of <see
        /// cref="SC_MANAGER_ACCESS"/> values.</param>
        /// <returns>A <see cref="CloseServiceHandleSafeHandle"/> that represents the handle to the service control manager
        /// database.</returns>
        internal static CloseServiceHandleSafeHandle OpenSCManager(string? lpMachineName, string? lpDatabaseName, SC_MANAGER_ACCESS dwDesiredAccess)
        {
            var handle = PInvoke.OpenSCManager(lpMachineName, lpDatabaseName, (uint)dwDesiredAccess);
            if (handle.IsInvalid)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return handle;
        }

        /// <summary>
        /// Opens an existing service in the specified service control manager database.
        /// </summary>
        /// <param name="hSCManager">A handle to the service control manager database. This handle is obtained from a previous call to the
        /// OpenSCManager function.</param>
        /// <param name="lpServiceName">The name of the service to be opened. This name is case-sensitive and must match the service name exactly.</param>
        /// <param name="dwDesiredAccess">The access rights to the service. This parameter specifies the access level required for the service.</param>
        /// <returns>A <see cref="CloseServiceHandleSafeHandle"/> that represents the handle to the opened service. The handle
        /// must be closed using the appropriate method when it is no longer needed.</returns>
        internal static CloseServiceHandleSafeHandle OpenService(SafeHandle hSCManager, string lpServiceName, SERVICE_ACCESS_RIGHTS dwDesiredAccess)
        {
            var handle = PInvoke.OpenService(hSCManager, lpServiceName, (uint)dwDesiredAccess);
            if (handle.IsInvalid)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return handle;
        }

        /// <summary>
        /// Retrieves the current status of the specified service based on the provided information level.
        /// </summary>
        /// <param name="hService">A handle to the service. This handle is obtained from a previous call to the OpenService or CreateService
        /// function.</param>
        /// <param name="InfoLevel">The information level of the service status to be queried. This parameter specifies the type of information
        /// to retrieve.</param>
        /// <param name="lpBuffer">A buffer that receives the status information. The format of this data depends on the value of the <paramref
        /// name="InfoLevel"/> parameter.</param>
        /// <param name="pcbBytesNeeded">When the method returns, contains the number of bytes needed to store all the status information, if the
        /// buffer is too small.</param>
        /// <returns><see langword="true"/> if the function succeeds; otherwise, <see langword="false"/>.</returns>
        internal static BOOL QueryServiceStatusEx(SafeHandle hService, SC_STATUS_TYPE InfoLevel, Span<byte> lpBuffer, out uint pcbBytesNeeded)
        {
            var res = PInvoke.QueryServiceStatusEx(hService, InfoLevel, lpBuffer, out pcbBytesNeeded);
            if (!res && ((WIN32_ERROR)Marshal.GetLastWin32Error() is WIN32_ERROR lastWin32Error) && (lastWin32Error != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER || lpBuffer.Length != 0))
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            return res;
        }
    }
}

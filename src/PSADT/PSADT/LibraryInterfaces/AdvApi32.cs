using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.SafeHandles;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authentication.Identity;
using Windows.Win32.Security.Authorization;
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
        /// <param name="samDesired"></param>
        /// <param name="phkResult"></param>
        /// <returns></returns>
        internal static WIN32_ERROR RegOpenKeyEx(SafeHandle hKey, string lpSubKey, REG_SAM_FLAGS samDesired, out SafeRegistryHandle phkResult)
        {
            var res = PInvoke.RegOpenKeyEx(hKey, lpSubKey, 0, samDesired, out phkResult);
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
                var res = PInvoke.RegQueryInfoKey(hKey, lpClass, Span<char>.Empty != lpClass ? &lpcchClassLocal : null, lpcSubKeysPtr, lpcbMaxSubKeyLenPtr, lpcbMaxClassLenPtr, lpcValuesPtr, lpcbMaxValueNameLenPtr, lpcbMaxValueLenPtr, lpcbSecurityDescriptorPtr, lpftLastWriteTimePtr);
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
        internal static BOOL LookupPrivilegeValue(string? lpSystemName, SE_PRIVILEGE lpName, out LUID lpLuid)
        {
            var res = PInvoke.LookupPrivilegeValue(lpSystemName, lpName.ToString(), out lpLuid);
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
        /// <param name="cchName"></param>
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
            if (lpCommandLine != Span<char>.Empty && lpCommandLine.LastIndexOf('\0') == -1)
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

        /// <summary>
        /// Modifies an access control list (ACL) by adding or updating the specified access control entries (ACEs).
        /// </summary>
        /// <remarks>This method uses the Windows API function <c>SetEntriesInAcl</c> to modify the ACL.
        /// The caller must ensure that the <paramref name="OldAcl"/> handle, if provided, is valid and not closed. The
        /// <paramref name="NewAcl"/> handle must be released by the caller to avoid memory leaks.</remarks>
        /// <param name="pListOfExplicitEntries">A read-only span of <see cref="EXPLICIT_ACCESS_W"/> structures that define the access control entries to be
        /// added or updated in the ACL.</param>
        /// <param name="OldAcl">An optional handle to the existing ACL to be modified. If <see langword="null"/>, a new ACL is created.</param>
        /// <param name="NewAcl">When this method returns, contains a handle to the newly created or modified ACL. The caller is responsible
        /// for releasing this handle.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the operation is successful.</returns>
		internal unsafe static WIN32_ERROR SetEntriesInAcl(ReadOnlySpan<EXPLICIT_ACCESS_W> pListOfExplicitEntries, LocalFreeSafeHandle? OldAcl, out LocalFreeSafeHandle NewAcl)
        {
            fixed (EXPLICIT_ACCESS_W* pListOfExplicitEntriesLocal = pListOfExplicitEntries)
            {
                bool OldAclAddRef = false;
                ACL* NewAclLocal = null;
                try
                {
                    if (OldAcl is not null && !OldAcl.IsClosed)
                    {
                        OldAcl.DangerousAddRef(ref OldAclAddRef);
                    }
                    var res = PInvoke.SetEntriesInAcl((uint)pListOfExplicitEntries.Length, pListOfExplicitEntriesLocal, OldAcl is not null ? (ACL*)OldAcl.DangerousGetHandle() : (ACL*)null, &NewAclLocal);
                    if (res != WIN32_ERROR.ERROR_SUCCESS)
                    {
                        throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
                    }
                    NewAcl = new((IntPtr)NewAclLocal, true);
                    return res;
                }
                finally
                {
                    if (OldAclAddRef)
                    {
                        OldAcl?.DangerousRelease();
                    }
                }
            }
        }

        /// <summary>
        /// Sets the security information for a specified object, such as a file, registry key, or other securable
        /// object.
        /// </summary>
        /// <remarks>This method wraps the native <c>SetSecurityInfo</c> function and ensures proper
        /// reference management for the provided handles. Callers are responsible for ensuring that the handles passed
        /// to this method are valid and not closed.</remarks>
        /// <param name="handle">A <see cref="SafeHandle"/> representing the handle to the object whose security information is being set.
        /// The handle must not be null or closed.</param>
        /// <param name="ObjectType">The type of object for which security information is being set. This is specified as a value of the <see
        /// cref="SE_OBJECT_TYPE"/> enumeration.</param>
        /// <param name="SecurityInfo">A bitmask of <see cref="OBJECT_SECURITY_INFORMATION"/> values that specify the type of security information
        /// to set (e.g., owner, group, DACL, or SACL).</param>
        /// <param name="psidOwner">An optional <see cref="SafeHandle"/> representing the new owner SID to set. Pass <c>null</c> to leave
        /// the owner unchanged.</param>
        /// <param name="psidGroup">An optional <see cref="SafeHandle"/> representing the new group SID to set. Pass <c>null</c> to leave
        /// the group unchanged.</param>
        /// <param name="pDacl">An optional <see cref="LocalFreeSafeHandle"/> representing the new discretionary access control list (DACL)
        /// to set. Pass <c>null</c> to leave the DACL unchanged.</param>
        /// <param name="pSacl">An optional <see cref="LocalFreeSafeHandle"/> representing the new system access control list (SACL) to set.
        /// Pass <c>null</c> to leave the SACL unchanged.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the operation succeeds.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handle"/> is null or closed.</exception>
        internal unsafe static WIN32_ERROR SetSecurityInfo(SafeHandle handle, SE_OBJECT_TYPE ObjectType, OBJECT_SECURITY_INFORMATION SecurityInfo, SafeHandle? psidOwner, SafeHandle? psidGroup, LocalFreeSafeHandle? pDacl, LocalFreeSafeHandle? pSacl)
        {
            bool handleAddRef = false;
            bool psidOwnerAddRef = false;
            bool psidGroupAddRef = false;
            bool pDaclAddRef = false;
            bool pSaclAddRef = false;
            try
            {
                if (handle is null || handle.IsClosed)
                {
                    throw new ArgumentNullException(nameof(handle));
                }
                if (psidOwner is not null && !psidOwner.IsClosed)
                {
                    psidOwner.DangerousAddRef(ref psidOwnerAddRef);
                }
                if (psidGroup is not null && !psidGroup.IsClosed)
                {
                    psidGroup.DangerousAddRef(ref psidGroupAddRef);
                }
                if (pDacl is not null && !pDacl.IsClosed)
                {
                    pDacl.DangerousAddRef(ref pDaclAddRef);
                }
                if (pSacl is not null && !pSacl.IsClosed)
                {
                    pSacl.DangerousAddRef(ref pSaclAddRef);
                }
                handle.DangerousAddRef(ref handleAddRef);
                var res = PInvoke.SetSecurityInfo((HANDLE)handle.DangerousGetHandle(), ObjectType, SecurityInfo, psidOwner is not null ? new PSID(psidOwner.DangerousGetHandle()) : (PSID)null, psidGroup is not null ? new PSID(psidGroup.DangerousGetHandle()) : (PSID)null, pDacl is not null ? (ACL*)pDacl.DangerousGetHandle() : (ACL*)null, pSacl is not null ? (ACL*)pSacl.DangerousGetHandle() : (ACL*)null);
                if (res != WIN32_ERROR.ERROR_SUCCESS)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
                }
                return res;
            }
            finally
            {
                if (pSaclAddRef)
                {
                    pSacl?.DangerousRelease();
                }
                if (pDaclAddRef)
                {
                    pDacl?.DangerousRelease();
                }
                if (psidGroupAddRef)
                {
                    psidGroup?.DangerousRelease();
                }
                if (psidOwnerAddRef)
                {
                    psidOwner?.DangerousRelease();
                }
                if (handleAddRef)
                {
                    handle.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Retrieves security information for a specified object, such as its owner, group, DACL, or SACL.
        /// </summary>
        /// <remarks>This method wraps the native <c>GetNamedSecurityInfo</c> function and provides a
        /// managed interface for retrieving security information. The caller is responsible for freeing any handles or
        /// memory returned by this method to avoid resource leaks.</remarks>
        /// <param name="pObjectName">The name of the object for which to retrieve security information. This can be a file, registry key, or
        /// other securable object.</param>
        /// <param name="ObjectType">The type of the object specified by <paramref name="pObjectName"/>. This determines how the object name is
        /// interpreted.</param>
        /// <param name="SecurityInfo">A combination of flags that specify the type of security information to retrieve, such as owner, group,
        /// DACL, or SACL.</param>
        /// <param name="ppsidOwner">When the method returns, contains a handle to the security identifier (SID) of the object's owner. This
        /// handle must be freed by the caller using the appropriate method.</param>
        /// <param name="ppsidGroup">When the method returns, contains a handle to the security identifier (SID) of the object's primary group.
        /// This handle must be freed by the caller using the appropriate method.</param>
        /// <param name="ppDacl">A pointer to a pointer that, when the method returns, contains the discretionary access control list (DACL)
        /// of the object. This value is null if the DACL is not requested or does not exist.</param>
        /// <param name="ppSacl">A pointer to a pointer that, when the method returns, contains the system access control list (SACL) of the
        /// object. This value is null if the SACL is not requested or does not exist.</param>
        /// <param name="ppSecurityDescriptor">When the method returns, contains a pointer to the security descriptor of the object. The caller is
        /// responsible for freeing this memory.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the operation succeeds.</returns>
        internal unsafe static WIN32_ERROR GetNamedSecurityInfo(string pObjectName, SE_OBJECT_TYPE ObjectType, OBJECT_SECURITY_INFORMATION SecurityInfo, out SafeNoReleaseHandle? ppsidOwner, out SafeNoReleaseHandle? ppsidGroup, out LocalFreeSafeHandle? ppDacl, out LocalFreeSafeHandle? ppSacl, out LocalFreeSafeHandle ppSecurityDescriptor)
        {
            fixed (char* pObjectNameLocal = pObjectName)
            {
                PSID psidOwner = default, pSidGroup = default; ACL* pDacl = null, pSacl = null; PSECURITY_DESCRIPTOR pSECURITY_DESCRIPTOR = default;
                var res = PInvoke.GetNamedSecurityInfo(pObjectNameLocal, ObjectType, SecurityInfo, &psidOwner, &pSidGroup, &pDacl, &pSacl, &pSECURITY_DESCRIPTOR);
                if (res != WIN32_ERROR.ERROR_SUCCESS)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
                }
                if (pSECURITY_DESCRIPTOR == default)
                {
                    throw new InvalidOperationException("Failed to retrieve security descriptor.");
                }
                ppsidOwner = psidOwner != default ? new((IntPtr)psidOwner.Value) : null;
                ppsidGroup = pSidGroup != default ? new((IntPtr)pSidGroup.Value) : null;
                ppDacl = pDacl is not null ? new((IntPtr)pDacl, false) : null;
                ppSacl = pSacl is not null ? new((IntPtr)pSacl, false) : null;
                ppSecurityDescriptor = new((IntPtr)pSECURITY_DESCRIPTOR, true);
                return res;
            }
        }

        /// <summary>
        /// Initializes a new Authz resource manager for managing access control and authorization operations.
        /// </summary>
        /// <param name="Flags">A set of flags that specify the behavior of the resource manager. This parameter can include values such as
        /// <see langword="0"/> for default behavior.</param>
        /// <param name="pfnDynamicAccessCheck">A callback function for performing dynamic access checks. This parameter can be <see langword="null"/> if no
        /// dynamic access checks are required.</param>
        /// <param name="pfnComputeDynamicGroups">A callback function for computing dynamic groups. This parameter can be <see langword="null"/> if no dynamic
        /// group computation is required.</param>
        /// <param name="pfnFreeDynamicGroups">A callback function for freeing resources allocated for dynamic groups. This parameter can be <see
        /// langword="null"/> if no dynamic group resources need to be freed.</param>
        /// <param name="szResourceManagerName">The name of the resource manager. This parameter cannot be <see langword="null"/> and must be a valid
        /// string.</param>
        /// <param name="phAuthzResourceManager">When this method returns, contains a handle to the initialized Authz resource manager. This handle must be
        /// released using the appropriate cleanup method.</param>
        /// <returns><see langword="true"/> if the resource manager was successfully initialized; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="Win32Exception">Thrown if the initialization fails due to a system error.</exception>
        internal static BOOL AuthzInitializeResourceManager(AUTHZ_RESOURCE_MANAGER_FLAGS Flags, PFN_AUTHZ_DYNAMIC_ACCESS_CHECK? pfnDynamicAccessCheck, PFN_AUTHZ_COMPUTE_DYNAMIC_GROUPS? pfnComputeDynamicGroups, PFN_AUTHZ_FREE_DYNAMIC_GROUPS? pfnFreeDynamicGroups, string szResourceManagerName, out AuthzFreeResourceManagerSafeHandle phAuthzResourceManager)
        {
            var res = PInvoke.AuthzInitializeResourceManager((uint)Flags, pfnDynamicAccessCheck, pfnComputeDynamicGroups, pfnFreeDynamicGroups, szResourceManagerName, out phAuthzResourceManager);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            if (phAuthzResourceManager.IsInvalid)
            {
                throw new InvalidOperationException("Failed to initialize Authz Resource Manager.");
            }
            return res;
        }

        /// <summary>
        /// Initializes an authorization context from a security identifier (SID).
        /// </summary>
        /// <remarks>This method wraps the native <c>AuthzInitializeContextFromSid</c> function and
        /// ensures proper error handling. It throws exceptions for common failure scenarios, such as invalid handles or
        /// system errors.</remarks>
        /// <param name="Flags">A combination of <see cref="AUTHZ_CONTEXT_FLAGS"/> values that specify the behavior of the authorization
        /// context.</param>
        /// <param name="UserSid">A <see cref="SafeHandle"/> representing the security identifier (SID) of the user for whom the context is
        /// being initialized. This parameter cannot be null.</param>
        /// <param name="hAuthzResourceManager">A <see cref="SafeHandle"/> representing the handle to the resource manager associated with the authorization
        /// context. This parameter cannot be null.</param>
        /// <param name="pExpirationTime">An optional expiration time for the context, specified as a nullable <see cref="long"/>. If null, the
        /// context does not expire.</param>
        /// <param name="Identifier">A <see cref="LUID"/> that uniquely identifies the authorization context.</param>
        /// <param name="DynamicGroupArgs">A pointer to application-defined data used to compute dynamic groups. This parameter can be null if no
        /// dynamic groups are required.</param>
        /// <param name="phAuthzClientContext">When this method returns, contains an <see cref="AuthzFreeContextSafeHandle"/> representing the initialized
        /// authorization context. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the authorization context is successfully initialized; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="Win32Exception">Thrown if the initialization fails due to a Win32 error, or if the resulting authorization context is
        /// invalid.</exception>
        internal unsafe static BOOL AuthzInitializeContextFromSid(AUTHZ_CONTEXT_FLAGS Flags, SafeHandle UserSid, SafeHandle hAuthzResourceManager, long? pExpirationTime, LUID Identifier, IntPtr DynamicGroupArgs, out AuthzFreeContextSafeHandle phAuthzClientContext)
        {
            bool UserSidAddRef = false;
            try
            {
                UserSid.DangerousAddRef(ref UserSidAddRef);
                var res = PInvoke.AuthzInitializeContextFromSid((uint)Flags, new(UserSid.DangerousGetHandle()), hAuthzResourceManager, pExpirationTime, Identifier, DynamicGroupArgs.ToPointer(), out phAuthzClientContext);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                if (phAuthzClientContext.IsInvalid)
                {
                    throw new InvalidOperationException("Failed to initialize Authz Client Context from SID.");
                }
                return res;
            }
            finally
            {
                if (UserSidAddRef)
                {
                    UserSid.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Initializes an authorization context from a specified token.
        /// </summary>
        /// <param name="Flags">A combination of <see cref="AUTHZ_CONTEXT_FLAGS"/> values that specify the behavior of the authorization
        /// context.</param>
        /// <param name="TokenHandle">A handle to the token from which the authorization context is initialized. This handle must be valid and
        /// cannot be null.</param>
        /// <param name="hAuthzResourceManager">A handle to the resource manager associated with the authorization context. This handle must be valid and
        /// cannot be null.</param>
        /// <param name="pExpirationTime">An optional expiration time for the authorization context, specified as a <see cref="long"/> value. If
        /// null, the context does not have an expiration time.</param>
        /// <param name="Identifier">A <see cref="LUID"/> that uniquely identifies the authorization context.</param>
        /// <param name="DynamicGroupArgs">A pointer to dynamic group arguments used during the initialization of the authorization context. This
        /// value can be <see cref="IntPtr.Zero"/> if no dynamic group arguments are provided.</param>
        /// <param name="phAuthzClientContext">When this method returns, contains an <see cref="AuthzFreeContextSafeHandle"/> representing the initialized
        /// authorization context. This handle must be released by the caller when no longer needed.</param>
        /// <returns><see langword="true"/> if the authorization context is successfully initialized; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the authorization context is initialized but the resulting handle is invalid.</exception>
        internal unsafe static BOOL AuthzInitializeContextFromToken(AUTHZ_CONTEXT_FLAGS Flags, SafeHandle TokenHandle, SafeHandle hAuthzResourceManager, long? pExpirationTime, LUID Identifier, IntPtr DynamicGroupArgs, out AuthzFreeContextSafeHandle phAuthzClientContext)
        {
            var res = PInvoke.AuthzInitializeContextFromToken((uint)Flags, TokenHandle, hAuthzResourceManager, pExpirationTime, Identifier, DynamicGroupArgs.ToPointer(), out phAuthzClientContext);
            if (!res)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error();
            }
            if (phAuthzClientContext.IsInvalid)
            {
                throw new InvalidOperationException("Failed to initialize Authz Client Context from Token.");
            }
            return res;
        }

        /// <summary>
        /// Performs an access check using the specified client context, access request, and security descriptors.
        /// </summary>
        /// <remarks>This method wraps the native <c>AuthzAccessCheck</c> function and performs additional
        /// error handling to ensure proper resource management. The caller must ensure that all input handles and
        /// structures are valid and properly initialized before calling this method.</remarks>
        /// <param name="Flags">Flags that specify the behavior of the access check. This parameter can include one or more values from the
        /// <see cref="AUTHZ_ACCESS_CHECK_FLAGS"/> enumeration.</param>
        /// <param name="hAuthzClientContext">A handle to the client context used for the access check. This handle must be valid and initialized.</param>
        /// <param name="pRequest">The access request structure that specifies the desired access rights and other parameters for the access
        /// check.</param>
        /// <param name="hAuditEvent">An optional handle to an audit event. If provided, the access check may generate audit events based on the
        /// result.</param>
        /// <param name="pSecurityDescriptor">A handle to the primary security descriptor against which the access check is performed. This handle must be
        /// valid and properly initialized.</param>
        /// <param name="OptionalSecurityDescriptorArray">An optional array of additional security descriptors to be considered during the access check. This
        /// parameter can be empty if no additional descriptors are required.</param>
        /// <param name="pReply">A reference to an <see cref="AUTHZ_ACCESS_REPLY"/> structure that receives the results of the access check,
        /// including granted access rights and any error information.</param>
        /// <param name="phAccessCheckResults">When the method returns, contains a handle to the access check results. The caller is responsible for
        /// releasing this handle when it is no longer needed.</param>
        /// <returns><see langword="true"/> if the access check is successful and the results are valid; otherwise, <see
        /// langword="false"/>.</returns>
        /// <exception cref="Win32Exception">Thrown if the access check fails or if the results handle is invalid.</exception>
        internal unsafe static BOOL AuthzAccessCheck(AUTHZ_ACCESS_CHECK_FLAGS Flags, SafeHandle hAuthzClientContext, in AUTHZ_ACCESS_REQUEST pRequest, SafeHandle? hAuditEvent, LocalFreeSafeHandle pSecurityDescriptor, ReadOnlySpan<PSECURITY_DESCRIPTOR> OptionalSecurityDescriptorArray, ref AUTHZ_ACCESS_REPLY pReply, out AuthzFreeHandleSafeHandle phAccessCheckResults)
        {
            bool pSecurityDescriptorAddRef = false;
            try
            {
                pSecurityDescriptor.DangerousAddRef(ref pSecurityDescriptorAddRef);
                var res = PInvoke.AuthzAccessCheck(Flags, hAuthzClientContext, in pRequest, hAuditEvent, (PSECURITY_DESCRIPTOR)pSecurityDescriptor.DangerousGetHandle(), OptionalSecurityDescriptorArray, ref pReply, out phAccessCheckResults);
                if (!res)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error();
                }
                if (phAccessCheckResults.IsInvalid)
                {
                    throw new InvalidOperationException("Failed to perform Authz Access Check.");
                }
                return res;
            }
            finally
            {
                if (pSecurityDescriptorAddRef)
                {
                    pSecurityDescriptor.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Renames a subkey of the specified registry key.
        /// </summary>
        /// <param name="hKey">A handle to an open registry key. This handle must have the appropriate access rights for the operation.</param>
        /// <param name="lpSubKeyName">The name of the subkey to be renamed. This cannot be <see langword="null"/> or an empty string.</param>
        /// <param name="lpNewKeyName">The new name for the subkey. This cannot be <see langword="null"/> or an empty string.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the operation succeeds.</returns>
        internal static WIN32_ERROR RegRenameKey(SafeHandle hKey, string? lpSubKeyName, string lpNewKeyName)
        {
            var res = PInvoke.RegRenameKey(hKey, lpSubKeyName, lpNewKeyName);
            if (res != WIN32_ERROR.ERROR_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            return res;
        }

        /// <summary>
        /// Opens a handle to the Local Security Authority (LSA) Policy object on a specified system.
        /// </summary>
        /// <remarks>This method wraps the native LsaOpenPolicy function and provides error handling by
        /// throwing a <see cref="Win32Exception"/> if the operation fails. Ensure that the caller has the necessary 
        /// privileges to access the specified policy object.</remarks>
        /// <param name="SystemName">An optional <see cref="LSA_UNICODE_STRING"/> that specifies the name of the system whose LSA Policy object
        /// is to be opened. If <see langword="null"/>, the local system's LSA Policy object is opened.</param>
        /// <param name="ObjectAttributes">A reference to an <see cref="LSA_OBJECT_ATTRIBUTES"/> structure that specifies attributes for the policy
        /// object. This parameter is typically initialized to default values.</param>
        /// <param name="DesiredAccess">A bitmask specifying the access rights requested for the policy object. Use constants defined in the
        /// LSA_POLICY_ACCESS enumeration to specify the desired access.</param>
        /// <param name="PolicyHandle">When this method returns, contains a <see cref="LsaCloseSafeHandle"/> that represents the opened policy
        /// object. The caller is responsible for closing this handle using the appropriate method.</param>
        /// <returns>An <see cref="NTSTATUS"/> value indicating the result of the operation. Returns <see
        /// cref="NTSTATUS.STATUS_SUCCESS"/> if the operation is successful.</returns>
        /// <exception cref="Win32Exception">Thrown if the operation fails. The exception's error code corresponds to the Windows error code derived
        /// from the returned <see cref="NTSTATUS"/> value.</exception>
        internal static NTSTATUS LsaOpenPolicy(LSA_UNICODE_STRING? SystemName, in LSA_OBJECT_ATTRIBUTES ObjectAttributes, LSA_POLICY_ACCESS DesiredAccess, out LsaCloseSafeHandle PolicyHandle)
        {
            var res = PInvoke.LsaOpenPolicy(SystemName, in ObjectAttributes, (uint)DesiredAccess, out PolicyHandle);
            if (res != NTSTATUS.STATUS_SUCCESS)
            {
                throw new Win32Exception((int)PInvoke.LsaNtStatusToWinError(res));
            }
            return res;
        }

        /// <summary>
        /// Queries information from a policy object based on the specified information class.
        /// </summary>
        /// <remarks>This method wraps the native LSA (Local Security Authority) function
        /// <c>LsaQueryInformationPolicy</c>. The caller must ensure that the <paramref name="PolicyHandle"/> is valid
        /// and has the necessary access rights. The returned buffer must be freed using the appropriate mechanism to
        /// avoid memory leaks.</remarks>
        /// <param name="PolicyHandle">A handle to the policy object from which information is to be queried. This handle must have the appropriate
        /// access rights for the requested information.</param>
        /// <param name="InformationClass">The class of information to query. This determines the type of policy information returned.</param>
        /// <param name="Buffer">When this method returns, contains a handle to the buffer that holds the queried policy information. The
        /// caller is responsible for freeing this buffer.</param>
        /// <returns>An <see cref="NTSTATUS"/> value indicating the result of the operation. A value of <see
        /// cref="NTSTATUS.STATUS_SUCCESS"/> indicates success.</returns>
        /// <exception cref="Win32Exception">Thrown if the operation fails, wrapping the corresponding Windows error code.</exception>
        internal static unsafe NTSTATUS LsaQueryInformationPolicy(SafeHandle PolicyHandle, POLICY_INFORMATION_CLASS InformationClass, out SafeLsaFreeMemoryHandle Buffer)
        {
            var res = PInvoke.LsaQueryInformationPolicy(PolicyHandle, InformationClass, out var BufferLocal);
            if (res != NTSTATUS.STATUS_SUCCESS)
            {
                throw new Win32Exception((int)PInvoke.LsaNtStatusToWinError(res));
            }
            Buffer = new((IntPtr)BufferLocal, true);
            return res;
        }
    }
}

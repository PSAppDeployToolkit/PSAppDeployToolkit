using System;
using System.Linq;
using System.Security;
using System.Diagnostics;
using System.ComponentModel;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.PInvoke;
using PSADT.Logging;

namespace PSADT.AccessToken
{
    /// <summary>
    /// Manages Windows access tokens and their operations.
    /// </summary>
    public static class TokenManager
    {
        /// <summary>
        /// Retrieves the security identification token for the specified session ID.
        /// </summary>
        /// <param name="sessionId">The session ID to query the user token for.</param>
        /// <param name="securityIdentificationToken">When this method returns, contains the security identification token if the operation was successful.</param>
        /// <returns><c>true</c> if the token was retrieved successfully; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool GetSecurityIdentificationTokenForSessionId(uint sessionId, out SafeAccessToken securityIdentificationToken)
        {
            securityIdentificationToken = SafeAccessToken.Invalid;

            using var processToken = GetCurrentProcessToken();
            bool isSeTcbPrivilegeEnabled = PrivilegeManager.IsPrivilegeEnabled(processToken, TokenPrivilege.TrustedComputerBase);

            try
            {
                if (isSeTcbPrivilegeEnabled)
                {
                    if (!NativeMethods.WTSQueryUserToken(sessionId, out securityIdentificationToken))
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new Win32Exception(errorCode, $"'WTSQueryUserToken' failed to obtain the access token for session ID [{sessionId}] with error code [{errorCode}].");
                    }
                }
                else
                {
                    SafeAccessToken tempToken = SafeAccessToken.Invalid;

                    ImpersonateSystem(() =>
                    {
                        if (!NativeMethods.WTSQueryUserToken(sessionId, out tempToken))
                        {
                            int errorCode = Marshal.GetLastWin32Error();
                            throw new Win32Exception(errorCode, $"'WTSQueryUserToken' failed to obtain the access token for session ID [{sessionId}] during SYSTEM impersonation. Error code: {errorCode}");
                        }
                    }, sessionId);

                    securityIdentificationToken = tempToken;
                }

                UnifiedLogger.Create().Message($"User token queried successfully for session ID [{sessionId}].").Severity(LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to query user token for session ID [{sessionId}].").Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Creates a primary token with full access rights.
        /// </summary>
        /// <param name="token">The source token to duplicate.</param>
        /// <param name="primaryToken">When this method returns, contains the duplicated primary token if successful.</param>
        /// <returns><c>true</c> if the token was duplicated successfully; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool CreatePrimaryToken(SafeAccessToken token, out SafeAccessToken primaryToken)
        {
            if (token == null || token.IsInvalid || token.IsClosed) throw new ArgumentNullException(nameof(token));
            return SafeDuplicateToken(
                token,
                TokenAccess.TOKEN_ALL_ACCESS | TokenAccess.TOKEN_QUERY | TokenAccess.TOKEN_DUPLICATE,
                SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                TOKEN_TYPE.TokenPrimary,
                out primaryToken);
        }

        /// <summary>
        /// Creates an impersonation token with full access rights.
        /// </summary>
        /// <param name="token">The source token to duplicate.</param>
        /// <param name="impersonationToken">When this method returns, contains the duplicated impersonation token if successful.</param>
        /// <returns><c>true</c> if the token was duplicated successfully; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool CreateImpersonationToken(SafeAccessToken token, out SafeAccessToken impersonationToken)
        {
            if (token == null || token.IsInvalid || token.IsClosed) throw new ArgumentNullException(nameof(token));
            return SafeDuplicateToken(
                token,
                TokenAccess.TOKEN_ALL_ACCESS | TokenAccess.TOKEN_QUERY | TokenAccess.TOKEN_DUPLICATE,
                SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                TOKEN_TYPE.TokenImpersonation,
                out impersonationToken);
        }

        /// <summary>
        /// Safely duplicates a token with specific access rights and type.
        /// </summary>
        [SecurityCritical]
        private static bool SafeDuplicateToken(
            SafeAccessToken sourceToken,
            TokenAccess desiredAccess,
            SECURITY_IMPERSONATION_LEVEL impersonationLevel,
            TOKEN_TYPE tokenType,
            out SafeAccessToken duplicatedToken)
        {
            duplicatedToken = SafeAccessToken.Invalid;

            try
            {
                if (!NativeMethods.DuplicateTokenEx(
                    sourceToken,
                    desiredAccess,
                    SECURITY_ATTRIBUTES.Create(),
                    impersonationLevel,
                    tokenType,
                    out duplicatedToken))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error);
                }

                if (!duplicatedToken.IsInvalid)
                {
                    UnifiedLogger.Create().Message("Token duplicated successfully.").Severity(LogLevel.Debug);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to duplicate token: {ex.Message}").Error(ex);
                duplicatedToken?.Dispose();
                duplicatedToken = SafeAccessToken.Invalid;
                return false;
            }
        }

        [SecurityCritical]
        private static void ImpersonateSystem(Action action, uint sessionId)
        {
            bool isImpersonating = false;

            try
            {
                NativeMethods.ProcessIdToSessionId(sessionId, out uint winlogonSessionId);

                // Locate a SYSTEM process (e.g., winlogon.exe) from the target session id
                Process? targetProcess = Process.GetProcessesByName("winlogon")
                    .FirstOrDefault(p =>
                    {
                        if (NativeMethods.ProcessIdToSessionId((uint)p.Id, out uint processSessionId))
                        {
                            return processSessionId == sessionId;
                        }
                        return false;
                    });

                if (targetProcess == null)
                {
                    throw new InvalidOperationException("Unable to find SYSTEM process 'winlogon.exe'.");
                }

                // Open a handle to the SYSTEM process
                SafeProcessHandle systemProcessHandle = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_INFORMATION, false, (uint)targetProcess.Id);
                if (systemProcessHandle.IsInvalid || systemProcessHandle.IsClosed)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open handle to SYSTEM process.");
                }

                // Open the process token
                SafeAccessToken systemProcessToken = SafeAccessToken.Invalid;
                using (systemProcessHandle)
                {
                    var tokenAccess = TokenAccess.TOKEN_DUPLICATE | TokenAccess.TOKEN_QUERY | TokenAccess.TOKEN_IMPERSONATE;
                    if (!NativeMethods.OpenProcessToken(systemProcessHandle, tokenAccess, out systemProcessToken))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open process token for SYSTEM process.");
                    }
                }

                SafeAccessToken primarySystemToken = SafeAccessToken.Invalid;
                using (systemProcessToken)
                {
                    if (!CreatePrimaryToken(systemProcessToken, out primarySystemToken))
                    {
                        throw new InvalidOperationException("Failed to duplicate the SYSTEM token as a primary token.");
                    }
                }

                using (primarySystemToken)
                {
                    // Impersonate SYSTEM
                    if (!NativeMethods.ImpersonateLoggedOnUser(primarySystemToken))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to impersonate SYSTEM.");
                    }

                    isImpersonating = true;

                    // Execute the action as SYSTEM
                    action();
                }
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Error during SYSTEM impersonation.").Error(ex);
                throw;
            }
            finally
            {
                // Revert impersonation, if necessary
                if (isImpersonating && !NativeMethods.RevertToSelf())
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to revert to the original security context.");
                }
            }
        }

        /// <summary>
        /// Attempts to create a <see cref="WindowsIdentity"/> from the specified token.
        /// </summary>
        /// <param name="token">The access token to create the identity from.</param>
        /// <param name="windowsIdentity">When this method returns, contains the <see cref="WindowsIdentity"/> if successful.</param>
        /// <returns><c>true</c> if the identity was created successfully; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool TryGetWindowsIdentity(SafeAccessToken token, out WindowsIdentity? windowsIdentity)
        {
            if (token == null || token.IsInvalid || token.IsClosed) throw new ArgumentNullException(nameof(token));

            windowsIdentity = null;

            try
            {
                windowsIdentity = new WindowsIdentity(token.DangerousGetHandle());
                UnifiedLogger.Create().Message("Successfully created WindowsIdentity from token.").Severity(LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to get WindowsIdentity from token.").Error(ex);
                windowsIdentity?.Dispose();
                return false;
            }
        }

        /// <summary>
        /// Attempts to create a <see cref="WindowsPrincipal"/> from the specified token.
        /// </summary>
        /// <param name="token">The access token to create the principal from.</param>
        /// <param name="windowsPrincipal">When this method returns, contains the <see cref="WindowsPrincipal"/> if successful.</param>
        /// <returns><c>true</c> if the principal was created successfully; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool TryGetWindowsPrincipal(SafeAccessToken token, out WindowsPrincipal? windowsPrincipal)
        {
            windowsPrincipal = null;

            if (TryGetWindowsIdentity(token, out var windowsIdentity))
            {
                if (windowsIdentity == null)
                    return false;

                try
                {
                    return TryGetWindowsPrincipal(windowsIdentity, out windowsPrincipal);
                }
                finally
                {
                    windowsIdentity?.Dispose();
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to create a <see cref="WindowsPrincipal"/> from the specified Windows identity.
        /// </summary>
        /// <param name="windowsIdentity">The Windows identity to create the principal from.</param>
        /// <param name="windowsPrincipal">When this method returns, contains the <see cref="WindowsPrincipal"/> if successful.</param>
        /// <returns><c>true</c> if the principal was created successfully; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool TryGetWindowsPrincipal(WindowsIdentity windowsIdentity, out WindowsPrincipal? windowsPrincipal)
        {
            if (windowsIdentity == null) throw new ArgumentNullException(nameof(windowsIdentity));

            windowsPrincipal = null;

            try
            {
                windowsPrincipal = new WindowsPrincipal(windowsIdentity);
                UnifiedLogger.Create().Message("Successfully created WindowsPrincipal from WindowsIdentity.").Severity(LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to create WindowsPrincipal from WindowsIdentity.").Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the specified type of information about an access token.
        /// </summary>
        [SecurityCritical]
        private static T GetTokenInformation<T>(SafeAccessToken token, TOKEN_INFORMATION_CLASS tokenInformationClass) where T : struct
        {
            if (token == null || token.IsInvalid || token.IsClosed) throw new ArgumentNullException(nameof(token));

            bool result = NativeMethods.GetTokenInformation(token, tokenInformationClass, IntPtr.Zero, 0, out int bufferSize);
            int error = Marshal.GetLastWin32Error();

            if (!result && error != NativeMethods.ERROR_INSUFFICIENT_BUFFER)
            {
                throw new Win32Exception(error);
            }

            using var buffer = new SafeHGlobalHandle(bufferSize);

            if (!NativeMethods.GetTokenInformation(token, tokenInformationClass, buffer.DangerousGetHandle(), bufferSize, out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return Marshal.PtrToStructure<T>(buffer.DangerousGetHandle());
        }

        /// <summary>
        /// Retrieves the elevation type of the specified access token.
        /// </summary>
        /// <param name="token">The access token to retrieve the elevation type from.</param>
        /// <returns>A <see cref="TOKEN_ELEVATION_TYPE"/> value indicating the elevation type of the token.</returns>
        /// <exception cref="ArgumentNullException">Thrown when tokenHandle is null.</exception>
        /// <exception cref="Win32Exception">Thrown when the elevation type cannot be retrieved.</exception>
        [SecurityCritical]
        public static TOKEN_ELEVATION_TYPE GetTokenElevationType(SafeAccessToken token)
        {
            if (token == null || token.IsInvalid || token.IsClosed) throw new ArgumentNullException(nameof(token));

            try
            {
                var elevationType = GetTokenInformation<TOKEN_ELEVATION_TYPE>(token, TOKEN_INFORMATION_CLASS.TokenElevationType);
                if (elevationType != 0)
                    return elevationType;

                var elevation = GetTokenInformation<TOKEN_ELEVATION>(token, TOKEN_INFORMATION_CLASS.TokenElevation);
                return elevation.TokenIsElevated ? TOKEN_ELEVATION_TYPE.TokenElevationTypeFull : TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to get token elevation type.").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Determines whether the specified access token is elevated.
        /// </summary>
        /// <param name="token">The access token to check.</param>
        /// <returns><c>true</c> if the token is elevated; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool IsTokenElevated(SafeAccessToken token)
        {
            if (token == null || token.IsInvalid || token.IsClosed) throw new ArgumentNullException(nameof(token));

            try
            {
                TOKEN_ELEVATION elevation = GetTokenInformation<TOKEN_ELEVATION>(token, TOKEN_INFORMATION_CLASS.TokenElevation);
                return elevation.TokenIsElevated;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to determine if token is elevated.").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Determines whether UAC is enabled on this system.
        /// </summary>
        /// <returns><c>true</c> if UAC is enabled; otherwise, <c>false</c>.</returns>
        public static bool IsUACEnabled()
        {
            if (Environment.OSVersion.Version.Major < 6)
                return false;

            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", false);
                if (key != null)
                {
                    var uacValue = key.GetValue("EnableLUA");
                    return uacValue != null && Convert.ToInt32(uacValue) != 0;
                }
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to check UAC status.").Error(ex).Severity(LogLevel.Warning);
            }

            return false;
        }

        /// <summary>
        /// Retrieves the linked elevated token associated with the specified token, if available.
        /// </summary>
        /// <param name="token">The token to retrieve the linked elevated token from.</param>
        /// <param name="elevatedLinkedToken">When this method returns, contains the linked elevated token if available.</param>
        /// <returns><c>true</c> if the linked elevated token was retrieved; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool GetLinkedElevatedToken(SafeAccessToken token, out SafeAccessToken elevatedLinkedToken)
        {
            if (token == null || token.IsInvalid || token.IsClosed) throw new ArgumentNullException(nameof(token));
            elevatedLinkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_ELEVATION_TYPE elevationType = GetTokenElevationType(token);

                // If already elevated, return the token itself
                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull)
                {
                    elevatedLinkedToken = token;
                    return true;
                }

                // If default elevation type, no linked token exists
                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
                {
                    return false;
                }

                // Only proceed if system supports linked tokens
                if (Environment.OSVersion.Version.Major >= 6 && IsUACEnabled())
                {
                    // If limited, get the linked elevated token
                    if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited)
                    {
                        return GetLinkedToken(token, out elevatedLinkedToken);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get linked elevated token:").Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the linked standard token associated with the specified token, if available.
        /// </summary>
        /// <param name="token">The token to retrieve the linked standard token from.</param>
        /// <param name="standardLinkedToken">When this method returns, contains the linked standard token if available.</param>
        /// <returns><c>true</c> if the linked standard token was retrieved; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool GetLinkedStandardToken(SafeAccessToken token, out SafeAccessToken standardLinkedToken)
        {
            if (token == null || token.IsInvalid || token.IsClosed) throw new ArgumentNullException(nameof(token));
            standardLinkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_ELEVATION_TYPE elevationType = GetTokenElevationType(token);

                // If already limited, return the token itself
                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited)
                {
                    standardLinkedToken = token;
                    return true;
                }

                // If default elevation type, no linked token exists
                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
                {
                    return false;
                }

                // Only proceed if system supports linked tokens
                if (Environment.OSVersion.Version.Major >= 6 && IsUACEnabled())
                {
                    // If elevated, get the linked limited token
                    if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull)
                    {
                        return GetLinkedToken(token, out standardLinkedToken);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get linked standard token:").Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the linked token associated with a given access token, if available.
        /// </summary>
        /// <param name="token">The token to retrieve the linked token for.</param>
        /// <param name="linkedToken">When this method returns, contains the linked token if available.</param>
        /// <returns><c>true</c> if the linked token was retrieved; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        private static bool GetLinkedToken(SafeAccessToken token, out SafeAccessToken linkedToken)
        {
            if (token == null || token.IsInvalid || token.IsClosed) throw new ArgumentNullException(nameof(token));
            linkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_LINKED_TOKEN tokenLinkedToken = GetTokenInformation<TOKEN_LINKED_TOKEN>(token, TOKEN_INFORMATION_CLASS.TokenLinkedToken);

                if (tokenLinkedToken.LinkedToken == IntPtr.Zero)
                {
                    UnifiedLogger.Create().Message("No linked token found.").Severity(LogLevel.Debug);
                    return false;
                }

                linkedToken = new SafeAccessToken(tokenLinkedToken.LinkedToken);
                UnifiedLogger.Create().Message("Linked token retrieved successfully.").Severity(LogLevel.Debug);

                return true;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == NativeMethods.ERROR_NO_SUCH_LOGON_SESSION ||
                                          ex.NativeErrorCode == NativeMethods.ERROR_NOT_FOUND)
            {
                // These error codes indicate that there's no linked token, which is not necessarily an error
                UnifiedLogger.Create().Message($"No linked token found. Error code [{ex.NativeErrorCode}].").Severity(LogLevel.Debug);
                return false;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get linked token:").Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Determines whether the specified access token belongs to a user who is a member of the local Administrators group.
        /// </summary>
        /// <param name="token">The access token to check.</param>
        /// <returns><c>true</c> if the token belongs to a local administrator; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool IsTokenLocalAdmin(SafeAccessToken token)
        {
            if (token == null || token.IsInvalid || token.IsClosed) throw new ArgumentNullException(nameof(token));

            try
            {
                // First check if the current token is an admin
                if (TryGetWindowsPrincipal(token, out WindowsPrincipal? userPrincipal))
                {
                    if (userPrincipal!.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        return true;
                    }
                }

                // If not, check if there's a linked elevated token that is an admin
                if (GetLinkedElevatedToken(token, out SafeAccessToken linkedToken))
                {
                    using (linkedToken)
                    {
                        if (!linkedToken.IsInvalid && TryGetWindowsPrincipal(linkedToken, out WindowsPrincipal? linkedPrincipal))
                        {
                            return linkedPrincipal!.IsInRole(WindowsBuiltInRole.Administrator);
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to determine if token is local admin:").Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Creates an environment block for the specified user token.
        /// </summary>
        /// <param name="token">The user token to create the environment block for.</param>
        /// <param name="additionalVariables">Additional environment variables to include.</param>
        /// <param name="inherit">Whether to inherit the parent's environment variables.</param>
        /// <returns>A SafeEnvironmentBlock containing the created environment block.</returns>
        [SecurityCritical]
        public static SafeEnvironmentBlock CreateTokenEnvironmentBlock(
            SafeAccessToken token,
            IDictionary<string, string>? additionalVariables = null,
            bool inherit = true)
        {
            if (token == null || token.IsInvalid || token.IsClosed) throw new ArgumentNullException(nameof(token));

            try
            {
                if (!NativeMethods.CreateEnvironmentBlock(out SafeEnvironmentBlock envBlock, token, inherit))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (additionalVariables?.Count > 0)
                {
                    var environmentVars = ConvertEnvironmentBlockToDictionary(envBlock);
                    foreach (var kvp in additionalVariables)
                    {
                        environmentVars[kvp.Key] = kvp.Value;
                    }

                    envBlock.Dispose();
                    envBlock = CreateEnvironmentBlockFromDictionary(environmentVars);
                }

                return envBlock;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to create environment block.").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Converts an environment block to a dictionary.
        /// </summary>
        private static Dictionary<string, string> ConvertEnvironmentBlockToDictionary(SafeEnvironmentBlock envBlock)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            IntPtr ptr = envBlock.DangerousGetHandle();
            int offset = 0;

            while (true)
            {
                string entry = Marshal.PtrToStringUni(IntPtr.Add(ptr, offset)) ?? string.Empty;
                if (string.IsNullOrEmpty(entry)) break;

                int equalsIndex = entry.IndexOf('=');
                if (equalsIndex > 0)
                {
                    string key = entry.Substring(0, equalsIndex);
                    string value = entry.Substring(equalsIndex + 1);
                    result[key] = value;
                }

                offset += (entry.Length + 1) * sizeof(char);
            }

            UnifiedLogger.Create().Message($"Converted environment block to dictionary with [{result.Count}] entries.").Severity(LogLevel.Debug);

            return result;
        }

        /// <summary>
        /// Creates an environment block from a dictionary.
        /// </summary>
        private static SafeEnvironmentBlock CreateEnvironmentBlockFromDictionary(Dictionary<string, string> environmentVars)
        {
            var environmentString = string.Join("\0", environmentVars.Select(kvp => $"{kvp.Key}={kvp.Value}")) + "\0\0";
            var environmentBytes = System.Text.Encoding.Unicode.GetBytes(environmentString);

            var envBlockPtr = Marshal.AllocHGlobal(environmentBytes.Length);
            Marshal.Copy(environmentBytes, 0, envBlockPtr, environmentBytes.Length);

            UnifiedLogger.Create().Message($"Created environment block from dictionary with [{environmentVars.Count}] entries.").Severity(LogLevel.Debug);

            return new SafeEnvironmentBlock(envBlockPtr);
        }

        /// <summary>
        /// Gets the current process token with elevated access rights.
        /// </summary>
        internal static SafeAccessToken GetCurrentProcessToken()
        {
            SafeAccessToken processToken = SafeAccessToken.Invalid;

            // Request additional access rights
            var tokenAccess = TokenAccess.TOKEN_ADJUST_PRIVILEGES |
                              TokenAccess.TOKEN_QUERY |
                              TokenAccess.TOKEN_DUPLICATE |
                              TokenAccess.TOKEN_ASSIGN_PRIMARY;

            if (!NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(), tokenAccess, out processToken))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open process token.");
            }
            return processToken;
        }
    }
}

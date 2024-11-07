using System;
using System.Linq;
using PSADT.PInvoke;
using System.Security;
using System.ComponentModel;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PSADT.Logging;

namespace PSADT.AccessToken
{
    public static class TokenManager
    {
        /// <summary>
        /// Get's the token for a given session id. Returns a token with a SecurityIdentification level.
        /// Can obtain the client's security information but cannot fully impersonate the client.
        /// </summary>
        /// <param name="sessionId">The session ID to query the user token for.</param>
        /// <returns><c>true</c> if we obtained the access token of the log-on user specified by the session ID.</returns>
        /// <exception cref="Win32Exception">Thrown if we fail to obtain the access token.</exception>
        [SecurityCritical]
        public static bool GetSecurityIdentificationTokenForSessionId(uint sessionId, out SafeAccessToken securityIdentificationToken)
        {
            try
            {
                if (!NativeMethods.WTSQueryUserToken(sessionId, out securityIdentificationToken))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"'WTSQueryUserToken' failed to obtain the access token of the logged-on user specified by the session id [{sessionId}].");
                }

                UnifiedLogger.Create().Message($"User token queried successfully for session id [{sessionId}].").Severity(LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to query user token for session id [{sessionId}].").Error(ex);
                securityIdentificationToken = SafeAccessToken.Invalid;
                return false;
            }
        }

        /// <summary>
        /// Duplicates a given access token as an primary token. Primarily used to create new processes
        /// in the user's context, allowing the process to run as if the user themselves launched it.
        /// </summary>
        /// <param name="token">The token to duplicate.</param>
        /// <returns><c>true</c> if token duplication was successful.</returns>
        /// <exception cref="Win32Exception">Thrown if the token duplication fails.</exception>
        [SecurityCritical]
        public static bool CreatePrimaryToken(SafeAccessToken token, out SafeAccessToken primaryToken)
        {
            try
            {
                if (!NativeMethods.DuplicateTokenEx(
                    token,
                    TokenAccess.TOKEN_ALL_ACCESS,
                    SECURITY_ATTRIBUTES.Create(),
                    SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    TOKEN_TYPE.TokenPrimary,
                    out primaryToken))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"'DuplicateTokenEx' failed to create a primary access token from the existing token.");
                }

                UnifiedLogger.Create().Message($"Successfully duplicated token as a primary token.").Severity(LogLevel.Debug);

                // This assumes that the caller had no further use for the token.
                if (!token.IsInvalid)
                {
                    token.Dispose();
                }

                return true;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to duplicate token as primary token.").Error(ex);
                primaryToken = SafeAccessToken.Invalid;
                return false;
            }
        }

        /// <summary>
        /// Duplicates a given access token as an impersonation token. The impersonation
        /// token allows a process or thread to temporarily impersonate the user's
        /// security contex but cannot be used to create a new process.
        /// </summary>
        /// <param name="token">The token to duplicate.</param>
        /// <returns><c>true</c> if token duplication was successful.</returns>
        /// <exception cref="Win32Exception">Thrown if the token duplication fails.</exception>
        [SecurityCritical]
        public static bool CreateImpersonationToken(SafeAccessToken token, out SafeAccessToken impersonationToken)
        {
            try
            {
                if (!NativeMethods.DuplicateTokenEx(
                    token,
                    TokenAccess.TOKEN_ALL_ACCESS,
                    SECURITY_ATTRIBUTES.Create(),
                    SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    TOKEN_TYPE.TokenImpersonation,
                    out impersonationToken))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"'DuplicateTokenEx' failed to create an impersonation token from the existing token.");
                }

                UnifiedLogger.Create().Message($"Successfully duplicated token as a primary token.").Severity(LogLevel.Debug);

                // This assumes that the caller had no further use for the token.
                if (!token.IsInvalid)
                {
                    token.Dispose();
                }

                return true;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to duplicate token as an impersonation token.").Error(ex);
                impersonationToken = SafeAccessToken.Invalid;
                return false;
            }
        }

        public static bool TryGetWindowsIdentity(in SafeAccessToken token, out WindowsIdentity? windowsIdentity, out WindowsPrincipal? windowsPrincipal)
        {
            windowsIdentity = null;
            windowsPrincipal = null;

            try
            {
                windowsIdentity = new WindowsIdentity(token.DangerousGetHandle());
                UnifiedLogger.Create().Message($"Successfully duplicated token as a primary token.").Severity(LogLevel.Debug);

                windowsPrincipal = new WindowsPrincipal(windowsIdentity);
                UnifiedLogger.Create().Message($"Successfully duplicated token as a primary token.").Severity(LogLevel.Debug);

                return true;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get WindowsIdentity or WindowsPrincipal from token.").Error(ex);
                return false;
            }
        }

        /// <summary>Determines whether UAC is enabled on this system.</summary>
		/// <returns><c>true</c> if UAC is enabled; otherwise, <c>false</c>.</returns>
		public static bool IsUACEnabled()
        {
            if (Environment.OSVersion.Version.Major < 6)
                return false;

            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", false))
                {
                    if (key != null)
                    {
                        var uacValue = key.GetValue("EnableLUA");
                        return uacValue != null && Convert.ToInt32(uacValue) != 0;
                    }
                }
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to check UAC status.").Error(ex).Severity(LogLevel.Warning);
            }

            return false; // Default to false if we can't determine the UAC status
        }

        public static T GetTokenInformation<T>(SafeAccessToken tokenHandle, TOKEN_INFORMATION_CLASS tokenInformationClass)
        {

            // First, get the required buffer size
            if (!NativeMethods.GetTokenInformation(tokenHandle, tokenInformationClass, IntPtr.Zero, 0, out int tokenInfoLength))
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 122) // ERROR_INSUFFICIENT_BUFFER
                {
                    throw new Win32Exception(error, $"Failed to get token information size for type [{typeof(T)}]. Error code [{error}].");
                }
            }

            IntPtr tokenInfo = Marshal.AllocHGlobal(tokenInfoLength);

            try
            {
                if (!NativeMethods.GetTokenInformation(tokenHandle, tokenInformationClass, tokenInfo, tokenInfoLength, out _))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to retrieve token information of type [{typeof(T)}].");
                }

                if (typeof(T).IsEnum)
                {
                    int enumValue = Marshal.ReadInt32(tokenInfo);
                    return (T)Enum.ToObject(typeof(T), enumValue);
                }
                else
                {
                    return Marshal.PtrToStructure<T>(tokenInfo) ?? throw new InvalidOperationException($"Failed to marshal token information to type [{typeof(T)}].");
                }
            }
            finally
            {
                if (tokenInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tokenInfo);
                }
            }
        }

        public static TOKEN_ELEVATION_TYPE GetTokenElevationType(SafeAccessToken tokenHandle)
        {
            try
            {
                return GetTokenInformation<TOKEN_ELEVATION_TYPE>(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType);
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get token elevation type.").Error(ex);
                throw;
            }
        }

        public static bool IsTokenElevated(SafeAccessToken tokenHandle)
        {
            try
            {
                TOKEN_ELEVATION elevation = GetTokenInformation<TOKEN_ELEVATION>(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation);
                return elevation.TokenIsElevated;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to determine if the token is elevated.").Error(ex);
                throw;
            }
        }

        public static bool GetLinkedElevatedToken(in SafeAccessToken hToken, out SafeAccessToken hElevatedLinkedToken)
        {
            hElevatedLinkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_ELEVATION_TYPE elevationType = GetTokenElevationType(hToken);

                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull)
                {
                    hElevatedLinkedToken = hToken;
                    return true;
                }

                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
                {
                    // No linked token available
                    return false;
                }

                // Determine whether system is running Windows Vista or later operating systems (major version >= 6) because they support linked tokens, but
                // previous versions (major version < 6) do not.
                if (Environment.OSVersion.Version.Major >= 6 && IsUACEnabled())
                {
                    // If limited, get the linked elevated token for further check.
                    if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited)
                    {
                        return GetLinkedToken(hToken, out hElevatedLinkedToken);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get linked elevated token: {ex.Message}").Error(ex);
                return false;
            }
        }

        public static bool GetLinkedStandardToken(in SafeAccessToken hToken, out SafeAccessToken hStandardLinkedToken)
        {
            hStandardLinkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_ELEVATION_TYPE elevationType = GetTokenElevationType(hToken);

                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited)
                {
                    hStandardLinkedToken = hToken;
                    return true;
                }

                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
                {
                    // No linked token available
                    return false;
                }

                // Determine whether system is running Windows Vista or later operating systems (major version >= 6) because they support linked tokens
                if (Environment.OSVersion.Version.Major >= 6 && IsUACEnabled())
                {
                    // If full, get the linked limited token
                    if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull)
                    {
                        return GetLinkedToken(hToken, out hStandardLinkedToken);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get linked standard token: {ex.Message}").Error(ex);
                return false;
            }
        }

        /// <summary>
        /// The function checks whether a primary access token belongs to a user account that is a member of the local Administrators group.
        /// </summary>
        /// <param name="hToken">The process to check.</param>
        /// <returns>
        /// Returns true if the primary access token belongs to a user account that is a member of the local Administrators group. Returns false
        /// if the token does not.
        /// </returns>
        public static bool IsTokenLocalAdmin(in SafeAccessToken hToken)
        {
            try
            {
                if (!GetLinkedElevatedToken(in hToken, out SafeAccessToken hLinkedToken))
                    return false;

                if (hLinkedToken.IsInvalid)
                    return false;

                // Check if the token to be checked contains local admin SID.
                using (hLinkedToken)
                {
                    if (!TryGetWindowsIdentity(hLinkedToken, out _, out WindowsPrincipal? userPrincipal))
                    {
                        return false;
                    }

                    return userPrincipal!.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to determine if token is local admin: {ex.Message}").Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the linked token associated with a given access token, if available.
        /// </summary>
        /// <param name="token">The token to retrieve the linked token for.</param>
        /// <returns>A <see cref="SafeAccessToken"/> representing the linked token, or null if no linked token is available.</returns>
        /// <exception cref="Win32Exception">Thrown if an error occurs while retrieving the linked token.</exception>
        public static bool GetLinkedToken(SafeAccessToken token, out SafeAccessToken hLinkedToken)
        {
            hLinkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_LINKED_TOKEN tokenLinkedToken = GetTokenInformation<TOKEN_LINKED_TOKEN>(token, TOKEN_INFORMATION_CLASS.TokenLinkedToken);

                hLinkedToken = new SafeAccessToken(tokenLinkedToken.LinkedToken);
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
                UnifiedLogger.Create().Message($"Failed to get linked token: {ex.Message}").Error(ex);
                return false;
            }
        }

        /*/// <summary>
        /// Creates a restricted token with the SANDBOX_INERT flag. If set, the system does not check
        /// AppLocker rules or apply Software Restriction Policies.
        /// </summary>
        /// <param name="existingTokenHandle">The handle to the existing token.</param>
        /// <returns>A SafeAccessTokenHandle representing the new restricted token.</returns>
        /// <exception cref="SecureNamedPipeException">Thrown when the creation of the restricted token fails.</exception>
        public static void CreateSandboxInertToken(SafeAccessToken existingTokenHandle, out SafeAccessToken newTokenHandle)
        {
            if (!NativeMethods.CreateRestrictedToken(
                existingTokenHandle.DangerousGetHandle(),
                NativeMethods.SANDBOX_INERT,
                0, IntPtr.Zero, 0, IntPtr.Zero, 0, IntPtr.Zero,
                out newTokenHandle))
            {
                throw new InvalidOperationException("Failed to create restricted token.", new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }*/

        /// <summary>
        /// Creates an environment block for the specified user token, optionally inheriting the parent environment and adding additional variables.
        /// </summary>
        /// <param name="token">The user token to create the environment block for.</param>
        /// <param name="additionalVariables">Additional environment variables to include in the block.</param>
        /// <param name="inherit">Specifies whether to inherit the parent's environment variables.</param>
        /// <returns>A <see cref="SafeEnvironmentBlock"/> containing the created environment block.</returns>
        /// <exception cref="Win32Exception">Thrown if the environment block creation fails.</exception>
        public static SafeEnvironmentBlock CreateTokenEnvironmentBlock(SafeAccessToken token, IDictionary<string, string>? additionalVariables, bool inherit)
        {
            try
            {
                if (!NativeMethods.CreateEnvironmentBlock(out SafeEnvironmentBlock envBlock, token, inherit))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (additionalVariables != null && additionalVariables.Count > 0)
                {
                    var environmentVars = ConvertEnvironmentBlockToDictionary(envBlock);

                    foreach (var kvp in additionalVariables)
                    {
                        environmentVars[kvp.Key] = kvp.Value;
                    }

                    envBlock.Dispose();
                    envBlock = CreateEnvironmentBlockFromDictionary(environmentVars);
                }

                UnifiedLogger.Create().Message("Environment block created successfully.").Severity(LogLevel.Debug);
                return envBlock;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to create environment block.").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Converts an environment block to a dictionary of key-value pairs.
        /// </summary>
        /// <param name="pEnvBlock">A pointer to the environment block.</param>
        /// <returns>A dictionary containing the environment variables and their values.</returns>
        public static Dictionary<string, string> ConvertEnvironmentBlockToDictionary(SafeEnvironmentBlock pEnvBlock)
        {
            var result = new Dictionary<string, string>();
            var offset = 0;

            try
            {
                while (true)
                {
                    string entry = Marshal.PtrToStringUni(pEnvBlock.DangerousGetHandle(), offset);
                    if (string.IsNullOrWhiteSpace(entry)) break;

                    int equalsIndex = entry.IndexOf('=');
                    if (equalsIndex > 0)
                    {
                        string key = entry.Substring(0, equalsIndex);
                        string value = entry.Substring(equalsIndex + 1);
                        result[key] = value;
                    }

                    offset += (entry.Length + 1) * 2;
                }

                UnifiedLogger.Create().Message($"Converted environment block to dictionary with [{result.Count}] entries.").Severity(LogLevel.Debug);
                return result;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to convert environment block to dictionary.").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Creates an environment block from a dictionary of key-value pairs.
        /// </summary>
        /// <param name="environmentVars">The dictionary containing environment variables and their values.</param>
        /// <returns>A <see cref="SafeEnvironmentBlock"/> containing the created environment block.</returns>
        public static SafeEnvironmentBlock CreateEnvironmentBlockFromDictionary(Dictionary<string, string> environmentVars)
        {
            try
            {
                var environmentString = string.Join("\0", environmentVars.Select(kvp => $"{kvp.Key}={kvp.Value}")) + "\0\0";
                var environmentBytes = System.Text.Encoding.Unicode.GetBytes(environmentString);
                var envBlockPtr = Marshal.AllocHGlobal(environmentBytes.Length);
                Marshal.Copy(environmentBytes, 0, envBlockPtr, environmentBytes.Length);

                UnifiedLogger.Create().Message($"Created environment block from dictionary with [{environmentVars.Count}] entries.").Severity(LogLevel.Debug);
                return new SafeEnvironmentBlock(envBlockPtr);
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to create environment block from dictionary.").Error(ex);
                throw;
            }
        }
    }
}

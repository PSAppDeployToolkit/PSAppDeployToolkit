using System;
using System.Linq;
using System.Security;
using System.ComponentModel;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PSADT.PInvoke;
using PSADT.Logging;

namespace PSADT.AccessToken
{
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
            try
            {
                if (!NativeMethods.WTSQueryUserToken(sessionId, out securityIdentificationToken))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"'WTSQueryUserToken' failed to obtain the access token of the logged-on user specified by the session ID [{sessionId}].");
                }

                UnifiedLogger.Create().Message($"User token queried successfully for session ID [{sessionId}].").Severity(LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to query user token for session ID [{sessionId}].").Error(ex);
                securityIdentificationToken = SafeAccessToken.Invalid;
                return false;
            }
        }


        /// <summary>
        /// Duplicates a given access token as a primary token.
        /// </summary>
        /// <param name="token">The token to duplicate.</param>
        /// <param name="primaryToken">When this method returns, contains the duplicated primary token if the operation was successful.</param>
        /// <returns><c>true</c> if token duplication was successful; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool CreatePrimaryToken(SafeAccessToken token, out SafeAccessToken primaryToken)
        {
            try
            {
                if (!NativeMethods.DuplicateTokenEx(
                    token,
                    TokenAccess.TOKEN_ALL_ACCESS | TokenAccess.TOKEN_QUERY | TokenAccess.TOKEN_DUPLICATE,
                    SECURITY_ATTRIBUTES.Create(),
                    SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    TOKEN_TYPE.TokenPrimary,
                    out primaryToken))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"'DuplicateTokenEx' failed to create a primary access token from the existing token.");
                }

                UnifiedLogger.Create().Message("Successfully duplicated token as a primary token.").Severity(LogLevel.Debug);

                // Do not dispose of the input token; the caller manages its disposal.
                return true;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to duplicate token as a primary token.").Error(ex);
                primaryToken = SafeAccessToken.Invalid;
                return false;
            }
        }


        /// <summary>
        /// Duplicates a given access token as an impersonation token.
        /// </summary>
        /// <param name="token">The token to duplicate.</param>
        /// <param name="impersonationToken">When this method returns, contains the duplicated impersonation token if the operation was successful.</param>
        /// <returns><c>true</c> if token duplication was successful; otherwise, <c>false</c>.</returns>
        [SecurityCritical]
        public static bool CreateImpersonationToken(SafeAccessToken token, out SafeAccessToken impersonationToken)
        {
            try
            {
                if (!NativeMethods.DuplicateTokenEx(
                    token,
                    TokenAccess.TOKEN_ALL_ACCESS | TokenAccess.TOKEN_QUERY | TokenAccess.TOKEN_DUPLICATE,
                    SECURITY_ATTRIBUTES.Create(),
                    SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    TOKEN_TYPE.TokenImpersonation,
                    out impersonationToken))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"'DuplicateTokenEx' failed to create an impersonation token from the existing token.");
                }

                UnifiedLogger.Create().Message("Successfully duplicated token as an impersonation token.").Severity(LogLevel.Debug);

                // Do not dispose of the input token; the caller manages its disposal.
                return true;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to duplicate token as an impersonation token.").Error(ex);
                impersonationToken = SafeAccessToken.Invalid;
                return false;
            }
        }


        /// <summary>
        /// Attempts to create a <see cref="WindowsIdentity"/> and <see cref="WindowsPrincipal"/> from the specified token.
        /// </summary>
        /// <param name="token">The access token to create the identity and principal from.</param>
        /// <param name="windowsIdentity">When this method returns, contains the <see cref="WindowsIdentity"/> if successful.</param>
        /// <param name="windowsPrincipal">When this method returns, contains the <see cref="WindowsPrincipal"/> if successful.</param>
        /// <returns><c>true</c> if the identity and principal were created successfully; otherwise, <c>false</c>.</returns>
        public static bool TryGetWindowsIdentity(SafeAccessToken token, out WindowsIdentity? windowsIdentity, out WindowsPrincipal? windowsPrincipal)
        {
            windowsIdentity = null;
            windowsPrincipal = null;

            try
            {
                windowsIdentity = new WindowsIdentity(token.DangerousGetHandle());
                UnifiedLogger.Create().Message("Successfully created WindowsIdentity from token.").Severity(LogLevel.Debug);

                windowsPrincipal = new WindowsPrincipal(windowsIdentity);
                UnifiedLogger.Create().Message("Successfully created WindowsPrincipal from WindowsIdentity.").Severity(LogLevel.Debug);

                return true;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to get WindowsIdentity or WindowsPrincipal from token.").Error(ex);
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

        /// <summary>
        /// Retrieves the specified type of information about an access token.
        /// </summary>
        /// <typeparam name="T">The type of token information to retrieve.</typeparam>
        /// <param name="tokenHandle">The access token from which to retrieve information.</param>
        /// <param name="tokenInformationClass">Specifies a value from the <see cref="TOKEN_INFORMATION_CLASS"/> enumeration to identify the type of information to retrieve.</param>
        /// <returns>The requested token information of type <typeparamref name="T"/>.</returns>
        /// <exception cref="Win32Exception">Thrown if the token information cannot be retrieved.</exception>
        public static T? GetTokenInformation<T>(SafeAccessToken tokenHandle, TOKEN_INFORMATION_CLASS tokenInformationClass)
        {
            try
            {
                // First call to get the buffer size
                bool result = NativeMethods.GetTokenInformation(tokenHandle, tokenInformationClass, IntPtr.Zero, 0, out int tokenInfoLength);
                int error = Marshal.GetLastWin32Error();

                if (!result)
                {
                    if (error == NativeMethods.ERROR_INSUFFICIENT_BUFFER)
                    {
                        // Proceed to allocate buffer of the required size
                    }
                    else if (error == NativeMethods.ERROR_BAD_LENGTH)
                    {
                        // For certain TOKEN_INFORMATION_CLASS values, we must provide a fixed buffer size
                        tokenInfoLength = Marshal.SizeOf(typeof(T));
                    }
                    else
                    {
                        throw new Win32Exception(error);
                    }
                }

                if (tokenInfoLength == 0)
                {
                    // If the required buffer size is still zero, set it to the size of T
                    tokenInfoLength = Marshal.SizeOf(typeof(T));
                }

                using var buffer = new SafeHGlobalHandle(tokenInfoLength);

                // Second call to get the actual token information
                if (!NativeMethods.GetTokenInformation(
                    tokenHandle,
                    tokenInformationClass,
                    buffer.DangerousGetHandle(),
                    tokenInfoLength,
                    out _))
                {
                    error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error);
                }

                Type typeT = typeof(T);

                if (typeT.IsEnum)
                {
                    int intValue = Marshal.ReadInt32(buffer.DangerousGetHandle());
                    return (T)Enum.ToObject(typeT, intValue);
                }
                else if (typeT.IsValueType || typeT.IsLayoutSequential || typeT.IsExplicitLayout)
                {
                    return Marshal.PtrToStructure<T>(buffer.DangerousGetHandle());
                }
                else
                {
                    throw new NotSupportedException($"Type {typeT.FullName} is not supported.");
                }
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Exception in GetTokenInformation:").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the elevation type of the specified access token.
        /// </summary>
        /// <param name="tokenHandle">The access token to retrieve the elevation type from.</param>
        /// <returns>A <see cref="TOKEN_ELEVATION_TYPE"/> value indicating the elevation type of the token.</returns>
        /// <exception cref="Exception">Thrown if the elevation type cannot be retrieved.</exception>
        public static TOKEN_ELEVATION_TYPE GetTokenElevationType(SafeAccessToken tokenHandle)
        {
            try
            {
                // First try to get the elevation type directly
                var elevationType = GetTokenInformation<TOKEN_ELEVATION_TYPE>(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType);

                // If that succeeds, we can return it
                if (elevationType != 0)
                    return elevationType;

                // If it fails with access denied, try to check if the token is elevated
                var elevation = GetTokenInformation<TOKEN_ELEVATION>(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation);

                // If the token is elevated, it must be full elevation type
                if (elevation.TokenIsElevated)
                    return TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;

                // Otherwise assume limited
                return TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited;
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
        /// <param name="tokenHandle">The access token to check.</param>
        /// <returns><c>true</c> if the token is elevated; otherwise, <c>false</c>.</returns>
        /// <exception cref="Exception">Thrown if the token elevation status cannot be determined.</exception>
        public static bool IsTokenElevated(SafeAccessToken tokenHandle)
        {
            try
            {
                TOKEN_ELEVATION elevation = GetTokenInformation<TOKEN_ELEVATION>(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation);
                return elevation.TokenIsElevated;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to determine if the token is elevated.").Error(ex);
                throw;
            }
        }


        /// <summary>
        /// Retrieves the linked elevated token associated with the specified token, if available.
        /// </summary>
        /// <param name="token">The token to retrieve the linked elevated token from.</param>
        /// <param name="elevatedLinkedToken">When this method returns, contains the linked elevated token if available.</param>
        /// <returns><c>true</c> if the linked elevated token was retrieved; otherwise, <c>false</c>.</returns>
        public static bool GetLinkedElevatedToken(SafeAccessToken token, out SafeAccessToken elevatedLinkedToken)
        {
            elevatedLinkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_ELEVATION_TYPE elevationType = GetTokenElevationType(token);

                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull)
                {
                    elevatedLinkedToken = token;
                    return true;
                }

                // If default elevation type, no linked token exists
                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
                    return false;

                // Determine whether system supports linked tokens
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
        public static bool GetLinkedStandardToken(SafeAccessToken token, out SafeAccessToken standardLinkedToken)
        {
            standardLinkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_ELEVATION_TYPE elevationType = GetTokenElevationType(token);

                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited)
                {
                    standardLinkedToken = token;
                    return true;
                }

                // If default elevation type, no linked token exists
                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
                    return false;

                // Determine whether system supports linked tokens
                if (Environment.OSVersion.Version.Major >= 6 && IsUACEnabled())
                {
                    // If full, get the linked limited token
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
        /// Determines whether the specified access token belongs to a user who is a member of the local Administrators group.
        /// </summary>
        /// <param name="token">The access token to check.</param>
        /// <returns><c>true</c> if the token belongs to a local administrator; otherwise, <c>false</c>.</returns>
        public static bool IsTokenLocalAdmin(SafeAccessToken token)
        {
            try
            {
                if (TryGetWindowsIdentity(token, out _, out WindowsPrincipal? userPrincipal))
                {
                    if (userPrincipal!.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        return true;
                    }
                }

                if (GetLinkedElevatedToken(token, out SafeAccessToken linkedToken))
                {
                    using (linkedToken)
                    {
                        if (!linkedToken.IsInvalid && TryGetWindowsIdentity(linkedToken, out _, out WindowsPrincipal? linkedPrincipal))
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
        /// Retrieves the linked token associated with a given access token, if available.
        /// </summary>
        /// <param name="token">The token to retrieve the linked token for.</param>
        /// <param name="linkedToken">When this method returns, contains the linked token if available.</param>
        /// <returns><c>true</c> if the linked token was retrieved; otherwise, <c>false</c>.</returns>
        public static bool GetLinkedToken(SafeAccessToken token, out SafeAccessToken linkedToken)
        {
            linkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_LINKED_TOKEN tokenLinkedToken = GetTokenInformation<TOKEN_LINKED_TOKEN>(token, TOKEN_INFORMATION_CLASS.TokenLinkedToken);

                if (tokenLinkedToken.LinkedToken == IntPtr.Zero)
                {
                    UnifiedLogger.Create().Message("No linked token found.").Severity(LogLevel.Debug).LogAsync();
                    return false;
                }

                linkedToken = new SafeAccessToken(tokenLinkedToken.LinkedToken);
                UnifiedLogger.Create().Message("Linked token retrieved successfully.").Severity(LogLevel.Debug).LogAsync();

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
        /// Creates an environment block for the specified user token, optionally inheriting the parent environment and adding additional variables.
        /// </summary>
        /// <param name="token">The user token to create the environment block for.</param>
        /// <param name="additionalVariables">Additional environment variables to include in the block.</param>
        /// <param name="inherit">Specifies whether to inherit the parent's environment variables.</param>
        /// <returns>A <see cref="SafeEnvironmentBlock"/> containing the created environment block.</returns>
        /// <exception cref="Exception">Thrown if the environment block creation fails.</exception>
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
                UnifiedLogger.Create().Message("Failed to create environment block.").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Converts an environment block to a dictionary of key-value pairs.
        /// </summary>
        /// <param name="envBlock">The environment block to convert.</param>
        /// <returns>A dictionary containing the environment variables and their values.</returns>
        /// <exception cref="Exception">Thrown if the environment block cannot be converted.</exception>
        public static Dictionary<string, string> ConvertEnvironmentBlockToDictionary(SafeEnvironmentBlock envBlock)
        {
            var result = new Dictionary<string, string>();
            IntPtr ptr = envBlock.DangerousGetHandle();
            int offset = 0;

            try
            {
                while (true)
                {
                    string entry = Marshal.PtrToStringUni(IntPtr.Add(ptr, offset)) ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(entry))
                        break;

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
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to convert environment block to dictionary.").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Creates an environment block from a dictionary of key-value pairs.
        /// </summary>
        /// <param name="environmentVars">The dictionary containing environment variables and their values.</param>
        /// <returns>A <see cref="SafeEnvironmentBlock"/> containing the created environment block.</returns>
        /// <exception cref="Exception">Thrown if the environment block cannot be created.</exception>
        public static SafeEnvironmentBlock CreateEnvironmentBlockFromDictionary(Dictionary<string, string> environmentVars)
        {
            try
            {
                var environmentString = string.Join("\0", environmentVars.Select(kvp => $"{kvp.Key}={kvp.Value}")) + "\0\0";
                var environmentBytes = System.Text.Encoding.Unicode.GetBytes(environmentString);
                IntPtr envBlockPtr = Marshal.AllocHGlobal(environmentBytes.Length);
                Marshal.Copy(environmentBytes, 0, envBlockPtr, environmentBytes.Length);

                UnifiedLogger.Create().Message($"Created environment block from dictionary with [{environmentVars.Count}] entries.").Severity(LogLevel.Debug);
                return new SafeEnvironmentBlock(envBlockPtr);
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to create environment block from dictionary.").Error(ex);
                throw;
            }
        }

    }
}

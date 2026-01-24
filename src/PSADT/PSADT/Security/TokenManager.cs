using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Extensions;
using PSADT.Foundation;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
using PSADT.Utilities;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace PSADT.Security
{
    /// <summary>
    /// Provides static methods for retrieving and managing Windows security tokens for user sessions and processes.
    /// </summary>
    /// <remarks>The TokenManager class offers utility functions to obtain primary, linked, and unelevated
    /// tokens for users and processes, supporting scenarios such as impersonation, privilege elevation, and secure
    /// inter-process communication. All methods are intended for internal use and require appropriate permissions;
    /// callers must ensure they have administrative rights where necessary.</remarks>
    internal static class TokenManager
    {
        /// <summary>
        /// Retrieves the primary security token for a specified user, optionally obtaining a linked administrative
        /// token or the highest available token.
        /// </summary>
        /// <remarks>This method retrieves the primary token for a user by either querying the user's
        /// session or obtaining the token from an active process associated with the user. If the caller is not running
        /// as the local system account, the method attempts to locate the user's Explorer process and retrieve its
        /// token. If the caller is running as the local system account, the method directly queries the user's session
        /// token.</remarks>
        /// <param name="runAsActiveUser">The user for whom the primary token is being retrieved. This must represent an active session.</param>
        /// <param name="useLinkedAdminToken">A value indicating whether to retrieve the linked administrative token for the user, if available. If <see
        /// langword="true"/>, the method attempts to retrieve the linked token.</param>
        /// <param name="useHighestAvailableToken">A value indicating whether to retrieve the highest available token for the user if the linked administrative
        /// token cannot be obtained. If <see langword="true"/>, the method falls back to the highest available token
        /// when the linked token is unavailable.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the primary token for the specified user.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the primary token for the specified user cannot be retrieved. This may occur if the user is not
        /// logged on or does not have an active session.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the linked administrative token cannot be retrieved and <paramref
        /// name="useHighestAvailableToken"/> is <see langword="false"/>.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "No idea, but the compiler just doesn't understand that this is OK.")]
        internal static SafeFileHandle GetUserPrimaryToken(RunAsActiveUser runAsActiveUser, bool useLinkedAdminToken, bool useHighestAvailableToken)
        {
            // Internal helper to make the compiler happier.
            SafeFileHandle GetPrimaryFromUserToken(SafeFileHandle userToken)
            {
                if (useLinkedAdminToken || useHighestAvailableToken)
                {
                    try
                    {
                        return GetLinkedPrimaryToken(userToken);
                    }
                    catch (Exception ex)
                    {
                        if (!useHighestAvailableToken)
                        {
                            throw new UnauthorizedAccessException($"Failed to get the linked admin token for user [{runAsActiveUser.NTAccount}].", ex);
                        }
                    }
                }
                return GetPrimaryToken(userToken);
            }

            // Get the user's token.
            if (!AccountUtilities.CallerIsLocalSystem)
            {
                // When we're not local system, we need to find the user's Explorer process and get its token.
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeDebugPrivilege);
                foreach (Process explorerProcess in Process.GetProcessesByName("explorer").Where(p => p.SessionId == runAsActiveUser.SessionId).OrderBy(static p => p.StartTime))
                {
                    try
                    {
                        using (explorerProcess) using (SafeProcessHandle explorerProcessSafeHandle = explorerProcess.SafeHandle)
                        {
                            _ = AdvApi32.OpenProcessToken(explorerProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out SafeFileHandle hProcessToken);
                            if (TokenUtilities.GetTokenSid(hProcessToken) == runAsActiveUser.SID)
                            {
                                using (hProcessToken)
                                {
                                    return GetPrimaryFromUserToken(hProcessToken);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // It's possible the process may be inaccessible if Explorer is elevated by EPM but the caller is not.
                        continue;
                        throw;
                    }
                }
            }
            else
            {
                // When we're local system, we can just get the primary token for the user.
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                _ = WtsApi32.WTSQueryUserToken(runAsActiveUser.SessionId, out SafeFileHandle hUserToken);
                using (hUserToken)
                {
                    return GetPrimaryFromUserToken(hUserToken);
                }
            }
            throw new InvalidOperationException($"Failed to retrieve a primary token for user [{runAsActiveUser.NTAccount}]. Ensure the user is logged on and has an active session.");
        }

        /// <summary>
        /// Retrieves a primary token for the Explorer process with limited access rights.
        /// </summary>
        /// <remarks>This method obtains a token associated with the Explorer process and duplicates it to
        /// create a primary token. The returned token can be used for operations requiring an unelevated
        /// context.</remarks>
        /// <returns>A <see cref="SafeFileHandle"/> representing the primary token for the Explorer process, or <see
        /// langword="null"/> if the operation fails.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        internal static SafeFileHandle GetUnelevatedCallerToken()
        {
            if (AccountUtilities.CallerIsLocalSystem)
            {
                throw new InvalidOperationException("Cannot retrieve an unelevated token when running as the local system account.");
            }
            if (!AccountUtilities.CallerIsAdmin)
            {
                throw new InvalidOperationException("The current process is already running with an unelevated token.");
            }
            using SafeFileHandle hProcess = Kernel32.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, ShellUtilities.GetExplorerProcessId());
            _ = AdvApi32.OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out SafeFileHandle hProcessToken);
            using (hProcessToken)
            {
                if (TokenUtilities.GetTokenSid(hProcessToken) != AccountUtilities.CallerSid)
                {
                    throw new InvalidOperationException("Failed to retrieve an unelevated token for the calling account.");
                }
                if (TokenUtilities.IsTokenElevated(hProcessToken))
                {
                    throw new InvalidOperationException("The calling account's shell is running elevated, therefore unable to get unelevated token.");
                }
                return GetPrimaryToken(hProcessToken);
            }
        }

        /// <summary>
        /// Retrieves the primary token associated with the specified security token handle.
        /// </summary>
        /// <remarks>This method duplicates the specified security token to create a primary token, which
        /// can be used for impersonation or other security-related operations. Ensure that the caller has appropriate
        /// permissions to access and duplicate the token.</remarks>
        /// <param name="tokenHandle">A handle to the security token. This handle must have the necessary access rights to allow duplication.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the duplicated primary token.</returns>
        internal static SafeFileHandle GetPrimaryToken(SafeHandle tokenHandle)
        {
            _ = AdvApi32.DuplicateTokenEx(tokenHandle, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out SafeFileHandle hPrimaryToken);
            return hPrimaryToken;
        }

        /// <summary>
        /// Retrieves the linked token associated with the specified token handle.
        /// </summary>
        /// <remarks>This method retrieves the linked token, which is typically used in scenarios
        /// involving user impersonation or elevated privileges. The caller must ensure that the provided token handle
        /// is valid and has the necessary permissions to query linked token information.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the linked token is to be retrieved.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the linked token associated with the specified token
        /// handle.</returns>
        internal static SafeFileHandle GetLinkedToken(SafeHandle tokenHandle)
        {
            Span<byte> buffer = stackalloc byte[Marshal.SizeOf<TOKEN_LINKED_TOKEN>()];
            _ = AdvApi32.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenLinkedToken, buffer, out _);
            ref readonly TOKEN_LINKED_TOKEN tokenLinkedToken = ref buffer.AsReadOnlyStructure<TOKEN_LINKED_TOKEN>();
            return new(tokenLinkedToken.LinkedToken, true);
        }

        /// <summary>
        /// Retrieves the primary token linked to the specified token handle.
        /// </summary>
        /// <remarks>This method uses the provided token handle to obtain a linked token and then
        /// retrieves its primary token. The caller is responsible for ensuring the validity of the input token
        /// handle.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the linked primary token is to be
        /// retrieved.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the linked primary token.</returns>
        internal static SafeFileHandle GetLinkedPrimaryToken(SafeHandle tokenHandle)
        {
            using SafeFileHandle linkedToken = GetLinkedToken(tokenHandle);
            return GetPrimaryToken(linkedToken);
        }

        /// <summary>
        /// Retrieves the highest available primary token associated with the specified token handle.
        /// </summary>
        /// <remarks>This method attempts to retrieve the linked primary token associated with the
        /// specified token handle. If the linked token is unavailable, it falls back to retrieving the primary token of
        /// the original token handle.</remarks>
        /// <param name="tokenHandle">A <see cref="SafeHandle"/> representing the token handle for which the primary token is to be retrieved.</param>
        /// <returns>A <see cref="SafeFileHandle"/> representing the highest available primary token.</returns>
        internal static SafeFileHandle GetHighestPrimaryToken(SafeHandle tokenHandle)
        {
            // If the linked token is not available, fall back to the primary token of the original token handle.
            try
            {
                return GetLinkedPrimaryToken(tokenHandle);
            }
            catch
            {
                return GetPrimaryToken(tokenHandle);
                throw;
            }
        }
    }
}

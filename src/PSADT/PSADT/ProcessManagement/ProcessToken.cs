using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.LibraryInterfaces;
using PSADT.Security;
using PSADT.Utilities;
using Windows.Win32.Security;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides methods for retrieving and managing security tokens associated with user accounts and processes.
    /// </summary>
    /// <remarks>The <see cref="ProcessToken"/> class includes functionality for obtaining primary tokens for
    /// specific users, as well as tokens associated with processes such as Explorer. These tokens can be used for
    /// operations requiring specific user contexts, including elevated or unelevated access. The methods in this class
    /// handle scenarios where the caller is running as the local system account or as a standard user.</remarks>
    internal static class ProcessToken
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
        /// <param name="username">The <see cref="NTAccount"/> representing the user for whom the token is being retrieved.</param>
        /// <param name="sid">The <see cref="SecurityIdentifier"/> of the user. This is used to match the user's session or process.</param>
        /// <param name="sessionId">The session ID of the user. This is required when retrieving the token for a user logged into a specific
        /// session.</param>
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
        internal static SafeFileHandle GetUserPrimaryToken(NTAccount username, SecurityIdentifier sid, uint sessionId, bool useLinkedAdminToken, bool useHighestAvailableToken)
        {
            // Get the user's token.
            SafeFileHandle hUserToken = null!;
            if (!AccountUtilities.CallerIsLocalSystem)
            {
                // When we're not local system, we need to find the user's Explorer process and get its token.
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeDebugPrivilege);
                foreach (var explorerProcess in Process.GetProcessesByName("explorer").OrderBy(static p => p.StartTime))
                {
                    using (explorerProcess) using (explorerProcess.SafeHandle)
                    {
                        AdvApi32.OpenProcessToken(explorerProcess.SafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out var hProcessToken);
                        if (TokenManager.GetTokenSid(hProcessToken) == sid)
                        {
                            hUserToken = hProcessToken;
                            break;
                        }
                    }
                }
            }
            else
            {
                // When we're local system, we can just get the primary token for the user.
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                WtsApi32.WTSQueryUserToken(sessionId, out hUserToken);
            }

            // Throw if for whatever reason, we couldn't get a token.
            if (null == hUserToken)
            {
                throw new InvalidOperationException($"Failed to retrieve a primary token for user [{username}]. Ensure the user is logged on and has an active session.");
            }

            // Get the primary token for the user, either linked or not.
            using (hUserToken)
            {
                if (useLinkedAdminToken || useHighestAvailableToken)
                {
                    try
                    {
                        return TokenManager.GetLinkedPrimaryToken(hUserToken);
                    }
                    catch (Exception ex)
                    {
                        if (!useHighestAvailableToken)
                        {
                            throw new UnauthorizedAccessException($"Failed to get the linked admin token for user [{username}].", ex);
                        }
                        return TokenManager.GetPrimaryToken(hUserToken);
                    }
                }
                else
                {
                    return TokenManager.GetPrimaryToken(hUserToken);
                }
            }
        }

        /// <summary>
        /// Retrieves a primary token for the Explorer process with limited access rights.
        /// </summary>
        /// <remarks>This method obtains a token associated with the Explorer process and duplicates it to
        /// create a primary token. The returned token can be used for operations requiring an unelevated
        /// context.</remarks>
        /// <returns>A <see cref="SafeFileHandle"/> representing the primary token for the Explorer process, or <see
        /// langword="null"/> if the operation fails.</returns>
        internal static SafeFileHandle GetUnelevatedToken()
        {
            using (var cProcess = Process.GetProcessById((int)ShellUtilities.GetExplorerProcessId()))
            using (cProcess.SafeHandle)
            {
                AdvApi32.OpenProcessToken(cProcess.SafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out var hProcessToken);
                using (hProcessToken)
                {
                    if (TokenManager.GetTokenSid(hProcessToken) != AccountUtilities.CallerSid)
                    {
                        throw new InvalidOperationException("Failed to retrieve an unelevated token for the calling account.");
                    }
                    if (TokenManager.IsTokenElevated(hProcessToken))
                    {
                        throw new InvalidOperationException("The calling account's shell is running elevated, therefore unable to get unelevated token.");
                    }
                    return TokenManager.GetPrimaryToken(hProcessToken);
                }
            }
        }
    }
}

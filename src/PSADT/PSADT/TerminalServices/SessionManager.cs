using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using PSADT.AccountManagement;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using PSADT.Security;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.RemoteDesktop;

namespace PSADT.TerminalServices
{
    /// <summary>
    /// Utility methods for interacting with WTS.
    /// </summary>
    public static class SessionManager
    {
        /// <summary>
        /// Gets session info from all valid WTS sessions.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        public static IReadOnlyList<SessionInfo> GetSessionInfo()
        {
            WtsApi32.WTSEnumerateSessions(HANDLE.WTS_CURRENT_SERVER_HANDLE, out var pSessionInfo);
            using (pSessionInfo)
            {
                int objLength = Marshal.SizeOf(typeof(WTS_SESSION_INFOW));
                List<SessionInfo> sessions = [];
                for (int i = 0; i < pSessionInfo.Length / objLength; i++)
                {
                    if (GetSessionInfo(pSessionInfo.ToStructure<WTS_SESSION_INFOW>(objLength * i)) is SessionInfo session)
                    {
                        sessions.Add(session);
                    }
                }
                return sessions.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets session info for any provided valid session Id.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        internal static SessionInfo? GetSessionInfo(in WTS_SESSION_INFOW session)
        {
            static T? GetValue<T>(uint sessionId, WTS_INFO_CLASS infoClass)
            {
                WtsApi32.WTSQuerySessionInformation(HANDLE.WTS_CURRENT_SERVER_HANDLE, sessionId, infoClass, out var pBuffer);
                if (!pBuffer.IsInvalid)
                {
                    using (pBuffer)
                    {
                        if (typeof(T) == typeof(string))
                        {
                            if (pBuffer.ToStringUni()?.Trim() is string result && !string.IsNullOrWhiteSpace(result))
                            {
                                return (T)(object)result.TrimRemoveNull();
                            }
                        }
                        if (typeof(T) == typeof(ushort))
                        {
                            return (T)(object)(ushort)pBuffer.ReadInt16();
                        }
                        else if (typeof(T) == typeof(uint))
                        {
                            return (T)(object)(uint)pBuffer.ReadInt32();
                        }
                        else if (typeof(T) == typeof(WTSINFOEXW))
                        {
                            return (T)(object)pBuffer.ToStructure<WTSINFOEXW>();
                        }
                    }
                }
                return default;
            }

            // Get extended information about the session, bombing out if we have no username (not a proper session).
            var sessionInfo = GetValue<WTSINFOEXW>(session.SessionId, WTS_INFO_CLASS.WTSSessionInfoEx).Data.WTSInfoExLevel1;
            if (sessionInfo.UserName.ToString().TrimRemoveNull() is not string userName || string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            // Declare initial variables for data we need to get from a structured object.
            string domainName = sessionInfo.DomainName.ToString().TrimRemoveNull();
            NTAccount ntAccount = new(domainName, userName);
            SecurityIdentifier sid = GetWtsSessionSid(session.SessionId, ntAccount);
            string? clientName = GetValue<string>(session.SessionId, WTS_INFO_CLASS.WTSClientName);
            string pWinStationName = session.pWinStationName.ToString().TrimRemoveNull();
            ushort clientProtocolType = GetValue<ushort>(session.SessionId, WTS_INFO_CLASS.WTSClientProtocolType)!;

            // Determine whether the user is a local admin or not. This process can be unreliable for domain devices.
            Exception? isLocalAdminException = null; bool? isLocalAdmin = null;
            try
            {
                isLocalAdmin = IsWtsSessionUserLocalAdmin(session.SessionId, sid);
            }
            catch (Exception ex)
            {
                isLocalAdminException = ex;
            }

            // Instantiate a SessionInfo object and return it to the caller.
            return new(
                ntAccount,
                sid,
                userName,
                domainName,
                session.SessionId,
                pWinStationName,
                (LibraryInterfaces.WTS_CONNECTSTATE_CLASS)sessionInfo.SessionState,
                session.SessionId == AccountUtilities.CallerSessionId,
                session.SessionId == PInvoke.WTSGetActiveConsoleSessionId(),
                sessionInfo.SessionState == Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSActive,
                pWinStationName != "Services" && pWinStationName != "RDP-Tcp",
                clientProtocolType != 0,
                isLocalAdmin,
                isLocalAdminException,
                DateTime.FromFileTime(sessionInfo.LogonTime),
                DateTime.Now - DateTime.FromFileTime(sessionInfo.LastInputTime),
                sessionInfo.DisconnectTime != 0 ? DateTime.FromFileTime(sessionInfo.DisconnectTime) : null,
                clientName,
                (WTS_PROTOCOL_TYPE)clientProtocolType!,
                GetValue<string>(session.SessionId, WTS_INFO_CLASS.WTSClientDirectory),
                (null != clientName) ? GetValue<uint>(session.SessionId, WTS_INFO_CLASS.WTSClientBuildNumber) : null
            );
        }

        /// <summary>
        /// Retrieves the security identifier (SID) associated with a specified session and user account.
        /// </summary>
        /// <remarks>This method attempts multiple approaches to retrieve the SID, including translating
        /// the user account to a SID,  querying the user's token if the necessary privileges are enabled, and
        /// retrieving group policy information. If none of these methods succeed, the method returns <see
        /// langword="null"/>.</remarks>
        /// <param name="sessionid">The ID of the session for which the SID is being retrieved.</param>
        /// <param name="username">The user account, represented as an <see cref="NTAccount"/>, for which the SID is being retrieved.</param>
        /// <returns>A <see cref="SecurityIdentifier"/> representing the SID of the specified session and user account</returns>
        private static SecurityIdentifier GetWtsSessionSid(uint sessionid, NTAccount username)
        {
            // If we have the privileges, we can get the SID from the user's token.
            if (AccountUtilities.CallerIsLocalSystem)
            {
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                WtsApi32.WTSQueryUserToken(sessionid, out var hUserToken);
                using (hUserToken)
                {
                    return TokenManager.GetTokenSid(hUserToken);
                }
            }

            // If we're an admin, we can get the SID from a process running in the session.
            if (AccountUtilities.CallerIsAdmin)
            {
                WtsApi32.WTSEnumerateProcessesEx(HANDLE.WTS_CURRENT_SERVER_HANDLE, 0, sessionid, out var pProcessInfo);
                using (pProcessInfo)
                {
                    int objLength = Marshal.SizeOf(typeof(WTS_PROCESS_INFOW));
                    for (int i = 0; i < pProcessInfo.Length / objLength; i++)
                    {
                        WTS_PROCESS_INFOW process = pProcessInfo.ToStructure<WTS_PROCESS_INFOW>(objLength * i);
                        if (process.pProcessName.ToString()?.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            return new((IntPtr)process.pUserSid);
                        }
                    }
                }
            }

            // Attempt to get the SID from the caller's explorer.exe process if it exists.
            if ((AccountUtilities.CallerIsAdmin || sessionid == AccountUtilities.CallerSessionId) && Process.GetProcessesByName("explorer").Where(p => p.SessionId == sessionid).OrderBy(static p => p.StartTime).FirstOrDefault() is Process explorerProcess)
            {
                using (explorerProcess) using (var explorerProcessSafeHandle = explorerProcess.SafeHandle)
                {
                    AdvApi32.OpenProcessToken(explorerProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out var hProcessToken);
                    using (hProcessToken)
                    {
                        return TokenManager.GetTokenSid(hProcessToken);
                    }
                }
            }

            // If any of the above fail, just try to translate the SID using the builtin API.
            // We don't do this first off as it can fail for domain users while not on the network.
            return (SecurityIdentifier)username.Translate(typeof(SecurityIdentifier));
        }

        /// <summary>
        /// Determines whether the user associated with the specified Windows Terminal Services (WTS) session is a
        /// local administrator.
        /// </summary>
        /// <remarks>This method checks the user's administrative status by attempting to query the user's
        /// token and evaluating their group membership. If the required privileges are not enabled, it falls back to 
        /// checking the user's SID against the well-known local administrators group.</remarks>
        /// <param name="sessionid">The ID of the WTS session for which the user's administrative status is being checked.</param>
        /// <param name="sid">The security identifier (SID) of the user associated with the session.</param>
        /// <returns><see langword="true"/> if the user is a member of the local administrators group; otherwise, <see
        /// langword="false"/>.</returns>
        private static bool IsWtsSessionUserLocalAdmin(uint sessionid, SecurityIdentifier sid)
        {
            // If we have the privileges, we can get the user's token and do a WindowsIdentity check.
            if (AccountUtilities.CallerIsLocalSystem)
            {
                PrivilegeManager.EnablePrivilegeIfDisabled(SE_PRIVILEGE.SeTcbPrivilege);
                WtsApi32.WTSQueryUserToken(sessionid, out var hUserToken); using (hUserToken)
                using (var hPrimaryToken = TokenManager.GetHighestPrimaryToken(hUserToken))
                {
                    bool hPrimaryTokenAddRef = false;
                    try
                    {
                        hPrimaryToken.DangerousAddRef(ref hPrimaryTokenAddRef);
                        using var identity = new WindowsIdentity(hPrimaryToken.DangerousGetHandle());
                        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                    }
                    finally
                    {
                        if (hPrimaryTokenAddRef)
                        {
                            hPrimaryToken.DangerousRelease();
                        }
                    }
                }
            }
            return AccountUtilities.IsSidMemberOfWellKnownGroup(sid, WellKnownSidType.BuiltinAdministratorsSid);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Extensions;
using PSADT.Foundation;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
using PSADT.LibraryInterfaces.SafeHandles;
using PSADT.ProcessManagement;
using PSADT.Security;
using PSADT.Utilities;
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
            _ = WtsApi32.WTSEnumerateSessions(HANDLE.WTS_CURRENT_SERVER_HANDLE, out SafeWtsHandle pSessionInfo);
            using (pSessionInfo)
            {
                int objLength = Marshal.SizeOf<WTS_SESSION_INFOW>();
                int objCount = pSessionInfo.Length / objLength;
                ReadOnlySpan<byte> pSessionInfoSpan = pSessionInfo.AsReadOnlySpan<byte>();
                List<SessionInfo> sessions = new(objCount);
                for (int i = 0; i < pSessionInfo.Length / objLength; i++)
                {
                    ref readonly WTS_SESSION_INFOW session = ref pSessionInfoSpan.Slice(objLength * i).AsReadOnlyStructure<WTS_SESSION_INFOW>();
                    if (GetSessionInfo(in session) is SessionInfo sessionInfo)
                    {
                        sessions.Add(sessionInfo);
                    }
                }
                return sessions.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets session info for any provided valid session Id.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private static SessionInfo? GetSessionInfo(in WTS_SESSION_INFOW session)
        {
            // Internal helper for retrieving session information values.
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
            static T? GetValue<T>(uint sessionId, WTS_INFO_CLASS infoClass)
            {
                _ = WtsApi32.WTSQuerySessionInformation(HANDLE.WTS_CURRENT_SERVER_HANDLE, sessionId, infoClass, out SafeWtsHandle pBuffer);
                using (pBuffer)
                {
                    if (typeof(T) == typeof(string))
                    {
                        if (pBuffer.ToStringUni()?.TrimRemoveNull() is string result && !string.IsNullOrWhiteSpace(result))
                        {
                            return (T)(object)result;
                        }
                        return default;
                    }
                    else if (typeof(T) == typeof(ushort))
                    {
                        return (T)(object)(ushort)pBuffer.ReadInt16();
                    }
                    else if (typeof(T) == typeof(uint))
                    {
                        return (T)(object)(uint)pBuffer.ReadInt32();
                    }
                    else if (typeof(T) == typeof(WTSINFOEXW))
                    {
                        return (T)(object)pBuffer.AsReadOnlyStructure<WTSINFOEXW>();
                    }
                    else
                    {
                        throw new NotSupportedException($"The type {typeof(T).FullName} is not supported by {nameof(GetValue)}.");
                    }
                }
            }

            // Get extended information about the session, bombing out if we have no username (not a proper session).
            WTSINFOEX_LEVEL1_W sessionInfo = GetValue<WTSINFOEXW>(session.SessionId, WTS_INFO_CLASS.WTSSessionInfoEx).Data.WTSInfoExLevel1;
            if (sessionInfo.UserName.ToString().TrimRemoveNull() is not string userName || string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            // Declare initial variables for data we need to get from a structured object.
            string domainName = sessionInfo.DomainName.ToString().TrimRemoveNull();
            NTAccount ntAccount = new(domainName, userName);
            SecurityIdentifier sid = GetWtsSessionSid(session.SessionId, ntAccount);
            bool isCurrentSession = session.SessionId == AccountUtilities.CallerSessionId;
            bool isConsoleSession = session.SessionId == PInvoke.WTSGetActiveConsoleSessionId();
            bool isActiveUserSession = sessionInfo.SessionState == Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSActive;
            bool isValidUserSession = isActiveUserSession || sessionInfo.SessionState == Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSDisconnected;
            TimeSpan? idleTime = DateTime.Now - DateTime.FromFileTime(sessionInfo.LastInputTime);
            string? clientName = GetValue<string>(session.SessionId, WTS_INFO_CLASS.WTSClientName);
            string? pWinStationName = session.pWinStationName.ToString()?.TrimRemoveNull();
            ushort clientProtocolType = GetValue<ushort>(session.SessionId, WTS_INFO_CLASS.WTSClientProtocolType)!;

            // Determine whether the user is a local admin or not. This process can be unreliable for domain devices.
            Exception? isLocalAdminException = null; bool? isLocalAdmin = null;
            try
            {
                isLocalAdmin = IsWtsSessionUserLocalAdmin(session.SessionId, sid);
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                isLocalAdminException = ex;
            }

            // If there's an active console session and we've got the privileges, get the idle time via GetLastInputInfo().
            if (isConsoleSession)
            {
                if (isCurrentSession)
                {
                    idleTime = ShellUtilities.GetLastInputTime();
                }
                else if ((AccountUtilities.CallerIsLocalSystem || AccountUtilities.CallerIsAdmin) && isValidUserSession)
                {
                    try
                    {
                        RunAsActiveUser user = new(ntAccount, sid, session.SessionId, isLocalAdmin); AssemblyPermissions.Remediate(user);
                        string clientServerPath = typeof(SessionInfo).Assembly.Location.Replace(".dll", ".ClientServer.Client.exe");
                        ProcessLaunchInfo args = new(clientServerPath, ["/GetLastInputTime"], Environment.SystemDirectory, user, createNoWindow: true);
                        idleTime = new(long.Parse(ProcessManager.LaunchAsync(args)!.Task.GetAwaiter().GetResult().StdOut![0], CultureInfo.InvariantCulture));
                    }
                    catch (Exception ex) when (ex.Message is not null)
                    {
                        idleTime = null;
                    }
                }
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
                isCurrentSession,
                isConsoleSession,
                isActiveUserSession,
                isValidUserSession,
                pWinStationName is not "Services" and not "RDP-Tcp",
                clientProtocolType != 0,
                isLocalAdmin,
                isLocalAdminException,
                DateTime.FromFileTime(sessionInfo.LogonTime),
                idleTime,
                sessionInfo.DisconnectTime != 0 && !isActiveUserSession ? DateTime.FromFileTime(sessionInfo.DisconnectTime) : null,
                clientName,
                (WTS_PROTOCOL_TYPE)clientProtocolType,
                GetValue<string>(session.SessionId, WTS_INFO_CLASS.WTSClientDirectory),
                (clientName is not null) ? GetValue<uint>(session.SessionId, WTS_INFO_CLASS.WTSClientBuildNumber) : null
            );
        }

        /// <summary>
        /// Retrieves the security identifier (SID) associated with a specified session and user account.
        /// </summary>
        /// <remarks>This method attempts multiple approaches to retrieve the SID, including translating
        /// the user account to a SID, querying the user's token if the necessary privileges are enabled, and
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
                _ = WtsApi32.WTSQueryUserToken(sessionid, out SafeFileHandle hUserToken);
                using (hUserToken)
                {
                    return TokenUtilities.GetTokenSid(hUserToken);
                }
            }

            // If we're an admin, we can get the SID from a process running in the session.
            if (AccountUtilities.CallerIsAdmin)
            {
                _ = WtsApi32.WTSEnumerateProcessesEx(HANDLE.WTS_CURRENT_SERVER_HANDLE, 0, sessionid, out SafeWtsExHandle pProcessInfo);
                using (pProcessInfo)
                {
                    ReadOnlySpan<byte> pProcessInfoSpan = pProcessInfo.AsReadOnlySpan<byte>();
                    int objLength = Marshal.SizeOf<WTS_PROCESS_INFOW>();
                    for (int i = 0; i < pProcessInfo.Length / objLength; i++)
                    {
                        ref readonly WTS_PROCESS_INFOW process = ref pProcessInfoSpan.Slice(objLength * i).AsReadOnlyStructure<WTS_PROCESS_INFOW>();
                        if (process.pProcessName.ToString()?.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            return process.pUserSid.ToSecurityIdentifier();
                        }
                    }
                }
            }

            // Attempt to get the SID from the caller's explorer.exe process if it exists.
            if (AccountUtilities.CallerIsAdmin || sessionid == AccountUtilities.CallerSessionId)
            {
                foreach (Process explorerProcess in Process.GetProcessesByName("explorer").Where(p => p.SessionId == sessionid).OrderBy(static p => p.StartTime))
                {
                    try
                    {
                        using (explorerProcess) using (SafeProcessHandle explorerProcessSafeHandle = explorerProcess.SafeHandle)
                        {
                            _ = AdvApi32.OpenProcessToken(explorerProcessSafeHandle, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle hProcessToken);
                            using (hProcessToken)
                            {
                                return TokenUtilities.GetTokenSid(hProcessToken);
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
                _ = WtsApi32.WTSQueryUserToken(sessionid, out SafeFileHandle hUserToken); using (hUserToken)
                using (SafeFileHandle hPrimaryToken = TokenManager.GetHighestPrimaryToken(hUserToken))
                {
                    return TokenUtilities.IsTokenAdministrative(hPrimaryToken);
                }
            }
            return AccountUtilities.IsSidMemberOfWellKnownGroup(sid, WellKnownSidType.BuiltinAdministratorsSid);
        }
    }
}

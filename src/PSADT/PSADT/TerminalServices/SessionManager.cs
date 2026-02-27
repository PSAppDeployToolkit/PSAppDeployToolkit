using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
using PSADT.ProcessManagement;
using PSADT.Security;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.RemoteDesktop;

namespace PSADT.TerminalServices
{
    /// <summary>
    /// Utility methods for interacting with WTS.
    /// </summary>
    public static class SessionManager
    {
        /// <summary>
        /// Retrieves a read-only collection containing information about all active sessions on the current server.
        /// </summary>
        /// <remarks>This method enumerates all sessions available on the current server and returns their
        /// associated information. The returned collection is immutable and reflects the state of sessions at the time
        /// of the call. Subsequent changes to session state are not reflected in the returned list.</remarks>
        /// <returns>A read-only list of <see cref="SessionInfo"/> objects, each representing the details of an active session.
        /// The list is empty if no active sessions are found.</returns>
        public static IReadOnlyList<SessionInfo> GetSessionInfo()
        {
            _ = NativeMethods.WTSEnumerateSessions(HANDLE.WTS_CURRENT_SERVER_HANDLE, out SafeWtsHandle pSessionInfo);
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
        /// Retrieves detailed information about a specified terminal services session, including user account, session
        /// state, and client connection details.
        /// </summary>
        /// <remarks>This method is intended for internal use to facilitate the retrieval of session
        /// information. It handles various session states and may involve additional checks for user privileges. The
        /// returned SessionInfo object includes information such as whether the session is active, if the user is a
        /// local administrator, and the session's idle time. Some details may not be available for all session types or
        /// under certain privilege levels.</remarks>
        /// <param name="session">A structure containing information about the session for which to retrieve detailed session data.</param>
        /// <returns>A SessionInfo object containing user account details, session state, client information, and other relevant
        /// session attributes, or null if the session does not have a valid username.</returns>
        /// <exception cref="NotSupportedException">Thrown if the requested type for session information is not supported.</exception>
        private static SessionInfo? GetSessionInfo(in WTS_SESSION_INFOW session)
        {
            // Internal helper for retrieving session information values.
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
            static T? GetValue<T>(uint sessionId, WTS_INFO_CLASS infoClass)
            {
                _ = NativeMethods.WTSQuerySessionInformation(HANDLE.WTS_CURRENT_SERVER_HANDLE, sessionId, infoClass, out SafeWtsHandle pBuffer);
                using (pBuffer)
                {
                    if (typeof(T) == typeof(string))
                    {
                        try
                        {
                            return (T)(object)pBuffer.ToStringUni();
                        }
                        catch
                        {
                            return default;
                            throw;
                        }
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
            if (sessionInfo.UserName.ToString() is not string userName || string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            // Declare initial variables for data we need to get from a structured object.
            string domainName = sessionInfo.DomainName.ToString();
            NTAccount ntAccount = new(domainName, userName);
            SecurityIdentifier sid = GetWtsSessionSid(session.SessionId, ntAccount);
            bool isCurrentSession = session.SessionId == AccountUtilities.CallerSessionId;
            bool isConsoleSession = session.SessionId == PInvoke.WTSGetActiveConsoleSessionId();
            bool isActiveUserSession = sessionInfo.SessionState == Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSActive;
            bool isValidUserSession = isActiveUserSession || sessionInfo.SessionState == Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSDisconnected;
            TimeSpan? idleTime = DateTime.Now - DateTime.FromFileTime(sessionInfo.LastInputTime);
            string? clientName = GetValue<string>(session.SessionId, WTS_INFO_CLASS.WTSClientName);
            string? pWinStationName = session.pWinStationName.ToString();
            ushort clientProtocolType = GetValue<ushort>(session.SessionId, WTS_INFO_CLASS.WTSClientProtocolType)!;

            // Determine whether the user is a local admin or not. This process can be unreliable for domain devices.
            bool? isLocalAdmin = IsWtsSessionUserLocalAdmin(session.SessionId);

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
                        ProcessLaunchInfo args = new(EnvironmentInfo.ClientServerClientPath, ["/GetLastInputTime"], Environment.SystemDirectory, user, createNoWindow: true);
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
                (Interop.WTS_CONNECTSTATE_CLASS)sessionInfo.SessionState,
                isCurrentSession,
                isConsoleSession,
                isActiveUserSession,
                isValidUserSession,
                pWinStationName is not "Services" and not "RDP-Tcp",
                clientProtocolType != 0,
                isLocalAdmin,
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
            // Just return the caller's SID if it's the same session.
            if (sessionid == AccountUtilities.CallerSessionId)
            {
                return AccountUtilities.CallerSid;
            }

            // If we have the privileges, we can get the SID from the user's token.
            if (AccountUtilities.CallerIsAdmin)
            {
                using SafeFileHandle hUserToken = TokenManager.GetUserPrimaryToken(sessionid);
                return TokenUtilities.GetTokenSid(hUserToken);
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
        /// <returns><see langword="true"/> if the user is a member of the local administrators group; otherwise, <see
        /// langword="false"/>.</returns>
        private static bool? IsWtsSessionUserLocalAdmin(uint sessionid)
        {
            // Just return the caller's admin state if it's the same session.
            if (sessionid == AccountUtilities.CallerSessionId)
            {
                return AccountUtilities.CallerIsAdmin;
            }

            // If we have the privileges, we can get the user's token and do a WindowsIdentity check.
            if (AccountUtilities.CallerIsAdmin)
            {
                using SafeFileHandle hPrimaryToken = TokenManager.GetUserPrimaryToken(sessionid, ElevatedTokenType.HighestAvailable);
                return TokenUtilities.IsTokenAdministrative(hPrimaryToken);
            }

            // We don't know, and if we can't get it, it doesn't matter.
            return null;
        }
    }
}

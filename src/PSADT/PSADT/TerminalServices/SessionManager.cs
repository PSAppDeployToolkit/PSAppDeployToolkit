using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using PSADT.AccountManagement;
using PSADT.ClientServer;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.Interop.Extensions;
using PSADT.Interop.SafeHandles;
using PSADT.ProcessManagement;
using PSADT.Security;
using PSADT.Utilities;
using Windows.Win32;
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
            _ = NativeMethods.WTSEnumerateSessionsEx(out SafeWtsExHandle pSessionInfo);
            using (pSessionInfo)
            {
                int objLength = Marshal.SizeOf<WTS_SESSION_INFO_1W>(); int objCount = pSessionInfo.Length / objLength;
                ReadOnlySpan<byte> pSessionInfoSpan = pSessionInfo.AsReadOnlySpan<byte>();
                List<SessionInfo> sessions = new(objCount);
                for (int i = 0; i < objCount; i++)
                {
                    ref readonly WTS_SESSION_INFO_1W session = ref pSessionInfoSpan.Slice(objLength * i).AsReadOnlyStructure<WTS_SESSION_INFO_1W>();
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
        private static SessionInfo? GetSessionInfo(in WTS_SESSION_INFO_1W session)
        {
            // Internal helpers for retrieving session information values.
            static string? GetString(uint sessionId, WTS_INFO_CLASS infoClass)
            {
                _ = NativeMethods.WTSQuerySessionInformation(sessionId, infoClass, out SafeWtsHandle pBuffer);
                using (pBuffer)
                {
                    return pBuffer.AsReadOnlySpan<char>().ToStringUni();
                }
            }
            static T GetValue<T>(uint sessionId, WTS_INFO_CLASS infoClass) where T : unmanaged
            {
                _ = NativeMethods.WTSQuerySessionInformation(sessionId, infoClass, out SafeWtsHandle pBuffer);
                using (pBuffer)
                {
                    return pBuffer.AsReadOnlyStructure<T>();
                }
            }

            // Set up an NTAccount object for the user first and foremost.
            if (session.pUserName.ToString() is not string userName || string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }
            string domainName = session.pDomainName.ToString();
            NTAccount ntAccount = new(domainName, userName);

            // Get the SID and whether the user is administrative.
            SecurityIdentifier sid; bool? isLocalAdmin = null;
            if (ntAccount != AccountUtilities.CallerUsername)
            {
                if (AccountUtilities.CallerIsAdmin)
                {
                    using SafeFileHandle hPrimaryToken = TokenManager.GetUserPrimaryToken(session.SessionId, ElevatedTokenType.HighestAvailable);
                    sid = TokenUtilities.GetTokenSid(hPrimaryToken); isLocalAdmin = TokenUtilities.IsTokenAdministrative(hPrimaryToken);
                }
                else
                {
                    sid = (SecurityIdentifier)ntAccount.Translate(typeof(SecurityIdentifier));
                }
            }
            else
            {
                sid = AccountUtilities.CallerSid; isLocalAdmin = AccountUtilities.CallerIsAdmin;
            }

            // Set up the remaining session information values.
            bool isCurrentSession = session.SessionId == AccountUtilities.CallerSessionId;
            bool isConsoleSession = session.SessionId == PInvoke.WTSGetActiveConsoleSessionId();
            bool isActiveUserSession = session.State == Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSActive;
            bool isValidUserSession = isActiveUserSession || session.State == Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS.WTSDisconnected;
            ushort clientProtocolType = GetValue<ushort>(session.SessionId, WTS_INFO_CLASS.WTSClientProtocolType);
            string? clientName = GetString(session.SessionId, WTS_INFO_CLASS.WTSClientName);
            string? pWinStationName = session.pSessionName.ToString();
            if (string.IsNullOrWhiteSpace(pWinStationName))
            {
                pWinStationName = null;
            }

            // Get extended information about the session.
            TimeSpan? idleTime; DateTime logonTime; DateTime? disconnectTime = null;
            _ = NativeMethods.WTSQuerySessionInformation(session.SessionId, WTS_INFO_CLASS.WTSSessionInfoEx, out SafeWtsHandle pBuffer);
            using (pBuffer)
            {
                ref readonly WTSINFOEXW wtsInfoEx = ref pBuffer.AsReadOnlyStructure<WTSINFOEXW>();
                ref readonly WTSINFOEX_LEVEL1_W sessionInfo = ref wtsInfoEx.Data.WTSInfoExLevel1;
                if (sessionInfo.DisconnectTime != 0 && !isActiveUserSession)
                {
                    disconnectTime = DateTime.FromFileTime(sessionInfo.DisconnectTime);
                }
                idleTime = DateTime.Now - DateTime.FromFileTime(sessionInfo.LastInputTime);
                logonTime = DateTime.FromFileTime(sessionInfo.LogonTime);
            }

            // If there's an active console session and we've got the privileges, get the idle time via GetLastInputInfo().
            if (isConsoleSession)
            {
                if (isCurrentSession)
                {
                    idleTime = ShellUtilities.GetLastInputTime();
                }
                else if (AccountUtilities.CallerIsAdmin && isValidUserSession)
                {
                    try
                    {
                        RunAsActiveUser user = new(ntAccount, sid, session.SessionId, isLocalAdmin); AssemblyPermissions.Remediate(user);
                        ProcessLaunchInfo args = new(ClientServerUtilities.ClientCompatiblePath.FullName, ["/GetLastInputTime"], Environment.SystemDirectory, user, createNoWindow: true);
                        idleTime = new(long.Parse(ProcessManager.LaunchAsync(args)!.Task.GetAwaiter().GetResult().StdOut[0], CultureInfo.InvariantCulture));
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
                (Interop.WTS_CONNECTSTATE_CLASS)session.State,
                isCurrentSession,
                isConsoleSession,
                isActiveUserSession,
                isValidUserSession,
                pWinStationName is not "Services" and not "RDP-Tcp",
                clientProtocolType != 0,
                isLocalAdmin,
                logonTime,
                idleTime,
                disconnectTime,
                clientName,
                (WTS_PROTOCOL_TYPE)clientProtocolType,
                GetString(session.SessionId, WTS_INFO_CLASS.WTSClientDirectory),
                (clientName is not null) ? GetValue<uint>(session.SessionId, WTS_INFO_CLASS.WTSClientBuildNumber) : null
            );
        }
    }
}

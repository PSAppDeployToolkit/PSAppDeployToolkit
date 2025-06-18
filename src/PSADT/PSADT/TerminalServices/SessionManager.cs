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
            WtsApi32.WTSEnumerateSessions(HANDLE.WTS_CURRENT_SERVER_HANDLE, out var pSessionInfo, out var pCount);
            using (pSessionInfo)
            {
                int objLength = Marshal.SizeOf(typeof(WTS_SESSION_INFOW));
                List<SessionInfo> sessions = [];
                for (int i = 0; i < pCount; i++)
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

            // Bomb out if we have no username (not a proper session).
            string? userName = GetValue<string>(session.SessionId, WTS_INFO_CLASS.WTSUserName);
            if (null == userName)
            {
                return null;
            }

            // Declare initial variables for data we need to get from a structured object.
            string domainName = GetValue<string>(session.SessionId, WTS_INFO_CLASS.WTSDomainName)!;
            NTAccount ntAccount = new(domainName, userName);
            SecurityIdentifier sid = GetWtsSessionSid(session.SessionId, ntAccount);
            var state = (LibraryInterfaces.WTS_CONNECTSTATE_CLASS)GetValue<uint>(session.SessionId, WTS_INFO_CLASS.WTSConnectState)!;
            string? clientName = GetValue<string>(session.SessionId, WTS_INFO_CLASS.WTSClientName);
            string pWinStationName = session.pWinStationName.ToString().TrimRemoveNull();
            DateTime? logonTime = null;
            TimeSpan? idleTime = null;
            DateTime? disconnectTime = null;

            // Get the extended session info and fill the above variables.
            if (GetValue<WTSINFOEXW>(session.SessionId, WTS_INFO_CLASS.WTSSessionInfoEx) is WTSINFOEXW wtsInfoEx && (WTS_INFO_LEVEL)wtsInfoEx.Level == WTS_INFO_LEVEL.WTSINFOEX_LEVEL1)
            {
                logonTime = DateTime.FromFileTime(wtsInfoEx.Data.WTSInfoExLevel1.LogonTime);
                idleTime = DateTime.Now - DateTime.FromFileTime(wtsInfoEx.Data.WTSInfoExLevel1.LastInputTime);
                if (wtsInfoEx.Data.WTSInfoExLevel1.DisconnectTime != 0)
                {
                    disconnectTime = DateTime.FromFileTime(wtsInfoEx.Data.WTSInfoExLevel1.DisconnectTime);
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
                state,
                session.SessionId == CurrentSessionId,
                session.SessionId == PInvoke.WTSGetActiveConsoleSessionId(),
                state == LibraryInterfaces.WTS_CONNECTSTATE_CLASS.WTSActive,
                pWinStationName != "Services" && pWinStationName != "RDP-Tcp",
                GetValue<ushort>(session.SessionId, WTS_INFO_CLASS.WTSClientProtocolType) != 0,
                AccountUtilities.IsSidMemberOfWellKnownGroup(sid, WellKnownSidType.BuiltinAdministratorsSid),
                logonTime,
                idleTime,
                disconnectTime,
                clientName,
                (WTS_PROTOCOL_TYPE)GetValue<ushort>(session.SessionId, WTS_INFO_CLASS.WTSClientProtocolType)!,
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
        /// <param name="usermame">The user account, represented as an <see cref="NTAccount"/>, for which the SID is being retrieved.</param>
        /// <returns>A <see cref="SecurityIdentifier"/> representing the SID of the specified session and user account</returns>
        private static SecurityIdentifier GetWtsSessionSid(uint sessionid, NTAccount usermame)
        {
            // Try everything we can to get the SID for the given session and user.
            try
            {
                return (SecurityIdentifier)usermame.Translate(typeof(SecurityIdentifier));
            }
            catch
            {
                // If we have the privileges, we can get the SID from the user's token.
                if (PrivilegeManager.IsPrivilegeEnabled(SE_PRIVILEGE.SeTcbPrivilege))
                {
                    try
                    {
                        WtsApi32.WTSQueryUserToken(sessionid, out var hUserToken);
                        using (hUserToken)
                        {
                            return TokenManager.GetTokenSid(hUserToken);
                        }
                    }
                    catch
                    {
                        // Just fall through here.
                    }
                }

                // Try and retrieve it from group policy information. This is the last chance we have.
                if (GroupPolicyAccountInfo.Get().FirstOrDefault(info => info.Username.Equals(usermame))?.SID is not SecurityIdentifier sid)
                {
                    // Throw the original exception.
                    throw;
                }
                return sid;
            }
        }

        /// <summary>
        /// Session Id of the current user running this library.
        /// </summary>
        private static readonly uint CurrentSessionId = Kernel32.ProcessIdToSessionId((uint)Process.GetCurrentProcess().Id);
    }
}

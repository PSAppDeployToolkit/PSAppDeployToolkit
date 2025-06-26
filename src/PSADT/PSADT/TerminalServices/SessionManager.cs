using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
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
                AccountUtilities.IsSidMemberOfWellKnownGroup(sid, WellKnownSidType.BuiltinAdministratorsSid),
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
            // Try everything we can to get the SID for the given session and user.
            try
            {
                return (SecurityIdentifier)username.Translate(typeof(SecurityIdentifier));
            }
            catch (Exception ex)
            {
                // If we have the privileges, we can get the SID from the user's token.
                if (PrivilegeManager.IsPrivilegeEnabled(SE_PRIVILEGE.SeTcbPrivilege))
                {
                    WtsApi32.WTSQueryUserToken(sessionid, out var hUserToken);
                    using (hUserToken)
                    {
                        Console.WriteLine("WTSQueryUserToken");
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

                // Try and retrieve it from group policy information.
                if (GroupPolicyAccountInfo.Get().FirstOrDefault(info => info.Username.Equals(username))?.SID is SecurityIdentifier sid)
                {
                    return sid;
                }

                // Try and retrieve it from the ProfileList registry key. This really is the last chance we have.
                using (var profileList = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList"))
                {
                    if (null != profileList)
                    {
                        List<SecurityIdentifier> sids = []; string user = username.ToString().Split('\\').Last();
                        foreach (var subKeyName in profileList.GetSubKeyNames())
                        {
                            using (var subKey = profileList.OpenSubKey(subKeyName))
                            {
                                // We use StartsWith() here to avoid issues with profiles ending in a domain name or 000, etc.
                                if (subKey?.GetValue("ProfileImagePath") is string profilePath && profilePath.Split('\\').Last().StartsWith(user, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Accumlate these so we can confirm we found only one SID.
                                    try
                                    {
                                        sids.Add(new(subKeyName));
                                    }
                                    catch (ArgumentException)
                                    {
                                        // Just fall through here. Some admins monkey around with the ProfileList keys.
                                        // https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues/1493.
                                    }
                                }
                            }
                        }
                        if (sids.Count == 1)
                        {
                            return sids[0];
                        }
                    }
                }

                // We didn't make it... Throw the original exception.
                throw new InvalidProgramException($"Failed to retrieve the SID for {username}.", ex);
            }
        }
    }
}

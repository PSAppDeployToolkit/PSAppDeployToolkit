using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using PSADT.LibraryInterfaces;
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
        /// Gets session info from all valid WTS sessions.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        public static ReadOnlyCollection<SessionInfo> GetSessionInfo()
        {
            WtsApi32.WTSEnumerateSessions(HANDLE.WTS_CURRENT_SERVER_HANDLE, out var ppSessionInfo, out var pCount);
            try
            {
                int structSize = Marshal.SizeOf(typeof(WTS_SESSION_INFOW));
                IntPtr current = ppSessionInfo;
                List<SessionInfo> sessions = [];
                for (int i = 0; i < pCount; i++)
                {
                    var sessionInfo = Marshal.PtrToStructure<WTS_SESSION_INFOW>(current);
                    if (GetSessionInfo(sessionInfo.SessionId) is SessionInfo session)
                    {
                        sessions.Add(session);
                    }
                    current += structSize;
                }
                return sessions.AsReadOnly();
            }
            finally
            {
                WtsApi32.WTSFreeMemory(ref ppSessionInfo);
            }
        }

        /// <summary>
        /// Gets session info for any provided valid session Id.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public static SessionInfo? GetSessionInfo(uint sessionId)
        {
            T? GetValue<T>(WTS_INFO_CLASS infoClass)
            {
                WtsApi32.WTSQuerySessionInformation(HANDLE.WTS_CURRENT_SERVER_HANDLE, sessionId, infoClass, out var pBuffer, out _);
                if (pBuffer != default && IntPtr.Zero != pBuffer)
                {
                    try
                    {
                        if (typeof(T) == typeof(string))
                        {
                            if (Marshal.PtrToStringUni(pBuffer)?.Trim() is string result && !string.IsNullOrWhiteSpace(result))
                            {
                                return (T)(object)result;
                            }
                        }
                        if (typeof(T) == typeof(ushort))
                        {
                            return (T)(object)(ushort)Marshal.ReadInt16(pBuffer);
                        }
                        else if (typeof(T) == typeof(uint))
                        {
                            return (T)(object)(uint)Marshal.ReadInt32(pBuffer);
                        }
                        else if (typeof(T) == typeof(WTSINFOEXW))
                        {
                            return (T)(object)Marshal.PtrToStructure<WTSINFOEXW>(pBuffer);
                        }
                    }
                    finally
                    {
                        WtsApi32.WTSFreeMemory(ref pBuffer);
                    }
                }
                return default;
            }

            // Bomb out if we have no username (not a proper session).
            string? userName = GetValue<string>(WTS_INFO_CLASS.WTSUserName);
            if (null == userName)
            {
                return null;
            }

            // Declare initial variables for data we need to get from a structured object.
            string? domainName = GetValue<string>(WTS_INFO_CLASS.WTSDomainName);
            NTAccount ntAccount = new NTAccount($"{domainName}\\{userName}");
            SecurityIdentifier sid = (SecurityIdentifier)ntAccount.Translate(typeof(SecurityIdentifier));
            uint? state = GetValue<uint>(WTS_INFO_CLASS.WTSConnectState);
            string? clientName = GetValue<string>(WTS_INFO_CLASS.WTSClientName);
            DateTime? logonTime = null;
            TimeSpan? idleTime = null;
            DateTime? disconnectTime = null;

            // Get the extended session info and fill the above variables.
            if (GetValue<WTSINFOEXW>(WTS_INFO_CLASS.WTSSessionInfoEx) is WTSINFOEXW wtsInfoEx && (WTS_INFO_LEVEL)wtsInfoEx.Level == WTS_INFO_LEVEL.WTSINFOEX_LEVEL1)
            {
                logonTime = DateTime.FromFileTime(wtsInfoEx.Data.WTSInfoExLevel1.LogonTime);
                idleTime = DateTime.Now - DateTime.FromFileTime(wtsInfoEx.Data.WTSInfoExLevel1.LastInputTime);
                if (wtsInfoEx.Data.WTSInfoExLevel1.DisconnectTime != 0)
                {
                    disconnectTime = DateTime.FromFileTime(wtsInfoEx.Data.WTSInfoExLevel1.DisconnectTime);
                }
            }

            // Instantiate a SessionInfo object and return it to the caller.
            return new SessionInfo(
                ntAccount,
                sid,
                userName,
                domainName,
                sessionId,
                GetValue<string>(WTS_INFO_CLASS.WTSWinStationName),
                (LibraryInterfaces.WTS_CONNECTSTATE_CLASS)state!,
                sessionId == CurrentSessionId,
                sessionId == PInvoke.WTSGetActiveConsoleSessionId(),
                (LibraryInterfaces.WTS_CONNECTSTATE_CLASS)state == LibraryInterfaces.WTS_CONNECTSTATE_CLASS.WTSActive,
                !GetValue<string>(WTS_INFO_CLASS.WTSWinStationName)!.Equals("services", StringComparison.InvariantCultureIgnoreCase),
                GetValue<ushort>(WTS_INFO_CLASS.WTSClientProtocolType) != 0,
                AccountUtilities.IsSidMemberOfGroup(WellKnownSidType.BuiltinAdministratorsSid, sid),
                logonTime,
                idleTime,
                disconnectTime,
                clientName,
                (WTS_PROTOCOL_TYPE)GetValue<ushort>(WTS_INFO_CLASS.WTSClientProtocolType)!,
                GetValue<string>(WTS_INFO_CLASS.WTSClientDirectory),
                (null != clientName) ? GetValue<uint>(WTS_INFO_CLASS.WTSClientBuildNumber) : null
            );
        }

        /// <summary>
        /// Session Id of the current user running this library.
        /// </summary>
        private static readonly uint CurrentSessionId = Kernel32.ProcessIdToSessionId((uint)Process.GetCurrentProcess().Id);
    }
}

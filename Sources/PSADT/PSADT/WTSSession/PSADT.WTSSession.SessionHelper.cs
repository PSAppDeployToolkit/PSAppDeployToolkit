using System;
using System.Net;
using System.Linq;
using PSADT.PInvoke;
using PSADT.ConsoleEx;
using System.Security;
using Microsoft.Win32;
using PSADT.OperatingSystem;
using System.ComponentModel;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace PSADT.WTSSession
{
    /// <summary>
    /// Provides utility methods for working with enumerated sessions.
    /// </summary>
    public static class SessionHelper
    {
        /// <summary>
        /// Opens a handle to the specified machine. The local machine is used if the name is null, empty, or whitespace.
        /// </summary>
        /// <param name="hServerName"></param>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        private static SafeWTSServer GetWTSServer(string? hServerName = "")
        {
            try
            {
                IntPtr handle;

                if (string.IsNullOrWhiteSpace(hServerName))
                {
                    handle = IntPtr.Zero;
                    ConsoleHelper.DebugWrite("Using local server for WTS operations.", MessageType.Debug);
                }
                else
                {
                    handle = NativeMethods.WTSOpenServer(hServerName!);
                    if (handle == IntPtr.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), $"'WTSOpenServer' failed to open a handle to hServerName [{hServerName}].");
                    }
                    ConsoleHelper.DebugWrite($"Opened WTS server handle for [{hServerName}].", MessageType.Debug);
                }

                return new SafeWTSServer(handle);
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to get server for WTS operations.", MessageType.Error, ex);
                throw;
            }
        }

        /// <summary>
        /// Enumerates all sessions on the specified Remote Desktop Session Host (RD Session Host) server.
        /// </summary>
        /// <param name="hServerName">The name of the RD Session Host server. If <see langword="null"/>, the local server is used.</param>
        /// <param name="ppSessionInfo">
        /// An array of <see cref="WTS_SESSION_INFO"/> structures that receive information about the sessions on the server.
        /// This value is <see langword="null"/> if the enumeration fails or no sessions are found.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the enumeration succeeds and at least one session is found; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="Win32Exception">
        /// Thrown when the underlying call to <see cref="NativeMethods.WTSEnumerateSessions"/> fails.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the memory tokenInfo returned is invalid or the handle is closed.
        /// </exception>
        private static bool GetWTSEnumerateSessions(string? hServerName, out WTS_SESSION_INFO[]? ppSessionInfo)
        {
            ppSessionInfo = null;

            try
            {
                using SafeWTSServer hServer = GetWTSServer(hServerName);
                if (!NativeMethods.WTSEnumerateSessions(hServer, 0U, 1U, out SafeWtsMemory sessionInfo, out uint sessionCount))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to query 'WTSEnumerateSessions'.");
                }

                if (sessionInfo.IsInvalid || sessionInfo.IsClosed)
                {
                    throw new InvalidOperationException("Cannot convert from an invalid or closed handle.");
                }

                using (sessionInfo)
                {
                    ppSessionInfo = sessionInfo.ToArray<WTS_SESSION_INFO>((int)sessionCount);
                }

                ConsoleHelper.DebugWrite($"Enumerated [{sessionCount}] WTS sessions.", MessageType.Debug);
                return ppSessionInfo.Length > 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to enumerate WTS sessions.", MessageType.Error, ex);
                return false;
            }
        }


        /// <summary>
        /// Retrieves a session property of the specified type from a remote session on the given server.
        /// </summary>
        /// <typeparam name="T">The type of the property to retrieve.</typeparam>
        /// <param name="hServer">The handle to the server on which the session is running.</param>
        /// <param name="sessionId">The session ID to query for information.</param>
        /// <param name="wtsInfoClass">The information class specifying the property to retrieve.</param>
        /// <returns>
        /// The session property of the specified type.
        /// </returns>
        /// <exception cref="Win32Exception">
        /// Thrown when the underlying call to <see cref="NativeMethods.WTSQuerySessionInformation"/> fails.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the memory tokenInfo returned is invalid or the handle is closed.
        /// </exception>
        private static T GetWTSInfoClassProperty<T>(SafeWTSServer hServer, uint sessionId, WTS_INFO_CLASS wtsInfoClass)
        {
            if (!NativeMethods.WTSQuerySessionInformation(hServer, sessionId, wtsInfoClass, out SafeWtsMemory? ppBuffer, out uint pBytesReturned))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to query 'WTSQuerySessionInformation'.");
            }

            using (ppBuffer)
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)ppBuffer.ToString(pBytesReturned);
                }
                else
                {
                    return ppBuffer.ToStructure<T>(pBytesReturned);
                }
            }
        }

        /// <summary>
        /// Queries the specified session on the given server for information of type <see cref="WINSTATIONINFORMATIONW"/>.
        /// </summary>
        /// <param name="hServer">The handle to the server on which the session is running.</param>
        /// <param name="sessionId">The session ID to query for information.</param>
        /// <returns>
        /// A <see cref="WINSTATIONINFORMATIONW"/> structure containing the information about the specified session.
        /// </returns>
        /// <remarks>
        /// This method calls the native <see cref="NativeMethods.WinStationQueryInformation"/> function to retrieve session information.
        /// The information is returned in a managed <see cref="WINSTATIONINFORMATIONW"/> structure.
        /// </remarks>
        /// <exception cref="Win32Exception">
        /// Thrown when the underlying call to <see cref="NativeMethods.WinStationQueryInformation"/> fails.
        /// </exception>
        private static WINSTATIONINFORMATIONW WinStationQueryInformation(SafeWTSServer hServer, uint sessionId)
        {
            try
            {
                if (!NativeMethods.WinStationQueryInformation(hServer, sessionId, (int)WINSTATIONINFOCLASS.WinStationInformation, out WINSTATIONINFORMATIONW wsInfo, Marshal.SizeOf(typeof(WINSTATIONINFORMATIONW)), out _))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to query 'WinStationQueryInformation'.");
                }

                ConsoleHelper.DebugWrite($"WinStation information queried for session id [{sessionId}].", MessageType.Debug);
                return wsInfo;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to query WinStation information for session id [{sessionId}].", MessageType.Error, ex);
                throw;
            }
        }

        /// <summary>
        /// Enumerates all sessions on the specified machine. If the machine name is null or empty, the local machine is used.
        /// </summary>
        /// <param name="hServerName">The name of the machine to enumerate sessions on. Defaults to the local machine.</param>
        /// <returns>A list of enumerated session information.</returns>
        public static List<SessionInfo>? GetWTSSessions(string? hServerName = "")
        {
            WTS_SESSION_INFO[] sessionsInfo;

            if (!GetWTSEnumerateSessions(hServerName, out sessionsInfo!))
            {
                throw new Win32Exception($"'WTSEnumerateSessions' failed to enumerate sessions on machineName [{hServerName}].");
            }

            List<SessionInfo>? enumeratedSessions = new List<SessionInfo>();
            var isPrimaryActiveUserSessionSet = false;
            var isPrimaryActiveLocalAdminUserSessionSet = false;
            var isUserSessionSet = false;

            bool? isCurrentProcessSession = false;
            if (!NativeMethods.ProcessIdToSessionId(NativeMethods.GetCurrentProcessId(), out uint currentProcessSessionId))
            {
                isCurrentProcessSession = null;
            }

            OSVersionInfo osVersionInfo = OSHelper.GetOsVersionInfo();
            bool isLocalServer = string.IsNullOrEmpty(hServerName);

            foreach (WTS_SESSION_INFO session in sessionsInfo)
            {
                if (isCurrentProcessSession != null)
                {
                    isCurrentProcessSession = false;
                }

                var isConsoleSession = false;
                var isActiveSession = false;
                var isConnectedSession = false;
                var isDisconnectedSession = false;
                var isRemoteSession = false;
                var isRdpSession = false;
                var isHdxSession = false;
                var isRemoteListenerSession = false;
                var isLocalSession = false;
                var isSystemSession = false;
                var isServicesSession = false;
                var isConnectedConsoleSession = false;
                var isUserSession = false;
                var isActiveUserSession = false;
                var isConsoleActiveUserSession = false;
                var isPrimaryActiveUserSession = false;
                var isPrimaryActiveLocalAdminUserSession = false;
                var isConnectedUserSession = false;
                var isLocalAdminUserSession = false;

                if (isCurrentProcessSession != null && session.SessionId == currentProcessSessionId)
                {
                    isCurrentProcessSession = true;
                }

                if (session.pWinStationName.Equals("console", StringComparison.OrdinalIgnoreCase))
                {
                    isConsoleSession = true;
                }

                if (session.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                {
                    isActiveSession = true;
                }

                if (session.State == WTS_CONNECTSTATE_CLASS.WTSConnected)
                {
                    isConnectedSession = true;
                }

                if (session.State == WTS_CONNECTSTATE_CLASS.WTSDisconnected)
                {
                    isDisconnectedSession = true;
                }

                var isWinStationNameStartsWithRdp = session.pWinStationName.StartsWith("RDP-Tcp", StringComparison.OrdinalIgnoreCase);
                var isWinStationNameStartsWithIca = session.pWinStationName.StartsWith("ICA-Tcp", StringComparison.OrdinalIgnoreCase);
                if (isWinStationNameStartsWithRdp || isWinStationNameStartsWithIca)
                {
                    isRemoteSession = true;

                    if (isWinStationNameStartsWithRdp)
                    {
                        isRdpSession = true;
                    }

                    if (isWinStationNameStartsWithIca)
                    {
                        isHdxSession = true;
                    }
                }
                else
                {
                    isLocalSession = true;
                }

                if (osVersionInfo.IsWorkstation && OSHelper.GetIsWindows7OrGreater(osVersionInfo.OperatingSystem) &&
                    osVersionInfo.IsServer && OSHelper.GetIsWindowsServer2012OrGreater(osVersionInfo.OperatingSystem) &&
                    isLocalServer)
                {
                    isRemoteSession = GetWTSInfoClassProperty<bool>(SafeWTSServer.WTS_CURRENT_SERVER_HANDLE, session.SessionId, WTS_INFO_CLASS.WTSIsRemoteSession);
                }

                if (session.State == WTS_CONNECTSTATE_CLASS.WTSListen)
                {
                    isRemoteListenerSession = true;
                }

                if (session.pWinStationName.Equals("services", StringComparison.OrdinalIgnoreCase))
                {
                    isServicesSession = true;
                }

                if (isConsoleSession && isConnectedSession)
                {
                    isConnectedConsoleSession = true;
                }

                if (isServicesSession || isConnectedConsoleSession || isRemoteListenerSession)
                {
                    isSystemSession = true;
                }
                else
                {
                    isUserSession = true;
                    isUserSessionSet = true;

                    try
                    {
                        QueryUserToken(session.SessionId, out SafeAccessToken userImpersonationToken);
                        using (userImpersonationToken)
                        {
                            DuplicateTokenAsPrimary(userImpersonationToken, out SafeAccessToken userPrimaryToken);
                            using (userPrimaryToken)
                            {
                                isLocalAdminUserSession = IsTokenLocalAdmin(in userPrimaryToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleHelper.DebugWrite($"Failed to determine if the token belongs to a local admin.", MessageType.Error, ex);
                        throw new Win32Exception($"Failed to determine if the token belongs to a local admin.", ex);
                    }

                    if (isActiveSession)
                    {
                        isActiveUserSession = true;
                    }

                    if (isConsoleSession && isActiveSession)
                    {
                        isConsoleActiveUserSession = true;
                        isPrimaryActiveUserSession = true;
                        isPrimaryActiveUserSessionSet = true;

                        if (isLocalAdminUserSession)
                        {
                            isPrimaryActiveLocalAdminUserSession = true;
                            isPrimaryActiveLocalAdminUserSessionSet = true;
                        }
                    }

                    if (isConnectedSession)
                    {
                        isConnectedUserSession = true;
                    }
                }

                enumeratedSessions.Add(new SessionInfo(
                    session.SessionId,
                    session.pWinStationName,
                    session.State.ToString(),

                    isCurrentProcessSession,
                    isConsoleSession,
                    isActiveSession,

                    isConnectedSession,
                    isDisconnectedSession,

                    isRemoteSession,
                    isRdpSession,
                    isHdxSession,
                    isRemoteListenerSession,

                    isLocalSession,

                    isSystemSession,
                    isServicesSession,
                    isConnectedConsoleSession,

                    isUserSession,
                    isLocalAdminUserSession,
                    isActiveUserSession,
                    isConsoleActiveUserSession,
                    isPrimaryActiveUserSession,
                    isPrimaryActiveLocalAdminUserSession,
                    isConnectedUserSession));
            }

            if (enumeratedSessions == null || !isUserSessionSet) return enumeratedSessions;

            var userSessions = enumeratedSessions.Where(x => x.IsUserSession);

            if (!isPrimaryActiveUserSessionSet)
            {
                var firstActiveUserSession = userSessions.FirstOrDefault(x => x.IsActiveSession);
                if (firstActiveUserSession != null)
                {
                    foreach (var session in enumeratedSessions)
                    {
                        if (session.SessionId == firstActiveUserSession.SessionId)
                        {
                            session.IsPrimaryActiveUserSession = true;
                            isPrimaryActiveUserSessionSet = true;
                            break;
                        }
                    }
                }
            }

            if (!isPrimaryActiveLocalAdminUserSessionSet)
            {
                var firstActiveLocalAdminUserSession = userSessions.FirstOrDefault(x => x.IsActiveSession && x.IsLocalAdminUserSession);
                if (firstActiveLocalAdminUserSession != null)
                {
                    foreach (var session in enumeratedSessions)
                    {
                        if (session.SessionId == firstActiveLocalAdminUserSession.SessionId)
                        {
                            session.IsPrimaryActiveLocalAdminUserSession = true;
                            isPrimaryActiveLocalAdminUserSessionSet = true;
                            break;
                        }
                    }
                }
            }

            return enumeratedSessions;
        }

        public static ExtendedSessionInfo GetExtendedSessionInfo(uint sessionId, string? hServerName = "")
        {
            try
            {
                var hServer = GetWTSServer(hServerName);
                var sessionInfo = new ExtendedSessionInfo
                {
                    SessionId = GetWTSInfoClassProperty<long>(hServer, sessionId, WTS_INFO_CLASS.WTSSessionId),
                    SessionName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSWinStationName),

                    ClientBuildNumber = GetWTSInfoClassProperty<long>(hServer, sessionId, WTS_INFO_CLASS.WTSClientBuildNumber),
                    ClientDirectory = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSClientDirectory),
                    ClientName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSClientName),
                    ClientProtocolType = GetWTSInfoClassProperty<WTS_CLIENT_PROTOCOL_TYPE>(hServer, sessionId, WTS_INFO_CLASS.WTSClientProtocolType).ToString(),

                    ConnectionState = GetWTSInfoClassProperty<WTS_CONNECTSTATE_CLASS>(hServer, sessionId, WTS_INFO_CLASS.WTSConnectState).ToString()
                };

                string domainName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSDomainName).ToUpperInvariant();
                sessionInfo.DomainName = domainName;

                string userName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSUserName);
                sessionInfo.UserName = userName;

                if (!string.IsNullOrEmpty(domainName) && !string.IsNullOrEmpty(userName))
                {
                    sessionInfo.NTAccount = new NTAccount($@"{domainName}\{userName}");
                    sessionInfo.Sid = (SecurityIdentifier)sessionInfo.NTAccount.Translate(typeof(SecurityIdentifier));
                }

                WTS_CLIENT_ADDRESS clientAddress = GetWTSInfoClassProperty<WTS_CLIENT_ADDRESS>(hServer, sessionId, WTS_INFO_CLASS.WTSClientAddress);
                sessionInfo.ClientIPAddress = GetWtsIPAddress((ADDRESS_FAMILY_TYPE)clientAddress.AddressFamily, clientAddress.Address);
                sessionInfo.ClientIPAddressFamily = ((ADDRESS_FAMILY_TYPE)clientAddress.AddressFamily).ToString();

                WTS_SESSION_ADDRESS sessionAddress = GetWTSInfoClassProperty<WTS_SESSION_ADDRESS>(hServer, sessionId, WTS_INFO_CLASS.WTSSessionAddressV4);
                if (!sessionAddress.Equals(default(WTS_SESSION_ADDRESS)))
                {
                    sessionInfo.SessionIPAddress = GetWtsIPAddress((ADDRESS_FAMILY_TYPE)sessionAddress.AddressFamily, sessionAddress.Address);
                }

                WTS_CLIENT_DISPLAY clientDisplay = GetWTSInfoClassProperty<WTS_CLIENT_DISPLAY>(hServer, sessionId, WTS_INFO_CLASS.WTSSessionAddressV4);
                sessionInfo.HorizontalResolution = clientDisplay.HorizontalResolution;
                sessionInfo.VerticalResolution = clientDisplay.VerticalResolution;
                sessionInfo.ColorDepth = clientDisplay.ColorDepth;

                OSVersionInfo osVersionInfo = OSHelper.GetOsVersionInfo();

                if (OSHelper.GetIsWindowsVistaSP1OrGreater(osVersionInfo.OperatingSystem))
                {
                    WTSINFO wtsInfo = GetWTSInfoClassProperty<WTSINFO>(hServer, sessionId, WTS_INFO_CLASS.WTSSessionInfo);
                    DateTime? logonTime = FileTimeToDateTime(wtsInfo.LogonTime);
                    DateTime? lastInputTime = FileTimeToDateTime(wtsInfo.LastInputTime);
                    DateTime? disconnectTime = FileTimeToDateTime(wtsInfo.DisconnectTime);
                    DateTime? currentTime = FileTimeToDateTime(wtsInfo.CurrentTime);
                    sessionInfo.LogonTime = logonTime;
                    sessionInfo.IdleTime = currentTime != null && lastInputTime != null ? currentTime - lastInputTime : TimeSpan.Zero;
                    sessionInfo.DisconnectTime = disconnectTime;
                }
                else
                {
                    var winStationInfo = WinStationQueryInformation(hServer, sessionId);
                    DateTime? logonTime = FileTimeToDateTime(winStationInfo.LoginTime);
                    DateTime? lastInputTime = FileTimeToDateTime(winStationInfo.LastInputTime);
                    DateTime? disconnectTime = FileTimeToDateTime(winStationInfo.DisconnectTime);
                    DateTime? currentTime = FileTimeToDateTime(winStationInfo.CurrentTime);
                    sessionInfo.LogonTime = logonTime;
                    sessionInfo.IdleTime = currentTime != null && lastInputTime != null ? currentTime - lastInputTime : TimeSpan.Zero;
                    sessionInfo.DisconnectTime = disconnectTime;
                }

                if (osVersionInfo.IsWorkstation && OSHelper.GetIsWindows7OrGreater(osVersionInfo.OperatingSystem) &&
                    osVersionInfo.IsServer && OSHelper.GetIsWindowsServer2012OrGreater(osVersionInfo.OperatingSystem) &&
                    hServer.IsLocalServer)
                {
                    sessionInfo.IsRemoteSession = GetWTSInfoClassProperty<bool>(hServer, sessionId, WTS_INFO_CLASS.WTSIsRemoteSession);
                }

                ConsoleHelper.DebugWrite($"Retrieved extended session information for session id [{sessionId}].", MessageType.Debug);
                return sessionInfo;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Error in GetExtendedSessionInfo: {ex.Message}", MessageType.Error, ex);
                throw;
            }
        }

        public static string GetWtsUsernameById(uint sessionId, string? hServerName = "")
        {
            string domainName;
            string userName;

            using (var hServer = GetWTSServer(hServerName))
            {
                domainName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSDomainName).ToUpperInvariant();
                userName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSUserName);
            }

            ConsoleHelper.DebugWrite($"Domain: {domainName}, Username: {userName}.", MessageType.Debug);

            if (string.IsNullOrEmpty(domainName) || string.IsNullOrEmpty(userName))
            {
                throw new InvalidOperationException($"Failed to retrieve a valid domain or username for session id [{sessionId}].");
            }

            return $@"{domainName}\{userName}";
        }

        public static IPAddress GetWtsIPAddress(ADDRESS_FAMILY_TYPE family, byte[] rawAddress)
        {
            IPAddress parsedAddress = IPAddress.None;

            switch (family)
            {
                case ADDRESS_FAMILY_TYPE.IPv4:
                    string ipV4String = string.Join(".", rawAddress.Skip(2).Take(4));
                    if (!IPAddress.TryParse(ipV4String, out parsedAddress!))
                    {
                        parsedAddress = IPAddress.None;
                    }
                    break;
                case ADDRESS_FAMILY_TYPE.IPv6:
                    string ipV6String = string.Join(":", rawAddress.Skip(2).Take(16));
                    if (!IPAddress.TryParse(ipV6String, out parsedAddress!))
                    {
                        parsedAddress = IPAddress.None;
                    }
                    break;
            }

            return parsedAddress;
        }

        public static DateTime? FileTimeToDateTime(FILETIME fileTime)
        {
            if (!NativeMethods.FileTimeToSystemTime(in fileTime, out SYSTEMTIME systemTime))
            {
                return null;
            }
            if (systemTime.wYear < 1900)
            {
                // Got invalid date. Happens sometimes on Windows Server 2003.
                return null;
            }

            return systemTime.ToDateTime(DateTimeKind.Local);
        }

        public static List<SessionInfo>? GetAllActiveUserSessions(string? hServerName = "")
        {
            try
            {
                List<SessionInfo>? allActiveUserSessions = GetWTSSessions(hServerName)?
                    .Where(x => x.IsActiveUserSession)
                    .ToList();

                if (allActiveUserSessions != null && allActiveUserSessions.Any())
                {
                    ConsoleHelper.DebugWrite($"Discovered [{allActiveUserSessions.Count}] active user sessions.", MessageType.Debug);
                }
                else
                {
                    ConsoleHelper.DebugWrite("No active user sessions found.", MessageType.Warning);
                }

                return allActiveUserSessions;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to get all active user sessions.", MessageType.Error, ex);
                throw;
            }
        }

        public static SessionInfo? GetSession(uint sessionId, string? hServerName = "")
        {
            try
            {
                SessionInfo? session = GetWTSSessions(hServerName)?
                    .Where(x => x.SessionId == sessionId)
                    .FirstOrDefault();

                if (session != null)
                {
                    ConsoleHelper.DebugWrite($"Discoverd session for session id [{sessionId}].", MessageType.Debug);
                }
                else
                {
                    ConsoleHelper.DebugWrite($"No session found for session id [{sessionId}].", MessageType.Warning);
                }

                return session;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to get session for session id [{sessionId}].", MessageType.Error, ex);
                throw;
            }
        }

        public static SessionInfo? GetPrimaryActiveUserSession(string? hServerName = "")
        {
            /// Determine the primary active user session
            //  If an active console user exists, then that will be the active user session. In some scenarios, there can be an active console and non-console session. Since admins log into the console session, we give it preference.
            //  If no active console user exists but users are logged in, such as on terminal servers or VDIs, then the first logged-in non-console user that is 'Active' is the active user.

            try
            {
                SessionInfo? primaryActiveUserSession = GetWTSSessions(hServerName)?
                    .Where(x => x.IsPrimaryActiveUserSession)
                    .FirstOrDefault();

                if (primaryActiveUserSession != null)
                {
                    ConsoleHelper.DebugWrite($"Discovered primary active user session with id [{primaryActiveUserSession.SessionId}].", MessageType.Debug);
                }
                else
                {
                    ConsoleHelper.DebugWrite("No primary active user session found.", MessageType.Warning);
                }

                return primaryActiveUserSession;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to get primary active user session.", MessageType.Error, ex);
                throw;
            }
        }

        public static uint? GetPrimaryActiveUserSessionId(string? hServerName = "")
        {
            /// Determine the active user session ID
            //  If an active console user exists, then that will be the active user session. In some scenarios, there can be an active console and non-console session. Since admins log into the console session, we give it preference.
            //  If no active console user exists but users are logged in, such as on terminal servers or VDIs, then the first logged-in non-console user that is 'Active' is the active user.

            try
            {
                var session = GetPrimaryActiveUserSession(hServerName);
                if (session != null)
                {
                    ConsoleHelper.DebugWrite($"Primary active user session id [{session.SessionId}].", MessageType.Debug);
                    return session.SessionId;
                }
                ConsoleHelper.DebugWrite("No primary active user session found.", MessageType.Warning);
                return null;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to get primary active user session id.", MessageType.Error, ex);
                throw;
            }
        }

        public static SessionInfo? GetPrimaryActiveLocalAdminUserSession(string? hServerName = "")
        {
            /// Determine the primary active local admin user session
            //  If an active, local admin, console user exists, then that will be the primary session. In some scenarios, there can be an active console and non-console session. Since admins log into the console session, we give it preference.
            //  If no active, local admin, console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user that is 'Active' and is a local admin is the primary session.

            try
            {
                List<SessionInfo>? sessions = GetWTSSessions(hServerName);

                if (sessions == null || !sessions.Any())
                {
                    ConsoleHelper.DebugWrite("No sessions found.", MessageType.Warning);
                    return null;
                }

                SessionInfo? primaryActiveLocalAdminUserSession = sessions
                    .Where(x => x.IsPrimaryActiveLocalAdminUserSession)
                    .FirstOrDefault();

                if (primaryActiveLocalAdminUserSession != null)
                {
                    ConsoleHelper.DebugWrite($"Primary active local admin user session found with session id [{primaryActiveLocalAdminUserSession.SessionId}].", MessageType.Debug);
                }
                else
                {
                    ConsoleHelper.DebugWrite("No primary active local admin user session found.", MessageType.Warning);
                }

                return primaryActiveLocalAdminUserSession;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to find a primary active loacl admin user session.", MessageType.Error, ex);
                throw;
            }
        }

        public static uint? GetPrimaryActiveLocalAdminUserSessionId(string? hServerName = "")
        {
            /// Determine the primary active local admin user session
            //  If an active, local admin, console user exists, then that will be the primary session. In some scenarios, there can be an active console and non-console session. Since admins log into the console session, we give it preference.
            //  If no active, local admin, console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user that is 'Active' and is a local admin is the primary session.

            try
            {
                var session = GetPrimaryActiveLocalAdminUserSession(hServerName);
                if (session != null)
                {
                    ConsoleHelper.DebugWrite($"Discovered primary active local admin user session id [{session.SessionId}].", MessageType.Debug);
                    return session.SessionId;
                }

                ConsoleHelper.DebugWrite("No primary active local admin user session found.", MessageType.Warning);
                return null;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to get primary active local admin user session id.", MessageType.Error, ex);
                throw;
            }
        }

        public static bool TryGetWindowsIdentity(in SafeAccessToken token, out WindowsIdentity? windowsIdentity, out WindowsPrincipal? windowsPrincipal)
        {
            windowsIdentity = null;
            windowsPrincipal = null;

            try
            {
                windowsIdentity = new WindowsIdentity(token.DangerousGetHandle());
                ConsoleHelper.DebugWrite("WindowsIdentity created successfully from token.", MessageType.Debug);

                windowsPrincipal = new WindowsPrincipal(windowsIdentity);
                ConsoleHelper.DebugWrite("WindowsPrincipal created successfully from WindowsIdentity.", MessageType.Debug);

                return true;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite("Failed to get WindowsIdentity or WindowsPrincipal from token.", MessageType.Error, ex);
                return false;
            }
        }

        /// <summary>
        /// Queries the user token for a given session ID.
        /// </summary>
        /// <param name="sessionId">The session ID to query the user token for.</param>
        /// <returns><c>true</c> if we obtained the access token of the log-on user specified by the session ID.</returns>
        /// <exception cref="Win32Exception">Thrown if we fail to obtain the access token.</exception>
        [SecurityCritical]
        public static bool QueryUserToken(uint sessionId, out SafeAccessToken impersonationToken)
        {
            try
            {
                if (!NativeMethods.WTSQueryUserToken(sessionId, out impersonationToken))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"'WTSQueryUserToken' failed to obtain the access token of the logged-on user specified by the session id [{sessionId}].");
                }

                ConsoleHelper.DebugWrite($"User token queried successfully for session id [{sessionId}].", MessageType.Debug);
                return true;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to query user token for session id [{sessionId}].", MessageType.Error, ex);
                impersonationToken = SafeAccessToken.Invalid;
                return false;
            }
        }

        /// <summary>
        /// Duplicates a given access token as a primary token.
        /// </summary>
        /// <param name="token">The token to duplicate.</param>
        /// <returns><c>true</c> if token duplication was successful.</returns>
        /// <exception cref="Win32Exception">Thrown if the token duplication fails.</exception>
        [SecurityCritical]
        public static bool DuplicateTokenAsPrimary(SafeAccessToken token, out SafeAccessToken primaryToken)
        {
            try
            {
                if (!NativeMethods.DuplicateTokenEx(
                    token,
                    TokenAccess.TOKEN_ALL_ACCESS,
                    SECURITY_ATTRIBUTES.Create(),
                    SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    TOKEN_TYPE.TokenPrimary,
                    out primaryToken))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"'DuplicateTokenEx' failed to create a primary access token from the existing token.");
                }

                ConsoleHelper.DebugWrite("Successfully duplicated token as a primary token.", MessageType.Debug);

                // This assumes that the caller had no further use for the token.
                if (!token.IsInvalid)
                {
                    token.Dispose();
                }

                return true;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to duplicate token as primary token.", MessageType.Error, ex);
                primaryToken = SafeAccessToken.Invalid;
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
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", false))
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
                ConsoleHelper.DebugWrite($"Failed to check UAC status: {ex.Message}", MessageType.Warning);
            }

            return false; // Default to false if we can't determine the UAC status
        }

        public static T GetTokenInformation<T>(SafeAccessToken tokenHandle, TOKEN_INFORMATION_CLASS tokenInformationClass)
        {

            // First, get the required buffer size
            if (!NativeMethods.GetTokenInformation(tokenHandle, tokenInformationClass, IntPtr.Zero, 0, out int tokenInfoLength))
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 122) // ERROR_INSUFFICIENT_BUFFER
                {
                    throw new Win32Exception(error, $"Failed to get token information size for type [{typeof(T)}]. Error code [{error}].");
                }
            }

            IntPtr tokenInfo = Marshal.AllocHGlobal(tokenInfoLength);

            try
            {
                if (!NativeMethods.GetTokenInformation(tokenHandle, tokenInformationClass, tokenInfo, tokenInfoLength, out _))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to retrieve token information of type [{typeof(T)}].");
                }

                if (typeof(T).IsEnum)
                {
                    int enumValue = Marshal.ReadInt32(tokenInfo);
                    return (T)Enum.ToObject(typeof(T), enumValue);
                }
                else
                {
                    return Marshal.PtrToStructure<T>(tokenInfo) ?? throw new InvalidOperationException($"Failed to marshal token information to type [{typeof(T)}].");
                }
            }
            finally
            {
                if (tokenInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tokenInfo);
                }
            }
        }

        public static TOKEN_ELEVATION_TYPE GetTokenElevationType(SafeAccessToken tokenHandle)
        {
            try
            {
                return GetTokenInformation<TOKEN_ELEVATION_TYPE>(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType);
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to get token elevation type.", MessageType.Error, ex);
                throw;
            }
        }

        public static bool IsTokenElevated(SafeAccessToken tokenHandle)
        {
            try
            {
                TOKEN_ELEVATION elevation = GetTokenInformation<TOKEN_ELEVATION>(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevation);
                return elevation.TokenIsElevated;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to determine if the token is elevated.", MessageType.Error, ex);
                throw;
            }
        }

        public static bool GetLinkedElevatedToken(in SafeAccessToken hToken, out SafeAccessToken hElevatedLinkedToken)
        {
            hElevatedLinkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_ELEVATION_TYPE elevationType = GetTokenElevationType(hToken);

                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull)
                {
                    hElevatedLinkedToken = hToken;
                    return true;
                }

                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
                {
                    // No linked token available
                    return false;
                }

                // Determine whether system is running Windows Vista or later operating systems (major version >= 6) because they support linked tokens, but
                // previous versions (major version < 6) do not.
                if (Environment.OSVersion.Version.Major >= 6 && IsUACEnabled())
                {
                    // If limited, get the linked elevated token for further check.
                    if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited)
                    {
                        return GetLinkedToken(hToken, out hElevatedLinkedToken);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to get linked elevated token: {ex.Message}", MessageType.Error, ex);
                return false;
            }
        }

        public static bool GetLinkedStandardToken(in SafeAccessToken hToken, out SafeAccessToken hStandardLinkedToken)
        {
            hStandardLinkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_ELEVATION_TYPE elevationType = GetTokenElevationType(hToken);

                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited)
                {
                    hStandardLinkedToken = hToken;
                    return true;
                }

                if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault)
                {
                    // No linked token available
                    return false;
                }

                // Determine whether system is running Windows Vista or later operating systems (major version >= 6) because they support linked tokens
                if (Environment.OSVersion.Version.Major >= 6 && IsUACEnabled())
                {
                    // If full, get the linked limited token
                    if (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull)
                    {
                        return GetLinkedToken(hToken, out hStandardLinkedToken);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to get linked standard token: {ex.Message}", MessageType.Error, ex);
                return false;
            }
        }

        /// <summary>
        /// The function checks whether a primary access token belongs to a user account that is a member of the local Administrators group.
        /// </summary>
        /// <param name="hToken">The process to check.</param>
        /// <returns>
        /// Returns true if the primary access token belongs to a user account that is a member of the local Administrators group. Returns false
        /// if the token does not.
        /// </returns>
        public static bool IsTokenLocalAdmin(in SafeAccessToken hToken)
        {
            try
            {
                if (!GetLinkedElevatedToken(in hToken, out SafeAccessToken hLinkedToken))
                    return false;

                if (hLinkedToken.IsInvalid)
                    return false;

                // Check if the token to be checked contains local admin SID.
                using (hLinkedToken)
                {
                    if (!TryGetWindowsIdentity(hLinkedToken, out _, out WindowsPrincipal? userPrincipal))
                    {
                        return false;
                    }
                    
                    return userPrincipal!.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to determine if token is local admin: {ex.Message}", MessageType.Error, ex);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the linked token associated with a given access token, if available.
        /// </summary>
        /// <param name="token">The token to retrieve the linked token for.</param>
        /// <returns>A <see cref="SafeAccessToken"/> representing the linked token, or null if no linked token is available.</returns>
        /// <exception cref="Win32Exception">Thrown if an error occurs while retrieving the linked token.</exception>
        public static bool GetLinkedToken(SafeAccessToken token, out SafeAccessToken hLinkedToken)
        {
            hLinkedToken = SafeAccessToken.Invalid;

            try
            {
                TOKEN_LINKED_TOKEN tokenLinkedToken = GetTokenInformation<TOKEN_LINKED_TOKEN>(token, TOKEN_INFORMATION_CLASS.TokenLinkedToken);

                hLinkedToken = new SafeAccessToken(tokenLinkedToken.LinkedToken);
                return true;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == NativeMethods.ERROR_NO_SUCH_LOGON_SESSION ||
                                            ex.NativeErrorCode == NativeMethods.ERROR_NOT_FOUND)
            {
                // These error codes indicate that there's no linked token, which is not necessarily an error
                ConsoleHelper.DebugWrite($"No linked token found. Error code [{ex.NativeErrorCode}].", MessageType.Info);
                return false;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to get linked token: {ex.Message}", MessageType.Error, ex);
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
        /// <exception cref="Win32Exception">Thrown if the environment block creation fails.</exception>
        public static SafeEnvironmentBlock CreateEnvironmentBlock(SafeAccessToken token, IDictionary<string, string>? additionalVariables, bool inherit)
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

                ConsoleHelper.DebugWrite("Environment block created successfully.", MessageType.Debug);
                return envBlock;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to create environment block.", MessageType.Error, ex);
                throw;
            }
        }

        /// <summary>
        /// Converts an environment block to a dictionary of key-value pairs.
        /// </summary>
        /// <param name="pEnvBlock">A pointer to the environment block.</param>
        /// <returns>A dictionary containing the environment variables and their values.</returns>
        public static Dictionary<string, string> ConvertEnvironmentBlockToDictionary(SafeEnvironmentBlock pEnvBlock)
        {
            var result = new Dictionary<string, string>();
            var offset = 0;

            try
            {
                while (true)
                {
                    string entry = Marshal.PtrToStringUni(pEnvBlock.DangerousGetHandle(), offset);
                    if (string.IsNullOrEmpty(entry)) break;

                    int equalsIndex = entry.IndexOf('=');
                    if (equalsIndex > 0)
                    {
                        string key = entry.Substring(0, equalsIndex);
                        string value = entry.Substring(equalsIndex + 1);
                        result[key] = value;
                    }

                    offset += (entry.Length + 1) * 2;
                }

                ConsoleHelper.DebugWrite($"Converted environment block to dictionary with [{result.Count}] entries.", MessageType.Debug);
                return result;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to convert environment block to dictionary.", MessageType.Error, ex);
                throw;
            }
        }

        /// <summary>
        /// Creates an environment block from a dictionary of key-value pairs.
        /// </summary>
        /// <param name="environmentVars">The dictionary containing environment variables and their values.</param>
        /// <returns>A <see cref="SafeEnvironmentBlock"/> containing the created environment block.</returns>
        public static SafeEnvironmentBlock CreateEnvironmentBlockFromDictionary(Dictionary<string, string> environmentVars)
        {
            try
            {
                var environmentString = string.Join("\0", environmentVars.Select(kvp => $"{kvp.Key}={kvp.Value}")) + "\0\0";
                var environmentBytes = System.Text.Encoding.Unicode.GetBytes(environmentString);
                var envBlockPtr = Marshal.AllocHGlobal(environmentBytes.Length);
                Marshal.Copy(environmentBytes, 0, envBlockPtr, environmentBytes.Length);

                ConsoleHelper.DebugWrite($"Created environment block from dictionary with [{environmentVars.Count}] entries.", MessageType.Debug);
                return new SafeEnvironmentBlock(envBlockPtr);
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to create environment block from dictionary.", MessageType.Error, ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the ID of the current session.
        /// </summary>
        /// <returns>The ID of the current session.</returns>
        public static uint GetCurrentProcessSessionId()
        {
            if (!NativeMethods.ProcessIdToSessionId(NativeMethods.GetCurrentProcessId(), out uint sessionId))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return sessionId;
        }
    }
}
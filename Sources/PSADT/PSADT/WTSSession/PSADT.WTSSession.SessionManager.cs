using System;
using System.Net;
using System.Linq;
using PSADT.PInvoke;
using PSADT.AccessToken;
using PSADT.OperatingSystem;
using System.ComponentModel;
using System.Security.Principal;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using PSADT.Logging;

namespace PSADT.WTSSession
{
    /// <summary>
    /// Provides utility methods for working with enumerated sessions.
    /// </summary>
    public static class SessionManager
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
                    UnifiedLogger.Create().Message("Using local server for WTS operations.").Severity(LogLevel.Debug);
                }
                else
                {
                    handle = NativeMethods.WTSOpenServer(hServerName!);
                    if (handle == IntPtr.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), $"'WTSOpenServer' failed to open a handle to hServerName [{hServerName}].");
                    }
                    UnifiedLogger.Create().Message($"Opened WTS server handle for [{hServerName}].").Severity(LogLevel.Debug);
                }

                return new SafeWTSServer(handle);
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get server for WTS operations.").Error(ex);
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

                UnifiedLogger.Create().Message($"Enumerated [{sessionCount}] WTS sessions.").Severity(LogLevel.Debug);
                return ppSessionInfo.Length > 0;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to enumerate WTS sessions.").Error(ex);
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

                UnifiedLogger.Create().Message($"WinStation information queried for session id [{sessionId}].").Severity(LogLevel.Debug);
                return wsInfo;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to query WinStation information for session id [{sessionId}].").Error(ex);
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
            bool isLocalServer = string.IsNullOrWhiteSpace(hServerName);

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
                        TokenManager.GetSecurityIdentificationTokenForSessionId(session.SessionId, out SafeAccessToken userImpersonationToken);
                        using (userImpersonationToken)
                        {
                            TokenManager.CreatePrimaryToken(userImpersonationToken, out SafeAccessToken userPrimaryToken);
                            using (userPrimaryToken)
                            {
                                isLocalAdminUserSession = TokenManager.IsTokenLocalAdmin(in userPrimaryToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UnifiedLogger.Create().Message($"Failed to determine if the token belongs to a local admin.").Error(ex);
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

                if (!string.IsNullOrWhiteSpace(domainName) && !string.IsNullOrWhiteSpace(userName))
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

                UnifiedLogger.Create().Message($"Retrieved extended session information for session id [{sessionId}].").Severity(LogLevel.Debug);
                return sessionInfo;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Error in GetExtendedSessionInfo: {ex.Message}").Error(ex);
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

            UnifiedLogger.Create().Message($"Domain: {domainName}, Username: {userName}.").Severity(LogLevel.Debug);

            if (string.IsNullOrWhiteSpace(domainName) || string.IsNullOrWhiteSpace(userName))
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
                    UnifiedLogger.Create().Message($"Discovered [{allActiveUserSessions.Count}] active user sessions.").Severity(LogLevel.Debug);
                }
                else
                {
                    UnifiedLogger.Create().Message("No active user sessions found.").Severity(LogLevel.Warning);
                }

                return allActiveUserSessions;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get all active user sessions.").Error(ex);
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
                    UnifiedLogger.Create().Message($"Discoverd session for session id [{sessionId}].").Severity(LogLevel.Debug);
                }
                else
                {
                    UnifiedLogger.Create().Message($"No session found for session id [{sessionId}].").Severity(LogLevel.Warning);
                }

                return session;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get session for session id [{sessionId}].").Error(ex);
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
                    UnifiedLogger.Create().Message($"Primary active user session found with session id [{primaryActiveUserSession.SessionId}].").Severity(LogLevel.Debug);
                }
                else
                {
                    UnifiedLogger.Create().Message("No primary active user session found.").Severity(LogLevel.Warning);
                }

                return primaryActiveUserSession;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get primary active user session.").Error(ex);
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
                    UnifiedLogger.Create().Message($"Primary active user session id [{session.SessionId}].").Severity(LogLevel.Debug);
                    return session.SessionId;
                }
                UnifiedLogger.Create().Message("No primary active user session found.").Severity(LogLevel.Warning);
                return null;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get primary active user session id.").Error(ex);
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
                    UnifiedLogger.Create().Message("No sessions found.").Severity(LogLevel.Warning);
                    return null;
                }

                SessionInfo? primaryActiveLocalAdminUserSession = sessions
                    .Where(x => x.IsPrimaryActiveLocalAdminUserSession)
                    .FirstOrDefault();

                if (primaryActiveLocalAdminUserSession != null)
                {
                    UnifiedLogger.Create().Message($"Primary active local admin user session found with session id [{primaryActiveLocalAdminUserSession.SessionId}].").Severity(LogLevel.Debug);
                }
                else
                {
                    UnifiedLogger.Create().Message("No primary active local admin user session found.").Severity(LogLevel.Warning);
                }

                return primaryActiveLocalAdminUserSession;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to find a primary active loacl admin user session.").Error(ex);
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
                    UnifiedLogger.Create().Message($"Discovered primary active local admin user session id [{session.SessionId}].").Severity(LogLevel.Debug);
                    return session.SessionId;
                }

                UnifiedLogger.Create().Message("No primary active local admin user session found.").Severity(LogLevel.Warning);
                return null;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Failed to get primary active local admin user session id.").Error(ex);
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
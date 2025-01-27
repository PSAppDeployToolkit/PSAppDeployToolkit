using System;
using System.Net;
using System.Linq;
using System.ComponentModel;
using System.Security.Principal;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using PSADT.Logging;
using PSADT.Account;
using PSADT.PInvoke;
using PSADT.OperatingSystem;

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
        /// Thrown when the underlying call to <see cref="NativeMethods"/> WTSEnumerateSessions() method fails.
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

        public static int GetBufferSize<T>()
        {
            int bufferSize = 0;

            if (typeof(T) == typeof(string))
            {
                // Strings are handled differently; buffer size is managed by the API
                // You may need to handle strings separately in your application
                bufferSize = 0;
            }
            else if (typeof(T) == typeof(bool) || typeof(T).IsPrimitive)
            {
                bufferSize = Marshal.SizeOf(typeof(T));
            }
            else if (typeof(T).IsEnum)
            {
                // Use the underlying type for the enum
                Type enumUnderlyingType = Enum.GetUnderlyingType(typeof(T));
                bufferSize = Marshal.SizeOf(enumUnderlyingType);
            }
            else if (typeof(T).IsValueType || typeof(T).IsLayoutSequential || typeof(T).IsExplicitLayout)
            {
                bufferSize = Marshal.SizeOf(typeof(T));
            }
            else
            {
                throw new InvalidOperationException($"Type '{typeof(T)}' is not supported for buffer size calculation.");
            }

            return bufferSize;
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
        /// Thrown when the underlying call to <see cref="NativeMethods"/> WTSQuerySessionInformation() method fails.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the memory tokenInfo returned is invalid or the handle is closed.
        /// </exception>
        private static T? GetWTSInfoClassProperty<T>(SafeWTSServer hServer, uint sessionId, WTS_INFO_CLASS wtsInfoClass)
        {
            try
            {
                if (!NativeMethods.WTSQuerySessionInformation(hServer, sessionId, wtsInfoClass, out SafeWtsMemory? ppBuffer, out uint pBytesReturned))
                {
                    int error = Marshal.GetLastWin32Error();
                    UnifiedLogger.Create().Message($"WTSQuerySessionInformation failed with error code: {error} for session {sessionId}, info class {wtsInfoClass}").Severity(LogLevel.Debug);
                    return default;
                }

                using (ppBuffer)
                {
                    if (typeof(T) == typeof(string))
                    {
                        string? result = Marshal.PtrToStringUni(ppBuffer.DangerousGetHandle(), (int)pBytesReturned / sizeof(char));

                        return (T?)(object?)result;
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        bool result = Marshal.ReadInt32(ppBuffer.DangerousGetHandle()) != 0;

                        return (T?)(object)result;
                    }
                    else if (typeof(T).IsEnum)
                    {
                        Type enumUnderlyingType = Enum.GetUnderlyingType(typeof(T));
                        object? value = null;

                        if (enumUnderlyingType == typeof(int))
                        {
                            int intValue = Marshal.ReadInt32(ppBuffer.DangerousGetHandle());
                            value = Enum.ToObject(typeof(T), intValue);
                        }
                        else if (enumUnderlyingType == typeof(uint))
                        {
                            uint uintValue = (uint)Marshal.ReadInt32(ppBuffer.DangerousGetHandle());
                            value = Enum.ToObject(typeof(T), uintValue);
                        }
                        else if (enumUnderlyingType == typeof(short))
                        {
                            short shortValue = Marshal.ReadInt16(ppBuffer.DangerousGetHandle());
                            value = Enum.ToObject(typeof(T), shortValue);
                        }
                        else if (enumUnderlyingType == typeof(ushort))
                        {
                            ushort ushortValue = (ushort)Marshal.ReadInt16(ppBuffer.DangerousGetHandle());
                            value = Enum.ToObject(typeof(T), ushortValue);
                        }
                        else if (enumUnderlyingType == typeof(byte))
                        {
                            byte byteValue = Marshal.ReadByte(ppBuffer.DangerousGetHandle());
                            value = Enum.ToObject(typeof(T), byteValue);
                        }
                        else if (enumUnderlyingType == typeof(sbyte))
                        {
                            sbyte sbyteValue = (sbyte)Marshal.ReadByte(ppBuffer.DangerousGetHandle());
                            value = Enum.ToObject(typeof(T), sbyteValue);
                        }
                        else
                        {
                            return default;
                        }

                        return (T?)value;
                    }
                    else
                    {
                        // For structs and other value types
                        object? result = Marshal.PtrToStructure(ppBuffer.DangerousGetHandle(), typeof(T));
                        if (result == null)
                        {
                            return default;
                        }

                        return (T?)result;
                    }
                }
            }
            catch
            {
                return default;
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
        /// This method calls the native <see cref="NativeMethods"/> WinStationQueryInformation() function to retrieve session information.
        /// The information is returned in a managed <see cref="WINSTATIONINFORMATIONW"/> structure.
        /// </remarks>
        /// <exception cref="Win32Exception">
        /// Thrown when the underlying call to <see cref="NativeMethods"/> WinStationQueryInformation() fails.
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

                if (OSVersionInfo.Current.IsWorkstation && OSHelper.GetIsWindows7OrGreater(OSVersionInfo.Current.OperatingSystem) &&
                    OSVersionInfo.Current.IsServer && OSHelper.GetIsWindowsServer2012OrGreater(OSVersionInfo.Current.OperatingSystem) &&
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
                        isLocalAdminUserSession = AccountUtilities.IsUserInBuiltInAdministratorsGroup(GetWtsUsernameAndDomainById(session.SessionId));
                    }
                    catch (Exception ex)
                    {
                        UnifiedLogger.Create().Message($"Failed to determine if the token belongs to a local admin.").Error(ex);
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
            using var hServer = GetWTSServer(hServerName);

            var sessionInfo = new ExtendedSessionInfo
            {
                // Default to -3 because 0, -1, and -2 are reserved session IDs
                SessionId = -3
            };

            // Basic session information
            sessionInfo.SessionId = GetWTSInfoClassProperty<int>(hServer, sessionId, WTS_INFO_CLASS.WTSSessionId);
            sessionInfo.SessionName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSWinStationName)?.TrimEnd('\0')?.ToUpperInvariant() ?? string.Empty;
            sessionInfo.DomainName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSDomainName)?.TrimEnd('\0') ?? string.Empty;
            sessionInfo.UserName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSUserName)?.TrimEnd('\0') ?? string.Empty;

            // Handle user identity information
            if (!string.IsNullOrWhiteSpace(sessionInfo.UserName))
            {
                try
                {
                    sessionInfo.NTAccount = new NTAccount($@"{sessionInfo.DomainName}\{sessionInfo.UserName}");
                    sessionInfo.Sid = (SecurityIdentifier)sessionInfo.NTAccount.Translate(typeof(SecurityIdentifier));
                }
                catch (Exception ex)
                {
                    UnifiedLogger.Create().Message($"Error creating NTAccount or SecurityIdentifier: {ex.Message}").Error(ex);
                }
            }
            else
            {
                sessionInfo.Sid = new SecurityIdentifier(WellKnownSidType.NullSid, null);
            }

            // Connection state and client information
            sessionInfo.ConnectionState = GetWTSInfoClassProperty<WTS_CONNECTSTATE_CLASS>(hServer, sessionId, WTS_INFO_CLASS.WTSConnectState).ToString();
            sessionInfo.ClientBuildNumber = GetWTSInfoClassProperty<int>(hServer, sessionId, WTS_INFO_CLASS.WTSClientBuildNumber);
            sessionInfo.ClientComputerName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSClientName) ?? string.Empty;
            sessionInfo.ClientDirectory = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSClientDirectory) ?? string.Empty;
            sessionInfo.ClientProtocolType = GetWTSInfoClassProperty<WTS_CLIENT_PROTOCOL_TYPE>(hServer, sessionId, WTS_INFO_CLASS.WTSClientProtocolType).ToString();

            // Client address information
            try
            {
                WTS_CLIENT_ADDRESS clientAddress = GetWTSInfoClassProperty<WTS_CLIENT_ADDRESS>(hServer, sessionId, WTS_INFO_CLASS.WTSClientAddress);
                sessionInfo.ClientIPAddress = GetWtsIPAddress(clientAddress);
                sessionInfo.ClientIPAddressFamily = clientAddress.AddressFamily.ToString();

                try
                {
                    WTS_SESSION_ADDRESS sessionAddress = GetWTSInfoClassProperty<WTS_SESSION_ADDRESS>(hServer, sessionId, WTS_INFO_CLASS.WTSSessionAddressV4);
                    if (!sessionAddress.Equals(default(WTS_SESSION_ADDRESS)))
                    {
                        sessionInfo.SessionIPAddress = GetWtsIPAddress(sessionAddress);
                    }
                }
                catch (Win32Exception ex) when (ex.NativeErrorCode == 1722) // RPC server unavailable
                {
                    UnifiedLogger.Create().Message("Session address information unavailable").Error(ex).Severity(LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to retrieve address information").Error(ex).Severity(LogLevel.Warning);
            }

            // Display information
            try
            {
                var clientDisplay = GetWTSInfoClassProperty<WTS_CLIENT_DISPLAY>(hServer, sessionId, WTS_INFO_CLASS.WTSClientDisplay);
                sessionInfo.HorizontalResolution = clientDisplay.HorizontalResolution;
                sessionInfo.VerticalResolution = clientDisplay.VerticalResolution;
                sessionInfo.ColorDepth = clientDisplay.ColorDepth;
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to retrieve display information").Error(ex).Severity(LogLevel.Warning);
            }

            // Remote session status
            try
            {
                if (((OSVersionInfo.Current.IsWorkstation && OSHelper.GetIsWindows7OrGreater(OSVersionInfo.Current.OperatingSystem)) ||
                (OSVersionInfo.Current.IsServer && OSHelper.GetIsWindowsServer2012OrGreater(OSVersionInfo.Current.OperatingSystem))) &&
                hServer.IsLocalServer)
                {
                    sessionInfo.IsRemoteSession = GetWTSInfoClassProperty<bool>(hServer, sessionId, WTS_INFO_CLASS.WTSIsRemoteSession);
                }
                //else
                //{
                //    if (NativeMethods.GetSystemMetrics(SystemMetric.SM_REMOTESESSION) != 0)
                //    {
                //        sessionInfo.IsRemoteSession = true;
                //    }
                //}
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to determine remote session status").Error(ex).Severity(LogLevel.Warning);
            }

            // Time-related information
            try
            {
                if (OSHelper.GetIsWindowsVistaSP1OrGreater(OSVersionInfo.Current.OperatingSystem))
                {
                    try
                    {
                        WTSINFO wtsInfo = GetWTSInfoClassProperty<WTSINFO>(hServer, sessionId, WTS_INFO_CLASS.WTSSessionInfo);

                        UpdateSessionDateTimeInfo(
                            wtsInfo.LogonTimeUTC,
                            wtsInfo.LogonTime,
                            wtsInfo.DisconnectTimeUTC,
                            wtsInfo.DisconnectTime,
                            wtsInfo.CurrentTimeUTC,
                            wtsInfo.CurrentTime,
                            wtsInfo.LastInputTimeUTC,
                            wtsInfo.LastInputTime,
                            sessionInfo);
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == 1722) // RPC server unavailable
                    {
                        UnifiedLogger.Create().Message("Unable to retrieve session time information through WTS API").Error(ex).Severity(LogLevel.Warning);
                    }
                    catch (Exception ex)
                    {
                        UnifiedLogger.Create().Message("Failed to retrieve session time information through WTS API").Error(ex).Severity(LogLevel.Warning);
                    }

                    if (sessionInfo.LogonTimeUtc == null || sessionInfo.LogonTimeUtc == DateTime.MinValue)
                    {
                        WINSTATIONINFORMATIONW winStationInfo = WinStationQueryInformation(hServer, sessionId);

                        UpdateSessionDateTimeInfo(
                            winStationInfo.LogonTimeUTC,
                            winStationInfo.LogonTime,
                            winStationInfo.DisconnectTimeUTC,
                            winStationInfo.DisconnectTime,
                            winStationInfo.CurrentTimeUTC,
                            winStationInfo.CurrentTime,
                            winStationInfo.LastInputTimeUTC,
                            winStationInfo.LastInputTime,
                            sessionInfo);
                    }
                }
                else
                {
                    WINSTATIONINFORMATIONW winStationInfo = WinStationQueryInformation(hServer, sessionId);

                    UpdateSessionDateTimeInfo(
                        winStationInfo.LogonTimeUTC,
                        winStationInfo.LogonTime,
                        winStationInfo.DisconnectTimeUTC,
                        winStationInfo.DisconnectTime,
                        winStationInfo.CurrentTimeUTC,
                        winStationInfo.CurrentTime,
                        winStationInfo.LastInputTimeUTC,
                        winStationInfo.LastInputTime,
                        sessionInfo);
                }
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message("Failed to retrieve all session time information").Error(ex).Severity(LogLevel.Warning);
            }

            return sessionInfo;
        }

        public static ReadOnlyCollection<CompatibilitySessionInfo> GetCompatibilitySessionInfo()
        {
            // Open collector for all compatibility session objects.
            List<CompatibilitySessionInfo> compatibilitySessionInfos = [];

            // Get all current sessions.
            if (GetAllActiveUserSessions() is List<SessionInfo> activeUsers)
            {
                foreach (SessionInfo sessionInfo in activeUsers)
                {
                    // Get extended session information.
                    ExtendedSessionInfo extendedSessionInfo = GetExtendedSessionInfo(sessionInfo.SessionId);

                    // Create a new CompatibilitySessionInfo object.
                    compatibilitySessionInfos.Add(new CompatibilitySessionInfo(
                        extendedSessionInfo.NTAccount?.ToString(),
                        extendedSessionInfo.Sid?.ToString(),
                        extendedSessionInfo.UserName,
                        extendedSessionInfo.DomainName,
                        extendedSessionInfo.SessionId,
                        extendedSessionInfo.SessionName,
                        extendedSessionInfo.ConnectionState,
                        sessionInfo.IsActiveSession,
                        sessionInfo.IsConsoleSession,
                        sessionInfo.IsActiveUserSession,
                        sessionInfo.IsUserSession,
                        sessionInfo.IsRemoteSession,
                        sessionInfo.IsLocalAdminUserSession,
                        extendedSessionInfo.LogonTimeLocal,
                        extendedSessionInfo.IdleTime ?? TimeSpan.Zero,
                        extendedSessionInfo.DisconnectTimeLocal,
                        extendedSessionInfo.ClientComputerName,
                        extendedSessionInfo.ClientProtocolType,
                        extendedSessionInfo.ClientDirectory,
                        extendedSessionInfo.ClientBuildNumber));
                }
            }

            // Return the accumulated compatibility session objects as a read-only collection.
            return compatibilitySessionInfos.AsReadOnly();
        }

        public static string GetWtsUsernameById(uint sessionId, string? hServerName = "")
        {
            string userName;

            using (var hServer = GetWTSServer(hServerName))
            {
                userName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSUserName)?.TrimEnd('\0') ?? string.Empty;
            }

            UnifiedLogger.Create().Message($"Username: {userName}.").Severity(LogLevel.Debug);

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new InvalidOperationException($"Failed to retrieve a valid username for session id [{sessionId}].");
            }

            return userName;
        }

        public static string GetWtsUsernameAndDomainById(uint sessionId, string? hServerName = "")
        {
            string domainName;
            string userName;

            using (var hServer = GetWTSServer(hServerName))
            {
                domainName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSDomainName)?.TrimEnd('\0')?.ToUpperInvariant() ?? string.Empty;
                userName = GetWTSInfoClassProperty<string>(hServer, sessionId, WTS_INFO_CLASS.WTSUserName)?.TrimEnd('\0') ?? string.Empty;
            }

            UnifiedLogger.Create().Message($"Domain: {domainName}, Username: {userName}.").Severity(LogLevel.Debug);

            if (string.IsNullOrWhiteSpace(domainName) || string.IsNullOrWhiteSpace(userName))
            {
                throw new InvalidOperationException($"Failed to retrieve a valid domain or username for session id [{sessionId}].");
            }

            return $@"{domainName}\{userName}";
        }

        /// <summary>
        /// Retrieves the IP address from the provided WTS_CLIENT_ADDRESS or WTS_SESSION_ADDRESS structure.
        /// </summary>
        /// <typeparam name="T">The type of the address structure (WTS_CLIENT_ADDRESS or WTS_SESSION_ADDRESS).</typeparam>
        /// <param name="addressStruct">The address structure.</param>
        /// <returns>The extracted IPAddress if successful; otherwise, IPAddress.None.</returns>
        public static IPAddress? GetWtsIPAddress<T>(T addressStruct) where T : struct
        {
            IPAddress? parsedAddress = null;
            ADDRESS_FAMILY addressFamily;
            byte[]? sourceIpBytes;

            // Determine the type of the address structure and extract data accordingly
            switch (addressStruct)
            {
                case WTS_CLIENT_ADDRESS clientAddress:
                    addressFamily = clientAddress.AddressFamily;
                    sourceIpBytes = clientAddress.Address;
                    break;

                case WTS_SESSION_ADDRESS sessionAddress:
                    addressFamily = sessionAddress.AddressFamily;
                    sourceIpBytes = sessionAddress.Address;
                    break;

                default:
                    return parsedAddress;
            }

            if (sourceIpBytes == null || sourceIpBytes.Length == 0)
            {
                return parsedAddress;
            }

            switch (addressFamily)
            {
                case ADDRESS_FAMILY.AF_INET:
                    return IPAddress.Parse(string.Join(".", sourceIpBytes.Skip(2).Take(4)));
                case ADDRESS_FAMILY.AF_INET6:
                    // TODO: The current API call does not support IPv6 addresses.
                    return null;
                default:
                    // Unsupported address family
                    break;
            }

            return parsedAddress;
        }

        private static void UpdateSessionDateTimeInfo(
            long logonTimeUTC,
            DateTime logonTime,
            long disconnectTimeUTC,
            DateTime disconnectTime,
            long currentTimeUTC,
            DateTime currentTime,
            long lastInputTimeUTC,
            DateTime lastInputTime,
            ExtendedSessionInfo sessionInfo)
        {
            if (logonTimeUTC != 0)
            {
                sessionInfo.LogonTimeUtc = logonTime;
                sessionInfo.LogonTimeLocal = DateTime.FromFileTime(logonTimeUTC);
            }

            if (currentTimeUTC != 0 && lastInputTimeUTC != 0)
            {
                sessionInfo.IdleTime = currentTime - lastInputTime;
            }

            if (disconnectTimeUTC != 0)
            {
                sessionInfo.DisconnectTimeUtc = disconnectTime;
                sessionInfo.DisconnectTimeLocal = DateTime.FromFileTime(disconnectTimeUTC);
            }
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
            // Determine the primary active user session.
            // If an active console user exists, then that will be the active user session. In some scenarios, there can be an active console and non-console session. Since admins log into the console session, we give it preference.
            // If no active console user exists but users are logged in, such as on terminal servers or VDIs, then the first logged-in non-console user that is 'Active' is the active user.

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
            // Determine the active user session ID.
            // If an active console user exists, then that will be the active user session. In some scenarios, there can be an active console and non-console session. Since admins log into the console session, we give it preference.
            // If no active console user exists but users are logged in, such as on terminal servers or VDIs, then the first logged-in non-console user that is 'Active' is the active user.

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
            // Determine the primary active local admin user session.
            // If an active, local admin, console user exists, then that will be the primary session. In some scenarios, there can be an active console and non-console session. Since admins log into the console session, we give it preference.
            // If no active, local admin, console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user that is 'Active' and is a local admin is the primary session.

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
            // Determine the primary active local admin user session.
            // If an active, local admin, console user exists, then that will be the primary session. In some scenarios, there can be an active console and non-console session. Since admins log into the console session, we give it preference.
            // If no active, local admin, console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user that is 'Active' and is a local admin is the primary session.

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

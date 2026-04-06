using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
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
    /// A class to hold all information for a given WTS session.
    /// </summary>
    public sealed record SessionInfo
    {
        /// <summary>
        /// Retrieves a read-only collection containing information about all active sessions on the current server.
        /// </summary>
        /// <remarks>This method enumerates all sessions available on the current server and returns their
        /// associated information. The returned collection is immutable and reflects the state of sessions at the time
        /// of the call. Subsequent changes to session state are not reflected in the returned list.</remarks>
        /// <returns>A read-only list of <see cref="SessionInfo"/> objects, each representing the details of an active session.
        /// The list is empty if no active sessions are found.</returns>
        public static IReadOnlyList<SessionInfo> Get()
        {
            // Enumerate the sessions process each session in the returned buffer.
            _ = NativeMethods.WTSEnumerateSessionsEx(out SafeWtsExHandle pSessionInfo);
            using (pSessionInfo)
            {
                ReadOnlySpan<byte> pSessionInfoSpan = pSessionInfo.AsReadOnlySpan<byte>();
                int objLength = Marshal.SizeOf<WTS_SESSION_INFO_1W>();
                int objCount = pSessionInfo.Length / objLength;
                List<SessionInfo> sessions = new(objCount);
                for (int i = 0; i < objCount; i++)
                {
                    ref readonly WTS_SESSION_INFO_1W sessionInfo = ref pSessionInfoSpan.Slice(objLength * i).AsReadOnlyStructure<WTS_SESSION_INFO_1W>();
                    if (Get(in sessionInfo) is SessionInfo session)
                    {
                        sessions.Add(session);
                    }
                }
                return sessions.AsReadOnly();
            }
        }

        /// <summary>
        /// Retrieves information about a Terminal Services session with the specified session identifier.
        /// </summary>
        /// <param name="sessionId">The identifier of the session to retrieve information for.</param>
        /// <returns>A <see cref="SessionInfo"/> object containing details about the session if found; otherwise, <see
        /// langword="null"/>.</returns>
        public static SessionInfo? Get(uint sessionId)
        {
            // Enumerate the sessions process each session in the returned buffer.
            _ = NativeMethods.WTSEnumerateSessionsEx(out SafeWtsExHandle pSessionInfo);
            using (pSessionInfo)
            {
                ReadOnlySpan<byte> pSessionInfoSpan = pSessionInfo.AsReadOnlySpan<byte>();
                int objLength = Marshal.SizeOf<WTS_SESSION_INFO_1W>();
                int objCount = pSessionInfo.Length / objLength;
                for (int i = 0; i < objCount; i++)
                {
                    ref readonly WTS_SESSION_INFO_1W sessionInfo = ref pSessionInfoSpan.Slice(objLength * i).AsReadOnlyStructure<WTS_SESSION_INFO_1W>();
                    if (sessionInfo.SessionId == sessionId && Get(in sessionInfo) is SessionInfo session)
                    {
                        return session;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Retrieves detailed information about a Windows Terminal Services session based on the provided session
        /// structure.
        /// </summary>
        /// <remarks>This method queries various session attributes, including user identity, session
        /// state, client protocol, and idle time. Administrative privileges may be required to retrieve certain
        /// information, such as idle time for sessions other than the current user. If the session does not represent a
        /// valid user, the method returns null.</remarks>
        /// <param name="session">A reference to a WTS_SESSION_INFO_1W structure containing information about the session to query.</param>
        /// <returns>A SessionInfo object containing user, session, and client details if the session is valid; otherwise, null.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a required process to retrieve idle time information cannot be launched.</exception>
        private static SessionInfo? Get(in WTS_SESSION_INFO_1W session)
        {
            // Internal helpers for retrieving session information values.
            static string? GetString(uint sessionId, WTS_INFO_CLASS infoClass)
            {
                _ = NativeMethods.WTSQuerySessionInformation(sessionId, infoClass, out SafeWtsHandle pBuffer);
                using (pBuffer)
                {
                    return pBuffer.ReadNullTerminatedString();
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
                        using ProcessResult result = ProcessManager.LaunchAsync(args)?.Task.GetAwaiter().GetResult() ?? throw new InvalidOperationException("Failed to launch process to get idle time.");
                        idleTime = new(long.Parse(result.StdOut[0], CultureInfo.InvariantCulture));
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
                session.State,
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

        /// <summary>
        /// Initializes a new instance of the SessionInfo class with details about a user session, including user
        /// identity, session state, client information, and administrative status.
        /// </summary>
        /// <param name="ntAccount">The NTAccount representing the user associated with the session. Cannot be null.</param>
        /// <param name="sid">The SecurityIdentifier (SID) for the user associated with the session. Cannot be null.</param>
        /// <param name="userName">The user name for the session. Cannot be null or empty.</param>
        /// <param name="domainName">The domain name for the user. Cannot be null or empty.</param>
        /// <param name="sessionId">The unique identifier for the session. Must be greater than zero.</param>
        /// <param name="sessionName">The name of the session, or null if not specified.</param>
        /// <param name="connectState">The current connection state of the session, as defined by the WTS_CONNECTSTATE_CLASS enumeration.</param>
        /// <param name="isCurrentSession">true if this session is the current session; otherwise, false.</param>
        /// <param name="isConsoleSession">true if this session is the console session; otherwise, false.</param>
        /// <param name="isActiveUserSession">true if this session is an active user session; otherwise, false.</param>
        /// <param name="isValidUserSession">true if this session is considered a valid user session; otherwise, false.</param>
        /// <param name="isUserSession">true if this session is a user session; otherwise, false.</param>
        /// <param name="isRdpSession">true if this session is a Remote Desktop Protocol (RDP) session; otherwise, false.</param>
        /// <param name="isLocalAdmin">true if the user is a local administrator; otherwise, false. Can be null if the status is unknown.</param>
        /// <param name="logonTime">The date and time when the user logged on to the session.</param>
        /// <param name="idleTime">The duration for which the session has been idle, or null if not available.</param>
        /// <param name="disconnectTime">The date and time when the session was disconnected, or null if the session is currently connected.</param>
        /// <param name="clientName">The name of the client computer associated with the session, or null if not available.</param>
        /// <param name="clientProtocolType">The protocol type used by the client to connect to the session, as defined by the WTS_PROTOCOL_TYPE
        /// enumeration.</param>
        /// <param name="clientDirectory">The directory path of the client, or null if not available.</param>
        /// <param name="clientBuildNumber">The build number of the client, or null if not available.</param>
        /// <exception cref="ArgumentNullException">Thrown if ntAccount, sid, userName, or domainName is null, or if userName or domainName is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if sessionId is less than or equal to zero.</exception>
        private SessionInfo(
            NTAccount ntAccount,
            SecurityIdentifier sid,
            string userName,
            string domainName,
            uint sessionId,
            string? sessionName,
            Windows.Win32.System.RemoteDesktop.WTS_CONNECTSTATE_CLASS connectState,
            bool isCurrentSession,
            bool isConsoleSession,
            bool isActiveUserSession,
            bool isValidUserSession,
            bool isUserSession,
            bool isRdpSession,
            bool? isLocalAdmin,
            DateTime logonTime,
            TimeSpan? idleTime,
            DateTime? disconnectTime,
            string? clientName,
            WTS_PROTOCOL_TYPE clientProtocolType,
            string? clientDirectory,
            uint? clientBuildNumber)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ntAccount.Value, nameof(ntAccount));
            ArgumentException.ThrowIfNullOrWhiteSpace(userName);
            ArgumentException.ThrowIfNullOrWhiteSpace(domainName);
            if (sessionName is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(sessionName);
            }
            if (clientName is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
            }
            if (clientDirectory is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(clientDirectory);
                ClientDirectory = new(clientDirectory);
            }
            NTAccount = ntAccount;
            SID = sid;
            UserName = userName;
            DomainName = domainName;
            SessionId = sessionId;
            SessionName = sessionName;
            ConnectState = (Interop.WTS_CONNECTSTATE_CLASS)connectState;
            IsCurrentSession = isCurrentSession;
            IsConsoleSession = isConsoleSession;
            IsActiveUserSession = isActiveUserSession;
            IsValidUserSession = isValidUserSession;
            IsUserSession = isUserSession;
            IsRdpSession = isRdpSession;
            IsLocalAdmin = isLocalAdmin;
            LogonTime = logonTime;
            IdleTime = idleTime;
            DisconnectTime = disconnectTime;
            ClientName = clientName;
            ClientProtocolType = clientProtocolType;
            ClientBuildNumber = clientBuildNumber;
        }

        /// <summary>
        /// Converts the current instance to a <see cref="RunAsActiveUser"/> object.
        /// </summary>
        /// <returns>A new <see cref="RunAsActiveUser"/> instance initialized with the current object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RunAsActiveUser ToRunAsActiveUser()
        {
            return new(this);
        }

        /// <summary>
        /// The NTAccount for the session's user.
        /// </summary>
        public NTAccount NTAccount { get; }

        /// <summary>
        /// The SID for the session's user.
        /// </summary>
        public SecurityIdentifier SID { get; }

        /// <summary>
        /// The username for the session's user.
        /// </summary>
        public string UserName { get; }

        /// <summary>
        /// The domain name for the session's user.
        /// </summary>
        public string DomainName { get; }

        /// <summary>
        /// The Id of the session.
        /// </summary>
        public uint SessionId { get; }

        /// <summary>
        /// The session name for the session.
        /// </summary>
        public string? SessionName { get; }

        /// <summary>
        /// The connection state of the session.
        /// </summary>
        public Interop.WTS_CONNECTSTATE_CLASS ConnectState { get; }

        /// <summary>
        /// Whether the session is the current session of the caller.
        /// </summary>
        public bool IsCurrentSession { get; }

        /// <summary>
        /// Whether the session is a console session.
        /// </summary>
        public bool IsConsoleSession { get; }

        /// <summary>
        /// Whether the session is active or not.
        /// </summary>
        public bool IsActiveUserSession { get; }

        /// <summary>
        /// Whether the session's token can be used to create a process.
        /// </summary>
        public bool IsValidUserSession { get; }

        /// <summary>
        /// Whether the session is that of a user.
        /// </summary>
        public bool IsUserSession { get; }

        /// <summary>
        /// Whether the session is remote or local.
        /// </summary>
        public bool IsRdpSession { get; }

        /// <summary>
        /// Whether the user of the session is a local administrator.
        /// </summary>
        public bool? IsLocalAdmin { get; }

        /// <summary>
        /// The logon time of the session.
        /// </summary>
        public DateTime LogonTime { get; }

        /// <summary>
        /// How long the session has been idle for.
        /// </summary>
        public TimeSpan? IdleTime { get; }

        /// <summary>
        /// The last disconnection time of the session.
        /// </summary>
        public DateTime? DisconnectTime { get; }

        /// <summary>
        /// The name of the terminal server (workstation).
        /// </summary>
        public string? ClientName { get; }

        /// <summary>
        /// The protocol type of the session (console, RDP, etc).
        /// </summary>
        public WTS_PROTOCOL_TYPE ClientProtocolType { get; }

        /// <summary>
        /// The directory service providing the session.
        /// </summary>
        public FileInfo? ClientDirectory { get; }

        /// <summary>
        /// The Windows NT build number of the client.
        /// </summary>
        public uint? ClientBuildNumber { get; }

        /// <summary>
        /// Returns a string that provides a detailed, multi-line summary of the current user session, including
        /// account, session, and client information.
        /// </summary>
        /// <remarks>This method is useful for logging or debugging purposes, as it provides a
        /// comprehensive overview of the session's attributes in a human-readable format.</remarks>
        /// <returns>A formatted string containing the values of key session properties, each on a separate line. The string
        /// includes NTAccount, SID, UserName, DomainName, SessionId, SessionName, ConnectState, IsCurrentSession,
        /// IsConsoleSession, IsActiveUserSession, IsValidUserSession, IsUserSession, IsRdpSession, IsLocalAdmin,
        /// LogonTime, IdleTime, DisconnectTime, ClientName, ClientProtocolType, ClientDirectory, and ClientBuildNumber.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"NTAccount           : {NTAccount}{Environment.NewLine}SID                 : {SID}{Environment.NewLine}UserName            : {UserName}{Environment.NewLine}DomainName          : {DomainName}{Environment.NewLine}SessionId           : {SessionId}{Environment.NewLine}SessionName         : {SessionName}{Environment.NewLine}ConnectState        : {ConnectState}{Environment.NewLine}IsCurrentSession    : {IsCurrentSession}{Environment.NewLine}IsConsoleSession    : {IsConsoleSession}{Environment.NewLine}IsActiveUserSession : {IsActiveUserSession}{Environment.NewLine}IsValidUserSession  : {IsValidUserSession}{Environment.NewLine}IsUserSession       : {IsUserSession}{Environment.NewLine}IsRdpSession        : {IsRdpSession}{Environment.NewLine}IsLocalAdmin        : {IsLocalAdmin}{Environment.NewLine}LogonTime           : {LogonTime}{Environment.NewLine}IdleTime            : {IdleTime}{Environment.NewLine}DisconnectTime      : {DisconnectTime}{Environment.NewLine}ClientName          : {ClientName}{Environment.NewLine}ClientProtocolType  : {ClientProtocolType}{Environment.NewLine}ClientDirectory     : {ClientDirectory}{Environment.NewLine}ClientBuildNumber   : {ClientBuildNumber}";
        }
    }
}

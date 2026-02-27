using System;
using System.Security.Principal;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.Interop.Extensions;

namespace PSADT.TerminalServices
{
    /// <summary>
    /// A class to hold all information for a given WTS session.
    /// </summary>
    public sealed record SessionInfo
    {
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
        /// <param name="isLocalAdminException">The exception encountered when determining local administrator status, or null if no exception occurred.</param>
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
        internal SessionInfo(
            NTAccount ntAccount,
            SecurityIdentifier sid,
            string userName,
            string domainName,
            uint sessionId,
            string? sessionName,
            WTS_CONNECTSTATE_CLASS connectState,
            bool isCurrentSession,
            bool isConsoleSession,
            bool isActiveUserSession,
            bool isValidUserSession,
            bool isUserSession,
            bool isRdpSession,
            bool? isLocalAdmin,
            Exception? isLocalAdminException,
            DateTime logonTime,
            TimeSpan? idleTime,
            DateTime? disconnectTime,
            string? clientName,
            WTS_PROTOCOL_TYPE clientProtocolType,
            string? clientDirectory,
            uint? clientBuildNumber)
        {
            NTAccount = ntAccount ?? throw new ArgumentNullException(nameof(ntAccount), "NTAccount cannot be null.");
            SID = sid ?? throw new ArgumentNullException(nameof(sid), "SID cannot be null.");
            UserName = userName.ThrowIfNullOrWhiteSpace();
            DomainName = domainName.ThrowIfNullOrWhiteSpace();
            SessionId = sessionId > 0 ? sessionId : throw new ArgumentOutOfRangeException(nameof(sessionId), "SessionId must be greater than zero.");
            SessionName = !string.IsNullOrWhiteSpace(sessionName) ? sessionName : null;
            ConnectState = connectState;
            IsCurrentSession = isCurrentSession;
            IsConsoleSession = isConsoleSession;
            IsActiveUserSession = isActiveUserSession;
            IsValidUserSession = isValidUserSession;
            IsUserSession = isUserSession;
            IsRdpSession = isRdpSession;
            IsLocalAdmin = isLocalAdmin;
            IsLocalAdminException = isLocalAdminException;
            LogonTime = logonTime;
            IdleTime = idleTime;
            DisconnectTime = disconnectTime;
            ClientName = clientName;
            ClientProtocolType = clientProtocolType;
            ClientDirectory = clientDirectory;
            ClientBuildNumber = clientBuildNumber;
        }

        /// <summary>
        /// Converts the current instance to a <see cref="RunAsActiveUser"/> object.
        /// </summary>
        /// <returns>A new <see cref="RunAsActiveUser"/> instance initialized with the current object.</returns>
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
        public WTS_CONNECTSTATE_CLASS ConnectState { get; }

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
        /// Represents an exception that occurred while determining whether the current user is a local administrator.
        /// </summary>
        public Exception? IsLocalAdminException { get; }

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
        public string? ClientDirectory { get; }

        /// <summary>
        /// The Windows NT build number of the client.
        /// </summary>
        public uint? ClientBuildNumber { get; }
    }
}

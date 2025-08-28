using System;
using System.Security.Principal;
using PSADT.LibraryInterfaces;
using PSADT.Module;

namespace PSADT.TerminalServices
{
    /// <summary>
    /// A class to hold all information for a given WTS session.
    /// </summary>
    public sealed record SessionInfo
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="ntAccount"></param>
        /// <param name="sid"></param>
        /// <param name="userName"></param>
        /// <param name="domainName"></param>
        /// <param name="sessionId"></param>
        /// <param name="sessionName"></param>
        /// <param name="connectState"></param>
        /// <param name="isCurrentSession"></param>
        /// <param name="isConsoleSession"></param>
        /// <param name="isActiveUserSession"></param>
        /// <param name="isUserSession"></param>
        /// <param name="isRdpSession"></param>
        /// <param name="isLocalAdmin"></param>
        /// <param name="logonTime"></param>
        /// <param name="idleTime"></param>
        /// <param name="disconnectTime"></param>
        /// <param name="clientName"></param>
        /// <param name="clientProtocolType"></param>
        /// <param name="clientDirectory"></param>
        /// <param name="clientBuildNumber"></param>
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
            bool isUserSession,
            bool isRdpSession,
            bool? isLocalAdmin,
            Exception? isLocalAdminException,
            DateTime? logonTime,
            TimeSpan? idleTime,
            DateTime? disconnectTime,
            string? clientName,
            WTS_PROTOCOL_TYPE clientProtocolType,
            string? clientDirectory,
            uint? clientBuildNumber)
        {
            NTAccount = ntAccount ?? throw new ArgumentNullException("NTAccount cannot be null.", (Exception?)null);
            SID = sid ?? throw new ArgumentNullException("SID cannot be null.", (Exception?)null);
            UserName = !string.IsNullOrWhiteSpace(userName) ? userName : throw new ArgumentNullException("UserName cannot be null or empty.", (Exception?)null);
            DomainName = !string.IsNullOrWhiteSpace(domainName) ? domainName : throw new ArgumentNullException("DomainName cannot be null or empty.", (Exception?)null);
            SessionId = sessionId > 0 ? sessionId : throw new ArgumentOutOfRangeException("SessionId must be greater than zero.", (Exception?)null);
            SessionName = !string.IsNullOrWhiteSpace(sessionName) ? sessionName : null;
            ConnectState = connectState;
            IsCurrentSession = isCurrentSession;
            IsConsoleSession = isConsoleSession;
            IsActiveUserSession = isActiveUserSession;
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
        public readonly NTAccount NTAccount;

        /// <summary>
        /// The SID for the session's user.
        /// </summary>
        public readonly SecurityIdentifier SID;

        /// <summary>
        /// The username for the session's user.
        /// </summary>
        public readonly string UserName;

        /// <summary>
        /// The domain name for the session's user.
        /// </summary>
        public readonly string DomainName;

        /// <summary>
        /// The Id of the session.
        /// </summary>
        public readonly uint SessionId;

        /// <summary>
        /// The session name for the session.
        /// </summary>
        public readonly string? SessionName;

        /// <summary>
        /// The connection state of the session.
        /// </summary>
        public readonly WTS_CONNECTSTATE_CLASS ConnectState;

        /// <summary>
        /// Whether the session is the current session of the caller.
        /// </summary>
        public readonly bool IsCurrentSession;

        /// <summary>
        /// Whether the session is a console session.
        /// </summary>
        public readonly bool IsConsoleSession;

        /// <summary>
        /// Whether the session is active or not.
        /// </summary>
        public readonly bool IsActiveUserSession;

        /// <summary>
        /// Whether the session is that of a user.
        /// </summary>
        public readonly bool IsUserSession;

        /// <summary>
        /// Whether the session is remote or local.
        /// </summary>
        public readonly bool IsRdpSession;

        /// <summary>
        /// Whether the user of the session is a local administrator.
        /// </summary>
        public readonly bool? IsLocalAdmin;

        /// <summary>
        /// Represents an exception that occurred while determining whether the current user is a local administrator.
        /// </summary>
        public readonly Exception? IsLocalAdminException;

        /// <summary>
        /// The logon time of the session.
        /// </summary>
        public readonly DateTime? LogonTime;

        /// <summary>
        /// How long the session has been idle for.
        /// </summary>
        public readonly TimeSpan? IdleTime;

        /// <summary>
        /// The last disconnection time of the session.
        /// </summary>
        public readonly DateTime? DisconnectTime;

        /// <summary>
        /// The name of the terminal server (workstation).
        /// </summary>
        public readonly string? ClientName;

        /// <summary>
        /// The protocol type of the session (console, RDP, etc).
        /// </summary>
        public readonly WTS_PROTOCOL_TYPE ClientProtocolType;

        /// <summary>
        /// The directory service providing the session.
        /// </summary>
        public readonly string? ClientDirectory;

        /// <summary>
        /// The Windows NT build number of the client.
        /// </summary>
        public readonly uint? ClientBuildNumber;
    }
}

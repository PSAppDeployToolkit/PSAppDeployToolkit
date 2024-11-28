using System;

namespace PSADT.WTSSession
{
    /// <summary>
    /// Represents information about an enumerated session.
    /// </summary>
    public class SessionInfo
    {
        public uint SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public string ConnectionState { get; set; } = string.Empty;

        public bool? IsCurrentProcessSession { get; set; }
        public bool IsConsoleSession { get; set; }
        public bool IsActiveSession { get; set; }

        public bool IsConnectedSession { get; set; }
        public bool IsDisconnectedSession { get; set; }

        public bool IsRemoteSession { get; set; }
        public bool IsRdpSession { get; set; }
        public bool IsHdxSession { get; set; }
        public bool IsRemoteListenerSession { get; set; }

        public bool IsLocalSession { get; set; }

        public bool IsSystemSession { get; set; }
        public bool IsServicesSession { get; set; }
        public bool IsConnectedConsoleSession { get; set; }

        public bool IsUserSession { get; set; }
        public bool IsLocalAdminUserSession { get; set; }
        public bool IsActiveUserSession { get; set; }
        public bool IsConsoleActiveUserSession { get; set; }
        public bool IsPrimaryActiveUserSession { get; set; }
        public bool IsPrimaryActiveLocalAdminUserSession { get; set; }
        public bool IsConnectedUserSession { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionInfo"/> class.
        /// </summary>
        public SessionInfo()
        {
        }

        public SessionInfo(
            uint sessionId,
            string sessionName,
            string connectionState,
            bool? isCurrentProcessSession,
            bool isConsoleSession,
            bool isActiveSession,
            bool isConnectedSession,
            bool isDisconnectedSession,
            bool isRemoteSession,
            bool isRdpSession,
            bool isHdxSession,
            bool isRemoteListenerSession,
            bool isLocalSession,
            bool isSystemSession,
            bool isServicesSession,
            bool isConnectedConsoleSession,
            bool isUserSession,
            bool isLocalAdminUserSession,
            bool isActiveUserSession,
            bool isConsoleActiveUserSession,
            bool isPrimaryActiveUserSession,
            bool isPrimaryActiveLocalAdminUserSession,
            bool isConnectedUserSession)
        {
            SessionId = sessionId;
            SessionName = sessionName ?? throw new ArgumentNullException(nameof(sessionName));
            ConnectionState = connectionState ?? throw new ArgumentNullException(nameof(sessionName));

            IsCurrentProcessSession = isCurrentProcessSession;
            IsConsoleSession = isConsoleSession;
            IsActiveSession = isActiveSession;

            IsConnectedSession = isConnectedSession;
            IsDisconnectedSession = isDisconnectedSession;

            IsRemoteSession = isRemoteSession;
            IsRdpSession = isRdpSession;
            IsHdxSession = isHdxSession;
            IsRemoteListenerSession = isRemoteListenerSession;

            IsLocalSession = isLocalSession;

            IsSystemSession = isSystemSession;
            IsServicesSession = isServicesSession;
            IsConnectedConsoleSession = isConnectedConsoleSession;

            IsUserSession = isUserSession;
            IsLocalAdminUserSession = isLocalAdminUserSession;
            IsActiveUserSession = isActiveUserSession;
            IsConsoleActiveUserSession = isConsoleActiveUserSession;
            IsPrimaryActiveUserSession = isPrimaryActiveUserSession;
            IsPrimaryActiveLocalAdminUserSession = isPrimaryActiveLocalAdminUserSession;
            IsConnectedUserSession = isConnectedUserSession;
        }
    }
}
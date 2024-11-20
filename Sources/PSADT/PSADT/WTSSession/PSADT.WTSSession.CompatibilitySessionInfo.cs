using System;
using PSADT.WTSSession;

namespace PSADT.WTSSession
{
    public class CompatibilitySessionInfo
    {
        public string? NTAccount { get; }
        public string? SID { get; }
        public string? UserName { get; }
        public string? DomainName { get; }
        public int SessionId { get; }
        public string? SessionName { get; }
        public string? ConnectState { get; }
        public bool IsCurrentSession { get; }
        public bool IsConsoleSession { get; }
        public bool IsActiveUserSession { get; }
        public bool IsUserSession { get; }
        public bool IsRdpSession { get; }
        public bool IsLocalAdmin { get; }
        public DateTime? LogonTime { get; }
        public TimeSpan IdleTime { get; }
        public DateTime? DisconnectTime { get; }
        public string? ClientName { get; }
        public string? ClientProtocolType { get; }
        public string? ClientDirectory { get; }
        public long? ClientBuildNumber { get; }

        public CompatibilitySessionInfo(string? ntAccount, string? sid, string? userName, string? domainName, int sessionId, string? sessionName, string? connectState, bool isCurrentSession, bool isConsoleSession, bool isActiveUserSession, bool isUserSession, bool isRdpSession, bool isLocalAdmin, DateTime? logonTime, TimeSpan idleTime, DateTime? disconnectTime, string? clientName, string? clientProtocolType, string? clientDirectory, long? clientBuildNumber)
        {
            NTAccount = ntAccount;
            SID = sid;
            UserName = userName;
            DomainName = domainName;
            SessionId = sessionId;
            SessionName = sessionName;
            ConnectState = connectState;
            IsCurrentSession = isCurrentSession;
            IsConsoleSession = isConsoleSession;
            IsActiveUserSession = isActiveUserSession;
            IsUserSession = isUserSession;
            IsRdpSession = isRdpSession;
            IsLocalAdmin = isLocalAdmin;
            LogonTime = logonTime;
            IdleTime = idleTime;
            DisconnectTime = disconnectTime;
            ClientName = clientName;
            ClientProtocolType = clientProtocolType;
            ClientDirectory = clientDirectory;
            ClientBuildNumber = clientBuildNumber;
        }
    }
}

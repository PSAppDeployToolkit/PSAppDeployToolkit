namespace PSADT.TokenDiagnostics
{
    /// <summary>
    /// An LSA logon record copied before its native buffer is released.
    /// </summary>
    /// <param name="AuthenticationId">The authentication LUID, not the token object's identifier.</param>
    /// <param name="SessionId">The Terminal Services session reported by LSA.</param>
    /// <param name="Sid">The account SID, if supplied by LSA.</param>
    /// <param name="LogonType">The raw SECURITY_LOGON_TYPE value.</param>
    /// <param name="UserFlags">The unmodified LSA user flags.</param>
    /// <param name="WinlogonFlagPresent">Whether LOGON_WINLOGON is present.</param>
    /// <param name="LogonTimeFileTime">The raw LSA logon time, retained without assuming equality with WTS time.</param>
    internal sealed record class LogonSnapshot(string AuthenticationId, uint SessionId, string? Sid, uint LogonType, uint UserFlags, bool WinlogonFlagPresent, long LogonTimeFileTime);
}

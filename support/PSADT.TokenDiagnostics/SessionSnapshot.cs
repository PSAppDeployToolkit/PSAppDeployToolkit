namespace PSADT.TokenDiagnostics
{
    /// <summary>
    /// WTS owner information resolved independently of the candidate process tokens.
    /// </summary>
    /// <param name="SessionId">The requested Terminal Services session.</param>
    /// <param name="Account">The account name reported by WTS.</param>
    /// <param name="Sid">The resolved WTS owner's SID, if name translation succeeds.</param>
    /// <param name="State">The WTS connection state.</param>
    /// <param name="LogonTimeFileTime">The raw WTS logon time.</param>
    /// <param name="SidError">A name-translation failure, preventing a trusted SID comparison.</param>
    internal sealed record class SessionSnapshot(uint SessionId, string Account, string? Sid, string State, long LogonTimeFileTime, string? SidError);
}

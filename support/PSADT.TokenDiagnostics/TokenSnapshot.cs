namespace PSADT.TokenDiagnostics
{
    /// <summary>
    /// Query-only observations of one token; these do not establish suitability for launching a process.
    /// </summary>
    /// <param name="Sid">The token user SID.</param>
    /// <param name="SessionId">The token's Terminal Services session.</param>
    /// <param name="AuthenticationId">The token's authentication LUID.</param>
    /// <param name="TokenId">The identifier of this token object.</param>
    /// <param name="TokenType">Primary or impersonation token.</param>
    /// <param name="ElevationType">Default, full, or limited UAC token.</param>
    /// <param name="Elevated">Whether Windows reports the token as elevated.</param>
    /// <param name="IntegritySid">The mandatory integrity label SID.</param>
    /// <param name="Restricted">Whether the token has restricting SIDs.</param>
    /// <param name="AppContainer">Whether the token is an AppContainer token.</param>
    /// <param name="UIAccess">Whether the token has UIAccess.</param>
    /// <param name="Logon">The LSA record for this token's authentication LUID, if readable.</param>
    /// <param name="LogonError">A failure to read the LSA record, rather than an assumed absence of provenance.</param>
    internal sealed record class TokenSnapshot(string Sid, uint SessionId, string AuthenticationId, string TokenId, string TokenType, string ElevationType, bool Elevated, string IntegritySid, bool Restricted, bool AppContainer, bool UIAccess, LogonSnapshot? Logon, string? LogonError);
}

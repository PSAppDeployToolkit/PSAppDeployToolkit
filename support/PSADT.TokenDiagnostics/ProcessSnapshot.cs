namespace PSADT.TokenDiagnostics
{
    /// <summary>
    /// Observations of a process in the selected session, including independently queried linked tokens.
    /// </summary>
    /// <param name="ProcessId">The process identifier.</param>
    /// <param name="Name">The process name, without command-line arguments.</param>
    /// <param name="Token">The process primary token, if queryable.</param>
    /// <param name="LinkedToken">The linked token, if present and queryable.</param>
    /// <param name="Error">A process or primary-token query failure.</param>
    /// <param name="LinkedTokenError">A linked-token query failure for a full or limited token.</param>
    internal sealed record class ProcessSnapshot(int ProcessId, string Name, TokenSnapshot? Token, TokenSnapshot? LinkedToken, string? Error, string? LinkedTokenError);
}

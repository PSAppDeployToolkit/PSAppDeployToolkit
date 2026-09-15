using System.Collections.Generic;
using System.Linq;

namespace PSADT.TokenDiagnostics
{
    /// <summary>
    /// Observations from an explicitly requested source-token duplication and launch experiment.
    /// </summary>
    /// <param name="SourceProcessId">The user-selected source process.</param>
    /// <param name="SourceToken">The source token queried through the same handle used for duplication.</param>
    /// <param name="DuplicatedToken">The resulting primary token, if duplication succeeded.</param>
    /// <param name="Attempts">The process-creation attempts and their outcomes.</param>
    /// <param name="Error">A pre-launch validation, duplication, environment, or cleanup failure.</param>
    internal sealed record class LaunchProbeResult(int SourceProcessId, TokenSnapshot? SourceToken, TokenSnapshot? DuplicatedToken, IReadOnlyList<LaunchAttempt> Attempts, string? Error)
    {
        /// <summary>
        /// Whether at least one validated child ran successfully and the enclosing probe completed cleanly.
        /// </summary>
        public bool Succeeded => Error is null && Attempts.Any(static attempt => attempt.Succeeded);
    }
}

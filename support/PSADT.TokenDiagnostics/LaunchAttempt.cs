namespace PSADT.TokenDiagnostics
{
    /// <summary>
    /// The outcome of one fixed-command launch, including cleanup failures.
    /// </summary>
    /// <param name="Api">The Windows process-creation API used.</param>
    /// <param name="ChildProcessId">The created child, if process creation succeeded.</param>
    /// <param name="ChildToken">The child's token inspected while the child was suspended.</param>
    /// <param name="ExitCode">The child's exit code after resuming and waiting.</param>
    /// <param name="Error">A launch, validation, resume, or wait failure.</param>
    /// <param name="CleanupError">A failure to terminate or wait for the probe's own child during cleanup.</param>
    internal sealed record class LaunchAttempt(string Api, uint? ChildProcessId, TokenSnapshot? ChildToken, uint? ExitCode, string? Error, string? CleanupError)
    {
        /// <summary>
        /// Whether the validated child executed and exited successfully without cleanup errors.
        /// </summary>
        public bool Succeeded => ChildToken is not null && ExitCode is 0 && Error is null && CleanupError is null;
    }
}

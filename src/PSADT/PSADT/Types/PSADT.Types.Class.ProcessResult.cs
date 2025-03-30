namespace PSADT.Types
{
    /// <summary>
    /// Represents the result of a process execution, including exit code and standard output/error.
    /// </summary>
    public sealed class ProcessResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessResult"/> struct.
        /// </summary>
        /// <param name="exitCode">The exit code of the process.</param>
        /// <param name="stdOut">The standard output of the process.</param>
        /// <param name="stdErr">The standard error output of the process.</param>
        public ProcessResult(int exitCode, string? stdOut, string? stdErr)
        {
            ExitCode = exitCode;
            if (!string.IsNullOrWhiteSpace(stdOut)) StdOut = stdOut;
            if (!string.IsNullOrWhiteSpace(stdErr)) StdErr = stdErr;
        }

        /// <summary>
        /// Gets the exit code of the process.
        /// </summary>
        public readonly int ExitCode;

        /// <summary>
        /// Gets the standard output of the process.
        /// </summary>
        public readonly string? StdOut;

        /// <summary>
        /// Gets the standard error output of the process.
        /// </summary>
        public readonly string? StdErr;
    }
}

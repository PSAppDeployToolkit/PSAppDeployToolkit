namespace PSADT.Types
{
    /// <summary>
    /// Represents the result of a process execution, including exit code and standard output/error.
    /// </summary>
    public readonly struct ProcessResult
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
        public int ExitCode { get; }

        /// <summary>
        /// Gets the standard output of the process.
        /// </summary>
        public string? StdOut { get; }

        /// <summary>
        /// Gets the standard error output of the process.
        /// </summary>
        public string? StdErr { get; }

        /// <summary>
        /// Returns a string that represents the current <see cref="ProcessResult"/> object.
        /// </summary>
        /// <returns>A formatted string containing the exit code, standard output, and standard error.</returns>
        public override string ToString()
        {
            return $@"Exit Code: {ExitCode}
                      Standard Output: {StdOut}
                      Standard Error: {StdErr}";
        }
    }
}

namespace PSAppDeployToolkit.Logging
{
    /// <summary>
    /// Flag to indicate how to write log entries to the console.
    /// </summary>
    public enum HostLogStreamType
    {
        /// <summary>
        /// No log entries are written to the console.
        /// </summary>
        None = 0,

        /// <summary>
        /// Logs are written to PowerShell's host via the Information stream.
        /// </summary>
        Host = 1,

        /// <summary>
        /// Logs are written directly to the ConsoleHost output.
        /// </summary>
        Console = 2,

        /// <summary>
        /// Logs are written to PowerShell's host via the Verbose stream.
        /// </summary>
        Verbose = 3,
    }
}

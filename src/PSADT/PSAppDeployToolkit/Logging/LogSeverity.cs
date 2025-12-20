namespace PSAppDeployToolkit.Logging
{
    /// <summary>
    /// The severity of the log entry.
    /// </summary>
    public enum LogSeverity
    {
        /// <summary>
        /// The log entry indicates a successful operation.
        /// </summary>
        Success,

        /// <summary>
        /// The log entry provides informational messages.
        /// </summary>
        Info,

        /// <summary>
        /// The log entry indicates a warning condition.
        /// </summary>
        Warning,

        /// <summary>
        /// The log entry indicates an error condition.
        /// </summary>
        Error,
    }
}

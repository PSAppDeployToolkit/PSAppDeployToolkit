namespace PSADT.Core
{
    /// <summary>
    /// The severity of the log entry.
    /// </summary>
    public enum LogSeverity
    {
        /// <summary>
        /// The log entry indicates a successful operation.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The log entry provides informational messages.
        /// </summary>
        Info = 1,

        /// <summary>
        /// The log entry indicates a warning condition.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// The log entry indicates an error condition.
        /// </summary>
        Error = 3,
    }
}

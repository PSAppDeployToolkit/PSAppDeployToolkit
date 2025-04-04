using System;

namespace PSADT.Module
{
    /// <summary>
    /// Represents all data used as the basis for logging a PSAppDeployToolkit PowerShell log entry via `[ADTSession]::WriteLogEntry()`.
    /// </summary>
    public sealed class LogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> struct.
        /// </summary>
        /// <param name="timeStamp">The timestamp of the log entry.</param>
        /// <param name="message">The log entry message.</param>
        /// <param name="severity">The log entry's severity.</param>
        /// <param name="source">The log entry's source.</param>
        /// <param name="scriptSection">The log entry's script section, typically defaulting to the active session's InstallPhase value.</param>
        /// <param name="debugMessage">Indicates whether the log entry is a debug message.</param>
        /// <param name="callerFileName">The log entry's caller file name.</param>
        /// <param name="callerSource">The log entry's caller source.</param>
        public LogEntry(DateTime timeStamp, string message, LogSeverity severity, string source, string? scriptSection, bool debugMessage, string callerFileName, string callerSource, string consoleOutput, string diskOutput)
        {
            Timestamp = timeStamp;
            Message = message;
            Severity = severity;
            Source = source;
            ScriptSection = scriptSection;
            DebugMessage = debugMessage;
            CallerFileName = callerFileName;
            CallerSource = callerSource;
            ConsoleOutput = consoleOutput;
            DiskOutput = diskOutput;
        }

        /// <summary>
        /// Gets the timestamp of the log entry.
        /// </summary>
        public readonly DateTime Timestamp;

        /// <summary>
        /// Gets the log entry message.
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// Gets the log entry's severity.
        /// </summary>
        public readonly LogSeverity Severity;

        /// <summary>
        /// Gets the log entry's source.
        /// </summary>
        public readonly string Source;

        /// <summary>
        /// Gets the log entry's script section, typically defaulting to the active session's InstallPhase value.
        /// </summary>
        public readonly string? ScriptSection;

        /// <summary>
        /// Gets a value indicating whether the log entry is a debug message.
        /// </summary>
        public readonly bool DebugMessage;

        /// <summary>
        /// Gets the log entry's caller file name.
        /// </summary>
        public readonly string CallerFileName;

        /// <summary>
        /// Gets the log entry's caller source.
        /// </summary>
        public readonly string CallerSource;

        /// <summary>
        /// Gets the log entry as written to the console.
        /// </summary>
        public readonly string ConsoleOutput;

        /// <summary>
        /// Gets the log entry as written to the disk.
        /// </summary>
        public readonly string DiskOutput;

        /// <summary>
        /// Returns a string that represents the current <see cref="LogEntry"/> object.
        /// </summary>
        /// <returns>A formatted string containing the exit code, standard output, and standard error.</returns>
        public override string ToString()
        {
            return $"[{Timestamp.ToString("O")}]{(null != ScriptSection ? $" [{ScriptSection}]" : null)} [{Source}] [{Severity}] :: {Message}";
        }
    }
}

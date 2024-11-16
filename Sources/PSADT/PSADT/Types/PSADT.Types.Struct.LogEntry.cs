using System;
using System.Management.Automation;

namespace PSADT.Types
{
    /// <summary>
    /// Represents all data used as the basis for logging a PSAppDeployToolkit PowerShell log entry via `[ADTSession]::WriteLogEntry()`.
    /// </summary>
    public readonly struct LogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> struct.
        /// </summary>
        /// <param name="timeStamp">The timestamp of the log entry.</param>
        /// <param name="invoker">The standard output of the process.</param>
        /// <param name="Message">The log entry message.</param>
        /// <param name="Severity">The log entry's severity.</param>
        /// <param name="Source">The log entry's source.</param>
        /// <param name="ScriptSection">The log entry's script section, typically defaulting to the active session's InstallPhase value.</param>
        public LogEntry(DateTime timeStamp, CallStackFrame invoker, string message, uint severity, string source, string scriptSection)
        {
            Timestamp = timeStamp;
            Invoker = invoker;
            Message = message;
            Severity = severity;
            Source = source;
            ScriptSection = scriptSection;
        }

        /// <summary>
        /// Gets the timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the callstack of the log entry's caller.
        /// </summary>
        public CallStackFrame Invoker { get; }

        /// <summary>
        /// Gets the log entry message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the log entry's severity.
        /// </summary>
        public uint Severity { get; }

        /// <summary>
        /// Gets the log entry's source.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Gets the log entry's script section, typically defaulting to the active session's InstallPhase value.
        /// </summary>
        public string ScriptSection { get; }

        /// <summary>
        /// Returns a string that represents the current <see cref="LogEntry"/> object.
        /// </summary>
        /// <returns>A formatted string containing the exit code, standard output, and standard error.</returns>
        public override string ToString()
        {
            return $"[{Timestamp.ToString("O")}] [{ScriptSection}] [{Source}] :: {Message}";
        }
    }
}

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using PSADT.AccountManagement;
using PSADT.Extensions;

namespace PSADT.Core
{
    /// <summary>
    /// Represents all data used as the basis for logging a PSAppDeployToolkit PowerShell log entry via `[ADTSession]::WriteLogEntry()`.
    /// </summary>
    public sealed record LogEntry
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
        internal LogEntry(DateTime timeStamp, string message, LogSeverity severity, string source, string? scriptSection, bool debugMessage, string callerFileName, string callerSource)
        {
            // For CMTrace, we replace all empty lines with a space so OneTrace doesn't trim them.
            // When splitting the message, we want to trim all lines but not replace genuine
            // spaces. As such, replace all spaces and empty lines with a punctuation space.
            // C# identifies this character as whitespace but OneTrace does not so it works.
            // The empty line feed at the end is required by OneTrace to format correctly.
            Timestamp = timeStamp;
            Message = !string.IsNullOrWhiteSpace(message) ? message.TrimEndRemoveNull() : throw new ArgumentNullException("Message cannot be null or empty.", (Exception?)null);
            Severity = severity;
            Source = !string.IsNullOrWhiteSpace(source) ? source : throw new ArgumentNullException("Source cannot be null or empty.", (Exception?)null);
            ScriptSection = !string.IsNullOrWhiteSpace(scriptSection) ? scriptSection : null;
            DebugMessage = debugMessage;
            CallerFileName = !string.IsNullOrWhiteSpace(callerFileName) ? callerFileName : throw new ArgumentNullException("Caller file name cannot be null or empty.", (Exception?)null);
            CallerSource = !string.IsNullOrWhiteSpace(callerSource) ? callerSource : throw new ArgumentNullException("Caller source cannot be null or empty.", (Exception?)null);
            LegacyLogLine = $"[{timeStamp:O}]{(scriptSection is not null ? $" [{scriptSection}]" : null)} [{source}] [{severity}] :: {Message}";
            CMTraceLogLine = $"<![LOG[{(scriptSection is not null && Message != LogUtilities.LogDivider ? $"[{scriptSection}] :: " : null)}{(Message.Contains('\n') ? (string.Join(Environment.NewLine, Message.Replace("\r", null).Split('\n').Select(static m => string.IsNullOrWhiteSpace(m) ? LeadingSpaceString : CMTraceFirstChar.Match(m).Index is int start && start > 0 ? string.Concat(new(LeadingSpaceChar, start), m.Substring(start)) : m)) + Environment.NewLine) : Message)}]LOG]!><time=\"{timeStamp.ToString(@"HH\:mm\:ss.fff", CultureInfo.InvariantCulture)}{(TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes >= 0 ? $"+{TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes}" : TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes.ToString(CultureInfo.InvariantCulture))}\" date=\"{timeStamp.ToString("M-dd-yyyy", CultureInfo.InvariantCulture)}\" component=\"{source}\" context=\"{AccountUtilities.CallerUsername}\" type=\"{(uint)severity}\" thread=\"{AccountUtilities.CallerProcessId}\" file=\"{callerFileName}\">";
        }

        /// <summary>
        /// Gets the timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the log entry message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the log entry's severity.
        /// </summary>
        public LogSeverity Severity { get; }

        /// <summary>
        /// Gets the log entry's source.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Gets the log entry's script section, typically defaulting to the active session's InstallPhase value.
        /// </summary>
        public string? ScriptSection { get; }

        /// <summary>
        /// Gets a value indicating whether the log entry is a debug message.
        /// </summary>
        public bool DebugMessage { get; }

        /// <summary>
        /// Gets the log entry's caller file name.
        /// </summary>
        public string CallerFileName { get; }

        /// <summary>
        /// Gets the log entry's caller source.
        /// </summary>
        public string CallerSource { get; }

        /// <summary>
        /// Gets the log entry in legacy format.
        /// </summary>
        public string LegacyLogLine { get; }

        /// <summary>
        /// Gets the log entry as CMTrace format.
        /// </summary>
        public string CMTraceLogLine { get; }

        /// <summary>
        /// Returns a string that represents the current <see cref="LogEntry"/> object.
        /// </summary>
        /// <returns>A formatted string containing the exit code, standard output, and standard error.</returns>
        public override string ToString()
        {
            return LegacyLogLine;
        }

        /// <summary>
        /// Represents a compiled regular expression that matches the first non-whitespace character in a string.
        /// </summary>
        /// <remarks>This regular expression is precompiled for performance and is used to identify the
        /// first character in a string that is not a whitespace character.</remarks>
        private static readonly Regex CMTraceFirstChar = new(@"[^\s]", RegexOptions.Compiled);

        /// <summary>
        /// Represents the punctuation space character used to replace leading whitespace in CMTrace logs.
        /// </summary>
        /// <remarks>This character is a Unicode punctuation space (U+2008) and is used specifically to
        /// handle leading whitespace in log entries for CMTrace compatibility.</remarks>
        private const char LeadingSpaceChar = (char)0x2008;

        /// <summary>
        /// Represents a string containing a single leading space character.
        /// </summary>
        /// <remarks>This field is initialized using the <see cref="LeadingSpaceChar"/> constant and is
        /// intended for use in scenarios where a predefined string with a leading space is required.</remarks>
        private const string LeadingSpaceString = "\x2008";
    }
}

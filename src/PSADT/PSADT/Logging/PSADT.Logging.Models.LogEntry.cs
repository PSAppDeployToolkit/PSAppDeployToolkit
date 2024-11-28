using System;
using System.Text;
using System.Security.Principal;
using PSADT.Logging.Utilities;
using PSADT.Diagnostics.StackTraces;

namespace PSADT.Logging.Models
{
    /// <summary>
    /// Represents a log entry.
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; }
        public string Message { get; }
        public LogLevel MessageType { get; }
        public LogType LogCategory { get; }
        public CallerContext? CallerInfo { get; }
        public int ThreadId { get; }

        public LogEntry(string message,
                        LogLevel messageType,
                        CallerContext? callerInfo,
                        int threadId,
                        LogType logCategory = LogType.General)
        {
            Timestamp = DateTime.UtcNow;
            Message = message;
            MessageType = messageType;
            LogCategory = logCategory;
            CallerInfo = callerInfo;
            ThreadId = threadId;
        }

        /// <summary>
        /// Formats the log entry based on the LogOptions.
        /// </summary>
        /// <returns>Formatted log message.</returns>
        public string FormatMessage(TextLogFormat textLogFormat, string textSeparator = "")
        {
            textSeparator ??= "";

            // Default formatting based on TextLogFormat
            switch (textLogFormat)
            {
                case TextLogFormat.Standard:
                    return DefaultFormat(textSeparator);
                case TextLogFormat.CMTrace:
                    return CMTraceFormat();
                case TextLogFormat.Json:
                    return JsonFormat();
                default:
                    return DefaultFormat(textSeparator);
            }
        }

        /// <summary>
        /// Formats the log entry in the default format.
        /// </summary>
        /// <returns>Default formatted log message.</returns>
        private string DefaultFormat(string separator)
        {
            //return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logEntry.LogCategory}] {logEntry.MessageType}: {logEntry.Message} (Thread {logEntry.ThreadId})";
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss}{separator}{MessageType}{separator}{CallerInfo?.FormatCallerContextDefault()}{separator}Thread:{ThreadId}{separator}{Message}";
        }

        /// <summary>
        /// Formats the log entry in a format compatible with CMTrace.
        /// </summary>
        /// <returns>CMTrace-compatible formatted log message.</returns>
        private string CMTraceFormat()
        {
            // CMTrace Severity Mapping based on MessageType
            int cmTraceSeverity = MessageType switch
            {
                LogLevel.Debug => 0,
                LogLevel.Verbose => 1,
                LogLevel.Information => 2,
                LogLevel.Warning => 3,
                LogLevel.Error => 4,
                _ => 2 // Default to Information if unknown
            };

            string logTime = Timestamp.ToString("HH:mm:ss");
            string logDate = Timestamp.ToString("yyyy-MM-dd");

            // Get current user context
            string userContext = WindowsIdentity.GetCurrent()?.Name ?? "Unknown";

            // Get process ID
            int processId = Environment.CurrentManagedThreadId;

            // Get file name from Source (assuming Source holds the script or process name)
            string? fileName = CallerInfo?.FileName;

            // Constructing the CMTrace-compatible log entry
            string logEntry = $@"<![LOG[{Message}]LOG]!><time=""{logTime}"" date=""{logDate}"" component=""{CallerInfo?.FormatCallerContextDefault()}"" context=""{userContext}"" type=""{cmTraceSeverity}"" thread=""{processId}"" file=""{fileName}"">";

            return logEntry;
        }

        /// <summary>
        /// Formats the log entry in JSON format.
        /// </summary>
        /// <returns>JSON-formatted log message.</returns>
        private string JsonFormat()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"Timestamp\":\"{Timestamp:o}\",");
            sb.Append($"\"CallerContext\":\"{SharedLoggerUtilities.EscapeString(CallerInfo?.FormatCallerContextDefault())}\",");
            sb.Append($"\"MessageType\":\"{MessageType}\",");
            sb.Append($"\"Message\":\"{SharedLoggerUtilities.SanitizeMessage(Message)}\"");
            sb.Append($"\"ThreadId\":\"{ThreadId}\",");
            sb.Append("}");
            return sb.ToString();
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;

namespace PSADT.Logging.Utilities
{
    /// <summary>
    /// Contains shared utility methods for logging.
    /// </summary>
    public static class SharedLoggerUtilities
    {
        private static int _isHandlingLoggingException;

        /// <summary>
        /// Handles exceptions that occur during logging by logging them to the event log.
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        public static async Task LogToEventLogAsync(string? message = null, Exception? ex = null)
        {
            if (Interlocked.Exchange(ref _isHandlingLoggingException, 1) == 1)
                return;

            try
            {
                await LogToWindowsEventLogAsync(message, ex);
            }
            catch { }
            finally
            {
                Interlocked.Exchange(ref _isHandlingLoggingException, 0);
            }
        }

        /// <summary>
        /// Logs an exception or a custom message to the Windows Event Log.
        /// </summary>
        /// <param name="exception">The exception to log. If not provided, the message parameter will be logged.</param>
        /// <param name="message">Optional custom message to log, either as context for the exception or as a standalone log entry if no exception is provided.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task LogToWindowsEventLogAsync(string? message = null, Exception? exception = null)
        {
            // Asynchronous logging to the event log
            await Task.Run(() =>
            {
                try
                {
                    string source = "PSADTLogging";
                    string logName = "Application";

                    // Build the message to log
                    string logMessage = exception != null
                        ? $"{message}\nException: {exception.Message}\nStack Trace: {exception.StackTrace}"
                        : message ?? "An unspecified event occurred.";

                    if (!EventLog.SourceExists(source))
                    {
                        EventLog.CreateEventSource(source, logName);
                    }

                    EventLog.WriteEntry(source, logMessage, EventLogEntryType.Error);
                }
                catch
                {
                    // Suppress any exceptions thrown while logging to the event log
                }
            });
        }

        /// <summary>
        /// Sanitizes the message by removing sensitive information.
        /// </summary>
        /// <param name="message">The message to sanitize.</param>
        /// <returns>A sanitized message string.</returns>
        public static string SanitizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return message;

            message = ConvertControlCharsToUnicode(message);

            // Example: Remove email addresses
            message = Regex.Replace(message, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z]{2,}\b", "[REDACTED_EMAIL]", RegexOptions.IgnoreCase);

            // Example: Remove credit card numbers (simple pattern)
            message = Regex.Replace(message, @"\b(?:\d[ -]*?){13,16}\b", "[REDACTED_CC]", RegexOptions.IgnoreCase);

            // Add more sanitization rules as needed

            return message;
        }

        public static string ConvertControlCharsToUnicode(string message)
        {
            // Replace non-printable control characters unicode escape sequences
            var sanitized = new StringBuilder();
            foreach (char c in message)
            {
                if (char.IsControl(c) && !char.IsWhiteSpace(c))
                {
                    sanitized.Append(EscapeString(c.ToString()));
                }
                else
                {
                    sanitized.Append(c);
                }
            }

            return sanitized.ToString();
        }

        public static string EscapeString(string? str)
        {
            if (str == null)
                return "";

            var sb = new StringBuilder();
            foreach (char c in str)
            {
                switch (c)
                {
                    case '\"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (char.IsControl(c) || c > 127)
                        {
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}

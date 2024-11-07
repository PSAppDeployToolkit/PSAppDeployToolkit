using System;
using System.IO;
using System.Linq;
using PSADT.PathEx;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace PSADT.ConsoleEx
{
    /// <summary>
    /// Provides utility methods for console operations.
    /// </summary>
    public static class ConsoleHelper
    {
        internal static bool IsDebugMode = false;
        internal static bool IsHelpMode = false;

        /// <summary>
        /// Waits for any input from the console or for the timeout value to expire, before continuing.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for input.</param>
        /// <returns>The input string if received before the timeout, or an empty string if the timeout occurs.</returns>
        public static async Task<string> ReadLineWithTimeout(TimeSpan timeout)
        {
            var readLineTask = Task.Run(() => Console.ReadLine());
            var timeoutTask = Task.Delay(timeout);

            var completedTask = await Task.WhenAny(readLineTask, timeoutTask);
            return completedTask == readLineTask ? (await readLineTask ?? string.Empty) : string.Empty;
        }

        /// <summary>
        /// Writes a debug message to the console if debug mode is enabled or if the message type is Error.
        /// </summary>
        /// <param name="message">The message to be written to the console.</param>
        /// <param name="messageType">The type of the message (None, Debug, Info, Warning, or Error). Default is Debug.</param>
        /// <param name="exception">An optional exception object to be logged with the message.</param>
        /// <param name="callerMemberName">The name of the calling method. This is automatically filled by the compiler.</param>
        /// <param name="callerFilePath">The file path of the calling method. This is automatically filled by the compiler.</param>
        /// <param name="callerLineNumber">The line number where the method is called. This is automatically filled by the compiler.</param>
        /// <remarks>
        /// This method will only output Debug, Info, and Warning messages if IsDebugMode is true.
        /// Error messages are always written to the standard error output stream, regardless of the debug mode.
        /// If an exception is provided with an Error message, its details will also be written to the error stream.
        /// The output includes the message type, caller information (file name, method name, and line number), and the message itself.
        /// </remarks>
        /// <example>
        /// This shows how to use the DebugWrite method:
        /// <code>
        /// Program.DebugWrite("Processing started", MessageType.Info);
        /// try
        /// {
        ///     // Some processing
        /// }
        /// catch (Exception ex)
        /// {
        ///     Program.DebugWrite("An error occurred during processing", MessageType.Error, ex);
        /// }
        /// </code>
        /// </example>
        public static void DebugWrite(
            string message,
            MessageType messageType = MessageType.Debug,
            Exception? exception = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0,
            string logFilePath = "debug.log") // Default log file path
        {
            if (!ConsoleHelper.IsDebugMode && messageType != MessageType.Error) return;

            string prefix = messageType switch
            {
                MessageType.Debug => "[DEBUG]",
                MessageType.Info => "[INFO]",
                MessageType.Warning => "[WARNING]",
                MessageType.Error => "[ERROR]",
                _ => string.Empty
            };

            string fullMessage = string.Empty;

            if (!string.IsNullOrWhiteSpace(prefix))
            {
                var fileNameParts = Path.GetFileName(callerFilePath).Split('.');
                var lastTwoParts = fileNameParts.Length >= 2
                    ? string.Join(".", fileNameParts.Skip(fileNameParts.Length - 2))
                    : Path.GetFileName(callerFilePath);
                string callerInfo = $"[{lastTwoParts}::{callerMemberName}:{callerLineNumber}]";

                fullMessage = $"{prefix} {callerInfo} {message}";
            }

            // Log the message to the console
            if (messageType == MessageType.Error)
            {
                Console.Error.WriteLine(fullMessage);
                if (exception != null)
                {
                    string exceptionMessage = $"Exception: {exception.Message}\nStack Trace: {exception.StackTrace}";
                    Console.Error.WriteLine(exceptionMessage);
                    fullMessage += $"\n{exceptionMessage}";
                }
            }
            else
            {
                Console.WriteLine(fullMessage);
            }

            // Append the message to the log file
            try
            {
                using StreamWriter writer = new StreamWriter(@$"{PathHelper.GetExecutingAssemblyDirectory()}\{logFilePath}", append: true);
                writer.WriteLine(fullMessage);
                if (messageType == MessageType.Error && exception != null)
                {
                    writer.WriteLine($"Exception: {exception.Message}");
                    writer.WriteLine($"Stack Trace: {exception.StackTrace}");
                }
            }
            catch (Exception fileException)
            {
                // Handle any exceptions related to file writing
                Console.Error.WriteLine($"Failed to write to log file {logFilePath}: {fileException.Message}");
            }
        }
    }
}

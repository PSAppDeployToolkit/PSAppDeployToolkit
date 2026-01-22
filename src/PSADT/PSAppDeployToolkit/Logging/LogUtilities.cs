using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using PSAppDeployToolkit.Extensions;
using PSAppDeployToolkit.Foundation;

namespace PSAppDeployToolkit.Logging
{
    /// <summary>
    /// Provides utility methods for logging operations, including writing log entries to various outputs such as files,
    /// console, and PowerShell streams.
    /// </summary>
    /// <remarks>This class contains methods and constants to facilitate structured logging with support for
    /// configurable log severity, log styles, and output streams. It is designed to handle both debug and non-debug log
    /// messages, and supports writing logs to disk, console, or PowerShell streams based on the provided
    /// parameters.</remarks>
    public static class LogUtilities
    {
        /// <summary>
        /// Writes a log entry with detailed parameters.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="hostLogStreamType">What stream to write the message to.</param>
        /// <param name="debugMessage">Whether it is a debug message.</param>
        /// <param name="severity">The severity level.</param>
        /// <param name="source">The source of the log entry.</param>
        /// <param name="scriptSection">The script section.</param>
        /// <param name="logFileDirectory">The log file directory.</param>
        /// <param name="logFileName">The log file name.</param>
        /// <param name="logStyle">The type of log.</param>
        public static IReadOnlyList<LogEntry> WriteLogEntry(IReadOnlyList<string> message, HostLogStreamType hostLogStreamType, bool debugMessage, LogSeverity? severity = null, string? source = null, string? scriptSection = null, string? logFileDirectory = null, string? logFileName = null, LogStyle? logStyle = null)
        {
            // Establish logging date/time vars.
            DateTime dateNow = DateTime.Now;

            // Perform early return checks before wasting time.
            bool canLogToDisk = !string.IsNullOrWhiteSpace(logFileDirectory) && !string.IsNullOrWhiteSpace(logFileName);
            Hashtable? configToolkit = ModuleDatabase.IsInitialized() ? (Hashtable)ModuleDatabase.GetConfig()["Toolkit"]! : null;
            if (debugMessage && configToolkit?["LogDebugMessage"] is not true)
            {
                return new ReadOnlyCollection<LogEntry>([]);
            }

            // Get the caller's source and filename, factoring in whether we're running outside of PowerShell or not.
            bool noRunspace = (Runspace.DefaultRunspace is null) || (Runspace.DefaultRunspace.RunspaceStateInfo.State != RunspaceState.Opened);
            StackFrame[] stackFrames = [.. new StackTrace(true).GetFrames().Skip(1)]; string? callerFileName, callerSource;
            if (noRunspace || !stackFrames.Any(static f => f.GetMethod()?.DeclaringType?.Namespace?.StartsWith("System.Management.Automation", StringComparison.Ordinal) == true))
            {
                // Get the right stack frame. We want the first one that's not ours. If it's invalid, get our last one.
                StackFrame invoker = stackFrames.First(static f => !f.GetMethod()!.DeclaringType!.FullName!.StartsWith("PSADT", StringComparison.Ordinal));
                if (invoker.GetFileName() is null)
                {
                    invoker = stackFrames.Last(static f => f.GetMethod()!.DeclaringType!.FullName!.StartsWith("PSADT", StringComparison.Ordinal));
                }
                MethodBase method = invoker.GetMethod()!;
                callerFileName = invoker.GetFileName() ?? "<Unavailable>";
                callerSource = $"{method.DeclaringType!.FullName}.{method.Name}()";
            }
            else
            {
                // Get the first PowerShell stack frame that contains a valid command.
                CallStackFrame invoker = ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Get-PSCallStack'"), null).Skip(1).Select(static o => (CallStackFrame)o.BaseObject).First(static f => f.GetCommand() is string command && !string.IsNullOrWhiteSpace(command) && (!CallerCommandRegex.IsMatch(command) || (CallerScriptBlockRegex.IsMatch(command) && CallerScriptLocationRegex.IsMatch(f.GetScriptLocation()))));
                callerFileName = !string.IsNullOrWhiteSpace(invoker.ScriptName) ? invoker.ScriptName : invoker.GetScriptLocation();
                callerSource = invoker.GetCommand();
            }

            // Ensure we got a file name and a source.
            if (string.IsNullOrWhiteSpace(callerFileName))
            {
                throw new InvalidOperationException("Failed to determine a file name for the caller.");
            }
            if (string.IsNullOrWhiteSpace(callerSource))
            {
                throw new InvalidOperationException("Failed to determine a command source for the caller.");
            }

            // Set up default values if not specified.
            if (!logStyle.HasValue)
            {
                logStyle = configToolkit?["LogStyle"] is string styleString && Enum.TryParse(styleString, out LogStyle styleEnum) ? styleEnum : LogStyle.CMTrace;
            }
            if (string.IsNullOrWhiteSpace(source))
            {
                source = callerSource;
            }
            if (logFileDirectory is not null && !Directory.Exists(logFileDirectory))
            {
                _ = Directory.CreateDirectory(logFileDirectory);
            }
            if (string.IsNullOrWhiteSpace(scriptSection))
            {
                scriptSection = null;
            }
            severity ??= LogSeverity.Info;

            // Build out the log entries and confirm whether there's anything to log.
            ReadOnlyCollection<LogEntry> logEntries = new([.. message.Where(static msg => !string.IsNullOrWhiteSpace(msg)).Select(msg => new LogEntry(dateNow, msg, severity.Value, source!, scriptSection, debugMessage, callerFileName, callerSource))]);
            if (logEntries.Count == 0)
            {
                throw new InvalidOperationException("No valid log messages were provided to log.");
            }

            // Write out all messages to disk if configured/permitted to do so.
            if (canLogToDisk)
            {
                using StreamWriter logFileWriter = new(Path.Combine(logFileDirectory!, logFileName!), true, LogEncoding);
                foreach (string line in logStyle.Value == LogStyle.CMTrace ? logEntries.Select(static e => e.CMTraceLogLine) : logEntries.Select(static e => e.LegacyLogLine))
                {
                    logFileWriter.WriteLine(line);
                }
            }

            // Write out all messages to host if configured/permitted to do so.
            if (hostLogStreamType != HostLogStreamType.None)
            {
                IEnumerable<string> conOutput = logEntries.Select(static e => e.LegacyLogLine);
                ReadOnlyDictionary<string, ConsoleColor> sevCols = LogSeverityColors[(int)severity];
                if (hostLogStreamType == HostLogStreamType.Console || noRunspace)
                {
                    // Writing straight to the console.
                    bool colouredOutput = severity != LogSeverity.Info;
                    if (colouredOutput)
                    {
                        Console.ForegroundColor = sevCols["ForegroundColor"];
                        Console.BackgroundColor = sevCols["BackgroundColor"];
                    }
                    if (severity == LogSeverity.Error)
                    {
                        foreach (string line in conOutput)
                        {
                            Console.Error.WriteLine(line);
                        }
                    }
                    else
                    {
                        foreach (string line in conOutput)
                        {
                            Console.WriteLine(line);
                        }
                    }
                    if (colouredOutput)
                    {
                        Console.ResetColor();
                    }
                }
                else if (hostLogStreamType != HostLogStreamType.Verbose)
                {
                    // Write the host output to PowerShell's InformationStream.
                    _ = ModuleDatabase.InvokeScript(WriteHostDelegate, conOutput, sevCols);
                }
                else
                {
                    // Write the host output to PowerShell's VerboseStream.
                    _ = ModuleDatabase.InvokeScript(WriteVerboseDelegate, conOutput);
                }
            }
            return logEntries;
        }

        /// <summary>
        /// Gets the log severity colors.
        /// </summary>
        private static readonly ReadOnlyCollection<ReadOnlyDictionary<string, ConsoleColor>> LogSeverityColors = new(
        [
            new(new Dictionary<string, ConsoleColor>() { { "ForegroundColor", ConsoleColor.Green }, { "BackgroundColor", ConsoleColor.Black } }),
            new(new Dictionary<string, ConsoleColor>() { }),
            new(new Dictionary<string, ConsoleColor>() { { "ForegroundColor", ConsoleColor.Yellow }, { "BackgroundColor", ConsoleColor.Black } }),
            new(new Dictionary<string, ConsoleColor>() { { "ForegroundColor", ConsoleColor.Red }, { "BackgroundColor", ConsoleColor.Black } }),
        ]);

        /// <summary>
        /// Gets the session's default log file encoding.
        /// </summary>
        internal static readonly UTF8Encoding LogEncoding = new(true, true);

        /// <summary>
        /// Gets the log divider string.
        /// </summary>
        internal const string LogDivider = "-------------------------------------------------------------------------------";

        /// <summary>
        /// Gets the Write-Host delegate script block.
        /// </summary>
        private static readonly ScriptBlock WriteHostDelegate = ScriptBlock.Create("$colours = $args[1]; $args[0] | & $Script:CommandTable.'Write-Host' @colours");

        /// <summary>
        /// Gets the Write-Verbose delegate script block.
        /// </summary>
        private static readonly ScriptBlock WriteVerboseDelegate = ScriptBlock.Create("$args[0] | & $Script:CommandTable.'Write-Verbose'");

        /// <summary>
        /// Represents a compiled regular expression used to match specific caller commands.
        /// </summary>
        /// <remarks>The regular expression matches commands in the following formats: - "Write-Log" or
        /// "Write-ADTLogEntry" - "&lt;ScriptBlock&gt;" optionally followed by "&lt;tag&gt;" This regex is optimized for performance
        /// using the <see cref="RegexOptions.Compiled"/> option.</remarks>
        private static readonly Regex CallerCommandRegex = new(@"^(Write-(Log|ADTLogEntry)|<ScriptBlock>(<\w+>)?)$", RegexOptions.Compiled);

        /// <summary>
        /// Represents a compiled regular expression used to match script block patterns.
        /// </summary>
        /// <remarks>The regular expression matches strings that begin with "&lt;ScriptBlock&gt;" and optionally
        /// include an additional tag. This is useful for identifying specific script block structures in
        /// text.</remarks>
        private static readonly Regex CallerScriptBlockRegex = new(@"^(<ScriptBlock>(<\w+>)?)$", RegexOptions.Compiled);

        /// <summary>
        /// Represents a compiled regular expression used to match caller script locations.
        /// </summary>
        /// <remarks>The regular expression matches strings that begin and end with angle brackets (e.g.,
        /// "&lt;example&gt;"). This is typically used to identify script locations in a specific format.</remarks>
        private static readonly Regex CallerScriptLocationRegex = new("^<.+>$", RegexOptions.Compiled);
    }
}

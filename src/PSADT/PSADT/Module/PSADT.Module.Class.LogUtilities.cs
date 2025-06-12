using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PSADT.Extensions;

namespace PSADT.Module
{
    public static class LogUtilities
    {
        /// <summary>
        /// Writes a log entry with detailed parameters.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="hostLogStream">What stream to write the message to.</param>
        /// <param name="debugMessage">Whether it is a debug message.</param>
        /// <param name="severity">The severity level.</param>
        /// <param name="source">The source of the log entry.</param>
        /// <param name="scriptSection">The script section.</param>
        /// <param name="logFileDirectory">The log file directory.</param>
        /// <param name="logFileName">The log file name.</param>
        /// <param name="logType">The type of log.</param>
        public static IReadOnlyList<LogEntry> WriteLogEntry(IReadOnlyList<string> message, HostLogStream hostLogStream, bool debugMessage, LogSeverity? severity = null, string? source = null, string? scriptSection = null, string? logFileDirectory = null, string? logFileName = null, LogStyle? logType = null)
        {
            // Establish logging date/time vars.
            DateTime dateNow = DateTime.Now;

            // Perform early return checks before wasting time.
            bool canLogToDisk = !string.IsNullOrWhiteSpace(logFileDirectory) && !string.IsNullOrWhiteSpace(logFileName);
            Hashtable? configToolkit = ModuleDatabase.IsInitialized() ? (Hashtable)ModuleDatabase.GetConfig()["Toolkit"]! : null;
            if ((!canLogToDisk && hostLogStream.Equals(HostLogStream.None)) || (debugMessage && !(bool)configToolkit?["LogDebugMessage"]!))
            {
                return Array.Empty<LogEntry>();
            }

            // Get the caller's source and filename, factoring in whether we're running outside of PowerShell or not.
            bool noRunspace = (null == Runspace.DefaultRunspace) || (Runspace.DefaultRunspace.RunspaceStateInfo.State != RunspaceState.Opened);
            var stackFrames = new StackTrace(true).GetFrames().Skip(1); string callerFileName = string.Empty; string callerSource = string.Empty;
            if (noRunspace || !stackFrames.Any(static f => f.GetMethod()?.DeclaringType?.Namespace?.StartsWith("System.Management.Automation") == true))
            {
                // Get the right stack frame. We want the first one that's not ours. If it's invalid, get our last one.
                var invoker = stackFrames.First(static f => !f.GetMethod()!.DeclaringType!.FullName!.StartsWith("PSADT"));
                if (null == invoker.GetFileName())
                {
                    invoker = stackFrames.Last(static f => f.GetMethod()!.DeclaringType!.FullName!.StartsWith("PSADT"));
                }
                var method = invoker.GetMethod()!;
                callerFileName = invoker.GetFileName()!;
                callerSource = $"{method.DeclaringType!.FullName}.{method.Name}()";
            }
            else
            {
                // Get the first PowerShell stack frame that contains a valid command.
                var invoker = ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Get-PSCallStack'"), null).Skip(1).Select(static o => (CallStackFrame)o.BaseObject).First(static f => f.GetCommand() is string command && !string.IsNullOrWhiteSpace(command) && (!CallerCommandRegex.IsMatch(command) || (CallerScriptBlockRegex.IsMatch(command) && CallerScriptLocationRegex.IsMatch(f.GetScriptLocation()))));
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

            // Set up default values if not specified and build out the log entries.
            if (canLogToDisk && !logType.HasValue)
            {
                if (Enum.TryParse<LogStyle>((string)configToolkit?["LogStyle"]!, out var configStyle))
                {
                    logType = configStyle;
                }
                else
                {
                    logType = LogStyle.CMTrace;
                }
            }
            if (null == severity)
            {
                severity = LogSeverity.Info;
            }
            if (string.IsNullOrWhiteSpace(source))
            {
                source = callerSource;
            }
            if ((null != logFileDirectory) && !Directory.Exists(logFileDirectory))
            {
                Directory.CreateDirectory(logFileDirectory);
            }
            if (null != scriptSection && string.IsNullOrWhiteSpace(scriptSection))
            {
                scriptSection = null;
            }
            var logEntries = message.Where(static msg => !string.IsNullOrWhiteSpace(msg)).Select(msg => new LogEntry(dateNow, msg, severity.Value, source!, scriptSection, debugMessage, callerFileName, callerSource)).ToList().AsReadOnly();

            // Write out all messages to disk if configured/permitted to do so.
            if (canLogToDisk)
            {
                using (StreamWriter logFileWriter = new StreamWriter(Path.Combine(logFileDirectory!, logFileName!), true, LogEncoding))
                {
                    logFileWriter.WriteLine(string.Join(Environment.NewLine, logType!.Value == LogStyle.CMTrace ? logEntries.Select(static e => e.CMTraceLogLine) : logEntries.Select(static e => e.LegacyLogLine)));
                }
            }

            // Write out all messages to host if configured/permitted to do so.
            if (!hostLogStream.Equals(HostLogStream.None))
            {
                var conOutput = logEntries.Select(static e => e.LegacyLogLine);
                var sevCols = LogSeverityColors[(int)severity];
                if (hostLogStream.Equals(HostLogStream.Console) || noRunspace)
                {
                    // Writing straight to the console.
                    if (severity != LogSeverity.Info)
                    {
                        Console.ForegroundColor = sevCols["ForegroundColor"];
                        Console.BackgroundColor = sevCols["BackgroundColor"];
                    }
                    if (severity == LogSeverity.Error)
                    {
                        Console.Error.WriteLine(string.Join(Environment.NewLine, conOutput));
                    }
                    else
                    {
                        Console.WriteLine(string.Join(Environment.NewLine, conOutput));
                    }
                    Console.ResetColor();
                }
                else
                {
                    // Write the host output to PowerShell's InformationStream.
                    ModuleDatabase.InvokeScript(WriteLogEntryDelegate, conOutput, sevCols, source!, hostLogStream.Equals(HostLogStream.Verbose));
                }
            }
            return logEntries;
        }

        /// <summary>
        /// Gets the log severity colors.
        /// </summary>
        private static readonly ReadOnlyCollection<ReadOnlyDictionary<string, ConsoleColor>> LogSeverityColors = new List<ReadOnlyDictionary<string, ConsoleColor>>()
        {
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Green }, { "BackgroundColor", ConsoleColor.Black } }),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor>()),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Yellow }, { "BackgroundColor", ConsoleColor.Black } }),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Red }, { "BackgroundColor", ConsoleColor.Black } })
        }.AsReadOnly();

        /// <summary>
        /// Gets the log divider string.
        /// </summary>
        internal static readonly string LogDivider = new string('-', 79);

        /// <summary>
        /// Gets the session's default log file encoding.
        /// </summary>
        internal static readonly UTF8Encoding LogEncoding = new UTF8Encoding(true);

        /// <summary>
        /// Gets the Write-LogEntry delegate script block.
        /// </summary>
        private static readonly ScriptBlock WriteLogEntryDelegate = ScriptBlock.Create("$colours = $args[1]; $args[0] | & $Script:CommandTable.'Write-ADTLogEntryToOutputStream' @colours -Source $args[2] -Verbose:($args[3])");

        /// <summary>
        /// Represents a compiled regular expression used to match specific caller commands.
        /// </summary>
        /// <remarks>The regular expression matches commands in the following formats: - "Write-Log" or
        /// "Write-ADTLogEntry" - "<ScriptBlock>" optionally followed by "<tag>" This regex is optimized for performance
        /// using the <see cref="RegexOptions.Compiled"/> option.</remarks>
        private static readonly Regex CallerCommandRegex = new(@"^(Write-(Log|ADTLogEntry)|<ScriptBlock>(<\w+>)?)$", RegexOptions.Compiled);

        /// <summary>
        /// Represents a compiled regular expression used to match script block patterns.
        /// </summary>
        /// <remarks>The regular expression matches strings that begin with "<ScriptBlock>" and optionally
        /// include an additional tag. This is useful for identifying specific script block structures in
        /// text.</remarks>
        private static readonly Regex CallerScriptBlockRegex = new(@"^(<ScriptBlock>(<\w+>)?)$", RegexOptions.Compiled);

        /// <summary>
        /// Represents a compiled regular expression used to match caller script locations.
        /// </summary>
        /// <remarks>The regular expression matches strings that begin and end with angle brackets (e.g.,
        /// "<example>").  This is typically used to identify script locations in a specific format.</remarks>
        private static readonly Regex CallerScriptLocationRegex = new("^<.+>$", RegexOptions.Compiled);
    }
}

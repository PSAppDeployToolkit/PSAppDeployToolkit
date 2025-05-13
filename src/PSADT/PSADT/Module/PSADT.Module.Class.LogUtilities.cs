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
using PSADT.Utilities;

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
        public static IReadOnlyList<LogEntry> WriteLogEntry(string[] message, HostLogStream hostLogStream, bool debugMessage, LogSeverity? severity = null, string? source = null, string? scriptSection = null, string? logFileDirectory = null, string? logFileName = null, string? logType = null)
        {
            // Establish logging date/time vars.
            DateTime dateNow = DateTime.Now;

            // Perform early return checks before wasting time.
            bool canLogToDisk = !string.IsNullOrWhiteSpace(logFileDirectory) && !string.IsNullOrWhiteSpace(logFileName);
            if ((!canLogToDisk && hostLogStream.Equals(HostLogStream.None)) || (debugMessage && !(bool)((Hashtable)ModuleDatabase.GetConfig()["Toolkit"]!)["LogDebugMessage"]!))
            {
                return new List<LogEntry>().AsReadOnly();
            }

            // Get the caller's source and filename, factoring in whether we're running outside of PowerShell or not.
            bool noRunspace = (null == Runspace.DefaultRunspace) || (Runspace.DefaultRunspace.RunspaceStateInfo.State != RunspaceState.Opened);
            var stackFrames = new StackTrace(true).GetFrames().Skip(1);
            string callerFileName = string.Empty;
            string callerSource = string.Empty;
            if (noRunspace || !stackFrames.Any(static f => f.GetMethod()?.DeclaringType?.Namespace?.StartsWith("System.Management.Automation") == true))
            {
                var invoker = stackFrames.First(static f => !f.GetMethod()!.DeclaringType!.FullName!.StartsWith("PSADT")); var method = invoker.GetMethod()!;
                callerFileName = invoker.GetFileName()!;
                callerSource = $"{method.DeclaringType!.FullName}.{method.Name}()";
            }
            else
            {
                var invoker = ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Get-PSCallStack'"), null).Skip(1).Select(static o => (CallStackFrame)o.BaseObject).First(static f => f.GetCommand() is string command && !string.IsNullOrWhiteSpace(command) && (!Regex.IsMatch(command, @"^(Write-(Log|ADTLogEntry)|<ScriptBlock>(<\w+>)?)$") || (Regex.IsMatch(command, @"^(<ScriptBlock>(<\w+>)?)$") && Regex.IsMatch(f.GetScriptLocation(), "^<.+>$"))));
                callerFileName = !string.IsNullOrWhiteSpace(invoker.ScriptName) ? invoker.ScriptName : invoker.GetScriptLocation();
                callerSource = invoker.GetCommand();
            }

            // Set up default values if not specified.
            if (null == severity)
            {
                severity = LogSeverity.Info;
            }
            if (string.IsNullOrWhiteSpace(source))
            {
                source = callerSource;
            }
            if (canLogToDisk && string.IsNullOrWhiteSpace(logType))
            {
                try
                {
                    logType = (string)((Hashtable)ModuleDatabase.GetConfig()["Toolkit"]!)["LogStyle"]!;
                }
                catch
                {
                    logType = "CMTrace";
                }
            }
            if ((null != logFileDirectory) && !Directory.Exists(logFileDirectory))
            {
                Directory.CreateDirectory(logFileDirectory);
            }
            if (null != scriptSection && string.IsNullOrWhiteSpace(scriptSection))
            {
                scriptSection = null;
            }

            // Loop through each message and generate necessary log messages.
            // For CMTrace, we replace all empty lines with a space so OneTrace doesn't trim them.
            // When splitting the message, we want to trim all lines but not replace genuine
            // spaces. As such, replace all spaces and empty lines with a punctuation space.
            // C# identifies this character as whitespace but OneTrace does not so it works.
            // The empty line feed at the end is required by OneTrace to format correctly.
            List<LogEntry> logEntries = new List<LogEntry>(message.Length);
            List<string> dskOutput = new List<string>(message.Length);
            List<string> conOutput = new List<string>(message.Length);
            var conFormat = $"[{dateNow.ToString("O")}]{(null != scriptSection ? $" [{scriptSection}]" : null)} [{source}] [{severity}] :: {{0}}".Replace("{", "{{").Replace("}", "}}").Replace("{{0}}", "{0}");
            if (logType != "Legacy")
            {
                var dskFormat = $"<![LOG[{(null != scriptSection && message[0] != LogDivider ? $"[{scriptSection}] :: " : null)}{{0}}]LOG]!><time=\"{dateNow.ToString(@"HH\:mm\:ss.fff")}{(TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes >= 0 ? $"+{TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes}" : TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes.ToString())}\" date=\"{dateNow.ToString("M-dd-yyyy")}\" component=\"{source}\" context=\"{AccountUtilities.CallerUsername}\" type=\"{(uint)severity}\" thread=\"{PID}\" file=\"{callerFileName}\">".Replace("{", "{{").Replace("}", "}}").Replace("{{0}}", "{0}");
                foreach (string msg in message)
                {
                    var safeMsg = msg.Replace("\0", string.Empty).TrimEnd();
                    if (!string.IsNullOrWhiteSpace(safeMsg))
                    {
                        var dskLine = string.Format(dskFormat, safeMsg.Contains((char)10) ? (string.Join(Environment.NewLine, safeMsg.Trim().Split((char)10).Select(static m => Regex.Replace(m.Trim(), "^( +|$)", $"{(char)0x2008}"))) + Environment.NewLine) : safeMsg.Replace("\0", string.Empty));
                        var conLine = string.Format(conFormat, safeMsg);
                        logEntries.Add(new LogEntry(dateNow, safeMsg, severity.Value, source!, scriptSection, debugMessage, callerFileName, callerSource, conLine, dskLine));
                        dskOutput.Add(dskLine);
                        conOutput.Add(conLine);
                    }
                }
            }
            else
            {
                var dskFormat = conFormat;
                foreach (var msg in message)
                {
                    var safeMsg = msg.Replace("\0", string.Empty).TrimEnd();
                    if (!string.IsNullOrWhiteSpace(safeMsg))
                    {
                        var dskLine = string.Format(dskFormat, safeMsg);
                        var conLine = string.Format(conFormat, safeMsg);
                        logEntries.Add(new LogEntry(dateNow, safeMsg, severity.Value, source!, scriptSection, debugMessage, callerFileName, callerSource, conLine, dskLine));
                        dskOutput.Add(dskLine);
                        conOutput.Add(conLine);
                    }
                }
            }

            // Write out all messages to disk if configured/permitted to do so.
            if (canLogToDisk)
            {
                using (StreamWriter logFileWriter = new StreamWriter(Path.Combine(logFileDirectory!, logFileName!), true, LogEncoding))
                {
                    logFileWriter.WriteLine(string.Join(Environment.NewLine, dskOutput));
                }
            }

            // Write out all messages to host if configured/permitted to do so.
            if (!hostLogStream.Equals(HostLogStream.None))
            {
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
            return logEntries.AsReadOnly();
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
        /// Gets the Write-LogEntry delegate script block.
        /// </summary>
        private static readonly ScriptBlock WriteLogEntryDelegate = ScriptBlock.Create("$colours = $args[1]; $args[0] | & $Script:CommandTable.'Write-ADTLogEntryToOutputStream' @colours -Source $args[2] -Verbose:($args[3])");

        /// <summary>
        /// Gets the log divider string.
        /// </summary>
        internal static readonly string LogDivider = new string('-', 79);

        /// <summary>
        /// Gets the current process ID.
        /// </summary>
        private static readonly int PID = Process.GetCurrentProcess().Id;

        /// <summary>
        /// Gets the session's default log file encoding.
        /// </summary>
        internal static readonly UTF8Encoding LogEncoding = new UTF8Encoding(true);
    }
}

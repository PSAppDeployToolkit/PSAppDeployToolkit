using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PSADT.Module
{
    public static class LoggingUtilities
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
        public static void WriteLogEntry(string[] message, HostLogStream hostLogStream, bool debugMessage, uint? severity = null, string? source = null, string? scriptSection = null, string? logFileDirectory = null, string? logFileName = null, string? logType = null)
        {
            // Establish logging date/time vars.
            DateTime dateNow = DateTime.Now;

            // Determine whether we're able to log to disk.
            bool canLogToDisk = !string.IsNullOrWhiteSpace(logFileDirectory) && !string.IsNullOrWhiteSpace(logFileName);
            bool noRunspace = (null == Runspace.DefaultRunspace) || (Runspace.DefaultRunspace.RunspaceStateInfo.State != RunspaceState.Opened);

            // Perform early return checks before wasting time.
            if ((!canLogToDisk && hostLogStream.Equals(HostLogStream.None)) || (debugMessage && !(bool)((Hashtable)InternalDatabase.GetConfig()["Toolkit"]!)["LogDebugMessage"]!))
            {
                return;
            }

            // Variables used to determine the caller's source and filename.
            string callerFileName = string.Empty;
            string callerSource = string.Empty;

            // Handle situations where we might be calling this function without an active runspace.
            if (noRunspace)
            {
                var invoker = new StackTrace(true).GetFrames().Skip(1).Where(static f => !f.GetMethod()!.DeclaringType!.FullName!.StartsWith("PSADT")).First(); var method = invoker.GetMethod()!;
                callerFileName = invoker.GetFileName()!;
                callerSource = $"{method.DeclaringType!.FullName}.{method.Name}()";
            }
            else
            {
                CallStackFrame invoker = GetLogEntryCaller(InternalDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Get-PSCallStack'"), null).Skip(1).Select(static o => (CallStackFrame)o.BaseObject).ToArray());
                callerFileName = !string.IsNullOrWhiteSpace(invoker.ScriptName) ? invoker.ScriptName : invoker.GetScriptLocation();
                callerSource = GetPowerShellCallStackFrameCommand(invoker);
            }

            // Set up default values if not specified.
            if (null == severity)
            {
                severity = 1;
            }
            if (string.IsNullOrWhiteSpace(source))
            {
                source = callerSource;
            }
            if (canLogToDisk && string.IsNullOrWhiteSpace(logType))
            {
                logType = (string)((Hashtable)InternalDatabase.GetConfig()["Toolkit"]!)["LogStyle"]!;
            }
            if ((null != logFileDirectory) && !Directory.Exists(logFileDirectory))
            {
                Directory.CreateDirectory(logFileDirectory);
            }

            // Store log string to format with message.
            IReadOnlyDictionary<string, string> logFormats = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()
            {
                { "Legacy", $"[{dateNow.ToString("O")}]{(null != scriptSection ? $" [{scriptSection}]" : null)} [{source}] [{LogSeverityNames[(int)severity]}] :: {{0}}".Replace("{", "{{").Replace("}", "}}").Replace("{{0}}", "{0}") },
                { "CMTrace", $"<![LOG[{(null != scriptSection ? $"[{scriptSection}] :: " : null)}{{0}}]LOG]!><time=\"{dateNow.ToString("HH\\:mm\\:ss.fff")}{(TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes >= 0 ? $"+{TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes}" : TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes.ToString())}\" date=\"{dateNow.ToString("M-dd-yyyy")}\" component=\"{source}\" context=\"{Username}\" type=\"{severity}\" thread=\"{PID}\" file=\"{callerFileName}\">".Replace("{", "{{").Replace("}", "}}").Replace("{{0}}", "{0}") },
            });

            // Write out all messages to disk if configured/permitted to do so.
            if (canLogToDisk)
            {
                using (StreamWriter logFileWriter = new StreamWriter(Path.Combine(logFileDirectory!, logFileName!), true, LogEncoding))
                {
                    string logLine = logFormats[logType!]!;
                    switch (logType)
                    {
                        case "CMTrace":
                            // Replace all empty lines with a space so OneTrace doesn't trim them.
                            // When splitting the message, we want to trim all lines but not replace genuine
                            // spaces. As such, replace all spaces and empty lines with a punctuation space.
                            // C# identifies this character as whitespace but OneTrace does not so it works.
                            // The empty line feed at the end is required by OneTrace to format correctly.
                            logFileWriter.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg.Contains((char)10) ? (string.Join(Environment.NewLine, msg.Trim().Split((char)10).Select(static m => Regex.Replace(m.Replace("\0", string.Empty).Trim(), "^( +|$)", $"{(char)0x2008}"))) + Environment.NewLine) : msg.Replace("\0", string.Empty)))));
                            break;
                        case "Legacy":
                            logFileWriter.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg.Replace("\0", string.Empty)))));
                            break;
                    }
                }
            }

            // Write out all messages to host if configured/permitted to do so.
            if (!hostLogStream.Equals(HostLogStream.None))
            {
                var sevCols = LogSeverityColors[(int)severity];
                if (hostLogStream.Equals(HostLogStream.Console) || noRunspace)
                {
                    // Colour the console if we're not informational.
                    if (severity != 1)
                    {
                        Console.ForegroundColor = sevCols["ForegroundColor"];
                        Console.BackgroundColor = sevCols["BackgroundColor"];
                    }

                    // Write errors to stderr, otherwise send everything else to stdout.
                    string logLine = logFormats["Legacy"]!;
                    if (severity == 3)
                    {
                        Console.Error.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg.Replace("\0", string.Empty)))));
                    }
                    else
                    {
                        Console.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg.Replace("\0", string.Empty)))));
                    }

                    // Reset the console colours back to default.
                    Console.ResetColor();
                }
                else
                {
                    // Write the host output to PowerShell's InformationStream.
                    InternalDatabase.InvokeScript(WriteLogEntryDelegate, message, sevCols, source!, logFormats["Legacy"]!, hostLogStream.Equals(HostLogStream.Verbose));
                }
            }
        }

        /// <summary>
        /// Gets the caller of the log entry from the call stack frames.
        /// </summary>
        /// <param name="stackFrames">The call stack frames.</param>
        /// <returns>The call stack frame of the log entry caller.</returns>
        private static CallStackFrame GetLogEntryCaller(CallStackFrame[] stackFrames)
        {
            foreach (CallStackFrame frame in stackFrames)
            {
                // Get the command from the frame and test its validity.
                string command = GetPowerShellCallStackFrameCommand(frame);
                if (!string.IsNullOrWhiteSpace(command) && (!Regex.IsMatch(command, "^(Write-(Log|ADTLogEntry)|<ScriptBlock>(<\\w+>)?)$") || (Regex.IsMatch(command, "^(<ScriptBlock>(<\\w+>)?)$") && frame.GetScriptLocation().Equals("<No file>"))))
                {
                    return frame;
                }
            }
            return null!;
        }

        /// <summary>
        /// Gets the PowerShell call stack frame command.
        /// </summary>
        /// <param name="frame">The call stack frame.</param>
        /// <returns>The PowerShell call stack frame command.</returns>
        private static string GetPowerShellCallStackFrameCommand(CallStackFrame frame)
        {
            // We must re-create the "Command" ScriptProperty as it's only available in PowerShell.
            if (null == frame.InvocationInfo)
            {
                return frame.FunctionName;
            }
            if (null == frame.InvocationInfo.MyCommand)
            {
                return frame.InvocationInfo.InvocationName;
            }
            if (frame.InvocationInfo.MyCommand.Name != string.Empty)
            {
                return frame.InvocationInfo.MyCommand.Name;
            }
            return frame.FunctionName;
        }

        /// <summary>
        /// Gets the log severity colors.
        /// </summary>
        private static readonly IReadOnlyList<IReadOnlyDictionary<string, ConsoleColor>> LogSeverityColors = new List<IReadOnlyDictionary<string, ConsoleColor>>()
        {
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Green }, { "BackgroundColor", ConsoleColor.Black } }),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor>()),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Yellow }, { "BackgroundColor", ConsoleColor.Black } }),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Red }, { "BackgroundColor", ConsoleColor.Black } })
        }.AsReadOnly();

        /// <summary>
        /// Gets the log severity names.
        /// </summary>
        private static readonly IReadOnlyList<string> LogSeverityNames = new List<string>(["Success", "Info", "Warning", "Error"]).AsReadOnly();

        /// <summary>
        /// Gets the Write-LogEntry delegate script block.
        /// </summary>
        private static readonly ScriptBlock WriteLogEntryDelegate = ScriptBlock.Create("$colours = $args[1]; $args[0].Replace(\"`0\", $null) | & $Script:CommandTable.'Write-ADTLogEntryToOutputStream' @colours -Source $args[2] -Format $args[3] -Verbose:($args[4])");

        /// <summary>
        /// Gets the current process ID.
        /// </summary>
        private static readonly int PID = Process.GetCurrentProcess().Id;

        /// <summary>
        /// Gets the session caller's username.
        /// </summary>
        private static readonly string Username = WindowsIdentity.GetCurrent().Name;

        /// <summary>
        /// Gets the session's default log file encoding.
        /// </summary>
        private static readonly UTF8Encoding LogEncoding = new UTF8Encoding(true);
    }
}

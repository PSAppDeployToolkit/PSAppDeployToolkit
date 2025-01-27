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
            // Determine whether we're able to log to disk.
            bool canLogToDisk = !string.IsNullOrWhiteSpace(logFileDirectory) && !string.IsNullOrWhiteSpace(logFileName);

            // Perform early return checks before wasting time.
            if ((!canLogToDisk && hostLogStream.Equals(HostLogStream.None)) || (debugMessage && !(bool)((Hashtable)InternalDatabase.GetConfig()["Toolkit"]!)["LogDebugMessage"]!))
            {
                return;
            }

            // Establish logging date/time vars.
            DateTime dateNow = DateTime.Now;
            string logTime = dateNow.ToString("HH\\:mm\\:ss.fff");
            CallStackFrame invoker = GetLogEntryCaller(InternalDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Get-PSCallStack'"), null).Skip(1).Select(static o => (CallStackFrame)o.BaseObject).ToArray());

            // Determine the log file name; either a proper script/function, or a caller directly from the console.
            string logFile = !string.IsNullOrWhiteSpace(invoker.ScriptName) ? invoker.ScriptName : invoker.GetScriptLocation();

            // Set up default values if not specified.
            if (null == severity)
            {
                severity = 1;
            }
            if (string.IsNullOrWhiteSpace(source))
            {
                source = GetPowerShellCallStackFrameCommand(invoker);
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
            StringDictionary logFormats = new StringDictionary()
            {
                { "Legacy", $"[{dateNow.ToString("O")}]{(null != scriptSection ? $" [{scriptSection}]" : null)} [{source}] [{LogSeverityNames[(int)severity]}] :: {{0}}".Replace("{", "{{").Replace("}", "}}").Replace("{{0}}", "{0}") },
                { "CMTrace", $"<![LOG[{(null != scriptSection ? $"[{scriptSection}] :: " : null)}{{0}}]LOG]!><time=\"{logTime}{LogTimeOffset}\" date=\"{dateNow.ToString("M-dd-yyyy")}\" component=\"{source}\" context=\"{Username}\" type=\"{severity}\" thread=\"{PID}\" file=\"{logFile}\">".Replace("{", "{{").Replace("}", "}}").Replace("{{0}}", "{0}") },
            };

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
                            logFileWriter.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg.Contains((char)10) ? (string.Join("\n", msg.Replace("\r", null).Trim().Replace(' ', (char)0x2008).Split((char)10).Select(static m => Regex.Replace(m, "^$", $"{(char)0x2008}"))).Replace("\n", "\r\n") + "\r\n") : msg))));
                            break;
                        case "Legacy":
                            logFileWriter.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg))));
                            break;
                    }
                }
            }

            // Write out all messages to host if configured/permitted to do so.
            if (!hostLogStream.Equals(HostLogStream.None))
            {
                ReadOnlyDictionary<string, ConsoleColor> sevCols = LogSeverityColors[(int)severity];
                if (hostLogStream.Equals(HostLogStream.Console))
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
                        Console.Error.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg))));
                    }
                    else
                    {
                        Console.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg))));
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
        private static readonly ReadOnlyCollection<ReadOnlyDictionary<string, ConsoleColor>> LogSeverityColors = new(new[]
        {
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Green }, { "BackgroundColor", ConsoleColor.Black } }),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor>()),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Yellow }, { "BackgroundColor", ConsoleColor.Black } }),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Red }, { "BackgroundColor", ConsoleColor.Black } })
        });

        /// <summary>
        /// Gets the log severity names.
        /// </summary>
        private static readonly ReadOnlyCollection<string> LogSeverityNames = new(["Success", "Info", "Warning", "Error"]);

        /// <summary>
        /// Gets the Write-LogEntry delegate script block.
        /// </summary>
        private static readonly ScriptBlock WriteLogEntryDelegate = ScriptBlock.Create("$colours = $args[1]; $args[0] | & $Script:CommandTable.'Write-ADTLogEntryToOutputStream' @colours -Source $args[2] -Format $args[3] -Verbose:($args[4])");

        /// <summary>
        /// Gets the current timezone bias for the CMTrace log formatted string.
        /// </summary>
        private static readonly string LogTimeOffset = TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes >= 0 ? $"+{TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes}" : TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes.ToString();

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

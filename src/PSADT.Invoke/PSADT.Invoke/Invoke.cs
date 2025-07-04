using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Principal;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using PSADT.Invoke.LibraryInterfaces;
using PSADT.Invoke.Utilities;

namespace PSADT.Invoke
{
    /// <summary>
    /// A utility class to invoke a PowerShell script.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The entry point for the application.
        /// </summary>
        /// <param name="args"></param>
        private static int Main(string[] args)
        {
            // Configure debug mode if /Debug is specified.
            var cliArguments = args.ToList().ConvertAll(x => x.Trim());
            ConfigureDebugMode(cliArguments);

            // Announce commencement and begin.
            WriteDebugMessage($"Preparing for PSAppDeployToolkit invocation.");
            int exitCode = 0;
            try
            {
                // Establish the PowerShell process start information.
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = GetPowerShellPath(cliArguments),
                    Arguments = GetPowerShellArguments(cliArguments),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = currentPath,
                    RedirectStandardOutput = inDebugMode,
                    RedirectStandardError = inDebugMode,
                    UseShellExecute = !inDebugMode,
                    CreateNoWindow = true,
                };
                WriteDebugMessage($"PowerShell Path: [{processStartInfo.FileName}]");
                WriteDebugMessage($"PowerShell Args: [{processStartInfo.Arguments}]");
                WriteDebugMessage($"Working Directory: [{processStartInfo.WorkingDirectory}]");

                // Null out PSModulePath to prevent any module conflicts.
                // https://github.com/PowerShell/PowerShell/issues/18530#issuecomment-1325691850
                Environment.SetEnvironmentVariable("PSModulePath", null);

                // Invoke the given script as per the StartInfo.
                try
                {
                    using (var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = inDebugMode })
                    {
                        // Redirect the output and error streams if we're debugging, then start.
                        if (inDebugMode)
                        {
                            process.ErrorDataReceived += (sender, e) =>
                            {
                                if (!string.IsNullOrWhiteSpace(e.Data))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Error.WriteLine(e.Data);
                                    Console.ResetColor();
                                }
                            };
                            process.OutputDataReceived += (sender, e) =>
                            {
                                if (!string.IsNullOrWhiteSpace(e.Data))
                                {
                                    Console.WriteLine(e.Data);
                                }
                            };
                        }
                        WriteDebugMessage($"Commencing invocation.\n");
                        process.Start();

                        // If we're debugging, begin reading the output and error streams, then exit with the process's exit code.
                        if (inDebugMode)
                        {
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();
                        }
                        process.WaitForExit();
                        exitCode = process.ExitCode;
                    }
                }
                catch (Exception ex)
                {
                    WriteDebugMessage(ex.Message, MsgBoxStyle.Critical);
                    exitCode = 60011;
                }
            }
            catch (Exception ex)
            {
                WriteDebugMessage(ex.Message, MsgBoxStyle.Critical);
                exitCode = 60010;
            }
            finally
            {
                CloseDebugMode();
            }
            return exitCode;
        }

        /// <summary>
        /// Writes a debug message to the log file and optionally displays an error message.
        /// </summary>
        /// <param name="debugMessage"></param>
        /// <param name="messageBoxStyle"></param>
        private static void WriteDebugMessage(string debugMessage, MsgBoxStyle messageBoxStyle = MsgBoxStyle.Information)
        {
            // Determine whether this is an error message or not.
            bool isError = messageBoxStyle != MsgBoxStyle.Information;
            string logMessage = debugMessage.Replace("\n\n", " ");

            // Output to the log file.
            using (StreamWriter sw = new StreamWriter(logPath, true, LogEncoding))
            {
                sw.WriteLine(logMessage.Trim());
            }

            // Write to the console if we have one, otherwise display a modal dialog.
            if (inDebugMode)
            {
                if (isError)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(logMessage);
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine(logMessage);
                }
            }
            else if (isError)
            {
                User32.SetProcessDPIAware();
                Interaction.MsgBox(debugMessage, messageBoxStyle | MsgBoxStyle.SystemModal, $"{assemblyName} {assemblyVersion}");
            }
        }

        /// <summary>
        /// Configures the debug mode based on the command line arguments.
        /// </summary>
        /// <param name="cliArguments"></param>
        private static void ConfigureDebugMode(List<string> cliArguments)
        {
            if (cliArguments.Exists(x => x.Equals("/Debug", StringComparison.OrdinalIgnoreCase)))
            {
                if (!inDebugMode)
                {
                    inDebugMode = Kernel32.AllocConsole();
                }
                cliArguments.RemoveAll(x => x.Equals("/Debug", StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Pauses the console and waits for a key press to exit.
        /// </summary>
        private static void CloseDebugMode()
        {
            Console.WriteLine("\nPress any key to exit...");
            try
            {
                Kernel32.GetConsoleWindow(); Console.ReadKey();
                Kernel32.FreeConsole();
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Gets the path to the PowerShell executable.
        /// </summary>
        /// <param name="cliArguments"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private static string GetPowerShellPath(List<string> cliArguments)
        {
            // Confirm /32 and /Core both haven't been passed as it's not supported.
            bool x32Specified = cliArguments.Exists(x => x.Equals("/32", StringComparison.OrdinalIgnoreCase));
            bool coreSpecified = cliArguments.Exists(x => x.Equals("/Core", StringComparison.OrdinalIgnoreCase));
            if (x32Specified && coreSpecified)
            {
                throw new ArgumentException("The use of both [/32] and [/Core] on the command line is not supported.");
            }

            // Check if we're using PowerShell Core (7).
            string pwshExecutablePath = pwshDefaultPath;
            if (coreSpecified)
            {
                if (!(Environment.GetEnvironmentVariable("PATH").Split(';').Where(p => File.Exists(Path.Combine(p, "pwsh.exe"))).Select(p => Path.Combine(p, "pwsh.exe")).FirstOrDefault() is string pwshCorePath))
                {
                    throw new InvalidOperationException("The [/Core] parameter was specified, but PowerShell Core was not found on this system.");
                }
                WriteDebugMessage("The [/Core] parameter was specified on the command line. Running using PowerShell 7...");
                cliArguments.RemoveAll(x => x.Equals("/Core", StringComparison.OrdinalIgnoreCase));
                pwshExecutablePath = pwshCorePath;
            }

            // Check if x86 PowerShell mode was specified on command line.
            if (x32Specified)
            {
                // Remove the /32 command line argument so that it is not passed to PowerShell script
                WriteDebugMessage("The [/32] parameter was specified on the command line. Running in forced x86 PowerShell mode...");
                cliArguments.RemoveAll(x => x.Equals("/32", StringComparison.OrdinalIgnoreCase));
                if (RuntimeInformation.OSArchitecture.ToString().EndsWith("64"))
                {
                    pwshExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), @"WindowsPowerShell\v1.0\PowerShell.exe");
                }
            }

            // If the PowerShell mode hasn't been explicitly specified, override it if PowerShell Core (7) is a parent process.
            if (pwshExecutablePath.Equals(pwshDefaultPath))
            {
                foreach (var parentProcess in ProcessUtilities.GetParentProcesses())
                {
                    if (parentProcess.ProcessName.Equals("pwsh"))
                    {
                        return parentProcess.MainModule.FileName;
                    }
                }
            }
            return pwshExecutablePath;
        }

        /// <summary>
        /// Determines whether the current process is elevated.
        /// </summary>
        /// <returns></returns>
        private static bool IsElevated()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Gets the arguments to pass to PowerShell.
        /// </summary>
        /// <param name="cliArguments"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        private static string GetPowerShellArguments(List<string> cliArguments)
        {
            // Check for the App Deploy Script file being specified.
            if (cliArguments.Exists(x => x.StartsWith("-Command", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("The [-Command] parameter was specified on the command line. Please use the [-File] parameter instead, which will properly handle exit codes with PowerShell 3.0 and higher.");
            }

            // Determine the path to the script to invoke.
            string adtFrontendPath = Path.Combine(currentPath, $"{assemblyName}.ps1");
            var fileIndex = Array.FindIndex(cliArguments.ToArray(), x => x.Equals("-File", StringComparison.OrdinalIgnoreCase));
            if (fileIndex != -1)
            {
                adtFrontendPath = cliArguments[fileIndex + 1].Replace("\"", null);
                if (!Path.IsPathRooted(adtFrontendPath))
                {
                    adtFrontendPath = Path.Combine(currentPath, adtFrontendPath);
                }
                cliArguments.RemoveAt(fileIndex + 1);
                cliArguments.RemoveAt(fileIndex);
                WriteDebugMessage("The [-File] parameter was specified on command line. Passing command line untouched...");
            }
            else if (cliArguments.Exists(x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)))
            {
                adtFrontendPath = cliArguments.Find(x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)).Replace("\"", null);
                if (!Path.IsPathRooted(adtFrontendPath))
                {
                    adtFrontendPath = Path.Combine(currentPath, adtFrontendPath);
                }
                cliArguments.RemoveAt(cliArguments.FindIndex(x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)));
                WriteDebugMessage("Using script (.ps1) file directly specified on the command line...");
            }
            else
            {
                WriteDebugMessage($"Using default script path [{adtFrontendPath}]...");
            }

            // Verify if the App Deploy script file exists.
            if (!File.Exists(adtFrontendPath))
            {
                throw new FileNotFoundException($"A critical component of PSAppDeployToolkit is missing.\n\nUnable to find the App Deploy Script file at [{adtFrontendPath}].\n\nPlease ensure you have all of the required files available to start the installation.");
            }

            // Add the frontend script file to the arguments (Note that -File has been removed to resolve an issue with WDAC and Constrained Language Mode).
            string pwshArguments = pwshDefaultArgs + $" -Command & '{adtFrontendPath}'";
            if (cliArguments.Count > 0)
            {
                pwshArguments += $" {string.Join(" ", cliArguments)}";
            }
            return pwshArguments + "; exit $Global:LASTEXITCODE";
        }

        /// <summary>
        /// Determines if the application is in debug mode.
        /// </summary>
        private static bool inDebugMode = Debugger.IsAttached;

        /// <summary>
        /// The default path to PowerShell.
        /// </summary>
        private static readonly string pwshDefaultPath = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\PowerShell.exe");

        /// <summary>
        /// The default arguments to pass to PowerShell.
        /// </summary>
        private static readonly string pwshDefaultArgs = "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo";

        /// <summary>
        /// The current path of the executing assembly.
        /// </summary>
        private static readonly string currentPath = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// Represents the file path of the assembly containing the <see cref="Program"/> class.
        /// </summary>
        private static readonly string assemblyLocation = typeof(Program).Assembly.Location;

        /// <summary>
        /// The name of the executing assembly.
        /// </summary>
        private static readonly string assemblyName = Path.GetFileNameWithoutExtension(assemblyLocation);

        /// <summary>
        /// The version of the executing assembly.
        /// </summary>
        private static readonly Version assemblyVersion = new(FileVersionInfo.GetVersionInfo(assemblyLocation).ProductVersion.Split('+')[0]);

        /// <summary>
        /// The path to the logging directory.
        /// </summary>
        private static readonly string logDir = Directory.CreateDirectory(Path.Combine(Path.Combine(Environment.GetFolderPath(IsElevated() ? Environment.SpecialFolder.Windows : Environment.SpecialFolder.CommonApplicationData), "Logs"), $"{assemblyName}.exe")).FullName;

        /// <summary>
        /// The path to the log file.
        /// </summary>
        private static readonly string logFile = $"{assemblyName}.exe_{DateTime.Now.ToString("O").Split('.')[0].Replace(":", null)}.log";

        /// <summary>
        /// The full path to the log file.
        /// </summary>
        private static readonly string logPath = Path.Combine(logDir, logFile);

        /// <summary>
        /// The encoding to use for the log file.
        /// </summary>
        private static readonly Encoding LogEncoding = new UTF8Encoding(true);
    }
}

using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        /// <param name="argv"></param>
        private static int Main(string[] argv)
        {
            // Configure debug mode if /Debug is specified.
            var cliArguments = argv.ToList().ConvertAll(static x => x.Trim());
            ConfigureDebugMode(cliArguments);

            // Announce commencement and begin.
            WriteDebugMessage($"Preparing for PSAppDeployToolkit invocation.");
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
                        return process.ExitCode;
                    }
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Error launching [{processStartInfo.FileName} {processStartInfo.Arguments}].";
                    WriteDebugMessage($"{errorMessage} {ex}", true);
                    if (!inDebugMode)
                    {
                        Environment.FailFast($"{errorMessage}\nException Info: {ex}", ex);
                    }
                    return 60011;
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error while preparing to invoke deployment script.";
                WriteDebugMessage($"{errorMessage} {ex}", true);
                if (!inDebugMode)
                {
                    Environment.FailFast($"{errorMessage}\nException Info: {ex}", ex);
                }
                return 60010;
            }
            finally
            {
                CloseDebugMode();
            }
        }

        /// <summary>
        /// Writes a debug message to the log file and optionally displays an error message.
        /// </summary>
        /// <param name="debugMessage"></param>
        private static void WriteDebugMessage(string debugMessage, bool isError = false)
        {
            // Log only when we're in debug mode.
            if (inDebugMode)
            {
                if (isError)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(debugMessage);
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine(debugMessage);
                }
            }
        }

        /// <summary>
        /// Configures the debug mode based on the command line arguments.
        /// </summary>
        /// <param name="cliArguments"></param>
        private static void ConfigureDebugMode(List<string> cliArguments)
        {
            if (cliArguments.Exists(static x => x.Equals("/Debug", StringComparison.OrdinalIgnoreCase)))
            {
                if (!inDebugMode && Environment.UserInteractive)
                {
                    inDebugMode = Kernel32.AllocConsole();
                }
                cliArguments.RemoveAll(static x => x.Equals("/Debug", StringComparison.OrdinalIgnoreCase));
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
            bool x32Specified = cliArguments.Exists(static x => x.Equals("/32", StringComparison.OrdinalIgnoreCase));
            bool coreSpecified = cliArguments.Exists(static x => x.Equals("/Core", StringComparison.OrdinalIgnoreCase));
            if (x32Specified && coreSpecified)
            {
                throw new ArgumentException("The use of both [/32] and [/Core] on the command line is not supported.");
            }

            // Check if we're using PowerShell Core (7).
            string pwshExecutablePath = pwshDefaultPath;
            if (coreSpecified)
            {
                if (!(Environment.GetEnvironmentVariable("PATH").Split(';').Where(static p => File.Exists(Path.Combine(p, "pwsh.exe"))).Select(static p => Path.Combine(p, "pwsh.exe")).FirstOrDefault() is string pwshCorePath))
                {
                    throw new InvalidOperationException("The [/Core] parameter was specified, but PowerShell Core was not found on this system.");
                }
                WriteDebugMessage("The [/Core] parameter was specified on the command line. Running using PowerShell 7...");
                cliArguments.RemoveAll(static x => x.Equals("/Core", StringComparison.OrdinalIgnoreCase));
                pwshExecutablePath = pwshCorePath;
            }

            // Check if x86 PowerShell mode was specified on command line.
            if (x32Specified)
            {
                // Remove the /32 command line argument so that it is not passed to PowerShell script
                WriteDebugMessage("The [/32] parameter was specified on the command line. Running in forced x86 PowerShell mode...");
                cliArguments.RemoveAll(static x => x.Equals("/32", StringComparison.OrdinalIgnoreCase));
                if (RuntimeInformation.OSArchitecture.ToString().EndsWith("64"))
                {
                    pwshExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), @"WindowsPowerShell\v1.0\PowerShell.exe");
                }
            }

            // If the PowerShell mode hasn't been explicitly specified, override it if PowerShell Core (7) is a parent process.
            if (pwshExecutablePath == pwshDefaultPath)
            {
                foreach (var parentProcess in ProcessUtilities.GetParentProcesses())
                {
                    if (parentProcess.ProcessName == "pwsh")
                    {
                        return parentProcess.MainModule.FileName;
                    }
                }
            }
            return pwshExecutablePath;
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
            if (cliArguments.Exists(static x => x.StartsWith("-Command", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("The [-Command] parameter was specified on the command line. Please use the [-File] parameter instead, which will properly handle exit codes with PowerShell 3.0 and higher.");
            }

            // Determine the path to the script to invoke.
            string adtFrontendPath = Path.Combine(currentPath, $"{assemblyName}.ps1");
            var fileIndex = Array.FindIndex(cliArguments.ToArray(), static x => x.Equals("-File", StringComparison.OrdinalIgnoreCase));
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
            else if (cliArguments.Exists(static x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)))
            {
                adtFrontendPath = cliArguments.Find(static x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)).Replace("\"", null);
                if (!Path.IsPathRooted(adtFrontendPath))
                {
                    adtFrontendPath = Path.Combine(currentPath, adtFrontendPath);
                }
                cliArguments.RemoveAt(cliArguments.FindIndex(static x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)));
                WriteDebugMessage("Using script (.ps1) file directly specified on the command line...");
            }
            else
            {
                WriteDebugMessage($"Using default script path [{adtFrontendPath}]...");
            }

            // Verify if the App Deploy script file exists.
            if (!File.Exists(adtFrontendPath))
            {
                throw new FileNotFoundException($"Unable to find the deployment script file at [{adtFrontendPath}].");
            }

            // Return the full arguments we give to PowerShell.exe (Note that we use -Command resolve issues with WDAC and Constrained Language Mode).
            return $"{pwshDefaultArgs} -Command try {{ & '{adtFrontendPath}'{(cliArguments.Count > 0 ? $" {string.Join(" ", cliArguments)}" : null)} }} catch {{ throw }}; exit $Global:LASTEXITCODE";
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
        /// The name of the executing assembly.
        /// </summary>
        private static readonly string assemblyName = Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location);
    }
}

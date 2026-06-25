using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.Invoke
{
    /// <summary>
    /// Provides the application entry point and supporting methods for launching a PowerShell deployment script with
    /// configurable command-line arguments, debug support, and environment preparation.
    /// </summary>
    /// <remarks>This class is responsible for orchestrating the invocation of a PowerShell-based deployment
    /// script, including argument parsing, debug mode management, and process execution. It handles special
    /// command-line options such as "/Debug", "/32", and "/Core" to control script execution behavior and environment.
    /// Debug mode enables additional diagnostic output and console interaction. The class also manages error handling
    /// and exit codes to signal specific failure scenarios to callers.</remarks>
    internal static class Program
    {
        /// <summary>
        /// Serves as the application entry point, launching the PowerShell deployment script with the specified
        /// command-line arguments.
        /// </summary>
        /// <remarks>If debug mode is enabled via command-line arguments, additional diagnostic output is
        /// written to the console, and standard output and error streams from the PowerShell process are redirected. In
        /// the event of a critical error outside of debug mode, the process terminates immediately using
        /// Environment.FailFast. Exit codes 60010 and 60011 indicate specific failure scenarios during preparation or
        /// script launch, respectively.</remarks>
        /// <param name="argv">An array of command-line arguments to configure the deployment process and script invocation. Arguments may
        /// include options such as debug mode or script path.</param>
        /// <returns>An integer exit code indicating the result of the deployment operation. Returns 0 for success, or a nonzero
        /// value if an error occurs.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the PowerShell process fails to start or if specified command-line arguments are invalid. The exception message provides details about the failure.</exception>
        private static int Main(string[] argv)
        {
            // Internal worker to prevent access to array-based argv.
            static int MainImpl(List<string> argv)
            {
                // Configure debug mode if /Debug is specified, then commence.
                ConfigureDebugMode(argv);
                try
                {
                    // Display help if being asked to do so.
                    if (argv.Contains("/?", StringComparer.Ordinal) || argv.Contains("/Help", StringComparer.OrdinalIgnoreCase))
                    {
                        WriteHelpInformation();
                        return 1;
                    }

                    // Establish the PowerShell process start information.
                    WriteDebugMessage("Preparing for PSAppDeployToolkit invocation.");
                    ProcessStartInfo processStartInfo = new()
                    {
                        FileName = GetPowerShellPath(argv),
                        Arguments = GetPowerShellArguments(argv),
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
                    Environment.SetEnvironmentVariable("PSModulePath", value: null);

                    // Invoke the given script as per the StartInfo.
                    try
                    {
                        // Redirect the output and error streams if we're debugging, then start.
                        using Process process = new() { StartInfo = processStartInfo, EnableRaisingEvents = inDebugMode };
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
                        WriteDebugMessage("Commencing invocation.\n");
                        if (!process.Start())
                        {
                            throw new InvalidOperationException("Failed to start the PowerShell process.");
                        }

                        // If we're debugging, begin reading the output and error streams, then exit with the process's exit code.
                        if (inDebugMode)
                        {
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();
                        }
                        process.WaitForExit();
                        return process.ExitCode;
                    }
                    catch (Exception ex) when (ex.Message is not null)
                    {
                        string errorMessage = $"Error launching [{processStartInfo.FileName} {processStartInfo.Arguments}].";
                        WriteDebugMessage($"{errorMessage} {ex}", isError: true);
                        if (!inDebugMode)
                        {
                            Environment.FailFast($"{errorMessage}\nException Info: {ex}", ex);
                        }
                        return 60011;
                    }
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    const string errorMessage = "Error while preparing to invoke deployment script.";
                    WriteDebugMessage($"{errorMessage} {ex}", isError: true);
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

            // Clean up any input and invoke the main implementation.
            return MainImpl([.. argv.Select(static x => x.Trim())]);
        }

        /// <summary>
        /// Enables debug mode if the "/Debug" command-line argument is present and removes it from the argument list.
        /// </summary>
        /// <remarks>Debug mode is enabled only if the application is running in an interactive user
        /// environment. This method modifies the provided argument list by removing all instances of the "/Debug"
        /// argument, regardless of case.</remarks>
        /// <param name="argv">The list of command-line arguments to inspect and modify. Cannot be null.</param>
        private static void ConfigureDebugMode(List<string> argv)
        {
            if (!argv.Exists(static x => x.Equals("/Debug", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            if (!inDebugMode && Environment.UserInteractive)
            {
                inDebugMode = NativeMethods.AllocConsole();
            }
            _ = argv.RemoveAll(static x => x.Equals("/Debug", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Writes a debug message to the console output or error stream when debug mode is enabled.
        /// </summary>
        /// <remarks>This method has no effect if debug mode is not enabled. When isError is set to true,
        /// the message is written to the error stream with red text to indicate an error condition.</remarks>
        /// <param name="debugMessage">The message to write to the console. This value is displayed only if debug mode is active.</param>
        /// <param name="isError">true to write the message to the error stream in red; otherwise, false to write to the standard output. The
        /// default is false.</param>
        private static void WriteDebugMessage(string debugMessage, bool isError = false)
        {
            // Log only when we're in debug mode.
            if (!inDebugMode)
            {
                return;
            }
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

        /// <summary>
        /// Closes the debug console window and waits for a key press before exiting the application.
        /// </summary>
        /// <remarks>This method is intended for use in debugging scenarios where a console window is
        /// attached to the application. It prompts the user to press any key before releasing the console, allowing
        /// time to review output before the window closes.</remarks>
        private static void CloseDebugMode()
        {
            Console.WriteLine("\nPress any key to exit...");
            try
            {
                _ = NativeMethods.GetConsoleWindow(); _ = Console.ReadKey();
                _ = NativeMethods.FreeConsole();
            }
            catch
            {
                return;
                throw;
            }
        }

        /// <summary>
        /// Determines the appropriate PowerShell executable path based on the specified command-line arguments.
        /// </summary>
        /// <remarks>If neither "/32" nor "/Core" is specified, and the parent process is PowerShell Core,
        /// the method returns the path of the parent process's executable. The method modifies <paramref
        /// name="argv"/> by removing any recognized mode arguments to prevent them from being passed to the
        /// PowerShell script.</remarks>
        /// <param name="argv">A list of command-line arguments that may include PowerShell mode specifiers such as "/32" for x86 mode or
        /// "/Core" for PowerShell Core. The list is modified to remove any recognized mode arguments.</param>
        /// <returns>The full file system path to the selected PowerShell executable. Returns the path for PowerShell Core if
        /// "/Core" is specified, the x86 Windows PowerShell path if "/32" is specified, or the default PowerShell path
        /// otherwise.</returns>
        /// <exception cref="ArgumentException">Thrown if both "/32" and "/Core" arguments are present in <paramref name="argv"/>, as this
        /// combination is not supported.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the "/Core" argument is specified but PowerShell Core is not found on the system.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "It's OK here as we don't have access to our library.")]
        private static string GetPowerShellPath(List<string> argv)
        {
            // Confirm /32 and /Core both haven't been passed as it's not supported.
            bool x32Specified = argv.Exists(static x => x.Equals("/32", StringComparison.OrdinalIgnoreCase));
            bool coreSpecified = argv.Exists(static x => x.Equals("/Core", StringComparison.OrdinalIgnoreCase));
            if (x32Specified && coreSpecified)
            {
                throw new ArgumentException("The use of both [/32] and [/Core] on the command line is not supported.", nameof(argv));
            }

            // Check if we're using PowerShell Core (7).
            string pwshExecutablePath = pwshDefaultPath;
            if (coreSpecified)
            {
                if (Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator).Where(static p => File.Exists($"{p.TrimEnd('\\')}{Path.DirectorySeparatorChar}pwsh.exe")).Select(static p => $"{p.TrimEnd('\\')}{Path.DirectorySeparatorChar}pwsh.exe").FirstOrDefault() is not string pwshCorePath)
                {
                    throw new InvalidOperationException("The [/Core] parameter was specified, but PowerShell Core was not found on this system.");
                }
                WriteDebugMessage("The [/Core] parameter was specified on the command line. Running using PowerShell 7...");
                _ = argv.RemoveAll(static x => x.Equals("/Core", StringComparison.OrdinalIgnoreCase));
                pwshExecutablePath = pwshCorePath;
            }

            // Check if x86 PowerShell mode was specified on command line.
            if (x32Specified)
            {
                // Remove the /32 command line argument so that it is not passed to PowerShell script
                WriteDebugMessage("The [/32] parameter was specified on the command line. Running in forced x86 PowerShell mode...");
                _ = argv.RemoveAll(static x => x.Equals("/32", StringComparison.OrdinalIgnoreCase));
                if (RuntimeInformation.OSArchitecture.ToString().EndsWith("64", StringComparison.OrdinalIgnoreCase))
                {
                    pwshExecutablePath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)}{Path.DirectorySeparatorChar}WindowsPowerShell\v1.0\PowerShell.exe";
                }
            }

            // If the PowerShell mode hasn't been explicitly specified, override it if PowerShell Core (7) is a parent process.
            return pwshExecutablePath.Equals(pwshDefaultPath, StringComparison.OrdinalIgnoreCase) && GetParentProcesses().FirstOrDefault(static p => p.ProcessName.Equals("pwsh", StringComparison.OrdinalIgnoreCase)) is Process parentProcess
                ? parentProcess.MainModule.FileName
                : pwshExecutablePath;
        }

        /// <summary>
        /// Builds the full argument string to invoke PowerShell with the specified script and command-line arguments,
        /// ensuring correct handling of script file resolution and exit codes.
        /// </summary>
        /// <remarks>This method enforces the use of the -File parameter (or direct script file reference)
        /// instead of -Command to ensure compatibility with PowerShell 3.0 and higher, particularly for correct exit
        /// code propagation. The returned argument string wraps script invocation in a try/catch block to preserve
        /// error handling semantics.</remarks>
        /// <param name="argv">The list of command-line arguments to be passed to the PowerShell script. Must not include the -Command
        /// parameter. The list might be modified by this method.</param>
        /// <returns>A string containing the complete set of arguments to be supplied to PowerShell.exe, including the script
        /// path and any additional arguments.</returns>
        /// <exception cref="ArgumentException">Thrown if the -Command parameter is present in the <paramref name="argv"/> list. Use the -File
        /// parameter instead to ensure proper exit code handling.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified PowerShell script file cannot be found at the resolved path.</exception>
        private static string GetPowerShellArguments(List<string> argv)
        {
            // Check for the App Deploy Script file being specified.
            if (argv.Exists(static x => x.StartsWith("-Command", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("The [-Command] parameter was specified on the command line. Please use the [-File] parameter instead, which will properly handle exit codes with PowerShell 3.0 and higher.", nameof(argv));
            }

            // Determine the path to the script to invoke.
            string adtFrontendPath = $"{currentPath}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(AssemblyInfo.Location)}.ps1";
            int fileIndex = Array.FindIndex(argv.ToArray(), static x => x.Equals("-File", StringComparison.OrdinalIgnoreCase));
            if (fileIndex != -1)
            {
                adtFrontendPath = argv[fileIndex + 1].Replace("\"", newValue: null);
                if (!Path.IsPathRooted(adtFrontendPath))
                {
                    adtFrontendPath = $"{currentPath}{Path.DirectorySeparatorChar}{adtFrontendPath}";
                }
                argv.RemoveAt(fileIndex + 1);
                argv.RemoveAt(fileIndex);
                WriteDebugMessage("The [-File] parameter was specified on command line. Passing command line untouched...");
            }
            else if (argv.Exists(static x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)))
            {
                adtFrontendPath = argv.Find(static x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)).Replace("\"", newValue: null);
                if (!Path.IsPathRooted(adtFrontendPath))
                {
                    adtFrontendPath = $"{currentPath}{Path.DirectorySeparatorChar}{adtFrontendPath}";
                }
                argv.RemoveAt(argv.FindIndex(static x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)));
                WriteDebugMessage("Using script (.ps1) file directly specified on the command line...");
            }
            else
            {
                WriteDebugMessage($"Using default script path [{adtFrontendPath}]...");
            }

            // Verify if the App Deploy script file exists.
            if (!File.Exists(adtFrontendPath))
            {
                throw new FileNotFoundException($"Unable to find the deployment script file at [{adtFrontendPath}].", adtFrontendPath);
            }

            // Return the full arguments we give to PowerShell.exe (Note that we use -Command resolve issues with WDAC and Constrained Language Mode).
            return $"{pwshDefaultArgs} -Command \"try {{ & '{adtFrontendPath}'{(argv.Count > 0 ? $" {string.Join(" ", argv)}" : null)} }} catch {{ throw }}; exit $Global:LASTEXITCODE\"";
        }

        /// <summary>
        /// Retrieves a read-only collection containing the parent processes of the current process, ordered from
        /// immediate parent up the process hierarchy.
        /// </summary>
        /// <remarks>The returned collection does not include the current process itself. The order of the
        /// collection starts with the immediate parent and proceeds up the process tree. If a parent process cannot be
        /// accessed or does not exist, the collection may be truncated.</remarks>
        /// <returns>A read-only collection of <see cref="Process"/> objects representing the parent processes
        /// of the current process. The collection is empty if no parent processes can be determined.</returns>
        private static ReadOnlyCollection<Process> GetParentProcesses()
        {
            // Internal method to get the parent process of a given process.
            static int GetParentProcessId(int processId)
            {
                using SafeFileHandle hProcess = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, (uint)processId);
                _ = NativeMethods.NtQueryInformationProcess(hProcess, out PROCESS_BASIC_INFORMATION pbi);
                return (int)pbi.InheritedFromUniqueProcessId;
            }

            // Build a list of parent processes and return it to the caller.
            int processId = (int)PInvoke.GetCurrentProcessId();
            List<Process> processes = [];
            List<int> processesIds = [];
            while (true)
            {
                // Attempt to get the parent process ID. If this fails (e.g., process has exited or can't access parent), break the loop.
                try
                {
                    processId = GetParentProcessId(processId);
                }
                catch
                {
                    break;
                    throw;
                }

                // Check for circular reference to prevent infinite loop in case of unexpected system behavior.
                if (processesIds.Contains(processId))
                {
                    break;
                }
                processesIds.Add(processId);

                // Attempt to get the Process object for the parent process. If this fails (e.g., process has exited), break the loop.
                try
                {
                    processes.Add(Process.GetProcessById(processId));
                }
                catch (ArgumentException)
                {
                    break;
                }
            }
            return processes.AsReadOnly();
        }

        /// <summary>
        /// Displays the help message and exits the application.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when assembly information cannot be retrieved.</exception>
        private static void WriteHelpInformation()
        {
            // Set up the help information then display a modal message box if not in debug mode, otherwise write to the console.
            string helpVersion = AssemblyInfo.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? throw new InvalidOperationException("Failed to retrieve assembly version information.");
            string helpTitle = $"{AssemblyInfo.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? throw new InvalidOperationException("Failed to retrieve assembly title information.")} {new Version(helpVersion.Substring(0, helpVersion.IndexOf('+')))}";
            string helpMessage = string.Join(Environment.NewLine,
            [
                helpTitle,
                "",
                AssemblyInfo.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? throw new InvalidOperationException("Failed to retrieve assembly copyright information."),
                "",
                "Usage:",
                "",
                "  Invoke-AppDeployToolkit.exe",
                "",
                "  Invoke-AppDeployToolkit.exe [-DeploymentScriptParameter]",
                "",
                "  Invoke-AppDeployToolkit.exe [/32] [/File <FileName>] [/Debug] [-DeploymentScriptParameter]",
                "",
                "  Invoke-AppDeployToolkit.exe [/Core] [/File <FileName>] [/Debug] [-DeploymentScriptParameter]",
                "",
                "Available Options:",
                "",
                "  /32",
                "  Forces the deployment to use a 32-bit Windows PowerShell instance on 64-bit systems.",
                "",
                "  /Core",
                "  Forces the deployment to use PowerShell 7, throwing if PowerShell 7 is not installed.",
                "",
                "  /File",
                "  Specifies a PowerShell script file to run. By default, a script named after the executable is used.",
                "",
                "  /Debug",
                "  Allocates a console for debugging purposes. Do not use this switch on production deployments.",
                "",
                "  -DeploymentScriptParameter",
                "  Zero or more parameters to pass to the deployment script.",
                "",
                "  /?, /Help",
                "  Displays this help message.",
            ]);
            if (!inDebugMode)
            {
                _ = PInvoke.SetProcessDPIAware(); _ = NativeMethods.MessageBox(hWnd: null, helpMessage, helpTitle, MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_DEFBUTTON1 | MESSAGEBOX_STYLE.MB_TASKMODAL | MESSAGEBOX_STYLE.MB_SETFOREGROUND | MESSAGEBOX_STYLE.MB_ICONINFORMATION);
            }
            else
            {
                WriteDebugMessage($"{helpMessage}\n");
            }
        }

        /// <summary>
        /// Determines if the application is in debug mode.
        /// </summary>
        private static bool inDebugMode = Debugger.IsAttached;

        /// <summary>
        /// The <see cref="Assembly"/> containing the <see cref="Program"/> type.
        /// </summary>
        private static readonly Assembly AssemblyInfo = typeof(Program).Assembly;

        /// <summary>
        /// The current path of the executing assembly.
        /// </summary>
        private static readonly string currentPath = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// The default path to PowerShell.
        /// </summary>
        private static readonly string pwshDefaultPath = $@"{Environment.SystemDirectory}{Path.DirectorySeparatorChar}WindowsPowerShell\v1.0\PowerShell.exe";

        /// <summary>
        /// The default arguments to pass to PowerShell.
        /// </summary>
        private const string pwshDefaultArgs = "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo";
    }
}

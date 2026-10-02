using System;
using System.Collections.Generic;
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
        /// <remarks>If debug mode is enabled via command-line arguments, additional diagnostic output is written
        /// to the console, and the PowerShell process runs in that same console. In the event of a critical error
        /// outside of debug mode, the process terminates immediately using Environment.FailFast. Exit codes 60010
        /// and 60011 indicate specific failure scenarios during preparation or script launch, respectively.</remarks>
        /// <param name="argv">An array of command-line arguments to configure the deployment process and script invocation. Arguments may
        /// include options such as debug mode or script path.</param>
        /// <returns>An integer exit code indicating the result of the deployment operation. Returns 0 for success, or a nonzero
        /// value if an error occurs.</returns>
        /// <exception cref="InvalidOperationException">Thrown if specified command-line arguments are invalid. The exception message provides details about the failure.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "This executable stands alone and does not reference PSADT, so the wrapper is not available to it.")]
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

                    // Establish the PowerShell path and arguments.
                    WriteDebugMessage("Preparing for PSAppDeployToolkit invocation.");
                    string fileName = GetPowerShellPath(argv);
                    string arguments = GetPowerShellArguments(argv);
                    WriteDebugMessage($"PowerShell Path: [{fileName}]");
                    WriteDebugMessage($"PowerShell Args: [{arguments}]");
                    WriteDebugMessage($"Working Directory: [{currentPath}]");

                    // Null out PSModulePath to prevent any module conflicts.
                    // https://github.com/PowerShell/PowerShell/issues/18530#issuecomment-1325691850
                    Environment.SetEnvironmentVariable("PSModulePath", value: null);

                    // Invoke the given script.
                    WriteDebugMessage("Commencing invocation.\n");
                    try
                    {
                        // Run PowerShell in our console if debugging gave us one, otherwise without a console window.
                        STARTUPINFOW startupInfo = new() { cb = (uint)Marshal.SizeOf<STARTUPINFOW>() };
                        PROCESS_CREATION_FLAGS creationFlags = !inDebugMode || PInvoke.GetConsoleWindow().IsNull ? PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW : 0;
                        _ = NativeMethods.CreateProcess(fileName, $"\"{fileName}\" {arguments}\0".ToCharArray(), bInheritHandles: false, creationFlags, currentPath, in startupInfo, out PROCESS_INFORMATION pi);
                        using SafeProcessHandle hProcess = new(pi.hProcess, ownsHandle: true);
                        using SafeWaitHandle hThread = new(pi.hThread, ownsHandle: true);
                        _ = NativeMethods.WaitForSingleObject(hProcess, PInvoke.INFINITE);
                        _ = NativeMethods.GetExitCodeProcess(hProcess, out uint exitCode);
                        return unchecked((int)exitCode);
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Error launching [{fileName} {arguments}].";
                        WriteDebugMessage($"{errorMessage} {ex}", isError: true);
                        if (!inDebugMode)
                        {
                            Environment.FailFast($"{errorMessage}{Environment.NewLine}Exception Info: {ex}", ex);
                        }
                        return 60011;
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    const string errorMessage = "Error while preparing to invoke deployment script.";
                    WriteDebugMessage($"{errorMessage} {ex}", isError: true);
                    if (!inDebugMode)
                    {
                        Environment.FailFast($"{errorMessage}{Environment.NewLine}Exception Info: {ex}", ex);
                    }
                    return 60010;
                    throw;
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
        /// environment. The launcher then ignores Ctrl+C and Ctrl+Break, which still reach the PowerShell process
        /// sharing its console, even if the launcher was started ignoring Ctrl+C. This method modifies the provided
        /// argument list by removing all instances of the "/Debug" argument, regardless of case.</remarks>
        /// <param name="argv">The list of command-line arguments to inspect and modify. Cannot be null.</param>
        private static void ConfigureDebugMode(List<string> argv)
        {
            if (!argv.Exists(static x => x.Equals("/Debug", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            if (!inDebugMode && Environment.UserInteractive)
            {
                // PowerShell shares this console, so leave Ctrl+C and Ctrl+Break to it and keep waiting for its exit code.
                // A parent may have started us ignoring Ctrl+C, which PowerShell would inherit, so undo that too.
                try
                {
                    inDebugMode = NativeMethods.AllocConsole(); Console.CancelKeyPress += IgnoreCancelKeyPress;
                }
                catch (Exception ex)
                {
                    Environment.FailFast("Failed to allocate a console for debug mode.", ex);
                    throw;
                }
                _ = NativeMethods.SetConsoleCtrlHandler(HandlerRoutine: null, Add: false);
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
        /// <remarks>This method has no effect if debug mode is not enabled. It is intended for use in debugging scenarios
        /// where a console window is attached to the application. It prompts the user to press any key before releasing
        /// the console, allowing time to review output before the window closes.</remarks>
        private static void CloseDebugMode()
        {
            // Prompt only when we're in debug mode.
            if (!inDebugMode)
            {
                return;
            }
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
            finally
            {
                Console.CancelKeyPress -= IgnoreCancelKeyPress;
            }
        }

        /// <summary>
        /// Determines the appropriate PowerShell executable path based on the specified command-line arguments.
        /// </summary>
        /// <remarks>If neither "/32" nor "/Core" is specified, and an ancestor process is PowerShell Core,
        /// the method returns the path of the nearest such ancestor's executable. The method modifies <paramref
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
                if (Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator).Where(static p => File.Exists(Path.Join(p, "pwsh.exe"))).Select(static p => Path.Join(p, "pwsh.exe")).FirstOrDefault() is not string pwshCorePath)
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
                WriteDebugMessage("The [/32] parameter was specified on the command line. Running in forced x86 PowerShell mode...");
                _ = argv.RemoveAll(static x => x.Equals("/32", StringComparison.OrdinalIgnoreCase));
                if (RuntimeInformation.OSArchitecture.ToString().EndsWith("64", StringComparison.Ordinal))
                {
                    pwshExecutablePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), @"WindowsPowerShell\v1.0\PowerShell.exe");
                }
            }

            // If no mode was specified, follow a PowerShell Core (7) ancestor when there is one.
            return x32Specified || coreSpecified || GetParentProcessPaths().FirstOrDefault(static p => Path.GetFileNameWithoutExtension(p).Equals("pwsh", StringComparison.OrdinalIgnoreCase)) is not string parentPath
                ? pwshExecutablePath
                : parentPath;
        }

        /// <summary>
        /// Builds the full argument string to invoke PowerShell with the specified script and command-line arguments,
        /// ensuring correct handling of script file resolution and exit codes.
        /// </summary>
        /// <remarks>The script path is taken from a "-File" argument, a first argument ending in ".ps1", or a default beside
        /// the executable. It is run through -Command rather than -File to work under WDAC and Constrained Language Mode,
        /// wrapped in a try/catch that propagates the script's exit code. Each remaining argument that contains
        /// whitespace is single-quoted so its value is not split into separate tokens.</remarks>
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
            string adtFrontendPath = Path.Join(currentPath, $"{Path.GetFileNameWithoutExtension(AssemblyInfo.Location)}.ps1");
            int fileIndex = argv.FindIndex(static x => x.Equals("-File", StringComparison.OrdinalIgnoreCase));
            if (fileIndex != -1)
            {
                if (fileIndex + 1 >= argv.Count)
                {
                    throw new ArgumentException("The [-File] parameter was specified without a file path.", nameof(argv));
                }
                adtFrontendPath = argv[fileIndex + 1].Replace("\"", newValue: null);
                if (!Path.IsPathRooted(adtFrontendPath))
                {
                    adtFrontendPath = Path.Join(currentPath, adtFrontendPath);
                }
                argv.RemoveAt(fileIndex + 1);
                argv.RemoveAt(fileIndex);
                WriteDebugMessage("The [-File] parameter was specified on command line. Passing command line untouched...");
            }
            else if (argv.Count > 0 && (argv[0].EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || argv[0].EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)))
            {
                adtFrontendPath = argv[0].Replace("\"", newValue: null);
                if (!Path.IsPathRooted(adtFrontendPath))
                {
                    adtFrontendPath = Path.Join(currentPath, adtFrontendPath);
                }
                argv.RemoveAt(0);
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
        /// Retrieves the executable paths of the current process's ancestors, from its parent upwards.
        /// </summary>
        /// <remarks>The walk ends at the first parent that cannot be queried, or that started after its child, as a process
        /// given the ID of a parent that has since exited does.</remarks>
        /// <returns>The executable path of each ancestor, nearest first. The list is empty if no parent can be determined.</returns>
        private static IEnumerable<string> GetParentProcessPaths()
        {
            // Internal method to open a process for the queries made of it.
            static SafeFileHandle OpenForQuery(uint processId)
            {
                return NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, processId);
            }

            // Internal method to get when a process started, and the ID of the process that started it.
            static (System.Runtime.InteropServices.ComTypes.FILETIME CreationTime, uint ParentProcessId) QueryProcess(SafeHandle hProcess)
            {
                _ = NativeMethods.GetProcessTimes(hProcess, out System.Runtime.InteropServices.ComTypes.FILETIME creationTime, out _, out _, out _);
                _ = NativeMethods.NtQueryInformationProcess(hProcess, out PROCESS_BASIC_INFORMATION pbi);
                return (creationTime, (uint)pbi.InheritedFromUniqueProcessId);
            }

            // Internal method to get the executable path of a process.
            static string GetExecutablePath(SafeFileHandle hProcess)
            {
                char[] exeName = new char[short.MaxValue];
                uint size = (uint)exeName.Length;
                _ = NativeMethods.QueryFullProcessImageName(hProcess, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, exeName, ref size);
                return new(exeName, 0, (int)size);
            }

            // Walk up from this process, stopping at a parent that can't be queried, was seen already, or started after its child.
            using SafeProcessHandle hProcess = NativeMethods.GetCurrentProcess(); uint processId = PInvoke.GetCurrentProcessId();
            (System.Runtime.InteropServices.ComTypes.FILETIME childCreationTime, uint parentProcessId) = QueryProcess(hProcess);
            HashSet<uint> processIds = [processId];
            while (processIds.Add(parentProcessId))
            {
                System.Runtime.InteropServices.ComTypes.FILETIME creationTime;
                uint grandparentProcessId;
                string executablePath;
                try
                {
                    using SafeFileHandle hParent = OpenForQuery(parentProcessId);
                    (creationTime, grandparentProcessId) = QueryProcess(hParent);
                    if (PInvoke.CompareFileTime(in creationTime, in childCreationTime) > 0)
                    {
                        break;
                    }
                    executablePath = GetExecutablePath(hParent);
                }
                catch
                {
                    break;
                    throw;
                }
                yield return executablePath;
                parentProcessId = grandparentProcessId;
                childCreationTime = creationTime;
            }
        }

        /// <summary>
        /// Displays the help message and exits the application.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when assembly information cannot be retrieved.</exception>
        private static void WriteHelpInformation()
        {
            // Set up the help information then display a modal message box if not in debug mode, otherwise write to the console.
            string helpVersion = AssemblyInfo.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? throw new InvalidOperationException("Failed to retrieve assembly version information.");
            string helpTitle = $"{AssemblyInfo.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? throw new InvalidOperationException("Failed to retrieve assembly title information.")} {new Version(helpVersion.Split('+')[0])}";
            string helpMessage = string.Join(
                Environment.NewLine,
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
                "  Invoke-AppDeployToolkit.exe [/Debug] [/32] [-File <FileName>] [-DeploymentScriptParameter]",
                "",
                "  Invoke-AppDeployToolkit.exe [/Debug] [/Core] [-File <FileName>] [-DeploymentScriptParameter]",
                "",
                "Available Options:",
                "",
                "  /Debug",
                "  Allocates a console for debugging purposes. Do not use this switch on production deployments.",
                "",
                "  /32",
                "  Forces the deployment to use a 32-bit Windows PowerShell instance on 64-bit systems.",
                "",
                "  /Core",
                "  Forces the deployment to use PowerShell 7, throwing if PowerShell 7 is not installed.",
                "",
                "  -File",
                "  Specifies a PowerShell script file to run. By default, a script named after the executable is used.",
                "",
                "  -DeploymentScriptParameter",
                "  Zero or more parameters to pass to the deployment script.",
                "",
                "  /?, /Help",
                "  Displays this help message.");
            if (!inDebugMode && Environment.UserInteractive)
            {
                _ = PInvoke.SetProcessDPIAware(); _ = NativeMethods.MessageBox(hWnd: null, helpMessage, helpTitle, MESSAGEBOX_STYLE.MB_TASKMODAL | MESSAGEBOX_STYLE.MB_SETFOREGROUND | MESSAGEBOX_STYLE.MB_ICONINFORMATION);
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
        /// Ignores Ctrl+C and Ctrl+Break in debug mode, leaving them to the PowerShell process sharing the console.
        /// </summary>
        private static readonly ConsoleCancelEventHandler IgnoreCancelKeyPress = static (sender, e) => e.Cancel = true;

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
        private static readonly string pwshDefaultPath = Path.Join(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\PowerShell.exe");

        /// <summary>
        /// The default arguments to pass to PowerShell.
        /// </summary>
        private const string pwshDefaultArgs = "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo";
    }
}

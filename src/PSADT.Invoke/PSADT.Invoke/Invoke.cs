using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Language;
using Microsoft.VisualBasic;

namespace PSADT
{
    /// <summary>
    /// A utility class to invoke a PowerShell script.
    /// </summary>
    internal static class Invoke
    {
        /// <summary>
        /// The entry point for the application.
        /// </summary>
        /// <param name="args"></param>
        private static void Main(string[] args)
        {
            // Flag whether we're debugging or not.
            var cliArguments = args.ToList().ConvertAll(x => x.Trim());
            if (cliArguments.Exists(x => x.Equals("/Debug", StringComparison.OrdinalIgnoreCase)))
            {
                if (!inDebugMode)
                {
                    inDebugMode = ConsoleUtils.AllocConsole();
                }
                cliArguments.RemoveAll(x => x.Equals("/Debug", StringComparison.OrdinalIgnoreCase));
            }

            // Announce commencement and begin.
            WriteDebugMessage($"Preparing for PSAppDeployToolkit invocation.");
            try
            {
                // Establish the PowerShell process start information.
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = GetPowerShellPath(cliArguments),
                    Arguments = GetPowerShellArguments(cliArguments),
                    WorkingDirectory = currentPath,
                    RedirectStandardOutput = inDebugMode,
                    RedirectStandardError = inDebugMode,
                    UseShellExecute = !inDebugMode,
                    CreateNoWindow = inDebugMode,
                };
                if (RequireElevation() && (Environment.OSVersion.Version.Major >= 6))
                {
                    processStartInfo.Verb = "runas";
                }
                WriteDebugMessage($"PowerShell Path: [{processStartInfo.FileName}]");
                WriteDebugMessage($"PowerShell Args: [{processStartInfo.Arguments}]");
                WriteDebugMessage($"Working Directory: [{processStartInfo.WorkingDirectory}]");
                WriteDebugMessage($"Requires Admin: [{processStartInfo.Verb == "runas"}]");

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
                        ExitProcess(process.ExitCode);
                    }
                }
                catch (Exception ex)
                {
                    WriteDebugMessage(ex.Message, MsgBoxStyle.Critical);
                    ExitProcess(60011);
                }
            }
            catch (Exception ex)
            {
                WriteDebugMessage(ex.Message, MsgBoxStyle.Critical);
                ExitProcess(60010);
            }
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

            // Write to the console if we have one, otherwise display a modal dialog.
            if (inDebugMode)
            {
                // Strip any forced line breaks from the string.
                // These are only to make the GUI look nicer.
                debugMessage = debugMessage.Replace("\n\n", " ");
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
            else if (isError)
            {
                Interaction.MsgBox(debugMessage, messageBoxStyle | MsgBoxStyle.SystemModal, $"{assemblyName} {assemblyVersion}");
            }
        }

        /// <summary>
        /// Gets the PSModulePath environment variable values.
        /// </summary>
        /// <returns></returns>
        private static string[] GetPSModulePaths()
        {
            using (var rs = RunspaceFactory.CreateRunspace())
            {
                rs.Open(); return ((string)rs.SessionStateProxy.GetVariable("env:PSModulePath")).Split(';');
            }
        }

        /// <summary>
        /// Gets the path to the PSAppDeployToolkit module.
        /// </summary>
        /// <returns></returns>
        private static string GetToolkitPath()
        {
            // Check if there's a PSAppDeployToolkit module path three levels up (we're developing).
            if (Directory.Exists(devToolkitPath))
            {
                return devToolkitPath;
            }

            // Check if there's a PSAppDeployToolkit module path in the expected v4 directory.
            if (Directory.Exists(v4ToolkitPath))
            {
                return v4ToolkitPath;
            }

            // Check if there's a PSAppDeployToolkit module path in the expected v3 directory.
            if (Directory.Exists(v3ToolkitPath))
            {
                return v3ToolkitPath;
            }

            // Check if there's a PSAppDeployToolkit module path in the PSModulePath environment variable.
            foreach (var psModulePath in GetPSModulePaths())
            {
                var validPath = Path.Combine(psModulePath, "PSAppDeployToolkit");
                if (Directory.Exists(validPath))
                {
                    return validPath;
                }
            }
            throw new DirectoryNotFoundException($"A critical component of PSAppDeployToolkit is missing.\n\nUnable to find the [PSAppDeployToolkit] module directory.\n\nPlease ensure you have all of the required files available to start the installation.");
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
                if (NativeSystemInfo.GetNativeSystemInfo().wProcessorArchitecture.ToString().EndsWith("64"))
                {
                    pwshExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "WindowsPowerShell\\v1.0\\PowerShell.exe");
                }
            }

            // If the PowerShell mode hasn't been explicitly specified, override it with a PowerShell parent process if available.
            if (pwshExecutablePath.Equals(pwshDefaultPath))
            {
                foreach (var parentProcess in ParentProcessUtilities.GetParentProcesses())
                {
                    if (parentProcess.ProcessName.Equals("pwsh") || parentProcess.ProcessName.Equals("powershell"))
                    {
                        return parentProcess.MainModule.FileName;
                    }
                }
            }
            return pwshExecutablePath;
        }

        /// <summary>
        /// Determines whether the script requires elevation.
        /// </summary>
        /// <param name="toolkitPath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        private static bool RequireElevation()
        {
            // Test whether we've got a local config before continuing.
            if ((Path.Combine(currentPath, "Config\\config.psd1") is string adtLocalConfigPath) && File.Exists(adtLocalConfigPath))
            {
                // Test the file for validity prior to just blindly using it.
                var localConfigAst = Parser.ParseFile(adtLocalConfigPath, out Token[] localConfigTokens, out ParseError[] localConfigErrors);
                if (localConfigErrors.Length > 0)
                {
                    throw new InvalidDataException($"A critical component of PSAppDeployToolkit is corrupt.\n\nUnable to parse the [config.psd1] file at [{adtLocalConfigPath}].\n\nPlease review your configuration to ensure it's correct before starting the installation.");
                }

                // Test that the local config is a hashtable.
                if ((localConfigAst.Find(p => p.GetType() == typeof(HashtableAst), false) is HashtableAst localConfig) && (((Hashtable)localConfig.SafeGetValue())["Toolkit"] is Hashtable localConfigToolkit) && (localConfigToolkit["RequireAdmin"] is bool requireAdmin))
                {
                    if (requireAdmin)
                    {
                        return true;
                    }
                    return false;
                }
            }

            // Verify if the PSAppDeployToolkit config file exists.
            var adtConfigPath = Path.Combine(currentPath, $"{GetToolkitPath()}\\Config\\config.psd1");
            if (!File.Exists(adtConfigPath))
            {
                throw new FileNotFoundException($"A critical component of PSAppDeployToolkit is missing.\n\nUnable to find the [config.psd1] file at [{adtConfigPath}].\n\nPlease ensure you have all of the required files available to start the installation.");
            }

            // Parse our config and throw if we have any errors.
            var configAst = Parser.ParseFile(adtConfigPath, out Token[] configTokens, out ParseError[] configErrors);
            if (configErrors.Length > 0)
            {
                throw new InvalidDataException($"A critical component of PSAppDeployToolkit is corrupt.\n\nUnable to parse the [config.psd1] file at [{adtConfigPath}].\n\nPlease review your configuration to ensure it's correct before starting the installation.");
            }

            // Determine whether we require admin rights or not.
            if ((bool)((Hashtable)((Hashtable)configAst.EndBlock.Find(p => p.GetType() == typeof(HashtableAst), false).SafeGetValue())["Toolkit"])["RequireAdmin"])
            {
                return true;
            }
            return false;
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
                WriteDebugMessage("'-File' parameter specified on command line. Passing command line untouched...");
            }
            else if (cliArguments.Exists(x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)))
            {
                adtFrontendPath = cliArguments.Find(x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)).Replace("\"", null);
                if (!Path.IsPathRooted(adtFrontendPath))
                {
                    adtFrontendPath = Path.Combine(currentPath, adtFrontendPath);
                }
                cliArguments.RemoveAt(cliArguments.FindIndex(x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)));
                WriteDebugMessage("Script (.ps1) file directly specified on command line.");
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
            return pwshArguments + "; [System.Environment]::Exit($Global:LASTEXITCODE)";
        }

        /// <summary>
        /// Exits the process with the specified exit code.
        /// </summary>
        /// <param name="exitcode"></param>
        private static void ExitProcess(int exitcode)
        {
            if (inDebugMode)
            {
                Console.WriteLine("\nPress any key to exit...");
                if (ConsoleUtils.GetConsoleWindow() != IntPtr.Zero)
                {
                    Console.ReadKey();
                }
                inDebugMode = !ConsoleUtils.FreeConsole();
            }
            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Determines if the application is in debug mode.
        /// </summary>
        private static bool inDebugMode = System.Diagnostics.Debugger.IsAttached;

        /// <summary>
        /// The default path to PowerShell.
        /// </summary>
        private static readonly string pwshDefaultPath = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell\\v1.0\\PowerShell.exe");

        /// <summary>
        /// The default arguments to pass to PowerShell.
        /// </summary>
        private static readonly string pwshDefaultArgs = "-NonInteractive -NoProfile -NoLogo";

        /// <summary>
        /// The current path of the executing assembly.
        /// </summary>
        private static readonly string currentPath = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// The name of the executing assembly.
        /// </summary>
        private static readonly string assemblyName = Process.GetCurrentProcess().ProcessName;

        /// <summary>
        /// The version of the executing assembly.
        /// </summary>
        private static readonly string assemblyVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

        /// <summary>
        /// The path to the PSAppDeployToolkit module.
        /// </summary>
        private static readonly string v3ToolkitPath = Path.Combine(currentPath, "AppDeployToolkit\\PSAppDeployToolkit");

        /// <summary>
        /// The path to the PSAppDeployToolkit module.
        /// </summary>
        private static readonly string v4ToolkitPath = Path.Combine(currentPath, "PSAppDeployToolkit");

        /// <summary>
        /// The path to the PSAppDeployToolkit module.
        /// </summary>
        private static readonly string devToolkitPath = Path.Combine(currentPath, "..\\..\\..\\PSAppDeployToolkit");

        /// <summary>
        /// The encoding to use for the log file.
        /// </summary>
        private static readonly Encoding LogEncoding = new UTF8Encoding(true);
    }

    /// <summary>
    /// A utility class to determine a process parent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ParentProcessUtilities
    {
        // These members must match PROCESS_BASIC_INFORMATION
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Handle);
        }

        /// <summary>
        /// Gets the parent process of specified process.
        /// </summary>
        /// <param name="id">The process id.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(int id)
        {
            Process process = Process.GetProcessById(id);
            return GetParentProcess(process.Handle);
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(IntPtr handle)
        {
            ParentProcessUtilities pbi = new ParentProcessUtilities();
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out var returnLength);
            if (status != 0)
            {
                throw new Win32Exception(status);
            }
            return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
        }

        /// <summary>
        /// Gets a list of all parent processes of this one.
        /// </summary>
        /// <returns>An list of instances of the Process class.</returns>
        public static List<Process> GetParentProcesses()
        {
            List<Process> procs = [];
            var proc = Process.GetCurrentProcess();
            while (true)
            {
                try
                {
                    if (procs.Contains((proc = GetParentProcess(proc.Handle))))
                    {
                        break;
                    }
                    procs.Add(proc);
                }
                catch
                {
                    break;
                }
            }
            return procs;
        }
    }

    /// <summary>
    /// A utility class to determine the system information.
    /// </summary>
    internal static class NativeSystemInfo
    {
        /// <summary>
        /// Processor architecture types supported by Windows.
        /// </summary>
        public enum ProcessorArchitecture : ushort
        {
            PROCESSOR_ARCHITECTURE_INTEL = 0,
            PROCESSOR_ARCHITECTURE_MIPS = 1,
            PROCESSOR_ARCHITECTURE_ALPHA = 2,
            PROCESSOR_ARCHITECTURE_PPC = 3,
            PROCESSOR_ARCHITECTURE_SHX = 4,
            PROCESSOR_ARCHITECTURE_ARM = 5,
            PROCESSOR_ARCHITECTURE_IA64 = 6,
            PROCESSOR_ARCHITECTURE_ALPHA64 = 7,
            PROCESSOR_ARCHITECTURE_MSIL = 8,
            PROCESSOR_ARCHITECTURE_AMD64 = 9,
            PROCESSOR_ARCHITECTURE_IA32_ON_WIN64 = 10,
            PROCESSOR_ARCHITECTURE_NEUTRAL = 11,
            PROCESSOR_ARCHITECTURE_ARM64 = 12,
            PROCESSOR_ARCHITECTURE_ARM32_ON_WIN64 = 13,
            PROCESSOR_ARCHITECTURE_UNKNOWN = 0xFFFF
        }

        /// <summary>
        /// Contains information about the current computer system.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct SYSTEM_INFO
        {
            public ProcessorArchitecture wProcessorArchitecture;
            public ushort wReserved;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public UIntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        }

        /// <summary>
        /// Retrieves information about the current system to an application running under WOW64.
        /// </summary>
        /// <param name="lpSystemInfo"></param>
        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        private static extern void GetNativeSystemInfo(out SYSTEM_INFO lpSystemInfo);

        /// <summary>
        /// Retrieves information about the current system to an application running under WOW64.
        /// </summary>
        /// <returns></returns>
        public static SYSTEM_INFO GetNativeSystemInfo()
        {
            GetNativeSystemInfo(out SYSTEM_INFO sysInfo);
            return sysInfo;
        }
    }

    /// <summary>
    /// A utility class to allocate and free a console window.
    /// </summary>
    internal static class ConsoleUtils
    {
        /// <summary>
        /// Allocates a console to the process.
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AllocConsole();

        /// <summary>
        /// Gets a handle to the allocated console window.
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr GetConsoleWindow();

        /// <summary>
        /// Frees the console allocated to the process.
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeConsole();
    }
}

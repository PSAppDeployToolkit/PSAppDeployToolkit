using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Windows.Forms;

namespace PSADT
{
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

    internal static class NativeSystemInfo
    {
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

        [DllImport("kernel32.dll", SetLastError = false, ExactSpelling = true)]
        private static extern void GetNativeSystemInfo(out SYSTEM_INFO lpSystemInfo);

        public static SYSTEM_INFO GetNativeSystemInfo()
        {
            GetNativeSystemInfo(out SYSTEM_INFO sysInfo);
            return sysInfo;
        }
    }

    internal static class Invoke
    {
        private static readonly string pwshDefaultPath = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell\\v1.0\\PowerShell.exe");
        private static readonly string currentPath = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string assemblyName = Process.GetCurrentProcess().ProcessName;
        private static readonly string v3ToolkitPath = Path.Combine(currentPath, "AppDeployToolkit\\PSAppDeployToolkit");
        private static readonly string v4ToolkitPath = Path.Combine(currentPath, "PSAppDeployToolkit");
        private static readonly string loggingPath = Path.Combine((new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator) ? Environment.GetFolderPath(Environment.SpecialFolder.Windows) : Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Logs");
        private static readonly string timeStamp = DateTime.Now.ToString("O").Split('.')[0].Replace(":", null);
        private static readonly Encoding LogEncoding = new UTF8Encoding(true);

        private static void Main()
        {
            // Set up exit code.
            int exitCode = 60010;
            bool silentMode = false;

            try
            {
                // Set up variables.
                string adtFrontendPath = Path.Combine(currentPath, $"{assemblyName}.ps1");
                string adtToolkitPath = Directory.Exists(v4ToolkitPath) ? v4ToolkitPath : Directory.Exists(v3ToolkitPath) ? v3ToolkitPath : (PowerShell.Create().AddScript("$env:PSModulePath").Invoke().Select(o => o.BaseObject as string).First()!.Split(';').Where(p => Directory.Exists(Path.Combine(p, "PSAppDeployToolkit"))).Select(p => Path.Combine(p, "PSAppDeployToolkit")).FirstOrDefault() is string psGalleryPath) ? psGalleryPath : null;
                string adtConfigPath = Path.Combine(currentPath, $"{adtToolkitPath}\\Config\\config.psd1");
                string pwshExecutablePath = pwshDefaultPath;
                string pwshArguments = "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -WindowStyle Hidden";
                var cliArguments = Environment.GetCommandLineArgs().ToList().ConvertAll(x => x.Trim());
                bool isRequireAdmin = false;

                // Announce commencement
                WriteDebugMessage($"Commencing invocation of {adtFrontendPath}.");

                // Remove first command-line argument as this is always the executable name.
                cliArguments.RemoveAt(0);

                // Confirm /32 and /Core both haven't been passed as it's not supported.
                bool x32Specified = cliArguments.Exists(x => x.Equals("/32", StringComparison.OrdinalIgnoreCase));
                bool coreSpecified = cliArguments.Exists(x => x.Equals("/Core", StringComparison.OrdinalIgnoreCase));
                if (x32Specified && coreSpecified)
                {
                    throw new ArgumentException("The use of both '/32' and '/Core' on the command line is not supported.");
                }

                // Check if we're using PowerShell Core (7).
                if (coreSpecified)
                {
                    if (!(Environment.GetEnvironmentVariable("PATH").Split(';').Where(p => File.Exists(Path.Combine(p, "pwsh.exe"))).Select(p => Path.Combine(p, "pwsh.exe")).FirstOrDefault() is string pwshCorePath))
                    {
                        throw new InvalidOperationException("The '/Core' parameter was specified, but PowerShell Core was not found on this system.");
                    }
                    cliArguments.RemoveAll(x => x.Equals("/Core", StringComparison.OrdinalIgnoreCase));
                    pwshExecutablePath = pwshCorePath;
                }

                // Check if x86 PowerShell mode was specified on command line.
                if (x32Specified)
                {
                    // Remove the /32 command line argument so that it is not passed to PowerShell script
                    WriteDebugMessage("The '/32' parameter was specified on the command line. Running in forced x86 PowerShell mode...");
                    cliArguments.RemoveAll(x => x.Equals("/32", StringComparison.OrdinalIgnoreCase));
                    if (NativeSystemInfo.GetNativeSystemInfo().wProcessorArchitecture.ToString().EndsWith("64"))
                    {
                        pwshExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "WindowsPowerShell\\v1.0\\PowerShell.exe");
                    }
                }

                // If the PowerShell mode hasn't been explicitly specified, override it with a PowerShell parent process if available.
                if (pwshExecutablePath.Equals(pwshDefaultPath))
                {
                    if (ParentProcessUtilities.GetParentProcesses().Where(p => p.ProcessName.Equals("pwsh") || p.ProcessName.Equals("powershell")).Select(p => p.MainModule.FileName).FirstOrDefault() is string pwshParentProcessPath)
                    {
                        pwshExecutablePath = pwshParentProcessPath;
                    }
                }

                // Test whether we're running in silent mode.
                var deployModeIndex = Array.FindIndex(cliArguments.ToArray(), x => x.Equals("-DeployMode", StringComparison.OrdinalIgnoreCase));
                silentMode = (deployModeIndex != -1) && !cliArguments[deployModeIndex + 1].ToLower().Equals("interactive", StringComparison.OrdinalIgnoreCase);
                if (silentMode)
                {
                    WriteDebugMessage("Silent mode detected. No user interaction will be displayed.");
                }

                // Test whether we've got a local config before continuing.
                if ((Path.Combine(currentPath, "Config\\config.psd1") is string adtLocalConfigPath) && File.Exists(adtLocalConfigPath))
                {
                    // Test the file for validity prior to just blindly using it.
                    var localConfigAst = Parser.ParseFile(adtLocalConfigPath, out Token[] localConfigTokens, out ParseError[] localConfigErrors);
                    if (localConfigErrors.Length > 0)
                    {
                        throw new Exception($"A critical component of PSAppDeployToolkit is corrupt.\n\nUnable to parse the 'config.psd1' file at '{adtLocalConfigPath}'.\n\nPlease review your configuration to ensure it's correct before starting the installation.");
                    }

                    // Test that the local config is a hashtable.
                    if ((localConfigAst.Find(p => p.GetType() == typeof(HashtableAst), false) is HashtableAst localConfig) && (((Hashtable)localConfig.SafeGetValue())["Toolkit"] is Hashtable localConfigToolkit) && (null != localConfigToolkit["RequireAdmin"]))
                    {
                        adtConfigPath = adtLocalConfigPath;
                    }
                }

                // Verify if the App Deploy script file exists.
                if (!File.Exists(adtFrontendPath))
                {
                    throw new Exception($"A critical component of PSAppDeployToolkit is missing.\n\nUnable to find the App Deploy Script file at '{adtFrontendPath}'.\n\nPlease ensure you have all of the required files available to start the installation.");
                }

                // Verify if the PSAppDeployToolkit folder exists.
                if (String.IsNullOrWhiteSpace(adtToolkitPath))
                {
                    throw new Exception($"A critical component of PSAppDeployToolkit is missing.\n\nUnable to find the 'PSAppDeployToolkit' module directory'.\n\nPlease ensure you have all of the required files available to start the installation.");
                }

                // Verify if the PSAppDeployToolkit config file exists.
                if (!File.Exists(adtConfigPath))
                {
                    throw new Exception($"A critical component of PSAppDeployToolkit is missing.\n\nUnable to find the 'config.psd1' file at '{adtConfigPath}'.\n\nPlease ensure you have all of the required files available to start the installation.");
                }

                // Parse our config and throw if we have any errors.
                var configAst = Parser.ParseFile(adtConfigPath, out Token[] configTokens, out ParseError[] configErrors);
                if (configErrors.Length > 0)
                {
                    throw new Exception($"A critical component of PSAppDeployToolkit is corrupt.\n\nUnable to parse the 'config.psd1' file at '{adtConfigPath}'.\n\nPlease review your configuration to ensure it's correct before starting the installation.");
                }

                // Determine whether we require admin rights or not.
                if (isRequireAdmin = (bool)((Hashtable)((Hashtable)configAst.EndBlock.Find(p => p.GetType() == typeof(HashtableAst), false).SafeGetValue())["Toolkit"])["RequireAdmin"])
                {
                    WriteDebugMessage("Administrator rights are required. The verb 'RunAs' will be used with the invocation.");
                }

                // Check for the App Deploy Script file being specified.
                if (cliArguments.Exists(x => x.StartsWith("-Command", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new Exception("'-Command' parameter was specified on the command-line. Please use the '-File' parameter instead, which will properly handle exit codes with PowerShell 3.0 and higher.");
                }

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
                    WriteDebugMessage("'-File' parameter specified on command-line. Passing command-line untouched...");
                }
                else if (cliArguments.Exists(x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)))
                {
                    adtFrontendPath = cliArguments.Find(x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)).Replace("\"", null);
                    if (!Path.IsPathRooted(adtFrontendPath))
                    {
                        adtFrontendPath = Path.Combine(currentPath, adtFrontendPath);
                    }
                    cliArguments.RemoveAt(cliArguments.FindIndex(x => x.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".ps1\"", StringComparison.OrdinalIgnoreCase)));
                    WriteDebugMessage(".ps1 file specified on command-line. Appending '-File' parameter name...");
                }
                else
                {
                    WriteDebugMessage($"No '-File' parameter specified on command-line. Adding parameter '-File \"{adtFrontendPath}\"'...");
                }

                // Add the frontend script file to the arguments (Note that -File has been removed to resolve an issue with WDAC and Constrained Language Mode).
                pwshArguments += $" \"{adtFrontendPath}\"";
                if (cliArguments.Count > 0)
                {
                    pwshArguments += $" {string.Join(" ", cliArguments)}";
                }

                // Define PowerShell process.
                WriteDebugMessage($"Executable Path: {pwshExecutablePath}");
                WriteDebugMessage($"Arguments: {pwshArguments}");
                WriteDebugMessage($"Working Directory: {currentPath}");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = pwshExecutablePath,
                    Arguments = pwshArguments,
                    WorkingDirectory = currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };

                // Set the RunAs flag if the PSADT configuration file specifically calls for Admin Rights and OS Vista or higher
                if (isRequireAdmin && (Environment.OSVersion.Version.Major >= 6))
                {
                    processStartInfo.Verb = "runas";
                }

                // Start the PowerShell process and wait for completion
                exitCode = 60011;
                var process = new Process();
                try
                {
                    // Null out PSModulePath to prevent any module conflicts.
                    // https://github.com/PowerShell/PowerShell/issues/18530#issuecomment-1325691850
                    Environment.SetEnvironmentVariable("PSModulePath", null);
                    process.StartInfo = processStartInfo;
                    process.Start();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                    if ((exitCode == 1) || (exitCode == 64) || (exitCode == 255) || ((exitCode >= 60000) && (exitCode <= 79999) && (exitCode != 60012)))
                    {
                        throw new ExternalException($"An error occurred while running {Path.GetFileName(adtFrontendPath)}. Exit code: {exitCode}", exitCode);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    process?.Dispose();
                }

                // Exit with the script's code.
                WriteDebugMessage($"Exit Code: {exitCode}");
                Environment.Exit(exitCode);
            }
            catch (Exception ex)
            {
                WriteDebugMessage(ex.Message, !silentMode, MessageBoxIcon.Error);
                Environment.Exit(exitCode);
            }
        }

        private static void WriteDebugMessage(string debugMessage, bool IsDisplayError = false, MessageBoxIcon MsgBoxStyle = MessageBoxIcon.Information)
        {
            // Output to the log file.
            var logPath = Path.Combine(loggingPath, $"{assemblyName}.exe");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            using (StreamWriter sw = new StreamWriter(Path.Combine(logPath, $"{assemblyName}.exe_{timeStamp}.log"), true, LogEncoding))
            {
                sw.WriteLine(debugMessage.Replace("\n", " "));
            }

            // If we are to display an error message...
            if (IsDisplayError)
            {
                try
                {
                    MessageBox.Show(
                        new Form { TopMost = true },
                        debugMessage,
                        $"{Application.ProductName} {Application.ProductVersion}",
                        MessageBoxButtons.OK,
                        MsgBoxStyle,
                        MessageBoxDefaultButton.Button1);
                }
                catch
                {
                    // Do nothing with this.
                }
            }
        }
    }
}

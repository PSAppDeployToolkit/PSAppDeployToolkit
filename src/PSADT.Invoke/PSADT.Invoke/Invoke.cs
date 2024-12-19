using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Windows.Forms;

namespace PSADT
{
    internal static class Invoke
    {
        private enum ProcessorArchitecture : ushort
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
        private struct SYSTEM_INFO
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

        private static bool Is64BitOS()
        {
            GetNativeSystemInfo(out SYSTEM_INFO sysInfo);
            return nameof(sysInfo.wProcessorArchitecture).EndsWith("64");
        }

        private static readonly string currentPath = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string assemblyName = Process.GetCurrentProcess().ProcessName;
        private static readonly string psGalleryPath = PowerShell.Create().AddScript("$env:PSModulePath").Invoke().Select(o => o.BaseObject as string).First().Split(';').Where(p => Directory.Exists(Path.Combine(p, "PSAppDeployToolkit"))).Select(p => Path.Combine(p, "PSAppDeployToolkit")).FirstOrDefault();
        private static readonly string v3ToolkitPath = Path.Combine(currentPath, "AppDeployToolkit\\PSAppDeployToolkit");
        private static readonly string v4ToolkitPath = Path.Combine(currentPath, "PSAppDeployToolkit");
        private static readonly string loggingPath = Path.Combine((new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator) ? Environment.GetFolderPath(Environment.SpecialFolder.Windows) : Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Logs");
        private static readonly string timeStamp = DateTime.Now.ToString("O").Split('.')[0].Replace(":", null);
        private static readonly Encoding LogEncoding = new UTF8Encoding(true);
        private static readonly bool is64BitOS = Is64BitOS();

        private static void Main()
        {
            // Set up exit code.
            int exitCode = 60010;

            try
            {
                // Set up variables.
                string adtFrontendPath = Path.Combine(currentPath, $"{assemblyName}.ps1");
                string adtToolkitPath = Directory.Exists(v4ToolkitPath) ? v4ToolkitPath : Directory.Exists(v3ToolkitPath) ? v3ToolkitPath : !string.IsNullOrWhiteSpace(psGalleryPath) ? psGalleryPath : null;
                string adtConfigPath = Path.Combine(currentPath, $"{adtToolkitPath}\\Config\\config.psd1");
                string pwshExecutablePath = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell\\v1.0\\PowerShell.exe");
                string pwshArguments = "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -WindowStyle Hidden";
                var cliArguments = Environment.GetCommandLineArgs().ToList().ConvertAll(x => x.Trim());
                bool isForceX86Mode = false;
                bool isRequireAdmin = false;

                // Announce commencement
                WriteDebugMessage($"Commencing invocation of {adtFrontendPath}.");

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
                Hashtable toolkitConfig = (Hashtable)((Hashtable)configAst.EndBlock.Find(p => p.GetType() == typeof(HashtableAst), false).SafeGetValue())["Toolkit"];
                if (isRequireAdmin = (bool)toolkitConfig["RequireAdmin"])
                {
                    WriteDebugMessage("Administrator rights are required. The verb 'RunAs' will be used with the invocation.");
                }

                // Remove first command-line argument as this is always the executable name.
                cliArguments.RemoveAt(0);

                // Check if x86 PowerShell mode was specified on command line.
                if (cliArguments.Exists(x => x == "/32"))
                {
                    // Remove the /32 command line argument so that it is not passed to PowerShell script
                    WriteDebugMessage("'/32' parameter was specified on the command-line. Running in forced x86 PowerShell mode...");
                    cliArguments.RemoveAll(x => x == "/32");
                    isForceX86Mode = true;
                }

                // Check for the App Deploy Script file being specified.
                if (cliArguments.Exists(x => x.StartsWith("-Command ")))
                {
                    throw new Exception("'-Command' parameter was specified on the command-line. Please use the '-File' parameter instead, which will properly handle exit codes with PowerShell 3.0 and higher.");
                }

                if (cliArguments.Exists(x => x.StartsWith("-File ")))
                {
                    adtFrontendPath = cliArguments.Find(x => x.StartsWith("-File ")).Replace("-File ", null).Replace("\"", null);
                    if (!Path.IsPathRooted(adtFrontendPath))
                    {
                        adtFrontendPath = Path.Combine(currentPath, adtFrontendPath);
                    }
                    cliArguments.RemoveAt(cliArguments.FindIndex(x => x.StartsWith("-File")));
                    WriteDebugMessage("'-File' parameter specified on command-line. Passing command-line untouched...");
                }
                else if (cliArguments.Exists(x => x.EndsWith(".ps1") || x.EndsWith(".ps1\"")))
                {
                    adtFrontendPath = cliArguments.Find(x => x.EndsWith(".ps1") || x.EndsWith(".ps1\"")).Replace("\"", null);
                    if (!Path.IsPathRooted(adtFrontendPath))
                    {
                        adtFrontendPath = Path.Combine(currentPath, adtFrontendPath);
                    }
                    cliArguments.RemoveAt(cliArguments.FindIndex(x => x.EndsWith(".ps1") || x.EndsWith(".ps1\"")));
                    WriteDebugMessage(".ps1 file specified on command-line. Appending '-Command' parameter name...");
                }
                else
                {
                    WriteDebugMessage($"No '-File' parameter specified on command-line. Adding parameter '-File \"{adtFrontendPath}\"'...");
                }

                // Add the frontend script file to the arguments.
                pwshArguments += $" -File \"{adtFrontendPath}\"";
                if (cliArguments.Count > 0)
                {
                    pwshArguments += $" {string.Join(" ", cliArguments)}";
                }

                // Switch to x86 PowerShell if requested.
                if (is64BitOS && isForceX86Mode)
                {
                    pwshExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "WindowsPowerShell\\v1.0\\PowerShell.exe");
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
                    process.StartInfo = processStartInfo;
                    process.Start();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                    if ((exitCode == 1) || (exitCode == 64) || (exitCode == 255) || ((exitCode >= 60000) && (exitCode <= 79999) && (exitCode != 60012)))
                    {
                        throw new ApplicationException($"An error occurred while running {Path.GetFileName(adtFrontendPath)}. Exit code: {exitCode}");
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
                WriteDebugMessage(ex.Message, true, MessageBoxIcon.Error);
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
                sw.WriteLine(debugMessage);
            }

            // If we are to display an error message...
            if (IsDisplayError)
            {
                try
                {
                    MessageBox.Show(
                        new WindowWrapper(Process.GetCurrentProcess().MainWindowHandle),
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

        private class WindowWrapper : IWin32Window
        {
            public WindowWrapper(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Management.Automation.Language;
using System.Windows.Forms;

namespace PSAppDeployToolkit
{
    static class DeployApplication
    {
        public static void Main()
        {
            // Set up exit code.
            int exitCode = 60010;

            try
            {
                // Set up variables.
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
                string adtFrontendPath = Path.Combine(currentPath, "Deploy-Application.ps1");
                string adtToolkitPath = Path.Combine(currentPath, "AppDeployToolkit\\PSAppDeployToolkit\\PSAppDeployToolkit.psd1");
                string adtConfigPath = Path.Combine(currentPath, "AppDeployToolkit\\PSAppDeployToolkit\\Config\\config.psd1");
                string pwshExecutablePath = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell\\v1.0\\PowerShell.exe");
                string pwshArguments = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden";
                List<string> cliArguments = new List<string>(Environment.GetCommandLineArgs());
                bool isForceX86Mode = false;
                bool isRequireAdmin = false;
                bool is64BitOS = false;

                // Test whether we've got a local config before continuing.
                if (File.Exists(Path.Combine(currentPath, "Config\\config.psd1")))
                {
                    adtConfigPath = Path.Combine(currentPath, "Config\\config.psd1");
                }

                // Get OS Architecture and test whether it ends with 64 or not (covers both x64 and ARM64).
                if (nameof(RuntimeInformation.OSArchitecture).EndsWith("64"))
                {
                    is64BitOS = true;
                }

                // Trim ending & starting empty space from each element in the command-line.
                cliArguments = cliArguments.ConvertAll(s => s.Trim());
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
                    adtFrontendPath = cliArguments.Find(x => x.StartsWith("-File ")).Replace("-File ", string.Empty).Replace("\"", string.Empty);
                    if (!Path.IsPathRooted(adtFrontendPath))
                    {
                        adtFrontendPath = Path.Combine(currentPath, adtFrontendPath);
                    }
                    cliArguments.RemoveAt(cliArguments.FindIndex(x => x.StartsWith("-File")));
                    WriteDebugMessage("'-File' parameter specified on command-line. Passing command-line untouched...");
                }
                else if (cliArguments.Exists(x => x.EndsWith(".ps1") || x.EndsWith(".ps1\"")))
                {
                    adtFrontendPath = cliArguments.Find(x => x.EndsWith(".ps1") || x.EndsWith(".ps1\"")).Replace("\"", string.Empty);
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

                // Define the command line arguments to pass to PowerShell.
                pwshArguments = $"-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File \"{adtFrontendPath}\"";
                if (cliArguments.Count > 0)
                {
                    pwshArguments += " " + string.Join(" ", cliArguments.ToArray());
                }

                // Verify if the App Deploy script file exists.
                if (!File.Exists(adtFrontendPath))
                {
                    throw new Exception($"A critical component of PSAppDeployToolkit is missing.\n\nUnable to find the App Deploy Script file at '{adtFrontendPath}'.\n\nPlease ensure you have all of the required files available to start the installation.");
                }

                // Verify if the App Deploy Toolkit folder exists.
                if (!File.Exists(adtToolkitPath))
                {
                    throw new Exception($"A critical component of PSAppDeployToolkit is missing.\n\nUnable to find the 'PSAppDeployToolkit.psd1' module file at '{adtToolkitPath}'.\n\nPlease ensure you have all of the required files available to start the installation.");
                }

                // Verify if the App Deploy Toolkit Config XML file exists.
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
                Hashtable configTable = (Hashtable)configAst.EndBlock.Find(p => p.GetType() == typeof(HashtableAst), false).SafeGetValue();
                Hashtable toolkitConfig = (Hashtable)configTable["Toolkit"];
                if (isRequireAdmin = (bool)toolkitConfig["RequireAdmin"])
                {
                    WriteDebugMessage("Administrator rights are required...");
                }

                // Switch to x86 PowerShell if requested.
                if (is64BitOS && isForceX86Mode)
                {
                    pwshExecutablePath = Path.Combine(Environment.GetEnvironmentVariable("WinDir"), "SysWOW64\\WindowsPowerShell\\v1.0\\PowerShell.exe");
                }

                // Define PowerShell process.
                WriteDebugMessage("Executable Path: " + pwshExecutablePath);
                WriteDebugMessage("Arguments: " + pwshArguments);
                WriteDebugMessage("Working Directory: " + currentPath);

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
                WriteDebugMessage("Exit Code: " + exitCode);
                Environment.Exit(exitCode);
            }
            catch (Exception ex)
            {
                WriteDebugMessage(ex.Message, true, MessageBoxIcon.Error);
                Environment.Exit(exitCode);
            }
        }

        public static void WriteDebugMessage(string debugMessage = null, bool IsDisplayError = false, MessageBoxIcon MsgBoxStyle = MessageBoxIcon.Information)
        {
            // Output to the Console
            Console.WriteLine(debugMessage);

            // If we are to display an error message...
            if (IsDisplayError)
            {
                MessageBox.Show(
                    new WindowWrapper(Process.GetCurrentProcess().MainWindowHandle),
                    debugMessage,
                    Application.ProductName + " " + Application.ProductVersion,
                    MessageBoxButtons.OK,
                    MsgBoxStyle,
                    MessageBoxDefaultButton.Button1);
            }
        }

        public class WindowWrapper : IWin32Window
        {
            public WindowWrapper(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }
    }
}

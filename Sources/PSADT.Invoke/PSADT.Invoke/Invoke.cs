using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Windows.Forms;

namespace PSADT
{
    internal static class Invoke
    {
        public static void Main()
        {
            // Set up exit code.
            int exitCode = 60010;

            try
            {
                // Set up variables.
                string currentPath = AppDomain.CurrentDomain.BaseDirectory;
                string adtFrontendPath = Path.Combine(currentPath, "Invoke-AppDeployToolkit.ps1");
                string adtToolkitPath = Path.Combine(currentPath, "PSAppDeployToolkit\\PSAppDeployToolkit.psd1");
                string adtConfigPath = Path.Combine(currentPath, "PSAppDeployToolkit\\Config\\Config.psd1");
                string pwshExecutablePath = Path.Combine(Environment.GetEnvironmentVariable("WinDir"), "System32\\WindowsPowerShell\\v1.0\\PowerShell.exe");
                string pwshArguments = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden";
                List<string> cliArguments = new List<string>(Environment.GetCommandLineArgs());
                Boolean isForceX86Mode = false;
                Boolean isRequireAdmin = false;
                StringBuilder stringBuilder = new StringBuilder();

                // Get OS Architecture. Check does not return correct value when running in x86 process on x64 system but it works for our purpose.
                // To get correct OS architecture when running in x86 process on x64 system, we would also have to check environment variable: PROCESSOR_ARCHITEW6432.
                Boolean is64BitOS = false;
                if (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE").Contains("64"))
                    is64BitOS = true;

                // Trim ending & starting empty space from each element in the command-line
                cliArguments = cliArguments.ConvertAll(s => s.Trim());
                // Remove first command-line argument as this is always the executable name
                cliArguments.RemoveAt(0);

                // Check if x86 PowerShell mode was specified on command line
                if (cliArguments.Exists(x => x == "/32"))
                {
                    isForceX86Mode = true;
                    WriteDebugMessage(
                        "'/32' parameter was specified on the command-line. Running in forced x86 PowerShell mode...");
                    // Remove the /32 command line argument so that it is not passed to PowerShell script
                    cliArguments.RemoveAll(x => x == "/32");
                }

                // Check for the App Deploy Script file being specified
                var commandLineAppDeployScriptFileArg = string.Empty;
                var commandLineAppDeployScriptPath = string.Empty;
                if (cliArguments.Exists(x => x.StartsWith("-File ")))
                {
                    throw new Exception(
                        "'-File' parameter was specified on the command-line. Please use the '-Command' parameter instead because using the '-File' parameter can return the incorrect exit code in PowerShell 2.0.");
                }

                if (cliArguments.Exists(x => x.StartsWith("-Command ")))
                {
                    commandLineAppDeployScriptFileArg = cliArguments.Find(x => x.StartsWith("-Command "));
                    adtFrontendPath = commandLineAppDeployScriptFileArg.Replace("-Command ", string.Empty)
                        .Replace("\"", string.Empty);
                    if (!Path.IsPathRooted(adtFrontendPath))
                        adtFrontendPath = Path.Combine(currentPath, adtFrontendPath);
                    cliArguments.RemoveAt(cliArguments.FindIndex(x => x.StartsWith("-Command")));
                    WriteDebugMessage(
                        "'-Command' parameter specified on command-line. Passing command-line untouched...");
                }
                else if (cliArguments.Exists(x => x.EndsWith(".ps1") || x.EndsWith(".ps1\"")))
                {
                    adtFrontendPath = cliArguments.Find(x => x.EndsWith(".ps1") || x.EndsWith(".ps1\""))
                        .Replace("\"", string.Empty);
                    if (!Path.IsPathRooted(adtFrontendPath))
                        adtFrontendPath = Path.Combine(currentPath, adtFrontendPath);
                    cliArguments.RemoveAt(cliArguments.FindIndex(x => x.EndsWith(".ps1") || x.EndsWith(".ps1\"")));
                    WriteDebugMessage(".ps1 file specified on command-line. Appending '-Command' parameter name...");
                }
                else
                {
                    WriteDebugMessage(
                        "No '-Command' parameter specified on command-line. Adding parameter '-Command \"" +
                        adtFrontendPath + "\"'...");
                }

                // Define the command line arguments to pass to PowerShell
                pwshArguments = pwshArguments + " -Command & { & '" + adtFrontendPath + "'";
                if (cliArguments.Count > 0)
                    pwshArguments = pwshArguments + " " + string.Join(" ", cliArguments.ToArray());
                pwshArguments += "; Exit $LastExitCode }";

                // Verify if the App Deploy script file exists
                if (!File.Exists(adtFrontendPath))
                {
                    throw new Exception("A critical component of PSAppDeployToolkit is missing." + Environment.NewLine +
                                        Environment.NewLine + "Unable to find the App Deploy Script file: " +
                                        adtFrontendPath + "." + Environment.NewLine + Environment.NewLine +
                                        "Please ensure you have all of the required files available to start the installation.");
                }

                // Verify if the App Deploy Toolkit folder exists
                if (!File.Exists(adtToolkitPath))
                {
                    throw new Exception("A critical component of PSAppDeployToolkit is missing." + Environment.NewLine +
                                        Environment.NewLine + "Unable to find the 'PSAppDeployToolkit.psd1' module file." +
                                        Environment.NewLine + Environment.NewLine + adtToolkitPath +
                                        "Please ensure you have all of the required files available to start the installation.");
                }

                // Verify if the App Deploy Toolkit Config XML file exists
                if (!File.Exists(adtConfigPath))
                {
                    throw new Exception("A critical component of PSAppDeployToolkit is missing." + Environment.NewLine +
                                        Environment.NewLine + "Unable to find the 'Config.psd1' file." +
                                        Environment.NewLine + Environment.NewLine + adtConfigPath +
                                        "Please ensure you have all of the required files available to start the installation.");
                }

                using (PowerShell psExec = PowerShell.Create())
                {
                    psExec.AddCommand("Import-PowerShellDataFile").AddParameter("Path", adtConfigPath);

                    var results = psExec.Invoke();

                    Collection<ErrorRecord> errors = psExec.Streams.Error.ReadAll();

                    if (errors.Count > 0)
                    {
                        foreach (var error in errors)
                            stringBuilder.AppendLine(error.ToString());
                    }
                    else
                    {
                        if (results[0].Members["Toolkit"].Value is PSObject toolkit)
                        {
                            isRequireAdmin = (bool)toolkit.Members["RequireAdmin"].Value;
                            WriteDebugMessage("RequireAdmin: " + isRequireAdmin);
                        }
                    }
                }

                if (isRequireAdmin) WriteDebugMessage("Administrator rights are required...");

                // Switch to x86 PowerShell if requested
                if (is64BitOS && isForceX86Mode)
                {
                    pwshExecutablePath = Path.Combine(Environment.GetEnvironmentVariable("WinDir"),
                        "SysWOW64\\WindowsPowerShell\\v1.0\\PowerShell.exe");
                }

                // Define PowerShell process
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
                if (isRequireAdmin && (Environment.OSVersion.Version.Major >= 6)) processStartInfo.Verb = "runas";

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
                finally
                {
                    process?.Dispose();
                }

                // Exit
                WriteDebugMessage("Exit Code: " + exitCode);
                Environment.Exit(exitCode);
            }
            catch (Exception ex)
            {
                WriteDebugMessage(ex.Message, true, MessageBoxIcon.Error);
                Environment.Exit(exitCode);
            }
        }

        public static void WriteDebugMessage(string debugMessage = null, bool IsDisplayError = false,
            MessageBoxIcon MsgBoxStyle = MessageBoxIcon.Information)
        {
            // Output to the Console
            Console.WriteLine(debugMessage);

            // If we are to display an error message...
            var handle = Process.GetCurrentProcess().MainWindowHandle;
            if (IsDisplayError)
            {
                MessageBox.Show(new WindowWrapper(handle), debugMessage,
                    Application.ProductName + " " + Application.ProductVersion, MessageBoxButtons.OK, MsgBoxStyle,
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
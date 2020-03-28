using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Windows.Forms;

namespace PSAppDeployToolkit
{
    static class DeployApplication
    {
        public static void Main()
        {
            try
            {
                // Set up variables
                int processExitCode = 60010;
                string currentAppFolder = AppDomain.CurrentDomain.BaseDirectory;
                string appDeployScriptPath = Path.Combine(currentAppFolder, "Deploy-Application.ps1");
                string appDeployToolkitFolder = Path.Combine(currentAppFolder, "AppDeployToolkit");
                string appDeployToolkitXMLPath = Path.Combine(appDeployToolkitFolder, "AppDeployToolkitConfig.xml");
                string powershellExePath = Path.Combine(Environment.GetEnvironmentVariable("WinDir"), "System32\\WindowsPowerShell\\v1.0\\PowerShell.exe");
                string powershellArgs = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden";
                List<string> commandLineArgs = new List<string>(Environment.GetCommandLineArgs());
                bool isForceX86Mode = false;
                bool isRequireAdmin = false;

                // Get OS Architecture. Check does not return correct value when running in x86 process on x64 system but it works for our purpose.
                // To get correct OS architecture when running in x86 process on x64 system, we would also have to check environment variable: PROCESSOR_ARCHITEW6432.
                bool is64BitOS = false;
                if (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE").Contains("64"))
                    is64BitOS = true;

                // Trim ending & starting empty space from each element in the command-line
                commandLineArgs = commandLineArgs.ConvertAll(s => s.Trim());
                // Remove first command-line argument as this is always the executable name
                commandLineArgs.RemoveAt(0);

                // Check if x86 PowerShell mode was specified on command line
                if (commandLineArgs.Exists(x => x == "/32"))
                {
                    isForceX86Mode = true;
                    WriteDebugMessage("'/32' parameter was specified on the command-line. Running in forced x86 PowerShell mode...");
                    // Remove the /32 command line argument so that it is not passed to PowerShell script
                    commandLineArgs.RemoveAll(x => x == "/32");
                }
                
                // Check for the App Deploy Script file being specified
                string commandLineAppDeployScriptFileArg = String.Empty;
                string commandLineAppDeployScriptPath = String.Empty;
                if (commandLineArgs.Exists(x => x.StartsWith("-File ")))
                {
                    throw new Exception("'-File' parameter was specified on the command-line. Please use the '-Command' parameter instead because using the '-File' parameter can return the incorrect exit code in PowerShell 2.0.");
                }
                else if (commandLineArgs.Exists(x => x.StartsWith("-Command ")))
                {
                    commandLineAppDeployScriptFileArg = commandLineArgs.Find(x => x.StartsWith("-Command "));
                    appDeployScriptPath = commandLineAppDeployScriptFileArg.Replace("-Command ", String.Empty).Replace("\"", String.Empty);
                    if (!Path.IsPathRooted(appDeployScriptPath))
                        appDeployScriptPath = Path.Combine(currentAppFolder, appDeployScriptPath);
                    commandLineArgs.RemoveAt(commandLineArgs.FindIndex(x => x.StartsWith("-Command")));
                    WriteDebugMessage("'-Command' parameter specified on command-line. Passing command-line untouched...");
                }
                else if (commandLineArgs.Exists(x => x.EndsWith(".ps1") || x.EndsWith(".ps1\"")))
                {
                    appDeployScriptPath = commandLineArgs.Find(x => x.EndsWith(".ps1") || x.EndsWith(".ps1\"")).Replace("\"", String.Empty);
                    if (!Path.IsPathRooted(appDeployScriptPath))
                        appDeployScriptPath = Path.Combine(currentAppFolder, appDeployScriptPath);
                    commandLineArgs.RemoveAt(commandLineArgs.FindIndex(x => x.EndsWith(".ps1") || x.EndsWith(".ps1\"")));
                    WriteDebugMessage(".ps1 file specified on command-line. Appending '-Command' parameter name...");
                }
                else
                {
                    WriteDebugMessage("No '-Command' parameter specified on command-line. Adding parameter '-Command \"" + appDeployScriptPath + "\"'...");
                }

                // Define the command line arguments to pass to PowerShell
                powershellArgs = powershellArgs + " -Command & { & '" + appDeployScriptPath + "'";
                if (commandLineArgs.Count > 0)
                {
                    powershellArgs = powershellArgs + " " + string.Join(" ", commandLineArgs.ToArray());
                }
                powershellArgs = powershellArgs + "; Exit $LastExitCode }";

                // Verify if the App Deploy script file exists
                if (!File.Exists(appDeployScriptPath))
                {
                    throw new Exception("A critical component of the App Deployment Toolkit is missing." + Environment.NewLine + Environment.NewLine + "Unable to find the App Deploy Script file: " + appDeployScriptPath + "." + Environment.NewLine + Environment.NewLine + "Please ensure you have all of the required files available to start the installation.");
                }

                // Verify if the App Deploy Toolkit folder exists
                if (!Directory.Exists(appDeployToolkitFolder))
                {
                    throw new Exception("A critical component of the App Deployment Toolkit is missing." + Environment.NewLine + Environment.NewLine + "Unable to find the 'AppDeployToolkit' folder." + Environment.NewLine + Environment.NewLine + "Please ensure you have all of the required files available to start the installation.");
                }

                // Verify if the App Deploy Toolkit Config XML file exists
                if (!File.Exists(appDeployToolkitXMLPath))
                {
                    throw new Exception("A critical component of the App Deployment Toolkit is missing." + Environment.NewLine + Environment.NewLine + "Unable to find the 'AppDeployToolkitConfig.xml' file." + Environment.NewLine + Environment.NewLine + "Please ensure you have all of the required files available to start the installation.");
                }
                else
                {
                    // Read the XML and determine whether we need Admin Rights
                    XmlDocument xml = new XmlDocument();
                    xml.Load(appDeployToolkitXMLPath);
                    XmlNode xmlNode = null;
                    XmlElement xmlRoot = xml.DocumentElement;
                    xmlNode = xmlRoot.SelectSingleNode("/AppDeployToolkit_Config/Toolkit_Options/Toolkit_RequireAdmin");
                    isRequireAdmin = Convert.ToBoolean(xmlNode.InnerText);
                    if (isRequireAdmin)
                    {
                        WriteDebugMessage("Administrator rights are required...");
                    }
                }

                // Switch to x86 PowerShell if requested
                if (is64BitOS & isForceX86Mode)
                {
                    powershellExePath = Path.Combine(Environment.GetEnvironmentVariable("WinDir"), "SysWOW64\\WindowsPowerShell\\v1.0\\PowerShell.exe");
                }

                // Define PowerShell process
                WriteDebugMessage("PowerShell Path: " + powershellExePath);
                WriteDebugMessage("PowerShell Parameters: " + powershellArgs);
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = powershellExePath,
                    Arguments = powershellArgs,
                    WorkingDirectory = Path.GetDirectoryName(powershellExePath),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };
                // Set the RunAs flag if the XML specifically calls for Admin Rights and OS Vista or higher
                if (((isRequireAdmin) & (Environment.OSVersion.Version.Major >= 6)))
                {
                    processStartInfo.Verb = "runas";
                }

                // Start the PowerShell process and wait for completion
                processExitCode = 60011;
                Process process = new Process();
                try
                {
                    process.StartInfo = processStartInfo;
                    process.Start();
                    process.WaitForExit();
                    processExitCode = process.ExitCode;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if ((process != null))
                    {
                        process.Dispose();
                    }
                }

                // Exit
                WriteDebugMessage("Exit Code: " + processExitCode);
                Environment.Exit(processExitCode);
            }
            catch (Exception ex)
            {
                WriteDebugMessage(ex.Message, true, MessageBoxIcon.Error);
                Environment.Exit(processExitCode);
            }
        }

        public static void WriteDebugMessage(string debugMessage = null, bool IsDisplayError = false, MessageBoxIcon MsgBoxStyle = MessageBoxIcon.Information)
        {
            // Output to the Console
            Console.WriteLine(debugMessage);

            // If we are to display an error message...
            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
            if (IsDisplayError == true)
            {
                MessageBox.Show(new WindowWrapper(handle), debugMessage, Application.ProductName + " " + Application.ProductVersion, MessageBoxButtons.OK, (MessageBoxIcon)MsgBoxStyle, MessageBoxDefaultButton.Button1);
            }
        }

        public class WindowWrapper : System.Windows.Forms.IWin32Window
        {
            public WindowWrapper(IntPtr handle)
            {
                _hwnd = handle;
            }

            public IntPtr Handle
            {
                get { return _hwnd; }
            }

            private IntPtr _hwnd;
        }

        public static int processExitCode { get; set; }
    }
}

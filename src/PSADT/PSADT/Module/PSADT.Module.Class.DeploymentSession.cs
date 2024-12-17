using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Security.Principal;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using PSADT.Shared;
using PSADT.Types;
using PSADT.WTSSession;

namespace PSADT.Module
{
    public class DeploymentSession
    {
        #region Constructors.


        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentSession"/> class.
        /// </summary>
        /// <param name="parameters">All parameters from Open-ADTSession.</param>
        /// <param name="noExitOnClose">Indicates that the shell shouldn't exit on the last session closure.</param>
        /// <param name="callerSessionState">The caller session state.</param>
        public DeploymentSession(Dictionary<string, object>? parameters = null, bool? noExitOnClose = null, SessionState? callerSessionState = null)
        {
            try
            {
                #region Initialization


                // Establish start date/time first so we can accurately mark the start of execution.
                _currentDate = CurrentDateTime.ToString("dd-MM-yyyy");
                _currentTime = CurrentDateTime.ToString("HH:mm:ss");

                // Establish initial variable values.
                var adtData = InternalDatabase.Get();
                var adtEnv = InternalDatabase.GetEnvironment();
                var adtConfig = InternalDatabase.GetConfig();
                var moduleSessionState = InternalDatabase.GetSessionState();

                // Extrapolate the Toolkit options from the config hashtable.
                var configToolkit = (Hashtable)adtConfig["Toolkit"]!;

                // Set up other variable values based on incoming dictionary.
                if (noExitOnClose.HasValue && noExitOnClose.Value)
                {
                    Settings |= DeploymentSettings.NoExitOnClose;
                }
                if (null != parameters)
                {
                    if (parameters.ContainsKey("DeploymentType"))
                    {
                        _deploymentType = (string)parameters["DeploymentType"];
                    }
                    if (parameters.ContainsKey("DeployMode"))
                    {
                        _deployMode = (string)parameters["DeployMode"];
                    }
                    if (parameters.ContainsKey("AllowRebootPassThru"))
                    {
                        Settings |= DeploymentSettings.AllowRebootPassThru;
                    }
                    if (parameters.ContainsKey("TerminalServerMode"))
                    {
                        Settings |= DeploymentSettings.TerminalServerMode;
                    }
                    if (parameters.ContainsKey("DisableLogging"))
                    {
                        Settings |= DeploymentSettings.DisableLogging;
                    }
                    if (parameters.ContainsKey("AppVendor"))
                    {
                        _appVendor = (string)parameters["AppVendor"];
                    }
                    if (parameters.ContainsKey("AppName"))
                    {
                        _appName = (string)parameters["AppName"];
                    }
                    if (parameters.ContainsKey("AppVersion"))
                    {
                        _appVersion = (string)parameters["AppVersion"];
                    }
                    if (parameters.ContainsKey("AppArch"))
                    {
                        _appArch = (string)parameters["AppArch"];
                    }
                    if (parameters.ContainsKey("AppLang"))
                    {
                        _appLang = (string)parameters["AppLang"];
                    }
                    if (parameters.ContainsKey("AppRevision"))
                    {
                        _appRevision = (string)parameters["AppRevision"];
                    }
                    if (parameters.ContainsKey("AppScriptVersion"))
                    {
                        _appScriptVersion = (Version)parameters["AppScriptVersion"];
                    }
                    if (parameters.ContainsKey("AppScriptDate"))
                    {
                        _appScriptDate = (DateTime)parameters["AppScriptDate"];
                    }
                    if (parameters.ContainsKey("AppScriptAuthor"))
                    {
                        _appScriptAuthor = (string)parameters["AppScriptAuthor"];
                    }
                    if (parameters.ContainsKey("InstallName"))
                    {
                        _installName = (string)parameters["InstallName"];
                    }
                    if (parameters.ContainsKey("InstallTitle"))
                    {
                        _installTitle = (string)parameters["InstallTitle"];
                    }
                    if (parameters.ContainsKey("DeployAppScriptFriendlyName"))
                    {
                        _deployAppScriptFriendlyName = (string)parameters["DeployAppScriptFriendlyName"];
                    }
                    if (parameters.ContainsKey("DeployAppScriptVersion"))
                    {
                        _deployAppScriptVersion = (Version)parameters["DeployAppScriptVersion"];
                    }
                    if (parameters.ContainsKey("DeployAppScriptParameters"))
                    {
                        _deployAppScriptParameters = (Dictionary<string, object>)parameters["DeployAppScriptParameters"];
                    }
                    if (parameters.ContainsKey("AppSuccessExitCodes"))
                    {
                        _appSuccessExitCodes = new ReadOnlyCollection<int>((int[])parameters["AppSuccessExitCodes"]);
                    }
                    if (parameters.ContainsKey("AppRebootExitCodes"))
                    {
                        _appRebootExitCodes = new ReadOnlyCollection<int>((int[])parameters["AppRebootExitCodes"]);
                    }
                    if (parameters.ContainsKey("ScriptDirectory"))
                    {
                        _scriptDirectory = (string)parameters["ScriptDirectory"];
                    }
                    if (parameters.ContainsKey("DirFiles"))
                    {
                        _dirFiles = (string)parameters["DirFiles"];
                    }
                    if (parameters.ContainsKey("DirSupportFiles"))
                    {
                        _dirSupportFiles = (string)parameters["DirSupportFiles"];
                    }
                    if (parameters.ContainsKey("DefaultMsiFile"))
                    {
                        _defaultMsiFile = (string)parameters["DefaultMsiFile"];
                    }
                    if (parameters.ContainsKey("DefaultMstFile"))
                    {
                        _defaultMstFile = (string)parameters["DefaultMstFile"];
                    }
                    if (parameters.ContainsKey("DefaultMspFiles"))
                    {
                        _defaultMspFiles = new ReadOnlyCollection<string>((string[])parameters["DefaultMspFiles"]);
                    }
                    if (parameters.ContainsKey("LogName"))
                    {
                        _logName = (string)parameters["LogName"];
                    }
                }

                // Ensure DeploymentType is title cased for aesthetics.
                _deploymentType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_deploymentType.ToLower());
                _deploymentTypeName = (string)((Hashtable)InternalDatabase.GetStrings()["DeploymentType"]!)[_deploymentType]!;

                // Establish script directories.
                if (!string.IsNullOrWhiteSpace(_scriptDirectory))
                {
                    if (string.IsNullOrWhiteSpace(_dirFiles) && Directory.Exists(Path.Combine(_scriptDirectory, "Files")))
                    {
                        _dirFiles = Path.Combine(_scriptDirectory, "Files");
                    }
                    if (string.IsNullOrWhiteSpace(_dirSupportFiles) && Directory.Exists(Path.Combine(_scriptDirectory, "SupportFiles")))
                    {
                        _dirSupportFiles = Path.Combine(_scriptDirectory, "SupportFiles");
                    }
                }


                #endregion
                #region DetectDefaultWimFile


                // If the default frontend hasn't been modified, and there's not already a mounted WIM file, check for WIM files and modify the install accordingly.
                if (string.IsNullOrWhiteSpace(_appName) || ((bool)parameters?.ContainsKey("ForceWimDetection")! && (SwitchParameter)parameters["ForceWimDetection"]))
                {
                    // Only proceed if there isn't already a mounted WIM file and we have a WIM file to use.
                    if ((MountedWimFiles.Count == 0) && !string.IsNullOrWhiteSpace(_dirFiles) && (Directory.GetFiles(_dirFiles, "*.wim", SearchOption.TopDirectoryOnly).FirstOrDefault() is string wimFile))
                    {
                        // Mount the WIM file and reset DirFiles to the mount point.
                        WriteZeroConfigDivider(); Settings |= DeploymentSettings.ZeroConfigInitiated;
                        WriteLogEntry($"Discovered Zero-Config WIM file [{wimFile}].");
                        string mountPath = Path.Combine(_dirFiles, Path.GetRandomFileName());
                        InternalDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Mount-ADTWimFile' -ImagePath $args[0] -Path $args[1] -Index 1"), wimFile, mountPath);
                        AddMountedWimFile(new FileInfo(wimFile)); _dirFiles = mountPath;
                        WriteLogEntry($"Successfully mounted WIM file to [{mountPath}].");

                        // Subst the new DirFiles path to eliminate any potential path length issues.
                        IEnumerable<string> usedLetters = DriveInfo.GetDrives().Select(static d => d.Name);
                        if ((new string[] {"Z:\\", "Y:\\", "X:\\", "W:\\", "V:\\", "U:\\", "T:\\", "S:\\", "R:\\", "Q:\\", "P:\\", "O:\\", "N:\\", "M:\\", "L:\\", "K:\\", "J:\\", "I:\\", "H:\\", "G:\\", "F:\\", "E:\\", "D:\\", "C:\\", "B:\\", "A:\\"}).Where(l => !usedLetters.Contains(l)).FirstOrDefault() is string availLetter)
                        {
                            availLetter = availLetter.Trim('\\'); WriteLogEntry($"Creating substitution drive [{availLetter}] for [{_dirFiles}].");
                            InternalDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Invoke-ADTSubstOperation' -Drive $args[0] -Path $args[1]"), availLetter, _dirFiles);
                            _dirFiles = DirFilesSubstDrive = availLetter;
                        }
                        WriteLogEntry($"Using [{_dirFiles}] as the base DirFiles directory.");
                    }
                }


                #endregion
                #region DetectDefaultMsi


                // If the default frontend hasn't been modified, check for MSI / MST and modify the install accordingly.
                if (string.IsNullOrWhiteSpace(_appName))
                {
                    // Find the first MSI file in the Files folder and use that as our install.
                    if (string.IsNullOrWhiteSpace(_defaultMsiFile))
                    {
                        // Only proceed if the Files directory is set.
                        if (!string.IsNullOrWhiteSpace(_dirFiles))
                        {
                            // Get the first MSI file in the Files directory.
                            string[] msiFiles = Directory.GetFiles(_dirFiles, "*.msi", SearchOption.TopDirectoryOnly);
                            var envOSArchitecture = adtEnv["envOSArchitecture"]!.ToString();
                            var formattedOSArch = string.Empty;

                            // Build out the OS architecture string.
                            switch (envOSArchitecture)
                            {
                                case "X86":
                                    formattedOSArch = "x86";
                                    break;
                                case "AMD64":
                                    formattedOSArch = "x64";
                                    break;
                                case "ARM64":
                                    formattedOSArch = "arm64";
                                    break;
                                default:
                                    formattedOSArch = envOSArchitecture;
                                    break;
                            }

                            // If we have a specific architecture MSI file, use that. Otherwise, use the first MSI file found.
                            if (msiFiles.Where(f => !f.EndsWith($".{formattedOSArch}.msi", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault() is string msiFile)
                            {
                                _defaultMsiFile = new FileInfo(msiFile).FullName;
                            }
                            else if (msiFiles.Length > 0)
                            {
                                _defaultMsiFile = new FileInfo(msiFiles[0]).FullName;
                            }
                        }
                    }
                    else if (!Path.IsPathRooted(_defaultMsiFile) && !string.IsNullOrWhiteSpace(_dirFiles))
                    {
                        _defaultMsiFile = Path.Combine(_dirFiles, _defaultMsiFile);
                    }

                    // If we have a default MSI file, proceed further with the Zero-Config configuration.
                    if (!string.IsNullOrWhiteSpace(_defaultMsiFile))
                    {
                        WriteZeroConfigDivider(); Settings |= DeploymentSettings.ZeroConfigInitiated;
                        WriteLogEntry($"Discovered Zero-Config MSI installation file [{_defaultMsiFile}].");

                        // Discover if there is a zero-config MST file.
                        if (string.IsNullOrWhiteSpace(_defaultMstFile))
                        {
                            string mstFile = Path.ChangeExtension(_defaultMsiFile, "mst");
                            if (File.Exists(mstFile))
                            {
                                _defaultMstFile = mstFile;
                            }
                        }
                        else if (!Path.IsPathRooted(_defaultMstFile) && !string.IsNullOrWhiteSpace(_dirFiles))
                        {
                            _defaultMstFile = Path.Combine(_dirFiles, _defaultMstFile);
                        }
                        if (!string.IsNullOrWhiteSpace(_defaultMstFile))
                        {
                            WriteLogEntry($"Discovered Zero-Config MST installation file [{_defaultMstFile}].");
                        }

                        // Discover if there are zero-config MSP files. Name multiple MSP files in alphabetical order to control order in which they are installed.
                        if (_defaultMspFiles.Count == 0)
                        {
                            if (!string.IsNullOrWhiteSpace(_dirFiles) && (Directory.GetFiles(_dirFiles, "*.msp", SearchOption.TopDirectoryOnly) is string[] mspFiles) && (mspFiles.Length > 0))
                            {
                                _defaultMspFiles = new ReadOnlyCollection<string>(mspFiles);
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(_dirFiles) && (null != _defaultMspFiles.Where(static f => !Path.IsPathRooted(f)).FirstOrDefault()))
                        {
                            _defaultMspFiles = _defaultMspFiles.Select(f => !Path.IsPathRooted(f) ? Path.Combine(_dirFiles, f) : f).ToList().AsReadOnly();
                        }
                        if (_defaultMspFiles.Count > 0)
                        {
                            WriteLogEntry($"Discovered Zero-Config MSP installation file(s) [{string.Join(", ", _defaultMspFiles)}].");
                        }

                        // Read the MSI and get the installation details.
                        if (((bool)parameters?.ContainsKey("DisableDefaultMsiProcessList")! && (SwitchParameter)parameters["DisableDefaultMsiProcessList"]))
                        {
                            var exeProps = (ReadOnlyDictionary<string, object>)InternalDatabase.InvokeScript(ScriptBlock.Create("$gmtpParams = @{ Path = $args[0] }; if ($args[1]) { $gmtpParams.Add('TransformPath', $args[1]) }; & $Script:CommandTable.'Get-ADTMsiTableProperty' @gmtpParams -Table File"), DefaultMsiFile!, DefaultMstFile!).First().BaseObject;
                            List<ProcessObject> msiExecList = exeProps.Where(static p => Path.GetExtension(p.Key).Equals(".exe")).Select(static p => new ProcessObject(Regex.Replace(Path.GetFileNameWithoutExtension(p.Key), "^_", string.Empty))).ToList();

                            // Generate list of MSI executables for testing later on.
                            if (msiExecList.Count > 0)
                            {
                                DefaultMsiExecutablesList = msiExecList.AsReadOnly();
                                WriteLogEntry($"MSI Executable List [{string.Join(", ", DefaultMsiExecutablesList.Select(static p => p.Name))}].");
                            }
                        }

                        // Update our app variables with new values.
                        var msiProps = (ReadOnlyDictionary<string, object>)InternalDatabase.InvokeScript(ScriptBlock.Create("$gmtpParams = @{ Path = $args[0] }; if ($args[1]) { $gmtpParams.Add('TransformPath', $args[1]) }; & $Script:CommandTable.'Get-ADTMsiTableProperty' @gmtpParams -Table Property"), DefaultMsiFile!, DefaultMstFile!).First().BaseObject;
                        _appName = (string)msiProps["ProductName"];
                        _appVersion = (string)msiProps["ProductVersion"];
                        WriteLogEntry($"App Vendor [{(string)msiProps["Manufacturer"]}].");
                        WriteLogEntry($"App Name [{_appName}].");
                        WriteLogEntry($"App Version [{_appVersion}].");
                        Settings |= DeploymentSettings.UseDefaultMsi;
                    }
                }


                #endregion
                #region SetAppProperties


                // Set up sample variables if Dot Sourcing the script, app details have not been specified.
                if (string.IsNullOrWhiteSpace(_appName))
                {
                    _appName = (string)adtEnv["appDeployToolkitName"]!;

                    if (!string.IsNullOrWhiteSpace(_appVendor))
                    {
                        _appVendor = null;
                    }
                    if (string.IsNullOrWhiteSpace(_appVersion))
                    {
                        _appVersion = adtEnv["appDeployMainScriptVersion"]!.ToString()!;
                    }
                    if (string.IsNullOrWhiteSpace(_appLang))
                    {
                        _appLang = (string)adtEnv["currentLanguage"]!;
                    }
                    if (string.IsNullOrWhiteSpace(_appRevision))
                    {
                        _appRevision = "01";
                    }
                }

                // Sanitize the application details, as they can cause issues in the script.
                string invalidChars = string.Join(null, Path.GetInvalidFileNameChars());
                if (!string.IsNullOrWhiteSpace(_appVendor))
                {
                    _appVendor = Regex.Replace(_appVendor, invalidChars, string.Empty).Trim();
                }
                if (!string.IsNullOrWhiteSpace(_appName))
                {
                    _appName = Regex.Replace(_appName, invalidChars, string.Empty).Trim();
                }
                if (!string.IsNullOrWhiteSpace(_appVersion))
                {
                    _appVersion = Regex.Replace(_appVersion, invalidChars, string.Empty).Trim();
                }
                if (!string.IsNullOrWhiteSpace(_appArch))
                {
                    _appArch = Regex.Replace(_appArch, invalidChars, string.Empty).Trim();
                }
                if (!string.IsNullOrWhiteSpace(_appLang))
                {
                    _appLang = Regex.Replace(_appLang, invalidChars, string.Empty).Trim();
                }
                if (!string.IsNullOrWhiteSpace(_appRevision))
                {
                    _appRevision = Regex.Replace(_appRevision, invalidChars, string.Empty).Trim();
                }

                // If we're left with a blank AppName, throw a terminating error.
                if (string.IsNullOrWhiteSpace(_appName))
                {
                    throw new ArgumentNullException("AppName", "The application name was not specified.");
                }


                #endregion
                #region SetInstallProperties


                // Build the Installation Title.
                if (string.IsNullOrWhiteSpace(_installTitle))
                {
                    _installTitle = $"{_appVendor} {_appName} {_appVersion}".Trim();
                }
                _installTitle = Regex.Replace(_installTitle, "\\s{2,}", string.Empty);

                // Build the Installation Name.
                if (string.IsNullOrWhiteSpace(_installName))
                {
                    _installName = $"{_appVendor}_{_appName}_{_appVersion}_{_appArch}_{_appLang}_{_appRevision}";
                }
                _installName = Regex.Replace(_installName!.Trim('_').Replace(" ", null), "_+", "_");

                // Set the Defer History registry path.
                RegKeyDeferBase = $"{configToolkit["RegPath"]}\\{adtEnv["appDeployToolkitName"]}\\DeferHistory";
                RegKeyDeferHistory = $"{RegKeyDeferBase}\\{_installName}";


                #endregion
                #region InitLogging


                // Generate log paths from our installation properties.
                _logTempFolder = Path.Combine((string)adtEnv["envTemp"]!, $"{_installName}_{_deploymentType}");
                if ((bool)configToolkit["CompressLogs"]!)
                {
                    // If the temp log folder already exists from a previous ZIP operation, then delete all files in it to avoid issues.
                    if (Directory.Exists(_logTempFolder))
                    {
                        Directory.Delete(_logTempFolder, true);
                    }
                    LogPath = Directory.CreateDirectory(_logTempFolder).FullName;
                }
                else
                {
                    LogPath = Directory.CreateDirectory((string)configToolkit["LogPath"]!).FullName;
                }

                // Append subfolder path if configured to do so.
                if ((bool)configToolkit["LogToSubfolder"]!)
                {
                    LogPath = Directory.CreateDirectory(Path.Combine(LogPath, _installName)).FullName;
                }

                // Generate the log filename to use. Append the username to the log file name if the toolkit is not running as an administrator,
                // since users do not have the rights to modify files in the ProgramData folder that belong to other users.
                if (string.IsNullOrWhiteSpace(_logName))
                {
                    if ((bool)adtEnv["IsAdmin"]!)
                    {
                        _logName = $"{_installName}_{adtEnv["appDeployToolkitName"]}_{_deploymentType}.log";
                    }
                    else
                    {
                        _logName = $"{_installName}_{adtEnv["appDeployToolkitName"]}_{_deploymentType}_{adtEnv["envUserName"]}.log";
                    }
                }
                _logName = Regex.Replace(_logName, invalidChars, string.Empty);
                string logFile = Path.Combine(LogPath, _logName);
                FileInfo logFileInfo = new FileInfo(logFile);
                var logMaxSize = (int)configToolkit["LogMaxSize"]!;
                bool logFileSizeExceeded = logFileInfo.Exists && (logMaxSize > 0) && ((logFileInfo.Length / 1048576.0) > logMaxSize);

                // Check if log file needs to be rotated.
                if ((logFileInfo.Exists && !(bool)configToolkit["LogAppend"]!) || logFileSizeExceeded)
                {
                    try
                    {
                        // Get new log file path.
                        string logFileNameOnly = Path.GetFileNameWithoutExtension(_logName);
                        string logFileExtension = Path.GetExtension(_logName);
                        string logFileTimestamp = DateTime.Now.ToString("O").Split('.')[0].Replace(":", null);
                        string archiveLogFileName = $"{logFileNameOnly}_{logFileTimestamp}{logFileExtension}";
                        string archiveLogFilePath = Path.Combine(LogPath, archiveLogFileName);
                        var logMaxHistory = (int)configToolkit["LogMaxHistory"]!;

                        // Log message about archiving the log file.
                        if (logFileSizeExceeded)
                        {
                            WriteLogEntry($"Maximum log file size [{logMaxSize} MB] reached. Rename log file to [{archiveLogFileName}].", 2);
                        }

                        // Rename the file.
                        logFileInfo.MoveTo(archiveLogFilePath);

                        // Start new log file and log message about archiving the old log file.
                        if (logFileSizeExceeded)
                        {
                            WriteLogEntry($"Previous log file was renamed to [{archiveLogFileName}] because maximum log file size of [{logMaxSize} MB] was reached.", 2);
                        }

                        // Get all log files sorted by last write time.
                        IOrderedEnumerable<FileInfo> logFiles = new DirectoryInfo(LogPath).GetFiles($"{logFileNameOnly}*.log").OrderBy(static f => f.LastWriteTime);
                        int logFilesCount = logFiles.Count();

                        // Keep only the max number of log files.
                        if (logFilesCount > logMaxHistory)
                        {
                            foreach (FileInfo file in logFiles.Take(logFilesCount - logMaxHistory))
                            {
                                file.Delete();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLogEntry($"Failed to rotate the log file [{logFile}]: {ex.Message}", 3);
                    }
                }

                // Open log file with commencement message.
                WriteLogDivider(2);
                WriteLogEntry($"[{_installName}] {_deploymentTypeName.ToLower()} started.");


                #endregion
                #region LogScriptInfo


                // Announce provided deployment script info.
                if (!UseDefaultMsi)
                {
                    if (null != _appScriptVersion)
                    {
                        WriteLogEntry($"[{_installName}] script version is [{_appScriptVersion}].");
                    }
                    if ((_appScriptDate?.ToString("O").Split('T')[0] is string appScriptDate) && !appScriptDate.Equals("2000-12-31"))
                    {
                        WriteLogEntry($"[{_installName}] script date is [{appScriptDate}].");
                    }
                    if (!string.IsNullOrWhiteSpace(_appScriptAuthor) && !_appScriptAuthor!.Equals("<author name>"))
                    {
                        WriteLogEntry($"[{_installName}] script author is [{_appScriptAuthor}].");
                    }
                }
                if (!string.IsNullOrWhiteSpace(_deployAppScriptFriendlyName))
                {
                    if (null != _deployAppScriptVersion)
                    {
                        WriteLogEntry($"[{_deployAppScriptFriendlyName}] script version is [{_deployAppScriptVersion}].");
                    }
                    if ((null != _deployAppScriptParameters) && (_deployAppScriptParameters.Count > 0))
                    {
                        WriteLogEntry($"The following parameters were passed to [{_deployAppScriptFriendlyName}]: [{Utility.ConvertDictToPowerShellArgs(_deployAppScriptParameters).Replace("''", "'")}].");
                    }
                }
                var adtDirectories = (PSObject)adtData.Properties["Directories"].Value;
                var adtDurations = (PSObject)adtData.Properties["Durations"].Value;
                WriteLogEntry($"[{adtEnv["appDeployToolkitName"]}] module version is [{adtEnv["appDeployMainScriptVersion"]}].");
                WriteLogEntry($"[{adtEnv["appDeployToolkitName"]}] module imported in [{((TimeSpan)adtDurations.Properties["ModuleImport"].Value).TotalSeconds}] seconds.");
                WriteLogEntry($"[{adtEnv["appDeployToolkitName"]}] module initialized in [{((TimeSpan)adtDurations.Properties["ModuleInit"].Value).TotalSeconds}] seconds.");
                WriteLogEntry($"[{adtEnv["appDeployToolkitName"]}] module path is [{adtEnv["appDeployToolkitPath"]}].");
                WriteLogEntry($"[{adtEnv["appDeployToolkitName"]}] config path is [{adtDirectories.Properties["Config"].Value}].");
                WriteLogEntry($"[{adtEnv["appDeployToolkitName"]}] string path is [{adtDirectories.Properties["Strings"].Value}].");

                // Announce session instantiation mode.
                if (null != callerSessionState)
                {
                    WriteLogEntry($"[{adtEnv["appDeployToolkitName"]}] session mode is [Compatibility]. This mode is for the transition of v3.x scripts and is not for new development.", 2);
                    WriteLogEntry("Information on how to migrate this script to Native mode is available at [https://psappdeploytoolkit.com/].", 2);
                }
                else
                {
                    WriteLogEntry($"[{adtEnv["appDeployToolkitName"]}] session mode is [Native].");
                }


                #endregion
                #region LogSystemInfo


                // Report on all determined system info.
                WriteLogEntry($"Computer Name is [{adtEnv["envComputerNameFQDN"]}].");
                WriteLogEntry($"Current User is [{adtEnv["ProcessNTAccount"]}].");
                WriteLogEntry($"OS Version is [{adtEnv["envOSName"]}{$" {adtEnv["envOSServicePack"]}".Trim()} {adtEnv["envOSArchitecture"]} {adtEnv["envOSVersion"]}].");
                WriteLogEntry($"OS Type is [{adtEnv["envOSProductTypeName"]}].");
                WriteLogEntry($"Hardware Platform is [{adtEnv["envHardwareType"]}].");
                WriteLogEntry($"Current Culture is [{CultureInfo.CurrentCulture.Name}], language is [{adtEnv["currentLanguage"]}] and UI language is [{adtEnv["currentUILanguage"]}].");
                WriteLogEntry($"PowerShell Host is [{((PSHost)adtEnv["envHost"]!).Name}] with version [{adtEnv["envHostVersionSemantic"] ?? adtEnv["envHostVersion"]}].");
                WriteLogEntry($"PowerShell Version is [{adtEnv["envPSVersionSemantic"] ?? adtEnv["envPSVersion"]} {adtEnv["psArchitecture"]}].");
                if (adtEnv["envCLRVersion"] is Version envCLRVersion)
                {
                    WriteLogEntry($"PowerShell CLR (.NET) version is [{envCLRVersion}].");
                }


                #endregion
                #region LogUserInfo


                // Log details for all currently logged on users.
                WriteLogDivider();
                WriteLogEntry($"Display session information for all logged on users:\n{InternalDatabase.InvokeScript(ScriptBlock.Create("$args[0] | & $Script:CommandTable.'Format-List' | & $Script:CommandTable.'Out-String' -Width ([System.Int32]::MaxValue)"), adtEnv["LoggedOnUserSessions"]!)}", false);

                // Provide detailed info about current process state.
                if ((adtEnv["usersLoggedOn"] is var usersLoggedOn) && (null != usersLoggedOn))
                {
                    WriteLogEntry($"The following users are logged on to the system: [{string.Join(", ", usersLoggedOn)}].");

                    // Check if the current process is running in the context of one of the logged on users
                    if (adtEnv["CurrentLoggedOnUserSession"] is CompatibilitySessionInfo CurrentLoggedOnUserSession)
                    {
                        WriteLogEntry($"Current process is running with user account [{adtEnv["ProcessNTAccount"]}] under logged on user session for [{CurrentLoggedOnUserSession.NTAccount}].");
                    }
                    else
                    {
                        WriteLogEntry($"Current process is running under a system account [{adtEnv["ProcessNTAccount"]}].");
                    }

                    // Guard Intune detection code behind a variable.
                    if ((bool)configToolkit["OobeDetection"]!)
                    {
                        // Check if the device has completed the OOBE or not.
                        if ((Environment.OSVersion.Version >= new Version(10, 0, 16299, 0)) && !Utility.IsOOBEComplete())
                        {
                            WriteLogEntry("Detected OOBE in progress, changing deployment mode to silent.");
                            _deployMode = "Silent";
                        }
                        else if (Process.GetProcessesByName("WWAHost").Length > 0)
                        {
                            // If WWAHost is running, the device might be within the User ESP stage. But first, confirm whether the device is in Autopilot.
                            WriteLogEntry("The WWAHost process is running, confirming the device Autopilot-enrolled.");
                            var apRegKey = "Microsoft.PowerShell.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Provisioning\\Diagnostics\\AutoPilot";
                            if (!string.IsNullOrWhiteSpace((string)moduleSessionState.InvokeProvider.Property.Get([apRegKey], ["CloudAssignedTenantId"], true).First().Properties["CloudAssignedTenantId"].Value))
                            {
                                WriteLogEntry("The device is Autopilot-enrolled, checking ESP User Account Setup phase.");
                                var fsRegKey = "Microsoft.PowerShell.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Enrollments\\*\\FirstSync";
                                if (null == moduleSessionState.InvokeProvider.Property.Get([fsRegKey], null, false).Where(obj => (obj.Properties["IsSyncDone"] is PSPropertyInfo syncDone) && syncDone.Value.Equals(1)).FirstOrDefault())
                                {
                                    WriteLogEntry("The ESP User Account Setup phase is still in progress as IsSyncDone was not found, changing deployment mode to silent.");
                                    _deployMode = "Silent";
                                }
                                else
                                {
                                    WriteLogEntry("The ESP User Account Setup phase is already complete.");
                                }
                            }
                            else
                            {
                                WriteLogEntry("The device is not Autopilot-enrolled.");
                            }
                        }
                    }

                    // Display account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
                    if (adtEnv["CurrentConsoleUserSession"] is CompatibilitySessionInfo CurrentConsoleUserSession)
                    {
                        WriteLogEntry($"The following user is the console user [{CurrentConsoleUserSession.NTAccount}] (user with control of physical monitor, keyboard, and mouse).");
                    }
                    else
                    {
                        WriteLogEntry("There is no console user logged on (user with control of physical monitor, keyboard, and mouse).");
                    }

                    // Display the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
                    if (adtEnv["RunAsActiveUser"] is CompatibilitySessionInfo RunAsActiveUser)
                    {
                        WriteLogEntry($"The active logged on user is [{RunAsActiveUser.NTAccount}].");
                    }
                }
                else
                {
                    WriteLogEntry("No users are logged on to the system.");
                }

                // Log which language's UI messages are loaded from the config file
                WriteLogEntry($"The current execution context has a primary UI language of [{adtEnv["currentLanguage"]}].");

                // Advise whether the UI language was overridden.
                if (((Hashtable)adtConfig["UI"]!)["LanguageOverride"] is string languageOverride)
                {
                    WriteLogEntry($"The config file was configured to override the detected primary UI language with the following UI language: [{languageOverride}].");
                }
                WriteLogEntry($"The following UI messages were imported from the config file: [{adtData.Properties["Language"].Value}].");


                #endregion
                #region PerformConfigMgrTests


                // Check if script is running from a SCCM Task Sequence.
                if ((bool)adtEnv["RunningTaskSequence"]!)
                {
                    WriteLogEntry("Successfully found COM object [Microsoft.SMS.TSEnvironment]. Therefore, script is currently running from a SCCM Task Sequence.");
                }
                else
                {
                    WriteLogEntry("Unable to find COM object [Microsoft.SMS.TSEnvironment]. Therefore, script is not currently running from a SCCM Task Sequence.");
                }


                #endregion
                #region PerformSystemAccountTests


                // Return early if we're not in session 0.
                if ((bool)adtEnv["SessionZero"]!)
                {
                    // If the script was launched with deployment mode set to NonInteractive, then continue.
                    if (_deployMode != "Interactive")
                    {
                        WriteLogEntry($"Session 0 detected but deployment mode was manually set to [{_deployMode}].");
                    }
                    else if ((bool)configToolkit["SessionDetection"]!)
                    {
                        // If the process is not able to display a UI, enable NonInteractive mode.
                        if (!(bool)adtEnv["IsProcessUserInteractive"]!)
                        {
                            _deployMode = "NonInteractive";
                            WriteLogEntry($"Session 0 detected, process not running in user interactive mode; deployment mode set to [{_deployMode}].");
                        }
                        else if (null == adtEnv["usersLoggedOn"])
                        {
                            _deployMode = "NonInteractive";
                            WriteLogEntry($"Session 0 detected, process running in user interactive mode, no users logged on; deployment mode set to [{_deployMode}].");
                        }
                        else
                        {
                            WriteLogEntry("Session 0 detected, process running in user interactive mode, user(s) logged on.");
                        }
                    }
                    else
                    {
                        WriteLogEntry("Session 0 detected but toolkit is configured to not adjust deployment mode.");
                    }
                }
                else
                {
                    WriteLogEntry("Session 0 not detected.");
                }


                #endregion
                #region SetDeploymentProperties


                // Set Deploy Mode switches.
                WriteLogEntry($"Installation is running in [{_deployMode}] mode.");
                switch (_deployMode)
                {
                    case "Silent":
                        Settings |= DeploymentSettings.NonInteractive;
                        Settings |= DeploymentSettings.Silent;
                        break;
                    case "NonInteractive":
                        Settings |= DeploymentSettings.NonInteractive;
                        break;
                }


                // Check deployment type (install/uninstall).
                WriteLogEntry($"Deployment type is [{_deploymentTypeName}].");


                #endregion
                #region TestDefaultMsi


                // Advise the caller if a zero-config MSI was found.
                if (UseDefaultMsi)
                {
                    WriteLogEntry($"Discovered Zero-Config MSI installation file [{_defaultMsiFile}].");
                }


                #endregion
                #region TestAdminRequired


                // Check current permissions and exit if not running with Administrator rights.
                if ((bool)configToolkit["RequireAdmin"]! && !(bool)adtEnv["IsAdmin"]!)
                {
                    throw new UnauthorizedAccessException($"[{adtEnv["appDeployToolkitName"]}] has a toolkit config option [RequireAdmin] set to [True] and the current user is not an Administrator, or PowerShell is not elevated. Please re-run the deployment script as an Administrator or change the option in the config file to not require Administrator rights.");
                }


                #endregion
                #region Finalization


                // If terminal server mode was specified, change the installation mode to support it.
                if (TerminalServerMode)
                {
                    InternalDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Enable-ADTTerminalServerInstallMode'"));
                }

                // Export session's public variables to the user's scope. For these, we can't capture the Set-Variable
                // PassThru data as syntax like `$var = 'val'` constructs a new PSVariable every time.
                if (null != callerSessionState)
                {
                    foreach (PropertyInfo property in this.GetType().GetProperties())
                    {
                        callerSessionState.PSVariable.Set(new PSVariable(property.Name, property.GetValue(this)));
                    }
                    foreach (FieldInfo field in this.GetType().GetFields())
                    {
                        callerSessionState.PSVariable.Set(new PSVariable(field.Name, field.GetValue(this)));
                    }
                    CallerSessionState = callerSessionState;
                }

                // We made it! Add this session to the module's session list for tracking.
                InternalDatabase.GetSessionList().Add(this);


                #endregion
            }
            catch (Exception ex)
            {
                WriteLogEntry($"Failure occurred while instantiating new deployment session: \"{ex.Message}\".", 3);
                SetExitCode(60008);
                Close();
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }


        #endregion
        #region Methods.


        /// <summary>
        /// Gets the caller of the log entry from the call stack frames.
        /// </summary>
        /// <returns>The call stack frame of the log entry caller.</returns>
        private CallStackFrame GetLogEntryCallerInternal()
        {
            return GetLogEntryCaller(InternalDatabase.InvokeScript(ScriptBlock.Create("& $CommandTable.'Get-PSCallStack'"), null).Skip(1).Select(static o => (CallStackFrame)o.BaseObject));
        }

        /// <summary>
        /// Gets the caller of the log entry from the call stack frames.
        /// </summary>
        /// <param name="stackFrames">The call stack frames.</param>
        /// <returns>The call stack frame of the log entry caller.</returns>
        public static CallStackFrame GetLogEntryCaller(IEnumerable<CallStackFrame> stackFrames)
        {
            foreach (CallStackFrame frame in stackFrames)
            {
                // Get the command from the frame and test its validity.
                string command = GetPowerShellCallStackFrameCommand(frame);
                if (!string.IsNullOrWhiteSpace(command) && (!Regex.IsMatch(command, "^(Write-(Log|ADTLogEntry)|<ScriptBlock>(<\\w+>)?)$") || (Regex.IsMatch(command, "^(<ScriptBlock>(<\\w+>)?)$") && frame.GetScriptLocation().Equals("<No file>"))))
                {
                    return frame;
                }
            }
            return null!;
        }

        /// <summary>
        /// Gets the PowerShell call stack frame command.
        /// </summary>
        /// <param name="frame">The call stack frame.</param>
        /// <returns>The PowerShell call stack frame command.</returns>
        private static string GetPowerShellCallStackFrameCommand(CallStackFrame frame)
        {
            // We must re-create the "Command" ScriptProperty as it's only available in PowerShell.
            if (null == frame.InvocationInfo)
            {
                return frame.FunctionName;
            }
            if (null == frame.InvocationInfo.MyCommand)
            {
                return frame.InvocationInfo.InvocationName;
            }
            if (frame.InvocationInfo.MyCommand.Name != string.Empty)
            {
                return frame.InvocationInfo.MyCommand.Name;
            }
            return frame.FunctionName;
        }

        /// <summary>
        /// Closes the session and releases resources.
        /// </summary>
        /// <returns>The exit code.</returns>
        public int? Close()
        {
            // Throw if this object has already been disposed.
            if (Settings.HasFlag(DeploymentSettings.Disposed))
            {
                throw new ObjectDisposedException(this.GetType().Name, "This object has already been disposed.");
            }

            try
            {
                // Establish initial variable values.
                var adtData = InternalDatabase.Get();

                // If terminal server mode was specified, revert the installation mode to support it.
                if (TerminalServerMode)
                {
                    InternalDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Disable-ADTTerminalServerInstallMode'"));
                }

                // Store app/deployment details string. If we're exiting before properties are set, use a generic string.
                string deployString = !string.IsNullOrWhiteSpace(InstallName) ? $"[{InstallName}] {DeploymentTypeName.ToLower()}" : $"{InternalDatabase.GetEnvironment()["appDeployToolkitName"]} deployment";

                // Process resulting exit code.
                DeploymentStatus deploymentStatus = GetDeploymentStatus();
                switch (deploymentStatus)
                {
                    case DeploymentStatus.FastRetry:
                        // Just advise of the exit code with the appropriate severity.
                        WriteLogEntry($"{deployString} completed with exit code [{ExitCode}].", 2);
                        break;
                    case DeploymentStatus.Error:
                        WriteLogEntry($"{deployString} completed with exit code [{ExitCode}].", 3);
                        break;
                    default:
                        // Clean up app deferral history.
                        ResetDeferHistory();

                        // Handle reboot prompts on successful script completion.
                        if (deploymentStatus.Equals("RestartRequired") && AllowRebootPassThru)
                        {
                            WriteLogEntry("A restart has been flagged as required.");
                        }
                        else
                        {
                            ExitCode = 0;
                        }
                        WriteLogEntry($"{deployString} completed with exit code [{ExitCode}].", 0);
                        break;
                }

                // Update the module's last tracked exit code.
                if (ExitCode != 0)
                {
                    adtData.Properties["LastExitCode"].Value = ExitCode;
                }

                // Remove any subst paths if created in the zero-config WIM code.
                if (!string.IsNullOrWhiteSpace(DirFilesSubstDrive))
                {
                    InternalDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Invoke-ADTSubstOperation' -Drive $args[0] -Delete"), DirFilesSubstDrive!);
                }

                // Unmount any stored WIM file entries.
                if (MountedWimFiles.Count > 0)
                {
                    MountedWimFiles.Reverse(); InternalDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Dismount-ADTWimFile' -ImagePath $args[0]"), MountedWimFiles);
                    MountedWimFiles.Clear();
                }

                // Write out a log divider to indicate the end of logging.
                WriteLogDivider();
                Settings |= DeploymentSettings.Disposed;

                // Extrapolate the Toolkit options from the config hashtable.
                var configToolkit = (Hashtable)InternalDatabase.GetConfig()["Toolkit"]!;

                // Compress log files if configured to do so.
                if ((bool)configToolkit["CompressLogs"]!)
                {
                    // Archive the log files to zip format and then delete the temporary logs folder.
                    string destArchiveFileName = $"{InstallName}_{DeploymentType}_{{0}}.zip";
                    try
                    {
                        // Get all archive files sorted by last write time.
                        IOrderedEnumerable<FileInfo> archiveFiles = Directory.GetFiles((string)configToolkit["LogPath"]!, string.Format(destArchiveFileName, "*")).Select(static f => new FileInfo(f)).OrderBy(static f => f.LastWriteTime);
                        destArchiveFileName = string.Format(destArchiveFileName, DateTime.Now.ToString("O").Split('.')[0].Replace(":", null));

                        // Keep only the max number of archive files
                        var logMaxHistory = (int)configToolkit["LogMaxHistory"]!;
                        int archiveFilesCount = archiveFiles.Count();
                        if (archiveFilesCount > logMaxHistory)
                        {
                            foreach (FileInfo file in archiveFiles.Take(archiveFilesCount - logMaxHistory))
                            {
                                file.Delete();
                            }
                        }

                        // Compression of the log files.
                        ZipFile.CreateFromDirectory(LogTempFolder, destArchiveFileName, CompressionLevel.Optimal, false);
                        Directory.Delete(LogTempFolder, true);
                    }
                    catch (Exception ex)
                    {
                        WriteLogEntry($"Failed to manage archive file [{destArchiveFileName}]: {ex.Message}", 3);
                    }
                }

                // Return the module's cached exit code to the caller.
                return !Settings.HasFlag(DeploymentSettings.NoExitOnClose) ? (int)adtData.Properties["LastExitCode"].Value : null;
            }
            catch
            {
                throw;
            }
            finally
            {
                InternalDatabase.GetSessionList().Remove(this);
            }
        }

        /// <summary>
        /// Writes a log entry with detailed parameters.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="severity">The severity level.</param>
        /// <param name="source">The source of the log entry.</param>
        /// <param name="scriptSection">The script section.</param>
        /// <param name="writeHost">Whether to write to the host.</param>
        /// <param name="debugMessage">Whether it is a debug message.</param>
        /// <param name="logType">The type of log.</param>
        /// <param name="logFileDirectory">The log file directory.</param>
        /// <param name="logFileName">The log file name.</param>
        public void WriteLogEntry(string[] message, uint? severity, string source, string scriptSection, bool? writeHost, bool debugMessage, string logType, string logFileDirectory, string logFileName)
        {
            // Extrapolate the Toolkit options from the config hashtable.
            var configToolkit = (Hashtable)InternalDatabase.GetConfig()["Toolkit"]!;

            // Determine whether we can write to the console.
            if (null == writeHost)
            {
                writeHost = (bool)configToolkit["LogWriteToHost"]!;
            }

            // Perform early return checks before wasting time.
            if ((DisableLogging && (bool)!writeHost) || (debugMessage && !(bool)configToolkit["LogDebugMessage"]!))
            {
                return;
            }

            // Establish logging date/time vars.
            DateTime dateNow = DateTime.Now;
            string logTime = dateNow.ToString("HH\\:mm\\:ss.fff");
            CallStackFrame invoker = GetLogEntryCallerInternal();

            // Determine the log file name; either a proper script/function, or a caller directly from the console.
            string logFile = !string.IsNullOrWhiteSpace(invoker.ScriptName) ? invoker.ScriptName : invoker.GetScriptLocation();

            // Set up default values if not specified.
            if (null == severity)
            {
                severity = 1;
            }
            if (string.IsNullOrWhiteSpace(source))
            {
                source = GetPowerShellCallStackFrameCommand(invoker);
            }
            if (string.IsNullOrWhiteSpace(scriptSection))
            {
                scriptSection = InstallPhase;
            }
            if (string.IsNullOrWhiteSpace(logType))
            {
                logType = (string)configToolkit["LogStyle"]!;
            }
            if (string.IsNullOrWhiteSpace(logFileDirectory))
            {
                logFileDirectory = LogPath;
            }
            else if (!Directory.Exists(logFileDirectory))
            {
                Directory.CreateDirectory(logFileDirectory);
            }
            if (string.IsNullOrWhiteSpace(logFileName))
            {
                logFileName = LogName;
            }

            // Store log string to format with message.
            StringDictionary logFormats = new StringDictionary()
            {
                { "Legacy", $"[{dateNow.ToString("O").Split('T')[0]} {logTime}] [{scriptSection}] [{source}] [{LogSeverityNames[(int)severity]}] :: {{0}}" },
                { "CMTrace", $"<![LOG[[{scriptSection}] :: {{0}}]LOG]!><time=\"{logTime}{LogTimeOffset}\" date=\"{dateNow.ToString("M-dd-yyyy")}\" component=\"{source}\" context=\"{Username}\" type=\"{severity}\" thread=\"{PID}\" file=\"{logFile}\">" },
            };

            // Add this log message to the session's buffer.
            foreach (string msg in message)
            {
                LogBuffer.Add(new LogEntry(dateNow, invoker, msg, (uint)severity, source, scriptSection));
            }

            // Write out all messages to disk if configured/permitted to do so.
            if (!DisableLogging && !string.IsNullOrWhiteSpace(logFileDirectory) && !string.IsNullOrWhiteSpace(logFileName))
            {
                using (StreamWriter logFileWriter = new StreamWriter(Path.Combine(logFileDirectory, logFileName), true, LogEncoding))
                {
                    string logLine = logFormats[logType]!;
                    switch (logType)
                    {
                        case "CMTrace":
                            // Replace all empty lines with a space so OneTrace doesn't trim them.
                            // When splitting the message, we want to trim all lines but not replace genuine
                            // spaces. As such, replace all spaces and empty lines with a punctuation space.
                            // C# identifies this character as whitespace but OneTrace does not so it works.
                            // The empty line feed at the end is required by OneTrace to format correctly.
                            logFileWriter.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg.Contains((char)10) ? (string.Join("\n", msg.Replace("\r", null).Trim().Replace(' ', (char)0x2008).Split((char)10).Select(static m => Regex.Replace(m, "^$", $"{(char)0x2008}"))).Replace("\n", "\r\n") + "\r\n") : msg))));
                            break;
                        case "Legacy":
                            logFileWriter.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg))));
                            break;
                    }
                }
            }

            // Write out all messages to host if configured/permitted to do so.
            if ((bool)writeHost!)
            {
                ReadOnlyDictionary<string, ConsoleColor> sevCols = LogSeverityColors[(int)severity];
                if ((bool)configToolkit["LogHostOutputToStdStreams"]!)
                {
                    // Colour the console if we're not informational.
                    if (severity != 1)
                    {
                        Console.ForegroundColor = sevCols["ForegroundColor"];
                        Console.BackgroundColor = sevCols["BackgroundColor"];
                    }

                    // Write errors to stderr, otherwise send everything else to stdout.
                    string logLine = logFormats["Legacy"]!;
                    if (severity == 3)
                    {
                        Console.Error.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg))));
                    }
                    else
                    {
                        Console.WriteLine(string.Join(Environment.NewLine, message.Select(msg => string.Format(logLine, msg))));
                    }

                    // Reset the console colours back to default.
                    Console.ResetColor();
                }
                else
                {
                    // Write the host output to PowerShell's InformationStream.
                    InternalDatabase.InvokeScript(WriteLogEntryDelegate, message, sevCols, source, logFormats["Legacy"]!);
                }
            }
        }

        /// <summary>
        /// Writes a log entry with a message array.
        /// </summary>
        /// <param name="message">The log message array.</param>
        public void WriteLogEntry(string[] message)
        {
            WriteLogEntry(message, null, string.Empty, string.Empty, null, false, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Writes a log entry with a message array and severity.
        /// </summary>
        /// <param name="message">The log message array.</param>
        /// <param name="severity">The severity level.</param>
        public void WriteLogEntry(string[] message, uint? severity)
        {
            WriteLogEntry(message, severity, string.Empty, string.Empty, null, false, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Writes a log entry with a message array and host write option.
        /// </summary>
        /// <param name="message">The log message array.</param>
        /// <param name="writeHost">Whether to write to the host.</param>
        public void WriteLogEntry(string[] message, bool writeHost)
        {
            WriteLogEntry(message, null, string.Empty, string.Empty, writeHost, false, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Writes a log entry with a single message.
        /// </summary>
        /// <param name="message">The log message.</param>
        public void WriteLogEntry(string message)
        {
            WriteLogEntry([message], null, string.Empty, string.Empty, null, false, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Writes a log entry with a single message and severity.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="severity">The severity level.</param>
        public void WriteLogEntry(string message, uint? severity)
        {
            WriteLogEntry([message], severity, string.Empty, string.Empty, null, false, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Writes a log entry with a single message and host write option.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="writeHost">Whether to write to the host.</param>
        public void WriteLogEntry(string message, bool writeHost)
        {
            WriteLogEntry([message], null, string.Empty, string.Empty, writeHost, false, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Writes a log divider with a specified count.
        /// </summary>
        /// <param name="count">The number of dividers to write.</param>
        private void WriteLogDivider(uint count)
        {
            string[] dividers = new string[count]; for (uint i = 0; i < count; i++) { dividers[i] = new string('*', 79); }
            WriteLogEntry(dividers);
        }

        /// <summary>
        /// Writes a log divider.
        /// </summary>
        private void WriteLogDivider()
        {
            WriteLogDivider(1);
        }

        /// <summary>
        /// Writes a log divider prior to a Zero-Config setup.
        /// </summary>
        private void WriteZeroConfigDivider()
        {
            // Print an extra divider when we process a Zero-Config setup before the main logging starts.
            if (!Settings.HasFlag(DeploymentSettings.ZeroConfigInitiated))
            {
                WriteLogDivider(2);
            }
        }

        /// <summary>
        /// Tests the deferral history path.
        /// </summary>
        /// <returns>True if the deferral history path exists; otherwise, false.</returns>
        private bool TestDeferHistoryPath()
        {
            return InternalDatabase.GetSessionState().InvokeProvider.Item.Exists(RegKeyDeferHistory, true, true);
        }

        /// <summary>
        /// Creates the deferral history path.
        /// </summary>
        private void CreateDeferHistoryPath()
        {
            InternalDatabase.GetSessionState().InvokeProvider.Item.New([RegKeyDeferBase], InstallName, "None", null, true);
        }

        /// <summary>
        /// Gets the deferral history.
        /// </summary>
        /// <returns>The deferral history.</returns>
        public PSObject? GetDeferHistory()
        {
            if (string.IsNullOrWhiteSpace(RegKeyDeferHistory) || !TestDeferHistoryPath())
            {
                return null;
            }
            WriteLogEntry("Getting deferral history...");
            return InternalDatabase.GetSessionState().InvokeProvider.Property.Get(RegKeyDeferHistory, null).FirstOrDefault();
        }

        /// <summary>
        /// Sets the deferral history.
        /// </summary>
        /// <param name="deferDeadline">The deferral deadline.</param>
        /// <param name="deferTimesRemaining">The deferral times remaining.</param>
        public void SetDeferHistory(int? deferTimesRemaining, string deferDeadline)
        {
            // Get the module's session state before proceeding.
            var moduleSessionState = InternalDatabase.GetSessionState();

            if (null != deferTimesRemaining)
            {
                WriteLogEntry($"Setting deferral history: [DeferTimesRemaining = {deferTimesRemaining}].");
                if (!TestDeferHistoryPath())
                {
                    CreateDeferHistoryPath();
                }
                moduleSessionState.InvokeProvider.Property.New([RegKeyDeferHistory], "DeferTimesRemaining", "String", deferTimesRemaining, true, true);
            }
            if (!string.IsNullOrWhiteSpace(deferDeadline))
            {
                WriteLogEntry($"Setting deferral history: [DeferDeadline = {deferDeadline}].");
                if (!TestDeferHistoryPath())
                {
                    CreateDeferHistoryPath();
                }
                moduleSessionState.InvokeProvider.Property.New([RegKeyDeferHistory], "DeferDeadline", "String", deferDeadline, true, true);
            }
        }

        /// <summary>
        /// Resets the deferral history.
        /// </summary>
        public void ResetDeferHistory()
        {
            if (!string.IsNullOrWhiteSpace(RegKeyDeferHistory) && TestDeferHistoryPath())
            {
                WriteLogEntry("Removing deferral history...");
                InternalDatabase.GetSessionState().InvokeProvider.Item.Remove(RegKeyDeferHistory, true);
            }
        }

        /// <summary>
        /// Gets the deployment status.
        /// </summary>
        /// <returns>The deployment status.</returns>
        public DeploymentStatus GetDeploymentStatus()
        {
            // Extrapolate the UI options from the config hashtable.
            var configUI = (Hashtable)InternalDatabase.GetConfig()["UI"]!;

            if ((ExitCode == (int)configUI["DefaultExitCode"]!) || (ExitCode == (int)configUI["DeferExitCode"]!))
            {
                return DeploymentStatus.FastRetry;
            }
            else if (AppRebootExitCodes.Contains(ExitCode))
            {
                return DeploymentStatus.RestartRequired;
            }
            else if (AppSuccessExitCodes.Contains(ExitCode))
            {
                return DeploymentStatus.Complete;
            }
            else
            {
                return DeploymentStatus.Error;
            }
        }

        /// <summary>
        /// Add the mounted WIM files.
        /// </summary>
        /// <param>The WIM file to add to the list for dismounting upon session closure.</param>
        public void AddMountedWimFile(FileInfo wimFile)
        {
            MountedWimFiles.Add(wimFile);
        }

        /// <summary>
        /// Gets the default MSI executables list.
        /// </summary>
        /// <returns>An array of default MSI executables.</returns>
        public ReadOnlyCollection<ProcessObject> GetDefaultMsiExecutablesList()
        {
            return DefaultMsiExecutablesList;
        }

        /// <summary>
        /// Gets the deployment type name.
        /// </summary>
        /// <returns>The deployment type name.</returns>
        public string GetDeploymentTypeName()
        {
            return DeploymentTypeName;
        }

        /// <summary>
        /// Determines whether the mode is non-interactive.
        /// </summary>
        /// <returns>True if the mode is non-interactive; otherwise, false.</returns>
        public bool IsNonInteractive()
        {
            return Settings.HasFlag(DeploymentSettings.NonInteractive);
        }

        /// <summary>
        /// Determines whether the mode is silent.
        /// </summary>
        /// <returns>True if the mode is silent; otherwise, false.</returns>
        public bool IsSilent()
        {
            return Settings.HasFlag(DeploymentSettings.Silent);
        }

        /// <summary>
        /// Gets the exit code.
        /// </summary>
        public int GetExitCode()
        {
            return ExitCode;
        }

        /// <summary>
        /// Sets the exit code.
        /// </summary>
        /// <param name="exitCode">The exit code to set.</param>
        public void SetExitCode(int exitCode)
        {
            ExitCode = exitCode;
        }


        #endregion
        #region Internal variables.


        /// <summary>
        /// Gets the log severity colors.
        /// </summary>
        private static readonly ReadOnlyCollection<ReadOnlyDictionary<string, ConsoleColor>> LogSeverityColors = new(new[]
        {
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Green }, { "BackgroundColor", ConsoleColor.Black } }),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor>()),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Yellow }, { "BackgroundColor", ConsoleColor.Black } }),
            new ReadOnlyDictionary<string, ConsoleColor>(new Dictionary<string, ConsoleColor> { { "ForegroundColor", ConsoleColor.Red }, { "BackgroundColor", ConsoleColor.Black } })
        });

        /// <summary>
        /// Gets the log severity names.
        /// </summary>
        private static readonly ReadOnlyCollection<string> LogSeverityNames = new(["Success", "Info", "Warning", "Error"]);

        /// <summary>
        /// Gets the Write-LogEntry delegate script block.
        /// </summary>
        private static readonly ScriptBlock WriteLogEntryDelegate = ScriptBlock.Create("$colours = $args[1]; $args[0] | & $CommandTable.'Write-ADTLogEntryToInformationStream' @colours -Source $args[2] -Format $args[3]");

        /// <summary>
        /// Gets the current timezone bias for the CMTrace log formatted string.
        /// </summary>
        private static readonly string LogTimeOffset = TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes >= 0 ? $"+{TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes}" : TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes.ToString();

        /// <summary>
        /// Gets the current process ID.
        /// </summary>
        private static readonly int PID = Process.GetCurrentProcess().Id;

        /// <summary>
        /// Gets the session caller's username.
        /// </summary>
        private static readonly string Username = WindowsIdentity.GetCurrent().Name;

        /// <summary>
        /// Gets the session's default log file encoding.
        /// </summary>
        private static readonly UTF8Encoding LogEncoding = new UTF8Encoding(true);

        /// <summary>
        /// Bitfield with settings for this deployment.
        /// </summary>
        private DeploymentSettings Settings { get; set; }

        /// <summary>
        /// Gets the caller's SessionState from value that was supplied during object instantiation.
        /// </summary>
        private SessionState? CallerSessionState { get; }

        /// <summary>
        /// Gets the mounted WIM files within this session.
        /// </summary>
        private readonly List<FileInfo> MountedWimFiles = [];

        /// <summary>
        /// Gets the log entries written with this session.
        /// </summary>
        private readonly List<LogEntry> LogBuffer = [];

        /// <summary>
        /// Gets the list of executables found within a Zero-Config MSI file.
        /// </summary>
        private ReadOnlyCollection<ProcessObject> DefaultMsiExecutablesList { get; } = new ReadOnlyCollection<ProcessObject>([]);

        /// <summary>
        /// Gets the drive letter used with subst during a Zero-Config WIM file mount operation.
        /// </summary>
        private string? DirFilesSubstDrive { get; }

        /// <summary>
        /// Gets the base registry path used for getting/setting deferral information.
        /// </summary>
        private string RegKeyDeferBase { get; }

        /// <summary>
        /// Gets the registry path used for getting/setting deferral information.
        /// </summary>
        private string RegKeyDeferHistory { get; }

        /// <summary>
        /// Gets the deployment session's filesystem log path.
        /// </summary>
        private string LogPath { get; }

        /// <summary>
        /// Gets the deployment session's closing exit code.
        /// </summary>
        private int ExitCode { get; set; }


        #endregion
        #region Private backing fields.


        private string _deploymentType { get; } = "Install";
        private string _deploymentTypeName { get; }
        private string _deployMode { get; } = "Interactive";
        private string? _appVendor { get; }
        private string? _appName { get; }
        private string? _appVersion { get; }
        private string? _appArch { get; }
        private string? _appLang { get; }
        private string? _appRevision { get; }
        private ReadOnlyCollection<int> _appSuccessExitCodes { get; } = new ReadOnlyCollection<int>([0]);
        private ReadOnlyCollection<int> _appRebootExitCodes { get; } = new ReadOnlyCollection<int>([1641, 3010]);
        private Version? _appScriptVersion { get; }
        private DateTime? _appScriptDate { get; }
        private string? _appScriptAuthor { get; }
        private string _installName { get; }
        private string _installTitle { get; }
        private string? _deployAppScriptFriendlyName { get; }
        private Version? _deployAppScriptVersion { get; }
        private Dictionary<string, object>? _deployAppScriptParameters { get; }
        private string _installPhase { get; set; } = "Initialization";
        private string _currentDate { get; }
        private string _currentTime { get; }
        private string? _scriptDirectory { get; }
        private string? _dirFiles { get; set; }
        private string? _dirSupportFiles { get; set; }
        private string? _defaultMsiFile { get; }
        private string? _defaultMstFile { get; }
        private ReadOnlyCollection<string> _defaultMspFiles { get; } = new ReadOnlyCollection<string>([]);
        private string _logTempFolder { get; }
        private string _logName { get; }


        #endregion
        #region Frontend parameters.


        /// <summary>
        /// Gets the deployment session's deployment type.
        /// </summary>
        public string DeploymentType
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(DeploymentType)) : _deploymentType;
        }

        /// <summary>
        /// Gets the deployment type name from the language string table for the given DeploymentType.
        /// </summary>
        public string DeploymentTypeName
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(DeploymentTypeName)) : _deploymentTypeName;
        }

        /// <summary>
        /// Gets the deployment session's deployment mode.
        /// </summary>
        public string DeployMode
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(DeployMode)) : _deployMode;
        }

        /// <summary>
        /// Gets whether this deployment session is allowed to exit with a reboot exit code.
        /// </summary>
        public bool AllowRebootPassThru
        {
            get => (null != CallerSessionState) ? (bool)CallerSessionState.PSVariable.GetValue(nameof(AllowRebootPassThru)) : Settings.HasFlag(DeploymentSettings.AllowRebootPassThru);
        }

        /// <summary>
        /// Gets whether this deployment session should enable terminal services install mode.
        /// </summary>
        public bool TerminalServerMode
        {
            get => (null != CallerSessionState) ? (bool)CallerSessionState.PSVariable.GetValue(nameof(TerminalServerMode)) : Settings.HasFlag(DeploymentSettings.TerminalServerMode);
        }

        /// <summary>
        /// Gets whether this deployment session should disable logging for the operation.
        /// </summary>
        public bool DisableLogging
        {
            get => (null != CallerSessionState) ? (bool)CallerSessionState.PSVariable.GetValue(nameof(DisableLogging)) : Settings.HasFlag(DeploymentSettings.DisableLogging);
        }


        #endregion
        #region Frontend variables.


        /// <summary>
        /// Gets the deployment session's application vendor.
        /// </summary>
        public string? AppVendor
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(AppVendor)) : _appVendor;
        }

        /// <summary>
        /// Gets the deployment session's application name.
        /// </summary>
        public string? AppName
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(AppName)) : _appName;
        }

        /// <summary>
        /// Gets the deployment session's application version.
        /// </summary>
        public string? AppVersion
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(AppVersion)) : _appVersion;
        }

        /// <summary>
        /// Gets the deployment session's application architecture.
        /// </summary>
        public string? AppArch
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(AppArch)) : _appArch;
        }

        /// <summary>
        /// Gets the deployment session's application language.
        /// </summary>
        public string? AppLang
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(AppLang)) : _appLang;
        }

        /// <summary>
        /// Gets the deployment session's application package revision.
        /// </summary>
        public string? AppRevision
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(AppRevision)) : _appRevision;
        }

        /// <summary>
        /// Gets the deployment session's exit code(s) to indicate a successful deployment.
        /// </summary>
        public ReadOnlyCollection<int> AppSuccessExitCodes
        {
            get => (null != CallerSessionState) ? (ReadOnlyCollection<int>)CallerSessionState.PSVariable.GetValue(nameof(AppSuccessExitCodes)) : _appSuccessExitCodes;
        }

        /// <summary>
        /// Gets the deployment session's exit code(s) to indicate a reboot is required.
        /// </summary>
        public ReadOnlyCollection<int> AppRebootExitCodes
        {
            get => (null != CallerSessionState) ? (ReadOnlyCollection<int>)CallerSessionState.PSVariable.GetValue(nameof(AppRebootExitCodes)) : _appRebootExitCodes;
        }

        /// <summary>
        /// Gets the deployment session's application package version.
        /// </summary>
        public Version? AppScriptVersion
        {
            get => (null != CallerSessionState) ? new Version((string)CallerSessionState.PSVariable.GetValue(nameof(AppScriptVersion))) : _appScriptVersion;
        }

        /// <summary>
        /// Gets the deployment session's application package date.
        /// </summary>
        public DateTime? AppScriptDate
        {
            get => (null != CallerSessionState) ? DateTime.ParseExact((string)CallerSessionState.PSVariable.GetValue(nameof(AppScriptDate)), CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern, CultureInfo.InvariantCulture) : _appScriptDate;
        }

        /// <summary>
        /// Gets the deployment session's application package author.
        /// </summary>
        public string? AppScriptAuthor
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(AppScriptAuthor)) : _appScriptAuthor;
        }

        /// <summary>
        /// Gets an override to the deployment session's installation name.
        /// </summary>
        public string InstallName
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(InstallName)) : _installName;
        }

        /// <summary>
        /// Gets an override to the deployment session's installation title.
        /// </summary>
        public string InstallTitle
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(InstallTitle)) : _installTitle;
        }

        /// <summary>
        /// Gets the deployment session's frontend script name.
        /// </summary>
        public string? DeployAppScriptFriendlyName
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(DeployAppScriptFriendlyName)) : _deployAppScriptFriendlyName;
        }

        /// <summary>
        /// Gets the deployment session's frontend script version.
        /// </summary>
        public Version? DeployAppScriptVersion
        {
            get => (null != CallerSessionState) ? (Version?)CallerSessionState.PSVariable.GetValue(nameof(DeployAppScriptVersion)) : _deployAppScriptVersion;
        }

        /// <summary>
        /// Gets the deployment session's frontend script parameters.
        /// </summary>
        public Dictionary<string, object>? DeployAppScriptParameters
        {
            get => (null != CallerSessionState) ? (Dictionary<string, object>)CallerSessionState.PSVariable.GetValue(nameof(DeployAppScriptParameters)) : _deployAppScriptParameters;
        }

        /// <summary>
        /// Gets/sets the deployment session's installation phase'.
        /// </summary>
        public string InstallPhase
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(InstallPhase)) : _installPhase;
            set
            {
                _installPhase = value;
                CallerSessionState?.PSVariable.Set(new PSVariable(nameof(InstallPhase), value));
            }
        }


        #endregion
        #region Other public variables.


        /// <summary>
        /// Gets the deployment session's starting date and time.
        /// </summary>
        public readonly DateTime CurrentDateTime = DateTime.Now;

        /// <summary>
        /// Gets the deployment session's starting date as a string.
        /// </summary>
        public string CurrentDate
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(CurrentDate)) : _currentDate;
        }

        /// <summary>
        /// Gets the deployment session's starting time as a string.
        /// </summary>
        public string CurrentTime
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(CurrentTime)) : _currentTime;
        }

        /// <summary>
        /// Gets the deployment session's UTC offset from GMT 0.
        /// </summary>
        public static readonly TimeSpan CurrentTimeZoneBias = TimeZoneInfo.Local.BaseUtcOffset;

        /// <summary>
        /// Gets the script directory of the caller.
        /// </summary>
        public string? ScriptDirectory
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(ScriptDirectory)) : _scriptDirectory;
        }

        /// <summary>
        /// Gets the specified or determined path to the Files folder.
        /// </summary>
        public string? DirFiles
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(DirFiles)) : _dirFiles;
            set
            {
                _dirFiles = value;
                CallerSessionState?.PSVariable.Set(new PSVariable(nameof(DirFiles), value));
            }
        }

        /// <summary>
        /// Gets the specified or determined path to the SupportFiles folder.
        /// </summary>
        public string? DirSupportFiles
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(DirSupportFiles)) : _dirSupportFiles;
            set
            {
                _dirSupportFiles = value;
                CallerSessionState?.PSVariable.Set(new PSVariable(nameof(DirSupportFiles), value));
            }
        }

        /// <summary>
        /// Gets the deployment session's Zero-Config MSI file path.
        /// </summary>
        public string? DefaultMsiFile
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(DefaultMsiFile)) : _defaultMsiFile;
        }

        /// <summary>
        /// Gets the deployment session's Zero-Config MST file path.
        /// </summary>
        public string? DefaultMstFile
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(DefaultMstFile)) : _defaultMstFile;
        }

        /// <summary>
        /// Gets the deployment session's Zero-Config MSP file paths.
        /// </summary>
        public ReadOnlyCollection<string> DefaultMspFiles
        {
            get => (null != CallerSessionState) ? (ReadOnlyCollection<string>)CallerSessionState.PSVariable.GetValue(nameof(DefaultMspFiles)) : _defaultMspFiles;
        }

        /// <summary>
        /// Gets whether this deployment session found a valid Zero-Config MSI file.
        /// </summary>
        public bool UseDefaultMsi
        {
            get => (null != CallerSessionState) ? (bool)CallerSessionState.PSVariable.GetValue(nameof(UseDefaultMsi)) : Settings.HasFlag(DeploymentSettings.UseDefaultMsi);
        }

        /// <summary>
        /// Gets the deployment session's Zero-Config MSP file paths.
        /// </summary>
        public string LogTempFolder
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(LogTempFolder)) : _logTempFolder;
        }

        /// <summary>
        /// Gets the deployment session's log filename.
        /// </summary>
        public string LogName
        {
            get => (null != CallerSessionState) ? (string)CallerSessionState.PSVariable.GetValue(nameof(LogName)) : _logName;
        }


        #endregion
    }
}

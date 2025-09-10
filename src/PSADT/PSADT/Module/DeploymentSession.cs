using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Principal;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using PSADT.AccountManagement;
using PSADT.DeviceManagement;
using PSADT.ProcessManagement;
using PSADT.TerminalServices;
using PSADT.Utilities;

namespace PSADT.Module
{
    /// <summary>
    /// Represents a deployment session.
    /// </summary>
    public class DeploymentSession
    {
        #region Constructors.


        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentSession"/> class.
        /// </summary>
        /// <param name="parameters">All parameters from Open-ADTSession.</param>
        /// <param name="noExitOnClose">Indicates that the shell shouldn't exit on the last session closure.</param>
        /// <param name="callerSessionState">The caller session state.</param>
        public DeploymentSession(IReadOnlyDictionary<string, object>? parameters = null, bool? noExitOnClose = null, SessionState? callerSessionState = null)
        {
            int deferExitCode = 1602;
            try
            {
                #region Initialization


                // Establish start date/time first so we can accurately mark the start of execution.
                _currentDate = CurrentDateTime.ToString("dd-MM-yyyy");
                _currentTime = CurrentDateTime.ToString("HH:mm:ss");

                // Establish initial variable values.
                var adtData = ModuleDatabase.Get();
                var adtEnv = ModuleDatabase.GetEnvironment();
                var adtConfig = ModuleDatabase.GetConfig();
                var configUI = (Hashtable)adtConfig["UI"]!;
                var configToolkit = (Hashtable)adtConfig["Toolkit"]!;
                var moduleSessionState = ModuleDatabase.GetSessionState();
                bool writtenDivider = false; _ = _installPhase;
                deferExitCode = (int)configUI["DeferExitCode"]!;
                bool forceProcessDetection = false;

                // Pre-cache reused environment variables.
                var appDeployToolkitName = (string)adtEnv["appDeployToolkitName"]!;
                var appDeployMainScriptVersion = ((Version)adtEnv["appDeployMainScriptVersion"]!).ToString();
                var IsProcessUserInteractive = (bool)adtEnv["IsProcessUserInteractive"]!;
                var usersLoggedOn = (ReadOnlyCollection<NTAccount>?)adtEnv["usersLoggedOn"];
                var RunAsActiveUser = (RunAsActiveUser?)adtEnv["RunAsActiveUser"];
                var currentLanguage = (string)adtEnv["currentLanguage"]!;
                var envOSArchitecture = (Architecture)adtEnv["envOSArchitecture"]!;
                var processNtAccount = (NTAccount)adtEnv["ProcessNTAccount"]!;
                var isAdmin = (bool)adtEnv["IsAdmin"]!;

                // Set up other variable values based on incoming dictionary.
                if (null != parameters && parameters.Count > 0)
                {
                    if (parameters.TryGetValue("DeploymentType", out var paramValue) && (null != paramValue))
                    {
                        _deploymentType = (DeploymentType)paramValue;
                    }
                    if (parameters.TryGetValue("DeployMode", out paramValue) && (null != paramValue))
                    {
                        _deployMode = (DeployMode)paramValue;
                    }
                    if (parameters.TryGetValue("SuppressRebootPassThru", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.SuppressRebootPassThru;
                    }
                    if (parameters.TryGetValue("TerminalServerMode", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.TerminalServerMode;
                    }
                    if (parameters.TryGetValue("DisableLogging", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.DisableLogging;
                    }
                    if (parameters.TryGetValue("AppVendor", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _appVendor = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppName", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _appName = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppVersion", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _appVersion = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppArch", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _appArch = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppLang", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _appLang = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppRevision", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _appRevision = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppScriptVersion", out paramValue) && (null != paramValue))
                    {
                        _appScriptVersion = (Version)paramValue;
                    }
                    if (parameters.TryGetValue("AppScriptDate", out paramValue) && (null != paramValue))
                    {
                        _appScriptDate = (DateTime)paramValue;
                    }
                    if (parameters.TryGetValue("AppScriptAuthor", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _appScriptAuthor = (string)paramValue;
                    }
                    if (parameters.TryGetValue("RequireAdmin", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.RequireAdmin;
                    }
                    if (parameters.TryGetValue("InstallName", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _installName = (string)paramValue;
                    }
                    if (parameters.TryGetValue("InstallTitle", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _installTitle = (string)paramValue;
                    }
                    if (parameters.TryGetValue("DeployAppScriptFriendlyName", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _deployAppScriptFriendlyName = (string)paramValue;
                    }
                    if (parameters.TryGetValue("DeployAppScriptVersion", out paramValue) && (null != paramValue))
                    {
                        _deployAppScriptVersion = (Version)paramValue;
                    }
                    if (parameters.TryGetValue("DeployAppScriptParameters", out paramValue) && (null != paramValue))
                    {
                        _deployAppScriptParameters = new((Dictionary<string, object>)paramValue);
                    }
                    if (parameters.TryGetValue("AppSuccessExitCodes", out paramValue) && (null != paramValue))
                    {
                        _appSuccessExitCodes = new((int[])paramValue);
                    }
                    if (parameters.TryGetValue("AppRebootExitCodes", out paramValue) && (null != paramValue))
                    {
                        _appRebootExitCodes = new((int[])paramValue);
                    }
                    if (parameters.TryGetValue("AppProcessesToClose", out paramValue) && (null != paramValue))
                    {
                        _appProcessesToClose = new((ProcessDefinition[])paramValue);
                    }
                    if (parameters.TryGetValue("ScriptDirectory", out paramValue) && (null != paramValue))
                    {
                        _scriptDirectory = new((string[])paramValue);
                    }
                    if (parameters.TryGetValue("DirFiles", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _dirFiles = (string)paramValue;
                    }
                    if (parameters.TryGetValue("DirSupportFiles", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _dirSupportFiles = (string)paramValue;
                    }
                    if (parameters.TryGetValue("DefaultMsiFile", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _defaultMsiFile = (string)paramValue;
                    }
                    if (parameters.TryGetValue("DefaultMstFile", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _defaultMstFile = (string)paramValue;
                    }
                    if (parameters.TryGetValue("DefaultMspFiles", out paramValue) && (null != paramValue))
                    {
                        _defaultMspFiles = new((string[])paramValue);
                    }
                    if (parameters.TryGetValue("DisableDefaultMsiProcessList", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.DisableDefaultMsiProcessList;
                    }
                    if (parameters.TryGetValue("ForceMsiDetection", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.ForceMsiDetection;
                    }
                    if (parameters.TryGetValue("ForceWimDetection", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.ForceWimDetection;
                    }
                    if (parameters.TryGetValue("NoSessionDetection", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.NoSessionDetection;
                    }
                    if (parameters.TryGetValue("NoOobeDetection", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.NoOobeDetection;
                    }
                    if (parameters.TryGetValue("LogName", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        _logName = (string)paramValue;
                    }
                    if (parameters.TryGetValue("NoProcessDetection", out paramValue))
                    {
                        if ((SwitchParameter)paramValue)
                        {
                            Settings |= DeploymentSettings.NoProcessDetection;
                        }
                        else
                        {
                            forceProcessDetection = true;
                        }
                    }
                }
                if (noExitOnClose.HasValue && noExitOnClose.Value)
                {
                    Settings |= DeploymentSettings.NoExitOnClose;
                }

                // Establish script directories.
                if (_scriptDirectory.Count > 0)
                {
                    foreach (var directory in _scriptDirectory)
                    {
                        if (string.IsNullOrWhiteSpace(_dirFiles) && Directory.Exists(Path.Combine(directory, "Files")))
                        {
                            _dirFiles = Path.Combine(directory, "Files");
                        }
                        if (string.IsNullOrWhiteSpace(_dirSupportFiles) && Directory.Exists(Path.Combine(directory, "SupportFiles")))
                        {
                            _dirSupportFiles = Path.Combine(directory, "SupportFiles");
                        }
                    }
                }


                #endregion
                #region DetectDefaultWim


                // If the default frontend hasn't been modified, and there's not already a mounted WIM file, check for WIM files and modify the install accordingly.
                if (string.IsNullOrWhiteSpace(_appName) || Settings.HasFlag(DeploymentSettings.ForceWimDetection))
                {
                    // Only proceed if there isn't already a mounted WIM file and we have a WIM file to use.
                    if ((MountedWimFiles.Count == 0) && !string.IsNullOrWhiteSpace(_dirFiles) && (Directory.GetFiles(_dirFiles, "*", SearchOption.TopDirectoryOnly).FirstOrDefault(static f => f.EndsWith(".wim", StringComparison.OrdinalIgnoreCase)) is string wimFile))
                    {
                        // Mount the WIM file and reset DirFiles to the mount point.
                        WriteInitialDivider(ref writtenDivider);
                        WriteLogEntry($"Discovered Zero-Config WIM file [{wimFile}].");
                        string mountPath = Path.Combine(_dirFiles, Path.GetRandomFileName());
                        ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Mount-ADTWimFile' -ImagePath $args[0] -Path $args[1] -Index 1"), wimFile, mountPath);
                        AddMountedWimFile(new(wimFile)); _dirFiles = mountPath;
                        WriteLogEntry($"Successfully mounted WIM file to [{mountPath}].");

                        // Subst the new DirFiles path to eliminate any potential path length issues.
                        IEnumerable<string> usedLetters = DriveInfo.GetDrives().Select(static d => d.Name);
                        if (DriveLetters.FirstOrDefault(l => !usedLetters.Contains(l)) is string availLetter)
                        {
                            availLetter = availLetter.Trim('\\'); WriteLogEntry($"Creating substitution drive [{availLetter}] for [{_dirFiles}].");
                            ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Invoke-ADTSubstOperation' -Drive $args[0] -Path $args[1]"), availLetter, _dirFiles);
                            _dirFiles = DirFilesSubstDrive = availLetter;
                        }
                        WriteLogEntry($"Using [{_dirFiles}] as the base DirFiles directory.");
                    }
                }


                #endregion
                #region DetectDefaultMsi


                // If the default frontend hasn't been modified, check for MSI / MST and modify the install accordingly.
                if (string.IsNullOrWhiteSpace(_appName) || Settings.HasFlag(DeploymentSettings.ForceMsiDetection))
                {
                    // Find the first MSI file in the Files folder and use that as our install.
                    if (string.IsNullOrWhiteSpace(_defaultMsiFile))
                    {
                        // Only proceed if the Files directory is set.
                        if (!string.IsNullOrWhiteSpace(_dirFiles))
                        {
                            // Get the first MSI file in the Files directory.
                            var msiFiles = Directory.GetFiles(_dirFiles, "*", SearchOption.TopDirectoryOnly).Where(static f => f.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));
                            var formattedOSArch = string.Empty;

                            // If we have a specific architecture MSI file, use that. Otherwise, use the first MSI file found.
                            if (msiFiles.FirstOrDefault(f => !f.EndsWith($".{envOSArchitecture.ToString().ToLower()}.msi", StringComparison.OrdinalIgnoreCase)) is string msiFile)
                            {
                                _defaultMsiFile = new FileInfo(msiFile).FullName;
                            }
                            else if (msiFiles.Any())
                            {
                                _defaultMsiFile = new FileInfo(msiFiles.First()).FullName;
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
                        WriteInitialDivider(ref writtenDivider);
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
                            if (!string.IsNullOrWhiteSpace(_dirFiles) && (Directory.GetFiles(_dirFiles, "*", SearchOption.TopDirectoryOnly).Where(static f => f.EndsWith(".msp", StringComparison.OrdinalIgnoreCase)).ToArray() is string[] mspFiles) && (mspFiles.Length > 0))
                            {
                                _defaultMspFiles = new(mspFiles);
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(_dirFiles) && (null != _defaultMspFiles.FirstOrDefault(static f => !Path.IsPathRooted(f))))
                        {
                            _defaultMspFiles = _defaultMspFiles.Select(f => !Path.IsPathRooted(f) ? Path.Combine(_dirFiles, f) : f).ToList().AsReadOnly();
                        }
                        if (_defaultMspFiles.Count > 0)
                        {
                            WriteLogEntry($"Discovered Zero-Config MSP installation file(s) [{string.Join(", ", _defaultMspFiles)}].");
                        }

                        // Generate list of MSI executables for use with Show-ADTInstallationWelcome.
                        if (!Settings.HasFlag(DeploymentSettings.DisableDefaultMsiProcessList))
                        {
                            var gmtpOutput = ModuleDatabase.InvokeScript(ScriptBlock.Create("$gmtpParams = @{ Path = $args[0] }; if ($args[1]) { $gmtpParams.Add('TransformPath', $args[1]) }; & $Script:CommandTable.'Get-ADTMsiTableProperty' @gmtpParams -Table File"), DefaultMsiFile!, DefaultMstFile!);
                            if (gmtpOutput.Count > 0)
                            {
                                var msiExecList = ((IReadOnlyDictionary<string, object>)gmtpOutput.First().BaseObject).Where(static p => Path.GetExtension(p.Key).Equals(".exe", StringComparison.OrdinalIgnoreCase)).Select(static p => new ProcessDefinition(Regex.Replace(Path.GetFileNameWithoutExtension(p.Key), "^_", string.Empty)));
                                if (msiExecList.Any())
                                {
                                    _appProcessesToClose = _appProcessesToClose.Concat(msiExecList).GroupBy(static p => p.Name, StringComparer.OrdinalIgnoreCase).Select(static g => g.First()).ToList().AsReadOnly();
                                    WriteLogEntry($"MSI Executable List [{string.Join(", ", msiExecList.Select(static p => p.Name))}].");
                                }
                            }
                        }

                        // Update our app variables with new values.
                        var msiProps = (IReadOnlyDictionary<string, object>)ModuleDatabase.InvokeScript(ScriptBlock.Create("$gmtpParams = @{ Path = $args[0] }; if ($args[1]) { $gmtpParams.Add('TransformPath', $args[1]) }; & $Script:CommandTable.'Get-ADTMsiTableProperty' @gmtpParams -Table Property"), DefaultMsiFile!, DefaultMstFile!).First().BaseObject;
                        if (string.IsNullOrWhiteSpace(_appName))
                        {
                            _appName = (string)msiProps["ProductName"];
                        }
                        if (string.IsNullOrWhiteSpace(_appVersion))
                        {
                            _appVersion = (string)msiProps["ProductVersion"];
                        }
                        WriteLogEntry($"App Vendor [{(string)msiProps["Manufacturer"]}].");
                        WriteLogEntry($"App Name [{(string)msiProps["ProductName"]}].");
                        WriteLogEntry($"App Version [{(string)msiProps["ProductVersion"]}].");
                        Settings |= DeploymentSettings.UseDefaultMsi;
                    }
                }


                #endregion
                #region SetAppProperties


                // Set up sample variables if Dot Sourcing the script, app details have not been specified.
                if (string.IsNullOrWhiteSpace(_appName))
                {
                    _appName = appDeployToolkitName;

                    if (!string.IsNullOrWhiteSpace(_appVendor))
                    {
                        _appVendor = null;
                    }
                    if (string.IsNullOrWhiteSpace(_appVersion))
                    {
                        _appVersion = appDeployMainScriptVersion;
                    }
                    if (string.IsNullOrWhiteSpace(_appLang))
                    {
                        _appLang = currentLanguage;
                    }
                    if (string.IsNullOrWhiteSpace(_appRevision))
                    {
                        _appRevision = "01";
                    }
                }

                // If we're left with a blank AppName, throw a terminating error.
                if (string.IsNullOrWhiteSpace(_appName))
                {
                    throw new ArgumentNullException(nameof(AppName), "The application name was not specified.");
                }


                #endregion
                #region SetInstallProperties


                // Build the Installation Title.
                if (string.IsNullOrWhiteSpace(_installTitle))
                {
                    _installTitle = $"{_appVendor} {_appName} {_appVersion}".Trim();
                }
                _installTitle = Regex.Replace(_installTitle, @"\s{2,}", string.Empty);

                // Build the Installation Name.
                if (string.IsNullOrWhiteSpace(_installName))
                {
                    _installName = $"{_appVendor}_{_appName}_{_appVersion}_{_appArch}_{_appLang}_{_appRevision}";
                }
                var invalidChars = (Regex)adtEnv["invalidFileNameCharsRegExPattern"]!;
                _installName = invalidChars.Replace(Regex.Replace(_installName!.Trim('_').Replace(" ", null), "_+", "_"), string.Empty);

                // Set the Defer History registry path.
                RegKeyDeferBase = $@"{configToolkit["RegPath"]}\{appDeployToolkitName}\DeferHistory";
                RegKeyDeferHistory = $@"{RegKeyDeferBase}\{_installName}";


                #endregion
                #region InitLogging


                // Generate log paths from our installation properties.
                if ((bool)configToolkit["CompressLogs"]!)
                {
                    // If the temp log folder already exists from a previous ZIP operation, then delete all files in it to avoid issues.
                    var logTempFolder = Path.Combine((string)adtEnv["envTemp"]!, $"{_installName}_{_deploymentType}");
                    if (Directory.Exists(logTempFolder))
                    {
                        Directory.Delete(logTempFolder, true);
                    }
                    _logPath = Directory.CreateDirectory(logTempFolder).FullName;
                }
                else
                {
                    _logPath = Directory.CreateDirectory((string)configToolkit["LogPath"]!).FullName;
                }

                // Append subfolder path if configured to do so.
                if ((bool)configToolkit["LogToHierarchy"]!)
                {
                    _logPath = Directory.CreateDirectory(Path.Combine(_logPath, $@"{_appVendor}\{_appName}\{_appVersion}".Replace(@"\\", null))).FullName;
                }
                else if ((bool)configToolkit["LogToSubfolder"]!)
                {
                    _logPath = Directory.CreateDirectory(Path.Combine(_logPath, _installName)).FullName;
                }

                // Generate the log filename to use. Append the username to the log file name if the toolkit is not running as an administrator,
                // since users do not have the rights to modify files in the ProgramData folder that belong to other users.
                if (string.IsNullOrWhiteSpace(_logName))
                {
                    if (isAdmin)
                    {
                        _logName = $"{_installName}_{appDeployToolkitName}_{_deploymentType}.log";
                    }
                    else
                    {
                        _logName = $"{_installName}_{appDeployToolkitName}_{_deploymentType}_{adtEnv["envUserName"]}.log";
                    }
                }
                _logName = invalidChars.Replace(_logName, string.Empty);
                string logFile = Path.Combine(_logPath, _logName);
                FileInfo logFileInfo = new(logFile);
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
                        string archiveLogFilePath = Path.Combine(_logPath, archiveLogFileName);
                        var logMaxHistory = (int)configToolkit["LogMaxHistory"]!;

                        // Log message about archiving the log file.
                        if (logFileSizeExceeded)
                        {
                            WriteLogEntry($"Maximum log file size [{logMaxSize} MB] reached. Rename log file to [{archiveLogFileName}].", LogSeverity.Warning);
                        }

                        // Rename the file.
                        logFileInfo.MoveTo(archiveLogFilePath);

                        // Start new log file and log message about archiving the old log file.
                        if (logFileSizeExceeded)
                        {
                            WriteLogEntry($"Previous log file was renamed to [{archiveLogFileName}] because maximum log file size of [{logMaxSize} MB] was reached.", LogSeverity.Warning);
                        }

                        // Get all log files sorted by last write time.
                        IOrderedEnumerable<FileInfo> logFiles = new DirectoryInfo(_logPath).GetFiles($"{logFileNameOnly}*.log").Where(static f => f.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)).OrderBy(static f => f.LastWriteTime);
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
                        WriteLogEntry($"Failed to rotate the log file [{logFile}]: {ex}", LogSeverity.Error);
                    }
                }

                // Flush our log buffer out to disk.
                if (!DisableLogging && LogBuffer.Count > 0)
                {
                    if (!Enum.TryParse<LogStyle>((string)configToolkit["LogStyle"]!, out var configStyle))
                    {
                        throw new InvalidOperationException("Unable to retrieve the LogStyle from the config for an unknown reason.");
                    }
                    using StreamWriter logFileWriter = new(Path.Combine(_logPath, LogName), true, LogUtilities.LogEncoding);
                    logFileWriter.WriteLine(string.Join(Environment.NewLine, configStyle == LogStyle.CMTrace ? LogBuffer.Select(static o => o.CMTraceLogLine) : LogBuffer.Select(static o => o.LegacyLogLine)));
                }

                // Open log file with commencement message.
                WriteInitialDivider(ref writtenDivider);
                WriteLogEntry($"[{_installName}] {_deploymentType.ToString().ToLower()} started.");


                #endregion
                #region LogScriptInfo


                // Announce provided deployment script info.
                if (null != _appScriptVersion)
                {
                    WriteLogEntry($"[{_installName}] script version is [{_appScriptVersion}].");
                }
                if ((_appScriptDate?.ToString("O").Split('T')[0] is string appScriptDate) && appScriptDate != "2000-12-31")
                {
                    WriteLogEntry($"[{_installName}] script date is [{appScriptDate}].");
                }
                if (!string.IsNullOrWhiteSpace(_appScriptAuthor) && _appScriptAuthor != "<author name>")
                {
                    WriteLogEntry($"[{_installName}] script author is [{_appScriptAuthor}].");
                }
                if (!string.IsNullOrWhiteSpace(_deployAppScriptFriendlyName))
                {
                    if (null != _deployAppScriptVersion)
                    {
                        WriteLogEntry($"[{_deployAppScriptFriendlyName}] script version is [{_deployAppScriptVersion}].");
                    }
                    if (_deployAppScriptParameters?.Count > 0)
                    {
                        WriteLogEntry($"The following parameters were passed to [{_deployAppScriptFriendlyName}]: [{PowerShellUtilities.ConvertDictToPowerShellArgs(_deployAppScriptParameters).Replace("''", "'")}].");
                    }
                }
                var adtDirectories = (PSObject)adtData.Properties["Directories"].Value;
                var adtDurations = (PSObject)adtData.Properties["Durations"].Value;
                WriteLogEntry($"[{appDeployToolkitName}] module version is [{appDeployMainScriptVersion}].");
                WriteLogEntry($"[{appDeployToolkitName}] module imported in [{((TimeSpan)adtDurations.Properties["ModuleImport"].Value).TotalSeconds}] seconds.");
                WriteLogEntry($"[{appDeployToolkitName}] module initialized in [{((TimeSpan)adtDurations.Properties["ModuleInit"].Value).TotalSeconds}] seconds.");
                WriteLogEntry($"[{appDeployToolkitName}] module path is ['{adtEnv["appDeployToolkitPath"]}'].");
                WriteLogEntry($"[{appDeployToolkitName}] config path is ['{string.Join("', '", (string[])adtDirectories.Properties["Config"].Value)}'].");
                WriteLogEntry($"[{appDeployToolkitName}] string path is ['{string.Join("', '", (string[])adtDirectories.Properties["Strings"].Value)}'].");

                // Announce session instantiation mode.
                if (null != callerSessionState)
                {
                    WriteLogEntry($"[{appDeployToolkitName}] session mode is [Compatibility]. This mode is for the transition of v3.x scripts and is not for new development.", LogSeverity.Warning);
                    WriteLogEntry("Information on how to migrate this script to Native mode is available at [https://psappdeploytoolkit.com/].", LogSeverity.Warning);
                }
                else
                {
                    WriteLogEntry($"[{appDeployToolkitName}] session mode is [Native].");
                }

                // Test and warn if this toolkit was started with ServiceUI anywhere as a parent process.
                if (AccountUtilities.CallerUsingServiceUI)
                {
                    WriteLogEntry($"[{appDeployToolkitName}] was started with ServiceUI as a parent process. This is no longer required with PSAppDeployToolkit 4.1.x or higher and will be forbidden in a later release.", LogSeverity.Warning);
                }


                #endregion
                #region LogSystemInfo


                // Report on all determined system info.
                WriteLogEntry($"Computer Name is [{adtEnv["envComputerNameFQDN"]}].");
                WriteLogEntry($"Current User is [{processNtAccount}].");
                WriteLogEntry($"OS Version is [{adtEnv["envOSName"]}{$" {adtEnv["envOSServicePack"]}".Trim()} {envOSArchitecture} {adtEnv["envOSVersion"]}].");
                WriteLogEntry($"OS Type is [{adtEnv["envOSProductTypeName"]}].");
                WriteLogEntry($"Hardware Platform is [{adtEnv["envHardwareType"]}].");
                WriteLogEntry($"Current Culture is [{adtEnv["culture"]}], language is [{currentLanguage}] and UI language is [{adtEnv["currentUILanguage"]}].");
                WriteLogEntry($"PowerShell Host is [{((PSHost)adtEnv["envHost"]!).Name}] with version [{adtEnv["envHostVersionSemantic"] ?? adtEnv["envHostVersion"]}].");
                WriteLogEntry($"PowerShell Version is [{adtEnv["envPSVersionSemantic"] ?? adtEnv["envPSVersion"]} {adtEnv["psArchitecture"]}].");
                WriteLogEntry($"PowerShell Process Path is [{adtEnv["envPSProcessPath"]}].");
                if (adtEnv["envCLRVersion"] is Version envCLRVersion)
                {
                    WriteLogEntry($"PowerShell CLR (.NET) version is [{envCLRVersion}].");
                }


                #endregion
                #region LogUserInfo


                // Perform checks that need to factor in user context.
                if (usersLoggedOn?.Count > 0)
                {
                    // Log details for all currently logged on users.
                    WriteLogEntry($"The following users are logged on to the system: [{string.Join(", ", usersLoggedOn.Select(static u => u.Value))}].");
                    WriteLogEntry($"Session information for all logged on users:\n\n{adtEnv["LoggedOnUserSessionsText"]}", false);

                    // Check if the current process is running in the context of one of the logged on users
                    if (adtEnv["CurrentLoggedOnUserSession"] is SessionInfo CurrentLoggedOnUserSession)
                    {
                        WriteLogEntry($"Current process is running with user account [{processNtAccount}] under logged on user session for [{CurrentLoggedOnUserSession.NTAccount}].");
                    }
                    else
                    {
                        WriteLogEntry($"Current process is running under a system account [{processNtAccount}].");
                    }

                    // Display account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
                    if (adtEnv["CurrentConsoleUserSession"] is SessionInfo CurrentConsoleUserSession)
                    {
                        WriteLogEntry($"The following user is the console user [{CurrentConsoleUserSession.NTAccount}] (user with control of physical monitor, keyboard, and mouse).");
                    }
                    else
                    {
                        WriteLogEntry("There is no console user logged on (user with control of physical monitor, keyboard, and mouse).");
                    }

                    // Display the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
                    if (null != RunAsActiveUser)
                    {
                        WriteLogEntry($"The active logged on user who will receive UI elements is [{RunAsActiveUser.NTAccount}].");
                    }
                }
                else
                {
                    WriteLogEntry("No users are logged on to the system.");
                }


                #endregion
                #region TestSessionViability


                // Check current permissions and exit if not running with Administrator rights.
                if (Settings.HasFlag(DeploymentSettings.RequireAdmin) && !isAdmin)
                {
                    throw new UnauthorizedAccessException($"This deployment requires administrative permissions and the current user is not an Administrator, or PowerShell is not elevated. Please re-run the deployment script as an Administrator and try again.");
                }

                // Check if the caller explicitly wants interactivity but we can't do it.
                if (_deployMode == DeployMode.Interactive && null == RunAsActiveUser && !IsProcessUserInteractive)
                {
                    throw new NotSupportedException($"This deployment explicitly requires interactivity, however there are no suitable logged on users available and this process is running non-interactively.");
                }


                #endregion
                #region LogLanguageInfo


                // Log which language's UI messages are loaded from the config file
                WriteLogEntry($"The current execution context has a primary UI language of [{adtEnv["uiculture"]}].");

                // Advise whether the UI language was overridden.
                if (configUI["LanguageOverride"] is string languageOverride)
                {
                    WriteLogEntry($"The config file was configured to override the detected primary UI language with the following UI language: [{languageOverride}].");
                }
                WriteLogEntry($"The following locale was used to import UI messages from the strings.psd1 files: [{adtData.Properties["Language"].Value}].");


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
                #region SetDeploymentProperties


                // Check if the device has completed the OOBE or not.
                bool deployModeChanged = false;
                if ((Environment.OSVersion.Version >= new Version(10, 0, 16299, 0)) && !DeviceUtilities.IsOOBEComplete())
                {
                    if (deployModeChanged)
                    {
                        WriteLogEntry($"Detected OOBE in progress but deployment has already been changed to [{_deployMode}]");
                    }
                    if (_deployMode != DeployMode.Auto)
                    {
                        WriteLogEntry($"Detected OOBE in progress but deployment mode was explicitly set to [{_deployMode}].");
                    }
                    else if (!Settings.HasFlag(DeploymentSettings.NoOobeDetection))
                    {
                        WriteLogEntry($"Detected OOBE in progress, changing deployment mode to [{_deployMode = DeployMode.Silent}].");
                        deployModeChanged = true;
                    }
                    else
                    {
                        WriteLogEntry("Detected OOBE in progress but toolkit is configured to not adjust deployment mode.");
                    }
                }
                else if (Process.GetProcessesByName("WWAHost").Length > 0)
                {
                    // If WWAHost is running, the device might be within the User ESP stage. But first, confirm whether the device is in Autopilot.
                    WriteLogEntry("The WWAHost process is running, confirming the device is Autopilot-enrolled.");
                    if (!string.IsNullOrWhiteSpace((string?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Provisioning\Diagnostics\AutoPilot", "CloudAssignedTenantId", null)))
                    {
                        WriteLogEntry("The device is Autopilot-enrolled, checking ESP User Account setup phase.");
                        if (RunAsActiveUser?.SID is SecurityIdentifier userSid)
                        {
                            var fsRegData = moduleSessionState.InvokeProvider.Property.Get([$@"Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Enrollments\*\FirstSync\{userSid}"], null, false).FirstOrDefault();
                            if (null != fsRegData)
                            {
                                if (fsRegData.Properties["IsSyncDone"] is PSPropertyInfo isSyncDone && isSyncDone.Value is int syncDoneValue)
                                {
                                    if (syncDoneValue == 0)
                                    {
                                        if (deployModeChanged)
                                        {
                                            WriteLogEntry($"The ESP User Account Setup phase is still in progress but deployment has already been changed to [{_deployMode}]");
                                        }
                                        else if (_deployMode != DeployMode.Auto)
                                        {
                                            WriteLogEntry($"The ESP User Account Setup phase is still in progress but deployment mode was explicitly set to [{_deployMode}].");
                                        }
                                        else if ((bool)configToolkit["OobeDetection"]!)
                                        {
                                            WriteLogEntry($"The ESP User Account Setup phase is still in progress, changing deployment mode to [{_deployMode = DeployMode.Silent}].");
                                            deployModeChanged = true;
                                        }
                                        else
                                        {
                                            WriteLogEntry("The ESP User Account Setup phase is still in progress but toolkit is configured to not adjust deployment mode.");
                                        }
                                    }
                                    else if (syncDoneValue == 1)
                                    {
                                        WriteLogEntry("The ESP User Account Setup phase is already complete.");
                                    }
                                    else
                                    {
                                        WriteLogEntry($"The FirstSync property for SID [{userSid}] has an indeterminate value of [{syncDoneValue}].", LogSeverity.Warning);
                                    }
                                }
                                else
                                {
                                    WriteLogEntry($"Could not find a FirstSync property for SID [{userSid}].", LogSeverity.Warning);
                                }
                            }
                            else
                            {
                                WriteLogEntry($"Could not find any FirstSync information for SID [{userSid}].");
                            }
                        }
                        else
                        {
                            WriteLogEntry("The device currently has no users logged on.");
                        }
                    }
                    else
                    {
                        WriteLogEntry("The device is not Autopilot-enrolled.");
                    }
                }
                else
                {
                    WriteLogEntry("Device has completed the OOBE and toolkit is not running with an active ESP in progress.");
                }

                // Perform session 0 evaluation.
                if ((bool)adtEnv["SessionZero"]!)
                {
                    if (deployModeChanged)
                    {
                        WriteLogEntry($"Session 0 detected but deployment has already been changed to [{_deployMode}]");
                    }
                    else if (_deployMode != DeployMode.Auto)
                    {
                        WriteLogEntry($"Session 0 detected but deployment mode was explicitly set to [{_deployMode}].");
                    }
                    else if (!Settings.HasFlag(DeploymentSettings.NoSessionDetection))
                    {
                        // If the process is not able to display a UI, enable silent mode.
                        if (null == RunAsActiveUser)
                        {
                            // If there's no users logged on but we're interactive anyway, don't change the DeployMode.
                            if (!IsProcessUserInteractive)
                            {
                                WriteLogEntry($"Session 0 detected, no users logged on and process not running in user interactive mode; deployment mode set to [{_deployMode = DeployMode.Silent}].");
                                deployModeChanged = true;
                            }
                            else
                            {
                                WriteLogEntry($"Session 0 detected, no users logged on but process running in user interactive mode.");
                            }
                        }
                        else
                        {
                            WriteLogEntry("Session 0 detected, user(s) logged on to interact if required.");
                        }
                    }
                    else
                    {
                        WriteLogEntry("Session 0 detected but toolkit is configured to not adjust deployment mode.");
                    }
                }
                else
                {
                    WriteLogEntry("Session 0 not detected, toolkit running as non-SYSTEM user account.");
                }

                // Evaluate processes to close if they're specified.
                if (forceProcessDetection || _appProcessesToClose.Count > 0)
                {
                    if (deployModeChanged)
                    {
                        WriteLogEntry($"The processes ['{string.Join("', '", _appProcessesToClose.Select(static p => p.Name))}'] were specified as requiring closure but deployment has already been changed to [{_deployMode}]");
                    }
                    else if (_deployMode != DeployMode.Auto)
                    {
                        WriteLogEntry($"The processes ['{string.Join("', '", _appProcessesToClose.Select(static p => p.Name))}'] were specified as requiring closure but deployment mode was explicitly set to [{_deployMode}].");
                    }
                    else if (!Settings.HasFlag(DeploymentSettings.NoProcessDetection))
                    {
                        if (ProcessUtilities.GetRunningProcesses(_appProcessesToClose) is var runningProcs && (runningProcs.Count == 0))
                        {
                            if (!forceProcessDetection)
                            {
                                WriteLogEntry($"The processes ['{string.Join("', '", _appProcessesToClose.Select(static p => p.Name))}'] were specified as requiring closure but none were running, changing deployment mode to [{_deployMode = DeployMode.Silent}].");
                            }
                            else
                            {
                                WriteLogEntry($"No processes were specified as requiring closure and -NoProcessDetection was explicitly set to false, changing deployment mode to [{_deployMode = DeployMode.Silent}].");
                            }
                            deployModeChanged = true;
                        }
                        else
                        {
                            WriteLogEntry($"The processes ['{string.Join("', '", runningProcs.Select(static p => p.Process.ProcessName).Distinct())}'] were found to be running and will require closure.");
                        }
                    }
                    else
                    {
                        WriteLogEntry($"The processes ['{string.Join("', '", _appProcessesToClose.Select(static p => p.Name))}'] were specified as requiring closure but toolkit is configured to not adjust deployment mode.");
                    }
                }
                else
                {
                    WriteLogEntry($"No processes were specified as requiring closure.");
                }

                // If we're still in Auto mode, then set the deployment mode to Interactive.
                if (_deployMode == DeployMode.Auto)
                {
                    _deployMode = DeployMode.Interactive;
            }

                // Set Deploy Mode switches.
                WriteLogEntry($"Installation is running in [{_deployMode}] mode.");
                switch (_deployMode)
                {
                    case DeployMode.Silent:
                        Settings |= DeploymentSettings.NonInteractive;
                        Settings |= DeploymentSettings.Silent;
                        break;
                    case DeployMode.NonInteractive:
                        Settings |= DeploymentSettings.NonInteractive;
                        break;
                }

                // Check deployment type (install/uninstall).
                WriteLogEntry($"Deployment type is [{_deploymentType}].");


                #endregion
                #region Finalization


                // If terminal server mode was specified, change the installation mode to support it.
                if (TerminalServerMode)
                {
                    ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Enable-ADTTerminalServerInstallMode'"));
                }

                // Export session's public variables to the user's scope. For these, we can't capture the Set-Variable
                // PassThru data as syntax like `$var = 'val'` constructs a new PSVariable every time.
                if (null != callerSessionState)
                {
                    foreach (PropertyInfo property in typeof(DeploymentSession).GetProperties())
                    {
                        callerSessionState.PSVariable.Set(new(property.Name, property.GetValue(this)));
                    }
                    foreach (FieldInfo field in typeof(DeploymentSession).GetFields())
                    {
                        callerSessionState.PSVariable.Set(new(field.Name, field.GetValue(this)));
                    }
                    CallerSessionState = callerSessionState;
                }


                #endregion
            }
            catch (UnauthorizedAccessException ex)
            {
                WriteLogEntry(ex.Message, LogSeverity.Error);
                SetExitCode(60008); Close();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
            catch (NotSupportedException ex)
            {
                WriteLogEntry(ex.Message, LogSeverity.Error);
                SetExitCode(deferExitCode); Close();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
            catch (Exception ex)
            {
                WriteLogEntry($"Failure occurred while instantiating new deployment session: {ex}", LogSeverity.Error);
                SetExitCode(60008); Close();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }


        #endregion
        #region Methods.


        /// <summary>
        /// Closes the session and releases resources.
        /// </summary>
        /// <returns>The exit code.</returns>
        public int Close()
        {
            // Throw if this object has already been disposed.
            if (Settings.HasFlag(DeploymentSettings.Disposed))
            {
                throw new ObjectDisposedException(typeof(DeploymentSession).Name, "This object has already been disposed.");
            }

            // Establish initial variable values.
            var adtExitCode = ModuleDatabase.Get().Properties["LastExitCode"];

            // If terminal server mode was specified, revert the installation mode to support it.
            if (TerminalServerMode)
            {
                ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Disable-ADTTerminalServerInstallMode'"));
            }

            // Process resulting exit code.
            string deployString = $"{(!string.IsNullOrWhiteSpace(InstallName) ? $"[{Regex.Replace(InstallName, @"(?<!\{)\{(?!\{)|(?<!\})\}(?!\})", "$0$0")}] {DeploymentType.ToString().ToLower()}" : $"{ModuleDatabase.GetEnvironment()["appDeployToolkitName"]} deployment")} completed in [{{0}}] seconds with exit code [{{1}}].";
            DeploymentStatus deploymentStatus = GetDeploymentStatus();
            switch (deploymentStatus)
            {
                case DeploymentStatus.FastRetry:
                    // Just advise of the exit code with the appropriate severity.
                    WriteLogEntry(string.Format(deployString, (DateTime.Now - CurrentDateTime).TotalSeconds, ExitCode), LogSeverity.Warning);
                    break;
                case DeploymentStatus.Error:
                    WriteLogEntry(string.Format(deployString, (DateTime.Now - CurrentDateTime).TotalSeconds, ExitCode), LogSeverity.Error);
                    break;
                default:
                    // Clean up app deferral history.
                    ResetDeferHistory();

                    // Handle reboot prompts on successful script completion.
                    if (deploymentStatus == DeploymentStatus.RestartRequired && !SuppressRebootPassThru)
                    {
                        WriteLogEntry("A restart has been flagged as required.");
                    }
                    else
                    {
                        ExitCode = 0;
                    }
                    WriteLogEntry(string.Format(deployString, (DateTime.Now - CurrentDateTime).TotalSeconds, ExitCode), 0);
                    break;
            }

            // Update the module's last tracked exit code.
            if (ExitCode != 0)
            {
                adtExitCode.Value = ExitCode;
            }

            // Remove any subst paths if created in the zero-config WIM code.
            if (!string.IsNullOrWhiteSpace(DirFilesSubstDrive))
            {
                ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Invoke-ADTSubstOperation' -Drive $args[0] -Delete"), DirFilesSubstDrive!);
            }

            // Unmount any stored WIM file entries.
            if (MountedWimFiles.Count > 0)
            {
                MountedWimFiles.Reverse(); ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Dismount-ADTWimFile' -ImagePath $args[0]"), MountedWimFiles);
                MountedWimFiles.Clear();
            }

            // Write out a log divider to indicate the end of logging.
            WriteLogDivider();
            Settings |= DeploymentSettings.Disposed;

            // Extrapolate the Toolkit options from the config hashtable.
            var configToolkit = (Hashtable)ModuleDatabase.GetConfig()["Toolkit"]!;

            // Compress log files if configured to do so.
            if ((bool)configToolkit["CompressLogs"]!)
            {
                // Archive the log files to zip format and then delete the temporary logs folder.
                string destArchiveFileName = $"{InstallName}_{DeploymentType}_{{0}}.zip";
                var destArchiveFilePath = Directory.CreateDirectory((string)configToolkit["LogPath"]!);
                try
                {
                    // Get all archive files sorted by last write time.
                    IOrderedEnumerable<FileInfo> archiveFiles = destArchiveFilePath.GetFiles(string.Format(destArchiveFileName, "*")).Where(static f => f.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).OrderBy(static f => f.LastWriteTime);
                    destArchiveFileName = string.Format(destArchiveFileName, CurrentDateTime.ToString("O").Split('.')[0].Replace(":", null));

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
                    ZipFile.CreateFromDirectory(LogPath, Path.Combine(destArchiveFilePath.FullName, destArchiveFileName), CompressionLevel.Optimal, false);
                    Directory.Delete(LogPath, true);
                }
                catch (Exception ex)
                {
                    WriteLogEntry($"Failed to manage archive file [{destArchiveFileName}]: {ex}", LogSeverity.Error);
                }
            }

            // Return the module's cached exit code to the caller.
            return (int)adtExitCode.Value;
        }

        /// <summary>
        /// Gets the host log stream mode based on the configuration and parameters.
        /// </summary>
        /// <param name="writeHost"></param>
        /// <returns></returns>
        private HostLogStream GetHostLogStreamMode(bool? writeHost = null)
        {
            var configToolkit = (Hashtable)ModuleDatabase.GetConfig()["Toolkit"]!;
            if ((null != writeHost && !writeHost.Value) || !(bool)configToolkit["LogWriteToHost"]!)
            {
                return HostLogStream.None;
            }
            else if ((bool)configToolkit["LogHostOutputToStdStreams"]!)
            {
                return HostLogStream.Console;
            }
            else
            {
                return HostLogStream.Host;
            }
        }

        /// <summary>
        /// Writes a log entry with detailed parameters.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="debugMessage">Whether it is a debug message.</param>
        /// <param name="severity">The severity level.</param>
        /// <param name="source">The source of the log entry.</param>
        /// <param name="scriptSection">The script section.</param>
        /// <param name="logFileDirectory">The log file directory.</param>
        /// <param name="logFileName">The log file name.</param>
        /// <param name="logType">The type of log.</param>
        /// <param name="hostLogStream">What stream to write the message to.</param>
        public IReadOnlyList<LogEntry> WriteLogEntry(IReadOnlyList<string> message, bool debugMessage, LogSeverity? severity = null, string? source = null, string? scriptSection = null, string? logFileDirectory = null, string? logFileName = null, LogStyle? logType = null, HostLogStream? hostLogStream = null)
        {
            var logEntries = LogUtilities.WriteLogEntry(message, hostLogStream ?? GetHostLogStreamMode(), debugMessage, severity, source, scriptSection ?? InstallPhase, logFileDirectory ?? (!DisableLogging ? LogPath : null), logFileName ?? (!DisableLogging ? LogName : null), logType);
            LogBuffer.AddRange(logEntries);
            return logEntries;
        }

        /// <summary>
        /// Writes a log entry with a message array.
        /// </summary>
        /// <param name="message">The log message array.</param>
        public void WriteLogEntry(IReadOnlyList<string> message) => WriteLogEntry(message, false, null, null, null, null, null, null, null);

        /// <summary>
        /// Writes a log entry with a single message.
        /// </summary>
        /// <param name="message">The log message.</param>
        public void WriteLogEntry(string message) => WriteLogEntry([message], false, null, null, null, null, null, null, null);

        /// <summary>
        /// Writes a log entry with a single message and severity.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="severity">The severity level.</param>
        public void WriteLogEntry(string message, LogSeverity severity) => WriteLogEntry([message], false, severity, null, null, null, null, null, null);

        /// <summary>
        /// Writes a log entry with a single message and source.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="source">The source of the message.</param>
        public void WriteLogEntry(string message, string source) => WriteLogEntry([message], false, null, source, null, null, null, null, null);

        /// <summary>
        /// Writes a log entry with a single message, severity, and source.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="severity">The severity level.</param>
        /// <param name="source">The source of the message.</param>
        public void WriteLogEntry(string message, LogSeverity severity, string source) => WriteLogEntry([message], false, severity, source, null, null, null, null, null);

        /// <summary>
        /// Writes a log entry with a single message and host write option.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="writeHost">Whether to write to the host.</param>
        public void WriteLogEntry(string message, bool writeHost) => WriteLogEntry([message], false, null, null, null, null, null, null, GetHostLogStreamMode(writeHost));

        /// <summary>
        /// Writes a log divider.
        /// </summary>
        private void WriteLogDivider() => WriteLogEntry(LogUtilities.LogDivider);

        /// <summary>
        /// Writes a divider if one hasn't been written already.
        /// </summary>
        private void WriteInitialDivider(ref bool write)
        {
            if (write)
            {
                return;
            }
            WriteLogDivider();
            write = true;
        }

        /// <summary>
        /// Gets the log buffer as a read-only list.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<LogEntry> GetLogBuffer() => LogBuffer.AsReadOnly();

        /// <summary>
        /// Gets the value of a property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private T GetPropertyValue<T>([CallerMemberName] string propertyName = null!)
        {
            if (null != CallerSessionState)
            {
                return (T)CallerSessionState.PSVariable.GetValue(propertyName);
            }
            return (T)(Enum.TryParse(propertyName, out DeploymentSettings flag) ? Settings.HasFlag(flag) : BackingFields[propertyName!].GetValue(this)!);
        }

        /// <summary>
        /// Sets the value of a property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        private void SetPropertyValue<T>(T value, [CallerMemberName] string propertyName = null!)
        {
            if (null != CallerSessionState)
            {
                CallerSessionState.PSVariable.Set(new(propertyName, value));
            }
            BackingFields[propertyName!].SetValue(this, value);
        }

        /// <summary>
        /// Tests the deferral history path.
        /// </summary>
        /// <returns>True if the deferral history path exists; otherwise, false.</returns>
        private bool TestDeferHistoryPath() => ModuleDatabase.GetSessionState().InvokeProvider.Item.Exists(RegKeyDeferHistory, true, true);

        /// <summary>
        /// Creates the deferral history path.
        /// </summary>
        private void CreateDeferHistoryPath() => ModuleDatabase.GetSessionState().InvokeProvider.Item.New([RegKeyDeferBase], InstallName, "None", null, true);

        /// <summary>
        /// Gets the deferral history.
        /// </summary>
        /// <returns>The deferral history.</returns>
        public DeferHistory? GetDeferHistory()
        {
            if (string.IsNullOrWhiteSpace(RegKeyDeferHistory) || !TestDeferHistoryPath())
            {
                return null;
            }
            WriteLogEntry("Getting deferral history...");
            var history = ModuleDatabase.GetSessionState().InvokeProvider.Property.Get(RegKeyDeferHistory, null).FirstOrDefault();
            if (null == history)
            {
                return null;
            }
            var deferRunIntervalLastTime = history.Properties["DeferRunIntervalLastTime"]?.Value;
            var deferTimesRemaining = history.Properties["DeferTimesRemaining"]?.Value;
            var deferDeadline = history.Properties["DeferDeadline"]?.Value;
            if (null == deferRunIntervalLastTime && null == deferTimesRemaining && null == deferDeadline)
            {
                return null;
            }
            return new(
                null != deferTimesRemaining ? deferTimesRemaining is string ? (uint)int.Parse((string)deferTimesRemaining) : (uint)(int)deferTimesRemaining : null,
                null != deferDeadline ? DateTime.Parse((string)deferDeadline) : null,
                null != deferRunIntervalLastTime ? DateTime.Parse((string)deferRunIntervalLastTime) : null);
        }

        /// <summary>
        /// Sets the deferral history.
        /// </summary>
        /// <param name="deferDeadline">The deferral deadline.</param>
        /// <param name="deferTimesRemaining">The deferral times remaining.</param>
        /// <param name="deferRunInterval">The interval as a TimeSpan before prompting the user again after a deferral.</param>
        /// <param name="deferRunIntervalLastTime">The timestamp of the last deferRunInterval.</param>
        public void SetDeferHistory(uint? deferTimesRemaining, DateTime? deferDeadline, TimeSpan? deferRunInterval, DateTime? deferRunIntervalLastTime)
        {
            // Get the module's session state before proceeding.
            var moduleSessionState = ModuleDatabase.GetSessionState();

            // Test each property and set it if it exists.
            if (null != deferTimesRemaining)
            {
                var deferTimesRemainingValue = deferTimesRemaining.Value;
                WriteLogEntry($"Setting deferral history: [DeferTimesRemaining = {deferTimesRemainingValue}].");
                if (!TestDeferHistoryPath())
                {
                    CreateDeferHistoryPath();
                }
                moduleSessionState.InvokeProvider.Property.New([RegKeyDeferHistory], "DeferTimesRemaining", RegistryValueKind.DWord.ToString(), deferTimesRemainingValue, true, true);
            }
            if (null != deferDeadline)
            {
                var deferDeadlineValue = deferDeadline.Value.ToUniversalTime().ToString("O");
                WriteLogEntry($"Setting deferral history: [DeferDeadline = {deferDeadlineValue}].");
                if (!TestDeferHistoryPath())
                {
                    CreateDeferHistoryPath();
                }
                moduleSessionState.InvokeProvider.Property.New([RegKeyDeferHistory], "DeferDeadline", RegistryValueKind.String.ToString(), deferDeadlineValue, true, true);
            }
            if (null != deferRunInterval)
            {
                var deferRunIntervalValue = deferRunInterval.Value.ToString("c");
                WriteLogEntry($"Setting deferral history: [DeferRunInterval = {deferRunIntervalValue}].");
                if (!TestDeferHistoryPath())
                {
                    CreateDeferHistoryPath();
                }
                moduleSessionState.InvokeProvider.Property.New([RegKeyDeferHistory], "DeferRunInterval", RegistryValueKind.String.ToString(), deferRunIntervalValue, true, true);
            }
            if (null != deferRunIntervalLastTime)
            {
                var deferRunIntervalLastTimeValue = deferRunIntervalLastTime.Value.ToUniversalTime().ToString("O");
                WriteLogEntry($"Setting deferral history: [DeferRunIntervalLastTime = {deferRunIntervalLastTimeValue}].");
                if (!TestDeferHistoryPath())
                {
                    CreateDeferHistoryPath();
                }
                moduleSessionState.InvokeProvider.Property.New([RegKeyDeferHistory], "DeferRunIntervalLastTime", RegistryValueKind.String.ToString(), deferRunIntervalLastTimeValue, true, true);
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
                ModuleDatabase.GetSessionState().InvokeProvider.Item.Remove(RegKeyDeferHistory, true);
            }
        }

        /// <summary>
        /// Gets the deployment status.
        /// </summary>
        /// <returns>The deployment status.</returns>
        public DeploymentStatus GetDeploymentStatus()
        {
            // Extrapolate the UI options from the config hashtable.
            var configUI = (Hashtable)ModuleDatabase.GetConfig()["UI"]!;

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
        public void AddMountedWimFile(FileInfo wimFile) => MountedWimFiles.Add(wimFile);

        /// <summary>
        /// Determines whether the session is allowed to exit PowerShell on close.
        /// </summary>
        /// <returns>True if the session can exit; otherwise, false.</returns>
        public bool CanExitOnClose() => !Settings.HasFlag(DeploymentSettings.NoExitOnClose);

        /// <summary>
        /// Determines whether the mode is non-interactive.
        /// </summary>
        /// <returns>True if the mode is non-interactive; otherwise, false.</returns>
        public bool IsNonInteractive() => Settings.HasFlag(DeploymentSettings.NonInteractive);

        /// <summary>
        /// Determines whether the mode is silent.
        /// </summary>
        /// <returns>True if the mode is silent; otherwise, false.</returns>
        public bool IsSilent() => Settings.HasFlag(DeploymentSettings.Silent);

        /// <summary>
        /// Gets the exit code.
        /// </summary>
        public int GetExitCode() => ExitCode;

        /// <summary>
        /// Sets the exit code.
        /// </summary>
        /// <param name="exitCode">The exit code to set.</param>
        public void SetExitCode(int exitCode) => ExitCode = exitCode;

        /// <summary>
        /// Returns whether this session has been closed out.
        /// </summary>
        /// <returns>True if so; otherwise, false.</returns>
        public bool IsClosed() => Settings.HasFlag(DeploymentSettings.Disposed);


        #endregion
        #region Internal variables.


        /// <summary>
        /// Read-only list of all backing fields in the DeploymentSession class.
        /// </summary>
        private static readonly ReadOnlyDictionary<string, FieldInfo> BackingFields = new(typeof(DeploymentSession).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(static field => field.Name.StartsWith("_")).ToDictionary(static field => char.ToUpper(field.Name[1]) + field.Name.Substring(2), static field => field));

        /// <summary>
        /// Array of all possible drive letters in reverse order.
        /// </summary>
        private static readonly ReadOnlyCollection<string> DriveLetters = new([@"Z:\", @"Y:\", @"X:\", @"W:\", @"V:\", @"U:\", @"T:\", @"S:\", @"R:\", @"Q:\", @"P:\", @"O:\", @"N:\", @"M:\", @"L:\", @"K:\", @"J:\", @"I:\", @"H:\", @"G:\", @"F:\", @"E:\", @"D:\", @"C:\", @"B:\", @"A:\"]);

        /// <summary>
        /// Buffer for log entries.
        /// </summary>
        private readonly List<LogEntry> LogBuffer = [];

        /// <summary>
        /// Bitfield with settings for this deployment.
        /// </summary>
        private DeploymentSettings Settings;

        /// <summary>
        /// Gets the caller's SessionState from value that was supplied during object instantiation.
        /// </summary>
        private readonly SessionState? CallerSessionState;

        /// <summary>
        /// Gets the mounted WIM files within this session.
        /// </summary>
        private readonly List<FileInfo> MountedWimFiles = [];

        /// <summary>
        /// Gets the drive letter used with subst during a Zero-Config WIM file mount operation.
        /// </summary>
        private readonly string? DirFilesSubstDrive;

        /// <summary>
        /// Gets the base registry path used for getting/setting deferral information.
        /// </summary>
        private readonly string RegKeyDeferBase;

        /// <summary>
        /// Gets the registry path used for getting/setting deferral information.
        /// </summary>
        private readonly string RegKeyDeferHistory;

        /// <summary>
        /// Gets the deployment session's closing exit code.
        /// </summary>
        private int ExitCode;


        #endregion
        #region Private backing fields.


        private readonly DeploymentType _deploymentType = DeploymentType.Install;
        private readonly DeployMode _deployMode = DeployMode.Auto;
        private readonly string? _appVendor;
        private readonly string? _appName;
        private readonly string? _appVersion;
        private readonly string? _appArch;
        private readonly string? _appLang;
        private readonly string? _appRevision;
        private readonly ReadOnlyCollection<int> _appSuccessExitCodes = new([0]);
        private readonly ReadOnlyCollection<int> _appRebootExitCodes = new([1641, 3010]);
        private readonly ReadOnlyCollection<ProcessDefinition> _appProcessesToClose = new([]);
        private readonly Version? _appScriptVersion;
        private readonly DateTime? _appScriptDate;
        private readonly string? _appScriptAuthor;
        private readonly string _installName;
        private readonly string _installTitle;
        private readonly string? _deployAppScriptFriendlyName;
        private readonly Version? _deployAppScriptVersion;
        private readonly ReadOnlyDictionary<string, object>? _deployAppScriptParameters;
        private readonly string _currentDate;
        private readonly string _currentTime;
        private readonly ReadOnlyCollection<string> _scriptDirectory = new([]);
        private readonly string? _defaultMsiFile;
        private readonly string? _defaultMstFile;
        private readonly ReadOnlyCollection<string> _defaultMspFiles = new([]);
        private readonly string _logPath;
        private readonly string _logName;
        private string _installPhase = "Initialization";
        private string? _dirFiles;
        private string? _dirSupportFiles;


        #endregion
        #region Frontend parameters.


        /// <summary>
        /// Gets the deployment session's deployment type.
        /// </summary>
        public DeploymentType DeploymentType => GetPropertyValue<DeploymentType>();

        /// <summary>
        /// Gets the deployment session's deployment mode.
        /// </summary>
        public DeployMode DeployMode => GetPropertyValue<DeployMode>();

        /// <summary>
        /// Gets whether this deployment session is allowed to exit with a reboot exit code.
        /// </summary>
        public bool SuppressRebootPassThru => GetPropertyValue<bool>();

        /// <summary>
        /// Gets whether this deployment session should enable terminal services install mode.
        /// </summary>
        public bool TerminalServerMode => GetPropertyValue<bool>();

        /// <summary>
        /// Gets whether this deployment session should disable logging for the operation.
        /// </summary>
        public bool DisableLogging => GetPropertyValue<bool>();


        #endregion
        #region Frontend variables.


        /// <summary>
        /// Gets the deployment session's application vendor.
        /// </summary>
        public string? AppVendor => GetPropertyValue<string?>();

        /// <summary>
        /// Gets the deployment session's application name.
        /// </summary>
        public string? AppName => GetPropertyValue<string?>();

        /// <summary>
        /// Gets the deployment session's application version.
        /// </summary>
        public string? AppVersion => GetPropertyValue<string?>();

        /// <summary>
        /// Gets the deployment session's application architecture.
        /// </summary>
        public string? AppArch => GetPropertyValue<string?>();

        /// <summary>
        /// Gets the deployment session's application language.
        /// </summary>
        public string? AppLang => GetPropertyValue<string?>();

        /// <summary>
        /// Gets the deployment session's application package revision.
        /// </summary>
        public string? AppRevision => GetPropertyValue<string?>();

        /// <summary>
        /// Gets the deployment session's exit code(s) to indicate a successful deployment.
        /// </summary>
        public IReadOnlyList<int> AppSuccessExitCodes => GetPropertyValue<IReadOnlyList<int>>();

        /// <summary>
        /// Gets the deployment session's exit code(s) to indicate a reboot is required.
        /// </summary>
        public IReadOnlyList<int> AppRebootExitCodes => GetPropertyValue<IReadOnlyList<int>>();

        /// <summary>
        /// Gets the list of application processes that should be closed.
        /// </summary>
        public IReadOnlyList<ProcessDefinition> AppProcessesToClose => GetPropertyValue<IReadOnlyList<ProcessDefinition>>();

        /// <summary>
        /// Gets the deployment session's application package version.
        /// </summary>
        public Version? AppScriptVersion => GetPropertyValue<Version?>();

        /// <summary>
        /// Gets the deployment session's application package date.
        /// </summary>
        public DateTime? AppScriptDate => GetPropertyValue<DateTime?>();

        /// <summary>
        /// Gets the deployment session's application package author.
        /// </summary>
        public string? AppScriptAuthor => GetPropertyValue<string?>();

        /// <summary>
        /// Gets an override to the deployment session's installation name.
        /// </summary>
        public string InstallName => GetPropertyValue<string>();

        /// <summary>
        /// Gets an override to the deployment session's installation title.
        /// </summary>
        public string InstallTitle => GetPropertyValue<string>();

        /// <summary>
        /// Gets the deployment session's frontend script name.
        /// </summary>
        public string? DeployAppScriptFriendlyName => GetPropertyValue<string?>();

        /// <summary>
        /// Gets the deployment session's frontend script version.
        /// </summary>
        public Version? DeployAppScriptVersion => GetPropertyValue<Version?>();

        /// <summary>
        /// Gets the deployment session's frontend script parameters.
        /// </summary>
        public IReadOnlyDictionary<string, object>? DeployAppScriptParameters => GetPropertyValue<IReadOnlyDictionary<string, object>?>();

        /// <summary>
        /// Gets/sets the deployment session's installation phase.
        /// </summary>
        public string InstallPhase
        {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
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
        public string CurrentDate => GetPropertyValue<string>();

        /// <summary>
        /// Gets the deployment session's starting time as a string.
        /// </summary>
        public string CurrentTime => GetPropertyValue<string>();

        /// <summary>
        /// Gets the deployment session's UTC offset from GMT 0.
        /// </summary>
        public static readonly TimeSpan CurrentTimeZoneBias = TimeZoneInfo.Local.BaseUtcOffset;

        /// <summary>
        /// Gets the script directory of the caller.
        /// </summary>
        public IReadOnlyList<string> ScriptDirectory => GetPropertyValue<IReadOnlyList<string>>();

        /// <summary>
        /// Gets the specified or determined path to the Files folder.
        /// </summary>
        public string? DirFiles
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }

        /// <summary>
        /// Gets the specified or determined path to the SupportFiles folder.
        /// </summary>
        public string? DirSupportFiles
        {
            get => GetPropertyValue<string?>();
            set => SetPropertyValue(value);
        }

        /// <summary>
        /// Gets the deployment session's Zero-Config MSI file path.
        /// </summary>
        public string? DefaultMsiFile => GetPropertyValue<string?>();

        /// <summary>
        /// Gets the deployment session's Zero-Config MST file path.
        /// </summary>
        public string? DefaultMstFile => GetPropertyValue<string?>();

        /// <summary>
        /// Gets the deployment session's Zero-Config MSP file paths.
        /// </summary>
        public IReadOnlyList<string> DefaultMspFiles => GetPropertyValue<IReadOnlyList<string>>();

        /// <summary>
        /// Gets whether this deployment session found a valid Zero-Config MSI file.
        /// </summary>
        public bool UseDefaultMsi => GetPropertyValue<bool>();

        /// <summary>
        /// Gets the deployment session's log path.
        /// </summary>
        public string LogPath => GetPropertyValue<string>();

        /// <summary>
        /// Gets the deployment session's log filename.
        /// </summary>
        public string LogName => GetPropertyValue<string>();

        /// <summary>
        /// Gets a value indicating whether administrative privileges are required.
        /// </summary>
        public bool RequireAdmin => GetPropertyValue<bool>();


        #endregion
    }
}

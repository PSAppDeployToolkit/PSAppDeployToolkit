using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using PSADT.AccountManagement;
using PSADT.DeviceManagement;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.ProcessManagement;
using PSADT.TerminalServices;
using PSADT.WindowsInstaller;
using PSAppDeployToolkit.Logging;
using PSAppDeployToolkit.Utilities;
using Windows.Win32.Storage.FileSystem;

namespace PSAppDeployToolkit.Foundation
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
        /// <param name="compatibilityMode">Indicates whether compatibility mode is enabled.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "This exception type is fine here.")]
        public DeploymentSession(IReadOnlyDictionary<string, object>? parameters = null, bool? noExitOnClose = null, bool? compatibilityMode = null)
        {
            try
            {
                #region Initialization


                // Establish start date/time first so we can accurately mark the start of execution.
                CurrentDate = CurrentDateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
                CurrentTime = CurrentDateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

                // Establish initial variable values.
                PSObject adtData = ModuleDatabase.Get();
                EnvironmentTable adtEnv = ModuleDatabase.GetEnvironment();
                IDictionary adtConfig = ModuleDatabase.GetConfig();
                IDictionary configUI = (IDictionary)adtConfig["UI"]!;
                IDictionary configToolkit = (IDictionary)adtConfig["Toolkit"]!;
                bool forceProcessDetection = false;
                bool writtenDivider = false;

                // Pre-cache reused environment variables.
                string appDeployToolkitName = adtEnv.AppDeployToolkitName;
                string appDeployMainScriptVersion = adtEnv.AppDeployMainScriptVersion.ToString();
                bool IsProcessUserInteractive = adtEnv.IsProcessUserInteractive;
                IReadOnlyList<NTAccount>? usersLoggedOn = adtEnv.UsersLoggedOn;
                RunAsActiveUser? RunAsActiveUser = adtEnv.RunAsActiveUser;
                string currentLanguage = adtEnv.CurrentLanguage;
                Architecture envOSArchitecture = adtEnv.EnvOSArchitecture;
                NTAccount processNtAccount = adtEnv.ProcessNTAccount;
                bool isAdmin = adtEnv.IsAdmin;

                // Set up constant values for the lifetime of the deployment session.
                DefaultExitCode = (int)configUI["DefaultExitCode"]!;
                DeferExitCode = (int)configUI["DeferExitCode"]!;
                CompressLogs = (bool)configToolkit["CompressLogs"]!;
                ConfigLogPath = new((string)configToolkit["LogPath"]!);
                LogMaxHistory = (int)configToolkit["LogMaxHistory"]!;
                LogStyle = (LogStyle)Enum.Parse(typeof(LogStyle), (string)configToolkit["LogStyle"]!);
                LogWriteToHost = (bool)configToolkit["LogWriteToHost"]!;
                LogHostOutputToStdStreams = (bool)configToolkit["LogHostOutputToStdStreams"]!;

                // Set up other variable values based on incoming dictionary.
                if (parameters?.Count > 0)
                {
                    if (parameters.TryGetValue("SessionState", out object? paramValue) && (paramValue is not null))
                    {
                        SessionState = (SessionState)paramValue;
                    }
                    if (parameters.TryGetValue("DeploymentType", out paramValue) && (paramValue is not null))
                    {
                        DeploymentType = (DeploymentType)paramValue;
                    }
                    if (parameters.TryGetValue("DeployMode", out paramValue) && (paramValue is not null))
                    {
                        DeployMode = (DeployMode)paramValue;
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
                        AppVendor = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppName", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        AppName = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppVersion", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        AppVersion = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppArch", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        AppArch = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppLang", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        AppLang = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppRevision", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        AppRevision = (string)paramValue;
                    }
                    if (parameters.TryGetValue("AppScriptVersion", out paramValue) && (paramValue is not null))
                    {
                        AppScriptVersion = (Version)paramValue;
                    }
                    if (parameters.TryGetValue("AppScriptDate", out paramValue) && (paramValue is not null))
                    {
                        AppScriptDate = (DateTime)paramValue;
                    }
                    if (parameters.TryGetValue("AppScriptAuthor", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        AppScriptAuthor = (string)paramValue;
                    }
                    if (parameters.TryGetValue("RequireAdmin", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.RequireAdmin;
                    }
                    if (parameters.TryGetValue("InstallName", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        InstallName = (string)paramValue;
                    }
                    if (parameters.TryGetValue("InstallTitle", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        InstallTitle = (string)paramValue;
                    }
                    if (parameters.TryGetValue("DeployAppScriptFriendlyName", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        DeployAppScriptFriendlyName = (string)paramValue;
                    }
                    if (parameters.TryGetValue("DeployAppScriptVersion", out paramValue) && (paramValue is not null))
                    {
                        DeployAppScriptVersion = (Version)paramValue;
                    }
                    if (parameters.TryGetValue("DeployAppScriptParameters", out paramValue) && (paramValue is not null))
                    {
                        DeployAppScriptParameters = new ReadOnlyDictionary<string, object>((Dictionary<string, object>)paramValue);
                    }
                    if (parameters.TryGetValue("AppSuccessExitCodes", out paramValue) && (paramValue is not null))
                    {
                        AppSuccessExitCodes = new ReadOnlyCollection<int>((int[])paramValue);
                    }
                    if (parameters.TryGetValue("AppRebootExitCodes", out paramValue) && (paramValue is not null))
                    {
                        AppRebootExitCodes = new ReadOnlyCollection<int>((int[])paramValue);
                    }
                    if (parameters.TryGetValue("AppProcessesToClose", out paramValue) && (paramValue is not null))
                    {
                        AppProcessesToClose = new ReadOnlyCollection<ProcessDefinition>((ProcessDefinition[])paramValue);
                    }
                    if (parameters.TryGetValue("ScriptDirectory", out paramValue) && (paramValue is not null))
                    {
                        ScriptDirectory = new ReadOnlyCollection<DirectoryInfo>([.. ((string[])paramValue).Select(static d => new DirectoryInfo(d))]);
                    }
                    if (parameters.TryGetValue("DirFiles", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        DirFiles = new((string)paramValue);
                    }
                    if (parameters.TryGetValue("DirSupportFiles", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        DirSupportFiles = new((string)paramValue);
                    }
                    if (parameters.TryGetValue("DefaultMsiFile", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        DefaultMsiFile = new((string)paramValue);
                    }
                    if (parameters.TryGetValue("DefaultMstFile", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        DefaultMstFile = new((string)paramValue);
                    }
                    if (parameters.TryGetValue("DefaultMspFiles", out paramValue) && (paramValue is not null))
                    {
                        DefaultMspFiles = new ReadOnlyCollection<FileInfo>([.. ((string[])paramValue).Select(static f => new FileInfo(f))]);
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
                    if (parameters.TryGetValue("ExitWithMsiCodes", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.ExitWithMsiCodes;
                    }
                    if (parameters.TryGetValue("AllowWowProcess", out paramValue) && (SwitchParameter)paramValue)
                    {
                        Settings |= DeploymentSettings.AllowWowProcess;
                    }
                    if (parameters.TryGetValue("LogName", out paramValue) && !string.IsNullOrWhiteSpace((string?)paramValue))
                    {
                        LogName = (string)paramValue;
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
                if (noExitOnClose == true)
                {
                    Settings |= DeploymentSettings.NoExitOnClose;
                }


                #endregion
                #region DetectDefaultWim


                // If the default frontend hasn't been modified, and there's not already a mounted WIM file, check for WIM files and modify the install accordingly.
                if ((string.IsNullOrWhiteSpace(AppName) || Settings.HasFlag(DeploymentSettings.ForceWimDetection)) && MountedWimFiles.Count == 0 && DirFiles?.GetFiles("*", SearchOption.TopDirectoryOnly).FirstOrDefault(static f => f.Extension.EndsWith(".wim", StringComparison.OrdinalIgnoreCase)) is FileInfo wimFile)
                {
                    // Mount the WIM file and reset DirFiles to the mount point.
                    WriteInitialDivider(ref writtenDivider);
                    WriteLogEntry($"Discovered Zero-Config WIM file [{wimFile}].");
                    DirectoryInfo mountPath = new(Path.Combine(DirFiles.FullName, Path.GetRandomFileName()));
                    _ = ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Mount-ADTWimFile' -ImagePath $args[0] -Path $args[1] -Index 1"), wimFile, mountPath.FullName);
                    AddMountedWimFile(wimFile); DirFiles = mountPath;
                    WriteLogEntry($"Successfully mounted WIM file to [{mountPath}].");

                    // Subst the new DirFiles path to eliminate any potential path length issues.
                    IReadOnlyList<DriveInfo> usedLetters = adtEnv.EnvLogicalDrives;
                    if (DriveLetters.FirstOrDefault(l => !usedLetters.Contains(l)) is DriveInfo availLetter)
                    {
                        WriteLogEntry($"Creating substitution drive [{availLetter}] for [{DirFiles}].");
                        _ = NativeMethods.DefineDosDevice(0, availLetter.Name.Trim('\\'), DirFiles.FullName);
                        DirFiles = availLetter.RootDirectory; DirFilesSubstDrive = availLetter;
                    }
                    WriteLogEntry($"Using [{DirFiles}] as the base DirFiles directory.");
                }


                #endregion
                #region DetectDefaultMsi


                // If the default frontend hasn't been modified, check for MSI / MST and modify the install accordingly.
                if (string.IsNullOrWhiteSpace(AppName) || Settings.HasFlag(DeploymentSettings.ForceMsiDetection))
                {
                    // Find the first MSI file in the Files folder and use that as our install.
                    if (DefaultMsiFile is null)
                    {
                        // Only proceed if the Files directory is set.
                        if (DirFiles is not null)
                        {
                            // If we have a specific architecture MSI file, use that. Otherwise, use the first MSI file found.
                            FileInfo[] msiFiles = [.. DirFiles.GetFiles("*", SearchOption.TopDirectoryOnly).Where(static f => f.Extension.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))];
                            if (msiFiles.FirstOrDefault(f => !f.Name.EndsWith($".{envOSArchitecture}.msi", StringComparison.OrdinalIgnoreCase)) is FileInfo msiFile)
                            {
                                DefaultMsiFile = msiFile;
                            }
                            else if (msiFiles.Length > 0)
                            {
                                DefaultMsiFile = msiFiles[0];
                            }
                        }
                    }
                    else if (!Path.IsPathFullyQualified(DefaultMsiFile.ToString()) && DirFiles is not null)
                    {
                        DefaultMsiFile = new(Path.Combine(DirFiles.FullName, DefaultMsiFile.ToString()));
                    }

                    // If we have a default MSI file, proceed further with the Zero-Config configuration.
                    if (DefaultMsiFile is not null)
                    {
                        WriteInitialDivider(ref writtenDivider);
                        WriteLogEntry($"Discovered Zero-Config MSI installation file [{DefaultMsiFile}].");

                        // Discover if there is a zero-config MST file.
                        if (DefaultMstFile is null)
                        {
                            string mstFile = Path.ChangeExtension(DefaultMsiFile.FullName, "mst");
                            if (File.Exists(mstFile))
                            {
                                DefaultMstFile = new(mstFile);
                            }
                        }
                        else if (!Path.IsPathFullyQualified(DefaultMstFile.ToString()) && DirFiles is not null)
                        {
                            DefaultMstFile = new(Path.Combine(DirFiles.FullName, DefaultMstFile.ToString()));
                        }
                        if (DefaultMstFile is not null)
                        {
                            WriteLogEntry($"Discovered Zero-Config MST installation file [{DefaultMstFile}].");
                        }

                        // Discover if there are zero-config MSP files. Name multiple MSP files in alphabetical order to control order in which they are installed.
                        if (DefaultMspFiles.Count == 0)
                        {
                            if (DirFiles is not null)
                            {
                                DefaultMspFiles = new ReadOnlyCollection<FileInfo>([.. DirFiles.GetFiles("*", SearchOption.TopDirectoryOnly).Where(static f => f.Extension.EndsWith(".msp", StringComparison.OrdinalIgnoreCase))]);
                            }
                        }
                        else if (DirFiles is not null && DefaultMspFiles.Any(static f => !Path.IsPathFullyQualified(f.ToString())))
                        {
                            DefaultMspFiles = new ReadOnlyCollection<FileInfo>([.. DefaultMspFiles.Select(f => !Path.IsPathFullyQualified(f.ToString()) ? new(Path.Combine(DirFiles!.FullName, f.ToString())) : f)]);
                        }
                        if (DefaultMspFiles.Count > 0)
                        {
                            WriteLogEntry($"Discovered Zero-Config MSP installation file(s) [{string.Join(", ", DefaultMspFiles)}].");
                        }

                        // Generate list of MSI executables for use with Show-ADTInstallationWelcome.
                        if (!Settings.HasFlag(DeploymentSettings.DisableDefaultMsiProcessList))
                        {
                            ProcessDefinition[] msiExecList = [.. (DefaultMstFile is not null ? MsiUtilities.GetMsiTableColumnValues(DefaultMsiFile.FullName, "File", 3, [DefaultMstFile.FullName]) : MsiUtilities.GetMsiTableColumnValues(DefaultMsiFile.FullName, "File", 3)).Where(static p => p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)).Select(static p => new ProcessDefinition(Path.GetFileNameWithoutExtension(p.Split(['|'], StringSplitOptions.RemoveEmptyEntries).Last())))];
                            if (msiExecList.Length > 0)
                            {
                                AppProcessesToClose = new ReadOnlyCollection<ProcessDefinition>([.. AppProcessesToClose.Concat(msiExecList).GroupBy(static p => p.Name, StringComparer.OrdinalIgnoreCase).Select(static g => g.First())]);
                                WriteLogEntry($"MSI Executable List [{string.Join(", ", msiExecList.Select(static p => p.Name))}].");
                            }
                        }

                        // Update our app variables with new values.
                        IReadOnlyDictionary<string, string> msiProps = (DefaultMstFile is not null ? MsiUtilities.GetMsiTableDictionary(DefaultMsiFile.FullName, "Property", 1, 2, [DefaultMstFile.FullName]) : MsiUtilities.GetMsiTableDictionary(DefaultMsiFile.FullName, "Property", 1, 2))!;
                        if (string.IsNullOrWhiteSpace(AppVendor))
                        {
                            AppVendor = msiProps["Manufacturer"];
                        }
                        if (string.IsNullOrWhiteSpace(AppName))
                        {
                            AppName = msiProps["ProductName"];
                        }
                        if (string.IsNullOrWhiteSpace(AppVersion))
                        {
                            AppVersion = msiProps["ProductVersion"];
                        }
                        WriteLogEntry($"App Vendor [{msiProps["Manufacturer"]}].");
                        WriteLogEntry($"App Name [{msiProps["ProductName"]}].");
                        WriteLogEntry($"App Version [{msiProps["ProductVersion"]}].");
                        Settings |= DeploymentSettings.UseDefaultMsi;
                    }
                }


                #endregion
                #region SetAppProperties


                // Set up sample variables if Dot Sourcing the script, app details have not been specified.
                if (string.IsNullOrWhiteSpace(AppName))
                {
                    AppName = appDeployToolkitName;

                    if (!string.IsNullOrWhiteSpace(AppVendor))
                    {
                        AppVendor = null;
                    }
                    if (string.IsNullOrWhiteSpace(AppVersion))
                    {
                        AppVersion = appDeployMainScriptVersion;
                    }
                    if (string.IsNullOrWhiteSpace(AppLang))
                    {
                        AppLang = currentLanguage;
                    }
                    if (string.IsNullOrWhiteSpace(AppRevision))
                    {
                        AppRevision = "01";
                    }
                }


                #endregion
                #region SetInstallProperties


                // Build the Installation Title.
                if (string.IsNullOrWhiteSpace(InstallTitle))
                {
                    InstallTitle = $"{(!Settings.HasFlag(DeploymentSettings.UseDefaultMsi) ? $"{AppVendor} " : null)}{AppName} {AppVersion}".Trim();
                }
                InstallTitle = Regex.Replace(InstallTitle, @"\s{2,}", string.Empty);

                // Build the Installation Name.
                if (string.IsNullOrWhiteSpace(InstallName))
                {
                    InstallName = $"{(!Settings.HasFlag(DeploymentSettings.UseDefaultMsi) ? $"{AppVendor}_" : null)}{AppName}_{AppVersion}_{AppArch}_{AppLang}_{AppRevision}";
                }
                Regex invalidChars = adtEnv.InvalidFileNameCharsRegexPattern;
                InstallName = invalidChars.Replace(Regex.Replace(InstallName!.Trim('_').Replace(" ", null), "_+", "_"), string.Empty);

                // Set the Defer History registry path.
                RegKeyDeferBase = $@"{configToolkit["RegPath"]}\{appDeployToolkitName}\DeferHistory";
                RegKeyDeferHistory = $@"{RegKeyDeferBase}\{InstallName}";


                #endregion
                #region InitLogging


                // Generate log paths from our installation properties.
                if ((bool)configToolkit["CompressLogs"]!)
                {
                    // If the temp log folder already exists from a previous ZIP operation, then delete all files in it to avoid issues.
                    DirectoryInfo logTempFolder = new(Path.Combine(adtEnv.EnvTemp.FullName, $"{InstallName}_{DeploymentType}"));
                    if (logTempFolder.Exists)
                    {
                        logTempFolder.Delete(true);
                    }
                    LogPath = Directory.CreateDirectory(logTempFolder.FullName);
                }
                else
                {
                    LogPath = !ConfigLogPath.Exists ? Directory.CreateDirectory(ConfigLogPath.FullName) : ConfigLogPath;
                }

                // Append subfolder path if configured to do so.
                if ((bool)configToolkit["LogToHierarchy"]!)
                {
                    // Create the hierarchical log path based on vendor, app name and version before checking whether we need to clean up old log folders.
                    LogPath = Directory.CreateDirectory(Path.Combine(LogPath.FullName, $@"{AppVendor}\{AppName}\{AppVersion}".Replace(@"\\", null)));

                    // Check how many hierarchy levels to keep based on configuration.
                    DirectoryInfo[] hierarchyDirectories = [.. LogPath.Parent!.GetDirectories().Where(d => !d.FullName.Equals(LogPath.FullName, StringComparison.OrdinalIgnoreCase)).OrderBy(static d => d.CreationTime)];
                    int logMaxHierarchy = (int)configToolkit["LogMaxHierarchy"]!;
                    int hierarchyDirectoriesCount = hierarchyDirectories.Length;
                    if (hierarchyDirectoriesCount > logMaxHierarchy)
                    {
                        foreach (DirectoryInfo directory in hierarchyDirectories.Take(hierarchyDirectoriesCount - logMaxHierarchy))
                        {
                            directory.Delete(true);
                        }
                    }
                }
                else if ((bool)configToolkit["LogToSubfolder"]!)
                {
                    LogPath = Directory.CreateDirectory(Path.Combine(LogPath.FullName, $"{InstallName}_{DeploymentType}"));
                }

                // Generate the log filename to use. Append the username to the log file name if the toolkit is not running as an administrator,
                // since users do not have the rights to modify files in the ProgramData folder that belong to other users.
                DefaultLogName = invalidChars.Replace($"{InstallName}_{{0}}_{DeploymentType}{(!isAdmin ? $"_{adtEnv.EnvUserName}" : null)}.log", string.Empty);
                LogName = !string.IsNullOrWhiteSpace(LogName) ? invalidChars.Replace(LogName, string.Empty) : NewLogFileName(appDeployToolkitName);
                FileInfo logFile = new(Path.Combine(LogPath.FullName, LogName));
                int logMaxSize = (int)configToolkit["LogMaxSize"]!;
                bool logFileSizeExceeded = logFile.Exists && (logMaxSize > 0) && ((logFile.Length / 1048576.0) > logMaxSize);

                // Check if log file needs to be rotated.
                if ((logFile.Exists && !(bool)configToolkit["LogAppend"]!) || logFileSizeExceeded)
                {
                    try
                    {
                        // Get new log file path.
                        string logFileNameOnly = Path.GetFileNameWithoutExtension(LogName);
                        string logFileExtension = Path.GetExtension(LogName);
                        string logFileTimestamp = DateTime.Now.ToString("O").Split('.')[0].Replace(":", null);
                        string archiveLogFileName = $"{logFileNameOnly}_{logFileTimestamp}{logFileExtension}";
                        string archiveLogFilePath = Path.Combine(LogPath.FullName, archiveLogFileName);
                        int logMaxHistory = (int)configToolkit["LogMaxHistory"]!;

                        // Log message about archiving the log file.
                        if (logFileSizeExceeded)
                        {
                            WriteLogEntry($"Maximum log file size [{logMaxSize} MB] reached. Rename log file to [{archiveLogFileName}].", LogSeverity.Warning);
                        }

                        // Rename the file.
                        logFile.MoveTo(archiveLogFilePath);

                        // Start new log file and log message about archiving the old log file.
                        if (logFileSizeExceeded)
                        {
                            WriteLogEntry($"Previous log file was renamed to [{archiveLogFileName}] because maximum log file size of [{logMaxSize} MB] was reached.", LogSeverity.Warning);
                        }

                        // Get all log files sorted by last write time.
                        FileInfo[] logFiles = [.. LogPath.GetFiles($"{logFileNameOnly}*.log").Where(static f => f.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)).OrderBy(static f => f.LastWriteTime)];
                        int logFilesCount = logFiles.Length;

                        // Keep only the max number of log files.
                        if (logFilesCount > logMaxHistory)
                        {
                            foreach (FileInfo file in logFiles.Take(logFilesCount - logMaxHistory))
                            {
                                file.Delete();
                            }
                        }
                    }
                    catch (Exception ex) when (ex.Message is not null)
                    {
                        WriteLogEntry($"Failed to rotate the log file [{logFile}]: {ex}", LogSeverity.Error);
                    }
                }

                // Flush our log buffer out to disk.
                if (!DisableLogging && LogBuffer.Count > 0)
                {
                    using StreamWriter logFileWriter = new(Path.Combine(LogPath.FullName, LogName), true, LogUtilities.LogEncoding);
                    foreach (string line in LogStyle == LogStyle.CMTrace ? LogBuffer.Select(static o => o.CMTraceLogLine) : LogBuffer.Select(static o => o.LegacyLogLine))
                    {
                        logFileWriter.WriteLine(line);
                    }
                }

                // Open log file with commencement message.
                WriteInitialDivider(ref writtenDivider);
                WriteLogEntry($"[{InstallName}] {CultureInfo.InvariantCulture.TextInfo.ToLower(DeploymentType.ToString())} started.");


                #endregion
                #region LogScriptInfo


                // Announce provided deployment script info.
                if (AppScriptVersion is not null)
                {
                    WriteLogEntry($"[{InstallName}] script version is [{AppScriptVersion}].");
                }
                if ((AppScriptDate?.ToString("O").Split('T')[0] is string appScriptDate) && appScriptDate != "2000-12-31")
                {
                    WriteLogEntry($"[{InstallName}] script date is [{appScriptDate}].");
                }
                if (!string.IsNullOrWhiteSpace(AppScriptAuthor) && AppScriptAuthor != "<author name>")
                {
                    WriteLogEntry($"[{InstallName}] script author is [{AppScriptAuthor}].");
                }
                if (!string.IsNullOrWhiteSpace(DeployAppScriptFriendlyName))
                {
                    if (DeployAppScriptVersion is not null)
                    {
                        WriteLogEntry($"[{DeployAppScriptFriendlyName}] script version is [{DeployAppScriptVersion}].");
                    }
                    if (DeployAppScriptParameters?.Count > 0)
                    {
                        WriteLogEntry($"The following parameters were passed to [{DeployAppScriptFriendlyName}]: [{PowerShellUtilities.ConvertDictToPowerShellArgs(DeployAppScriptParameters).Replace("''", "'")}].");
                    }
                }
                PSObject adtDirectories = (PSObject)adtData.Properties["Directories"].Value;
                PSObject adtDurations = (PSObject)adtData.Properties["Durations"].Value;
                string[]? adtConfigDirs = (string[]?)adtDirectories.Properties["Config"].Value;
                string[]? adtStringDirs = (string[]?)adtDirectories.Properties["Strings"].Value;
                WriteLogEntry($"[{appDeployToolkitName}] module version is [{appDeployMainScriptVersion}].");
                WriteLogEntry($"[{appDeployToolkitName}] module imported in [{((TimeSpan)adtDurations.Properties["ModuleImport"].Value).TotalSeconds}] seconds.");
                WriteLogEntry($"[{appDeployToolkitName}] module initialized in [{((TimeSpan)adtDurations.Properties["ModuleInit"].Value).TotalSeconds}] seconds.");
                WriteLogEntry($"[{appDeployToolkitName}] module path is ['{adtEnv.AppDeployToolkitPath}'].");
                if (adtConfigDirs?.Length > 0)
                {
                    WriteLogEntry($"[{appDeployToolkitName}] config path is ['{string.Join("', '", adtConfigDirs)}'].");
                }
                if (adtStringDirs?.Length > 0)
                {
                    WriteLogEntry($"[{appDeployToolkitName}] string path is ['{string.Join("', '", adtStringDirs)}'].");
                }

                // Announce session instantiation mode.
                if (compatibilityMode == true)
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
                WriteLogEntry($"Computer Name is [{adtEnv.EnvComputerNameFQDN}].");
                WriteLogEntry($"Current User is [{processNtAccount}].");
                WriteLogEntry($"OS Version is [{adtEnv.EnvOSName} {envOSArchitecture} {adtEnv.EnvOSVersion}].");
                WriteLogEntry($"OS Type is [{adtEnv.EnvOSProductTypeName}].");
                if (adtEnv.EnvHardwareType is string envHardwareType)
                {
                    WriteLogEntry($"Hardware Platform is [{envHardwareType}].");
                }
                WriteLogEntry($"Current Culture is [{adtEnv.Culture}], language is [{currentLanguage}] and UI language is [{adtEnv.CurrentUILanguage}].");
                WriteLogEntry($"PowerShell Host is [{adtEnv.EnvHost.Name}] with version [{adtEnv.EnvHostVersion}].");
                WriteLogEntry($"PowerShell Version is [{adtEnv.EnvPSVersion} {adtEnv.PSArchitecture}].");
                WriteLogEntry($"PowerShell Process Path is [{adtEnv.EnvPSProcessPath}].");
                if (adtEnv.EnvCLRVersion is Version envCLRVersion)
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
                    WriteLogEntry($"Session information for all logged on users:{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, adtEnv.LoggedOnUserSessions.Select(static s => $"{s}{Environment.NewLine}{Environment.NewLine}")).TrimEnd()}", false);

                    // Check if the current process is running in the context of one of the logged on users
                    if (adtEnv.CurrentLoggedOnUserSession is SessionInfo CurrentLoggedOnUserSession)
                    {
                        WriteLogEntry($"Current process is running with user account [{processNtAccount}] under logged on user session for [{CurrentLoggedOnUserSession.NTAccount}].");
                    }
                    else
                    {
                        WriteLogEntry($"Current process is running under a system account [{processNtAccount}].");
                    }

                    // Display account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
                    if (adtEnv.CurrentConsoleUserSession is SessionInfo CurrentConsoleUserSession)
                    {
                        WriteLogEntry($"The following user is the console user [{CurrentConsoleUserSession.NTAccount}] (user with control of physical monitor, keyboard, and mouse).");
                    }
                    else
                    {
                        WriteLogEntry("There is no console user logged on (user with control of physical monitor, keyboard, and mouse).");
                    }

                    // Display the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
                    if (RunAsActiveUser is not null)
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

                // Throw if the process is 32-bit on a 64-bit OS.
                if (RuntimeInformation.ProcessArchitecture != RuntimeInformation.OSArchitecture && !Settings.HasFlag(DeploymentSettings.AllowWowProcess))
                {
                    throw new InvalidOperationException("The current PowerShell process is 32-bit on a 64-bit operating system. Please run the deployment script in a 64-bit PowerShell process.");
                }


                #endregion
                #region LogLanguageInfo


                // Log which language's UI messages are loaded from the config file
                WriteLogEntry($"The current execution context has a primary UI language of [{adtEnv.UICulture}].");

                // Advise whether the UI language was overridden.
                if (configUI["LanguageOverride"] is string languageOverride)
                {
                    WriteLogEntry($"The config file was configured to override the detected primary UI language with the following UI language: [{languageOverride}].");
                }
                WriteLogEntry($"The following locale was used to import UI messages from the strings.psd1 files: [{adtData.Properties["Language"].Value}].");


                #endregion
                #region PerformConfigMgrTests


                // Check if script is running from a SCCM Task Sequence.
                if (adtEnv.RunningTaskSequence)
                {
                    WriteLogEntry("Detected that the deployment script is running within a ConfigMgr task sequence.");
                }
                else if (adtEnv.RunningDeployrTaskSequence)
                {
                    WriteLogEntry("Detected that the deployment script is running within a DeployR task sequence.");
                }


                #endregion
                #region SetDeploymentProperties


                // Check if the device has completed the OOBE or not.
                bool deployModeChanged = false;
                if ((Environment.OSVersion.Version >= new Version(10, 0, 16299, 0)) && !DeviceUtilities.IsOOBEComplete())
                {
                    if (deployModeChanged)
                    {
                        WriteLogEntry($"Detected OOBE in progress but deployment has already been changed to [{DeployMode}]");
                    }
                    if (DeployMode != DeployMode.Auto)
                    {
                        WriteLogEntry($"Detected OOBE in progress but deployment mode was explicitly set to [{DeployMode}].");
                    }
                    else if (!Settings.HasFlag(DeploymentSettings.NoOobeDetection))
                    {
                        WriteLogEntry($"Detected OOBE in progress, changing deployment mode to [{DeployMode = DeployMode.NonInteractive}].");
                        deployModeChanged = true;
                    }
                    else
                    {
                        WriteLogEntry("Detected OOBE in progress but toolkit is configured to not adjust deployment mode.");
                    }
                }
                else if (Process.GetProcessesByName("WWAHost") is { Length: > 0 } wwaHostProcesses)
                {
                    // If WWAHost is running, the device might be within the User ESP stage. But first, confirm whether the device is in Autopilot.
                    WriteLogEntry("The WWAHost process is running, checking ESP User Account setup phase.");
                    if (RunAsActiveUser?.SID is SecurityIdentifier userSid)
                    {
                        if (wwaHostProcesses.FirstOrDefault(p => p.SessionId == RunAsActiveUser.SessionId) is not null)
                        {
                            PSObject? fsRegData = ModuleDatabase.GetSessionState().InvokeProvider.Property.Get([$@"Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Enrollments\*\FirstSync\{userSid}"], null, false).FirstOrDefault();
                            if (fsRegData is not null)
                            {
                                if (fsRegData.Properties["IsSyncDone"]?.Value is null or 0)
                                {
                                    if (deployModeChanged)
                                    {
                                        WriteLogEntry($"The ESP User Account Setup phase is still in progress but deployment has already been changed to [{DeployMode}]");
                                    }
                                    else if (DeployMode != DeployMode.Auto)
                                    {
                                        WriteLogEntry($"The ESP User Account Setup phase is still in progress but deployment mode was explicitly set to [{DeployMode}].");
                                    }
                                    else if (!Settings.HasFlag(DeploymentSettings.NoOobeDetection))
                                    {
                                        WriteLogEntry($"The ESP User Account Setup phase is still in progress, changing deployment mode to [{DeployMode = DeployMode.NonInteractive}].");
                                        deployModeChanged = true;
                                    }
                                    else
                                    {
                                        WriteLogEntry("The ESP User Account Setup phase is still in progress but toolkit is configured to not adjust deployment mode.");
                                    }
                                }
                                else
                                {
                                    WriteLogEntry("The ESP User Account Setup phase is already complete.");
                                }
                            }
                            else
                            {
                                WriteLogEntry($"Could not find any FirstSync information for SID [{userSid}].");
                            }
                        }
                        else
                        {
                            WriteLogEntry("There are no WWAHost processes running for the currently logged on user");
                        }
                    }
                    else
                    {
                        WriteLogEntry("The device currently has no users logged on.");
                    }
                }
                else
                {
                    WriteLogEntry("Device has completed the OOBE and toolkit is not running with an active ESP in progress.");
                }

                // Perform session 0 evaluation.
                if (adtEnv.SessionZero)
                {
                    if (deployModeChanged)
                    {
                        WriteLogEntry($"Session 0 detected but deployment has already been changed to [{DeployMode}]");
                    }
                    else if (DeployMode != DeployMode.Auto)
                    {
                        WriteLogEntry($"Session 0 detected but deployment mode was explicitly set to [{DeployMode}].");
                    }
                    else if (!Settings.HasFlag(DeploymentSettings.NoSessionDetection))
                    {
                        // If the process is not able to display a UI, enable silent mode.
                        if (RunAsActiveUser is null)
                        {
                            // If there's no users logged on but we're interactive anyway, don't change the DeployMode.
                            if (!IsProcessUserInteractive)
                            {
                                WriteLogEntry($"Session 0 detected, no users logged on and process not running in user interactive mode; deployment mode set to [{DeployMode = DeployMode.Silent}].");
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
                if (AppProcessesToClose.Count > 0 || forceProcessDetection)
                {
                    if (deployModeChanged)
                    {
                        WriteLogEntry($"The processes ['{string.Join("', '", AppProcessesToClose.Select(static p => p.Name))}'] were specified as requiring closure but deployment has already been changed to [{DeployMode}]");
                    }
                    else if (DeployMode != DeployMode.Auto)
                    {
                        WriteLogEntry($"The processes ['{string.Join("', '", AppProcessesToClose.Select(static p => p.Name))}'] were specified as requiring closure but deployment mode was explicitly set to [{DeployMode}].");
                    }
                    else if (!Settings.HasFlag(DeploymentSettings.NoProcessDetection))
                    {
                        if (AppProcessesToClose.Count == 0 || (RunningProcessInfo.Get(AppProcessesToClose) is var runningProcesses && (runningProcesses.Count == 0)))
                        {
                            if (AppProcessesToClose.Count > 0 && !forceProcessDetection)
                            {
                                WriteLogEntry($"The processes ['{string.Join("', '", AppProcessesToClose.Select(static p => p.Name))}'] were specified as requiring closure but none were running, changing deployment mode to [{DeployMode = DeployMode.Silent}].");
                            }
                            else
                            {
                                WriteLogEntry($"No processes were specified as requiring closure and -NoProcessDetection was explicitly set to false, changing deployment mode to [{DeployMode = DeployMode.Silent}].");
                            }
                            deployModeChanged = true;
                        }
                        else
                        {
                            WriteLogEntry($"The processes ['{string.Join("', '", runningProcesses.Select(static p => p.Process.ProcessName).Distinct())}'] were found to be running and will require closure.");
                        }
                    }
                    else
                    {
                        WriteLogEntry($"The processes ['{string.Join("', '", AppProcessesToClose.Select(static p => p.Name))}'] were specified as requiring closure but toolkit is configured to not adjust deployment mode.");
                    }
                }
                else if (DeployAppScriptVersion is null || DeployAppScriptVersion >= new Version(4, 2, 0))
                {
                    if (deployModeChanged)
                    {
                        WriteLogEntry($"No processes were specified as requiring closure but deployment has already been changed to [{DeployMode}]");
                    }
                    if (DeployMode != DeployMode.Auto)
                    {
                        WriteLogEntry($"No processes were specified as requiring closure but deployment mode was explicitly set to [{DeployMode}].");
                    }
                    else if (!Settings.HasFlag(DeploymentSettings.NoProcessDetection))
                    {
                        WriteLogEntry($"No processes were specified as requiring closure, changing deployment mode to [{DeployMode = DeployMode.Silent}].");
                        deployModeChanged = true;
                    }
                    else
                    {
                        WriteLogEntry("No processes were specified as requiring closure but toolkit is configured to not adjust deployment mode.");
                    }
                }
                else
                {
                    WriteLogEntry($"No processes were specified as requiring closure, not adjusting DeployMode as DeployAppScriptVersion is less than [4.2.0].");
                }

                // If we're still in Auto mode, then set the deployment mode to Interactive.
                if (DeployMode == DeployMode.Auto)
                {
                    DeployMode = DeployMode.Interactive;
                }

                // Set Deploy Mode switches.
                WriteLogEntry($"Installation is running in [{DeployMode}] mode.");
                if (DeployMode == DeployMode.Silent)
                {
                    Settings |= DeploymentSettings.NonInteractive;
                    Settings |= DeploymentSettings.Silent;
                }
                else if (DeployMode == DeployMode.NonInteractive)
                {
                    Settings |= DeploymentSettings.NonInteractive;
                }

                // Check deployment type (install/uninstall).
                WriteLogEntry($"Deployment type is [{DeploymentType}].");


                #endregion
                #region TestSessionViability


                // Check if the caller explicitly wants interactivity but we can't do it.
                if (DeployMode != DeployMode.Silent && RunAsActiveUser is null && !IsProcessUserInteractive)
                {
                    throw new NotSupportedException($"This deployment explicitly requires interactivity, however there are no suitable logged on users available and this process is running non-interactively.");
                }


                #endregion
                #region Finalization


                // If terminal server mode was specified, change the installation mode to support it.
                if (TerminalServerMode)
                {
                    if (!OperatingSystemInfo.Current.IsTerminalServer)
                    {
                        WriteLogEntry("The [-TerminalServerMode] parameter was specified but system is not a terminal server.", LogSeverity.Warning);
                    }
                    else
                    {
                        _ = ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Enable-ADTTerminalServerInstallMode'"));
                    }
                }

                // Export session's public variables to the user's scope. For these, we can't capture the Set-Variable
                // PassThru data as syntax like `$var = 'val'` constructs a new PSVariable every time.
                if (compatibilityMode == true)
                {
                    if (SessionState is null)
                    {
                        throw new InvalidOperationException("SessionState is not available to set compatibility mode variables.");
                    }
                    foreach (PropertyInfo property in typeof(DeploymentSession).GetProperties())
                    {
                        SessionState.PSVariable.Set(new(property.Name, property.GetValue(this)));
                    }
                    foreach (FieldInfo field in typeof(DeploymentSession).GetFields())
                    {
                        SessionState.PSVariable.Set(new(field.Name, field.GetValue(this)));
                    }
                    Settings |= DeploymentSettings.CompatibilityMode;
                }


                #endregion
            }
            catch (Exception ex)
            {
                WriteLogEntry(ex.Message, LogSeverity.Error);
                RemoveSubstDrive(); DismountWimFiles();
                SetExitCode(ex is NotSupportedException ? DeferExitCode : 60008);
                Environment.ExitCode = Close();
                throw new ApplicationException("Failure occurred while instantiating new deployment session.", ex);
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
                throw new ObjectDisposedException(nameof(DeploymentSession), "This object has already been disposed.");
            }

            // Establish initial variable values.
            PSPropertyInfo adtExitCode = ModuleDatabase.Get().Properties["LastExitCode"];

            // If terminal server mode was specified, revert the installation mode to support it.
            if (TerminalServerMode)
            {
                _ = ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Disable-ADTTerminalServerInstallMode'"));
            }

            // Process resulting exit code.
            string deployString = $"{(!string.IsNullOrWhiteSpace(InstallName) ? $"[{Regex.Replace(InstallName, @"(?<!\{)\{(?!\{)|(?<!\})\}(?!\})", "$0$0")}] {CultureInfo.InvariantCulture.TextInfo.ToLower(DeploymentType.ToString())}" : $"{ModuleDatabase.GetEnvironment().AppDeployToolkitName} deployment")} {{0}} in [{(DateTime.Now - CurrentDateTime).TotalSeconds}] seconds with exit code [{ExitCode}].";
            DeploymentStatus deploymentStatus = GetDeploymentStatus();
            switch (deploymentStatus)
            {
                case DeploymentStatus.FastRetry:
                    WriteLogEntry(string.Format(CultureInfo.InvariantCulture, deployString, "was deferred"), LogSeverity.Warning);
                    break;
                case DeploymentStatus.Error:
                    WriteLogEntry(string.Format(CultureInfo.InvariantCulture, deployString, "failed"), LogSeverity.Error);
                    break;
                case DeploymentStatus.RestartRequired:
                case DeploymentStatus.Complete:
                default:
                    if (Settings.HasFlag(DeploymentSettings.ExitWithMsiCodes))
                    {
                        ExitCode = deploymentStatus == DeploymentStatus.RestartRequired ? 3010 : 0;
                    }
                    WriteLogEntry(string.Format(CultureInfo.InvariantCulture, deployString, "completed"), LogSeverity.Success);
                    if (deploymentStatus == DeploymentStatus.RestartRequired && !SuppressRebootPassThru)
                    {
                        WriteLogEntry("A restart has been flagged as required.", LogSeverity.Warning);
                    }
                    else
                    {
                        ExitCode = 0;
                    }
                    ResetDeferHistory();
                    break;
            }

            // Update the module's last tracked exit code.
            if (ExitCode != 0)
            {
                adtExitCode.Value = ExitCode;
            }

            // Clean up state and write out a log divider to indicate the end of logging.
            RemoveSubstDrive();
            DismountWimFiles();
            WriteLogDivider();
            Settings |= DeploymentSettings.Disposed;

            // Compress log files if configured to do so before returning the exit code to the caller.
            if (CompressLogs)
            {
                // Archive the log files to zip format and then delete the temporary logs folder.
                string destArchiveFileName = $"{InstallName}_{DeploymentType}_{{0}}.zip";
                DirectoryInfo destArchiveFilePath = Directory.CreateDirectory(ConfigLogPath.FullName);
                try
                {
                    // Get all archive files sorted by last write time.
                    FileInfo[] archiveFiles = [.. destArchiveFilePath.GetFiles(string.Format(CultureInfo.InvariantCulture, destArchiveFileName, "*")).Where(static f => f.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).OrderBy(static f => f.LastWriteTime)];
                    destArchiveFileName = string.Format(CultureInfo.InvariantCulture, destArchiveFileName, CurrentDateTime.ToString("O").Split('.')[0].Replace(":", null));

                    // Keep only the max number of archive files
                    int archiveFilesCount = archiveFiles.Length;
                    if (archiveFilesCount > LogMaxHistory)
                    {
                        foreach (FileInfo file in archiveFiles.Take(archiveFilesCount - LogMaxHistory))
                        {
                            file.Delete();
                        }
                    }

                    // Compression of the log files.
                    ZipFile.CreateFromDirectory(LogPath.FullName, Path.Combine(destArchiveFilePath.FullName, destArchiveFileName), CompressionLevel.Optimal, false);
                    LogPath.Delete(true);
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    WriteLogEntry($"Failed to manage archive file [{destArchiveFileName}]: {ex}", LogSeverity.Error);
                }
            }
            return (int)adtExitCode.Value;
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
        /// <param name="logStyle">The type of log.</param>
        /// <param name="hostLogStreamType">What stream to write the message to.</param>
        public IReadOnlyList<LogEntry> WriteLogEntry(IReadOnlyList<string> message, bool debugMessage, LogSeverity? severity = null, string? source = null, string? scriptSection = null, string? logFileDirectory = null, string? logFileName = null, LogStyle? logStyle = null, HostLogStreamType? hostLogStreamType = null)
        {
            if (source is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(source);
            }
            if (scriptSection is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(scriptSection);
            }
            if (logFileDirectory is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(logFileDirectory);
            }
            if (logFileName is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(logFileName);
            }
            IReadOnlyList<LogEntry> logEntries = LogUtilities.WriteLogEntry(message, hostLogStreamType ?? GetHostLogStreamTypeMode(), debugMessage, severity, source, scriptSection ?? InstallPhase, logFileDirectory ?? (!DisableLogging ? LogPath?.FullName : null), logFileName ?? (!DisableLogging ? LogName : null), logStyle ?? LogStyle);
            LogBuffer.AddRange(logEntries);
            return logEntries;
        }

        /// <summary>
        /// Writes a log entry with a message array.
        /// </summary>
        /// <param name="message">The log message array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLogEntry(IReadOnlyList<string> message)
        {
            _ = WriteLogEntry(message, false, null, null, null, null, null, null, null);
        }

        /// <summary>
        /// Writes a log entry with a single message.
        /// </summary>
        /// <param name="message">The log message.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLogEntry(string message)
        {
            _ = WriteLogEntry([message], false, null, null, null, null, null, null, null);
        }

        /// <summary>
        /// Writes a log entry with a single message and severity.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="severity">The severity level.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLogEntry(string message, LogSeverity severity)
        {
            _ = WriteLogEntry([message], false, severity, null, null, null, null, null, null);
        }

        /// <summary>
        /// Writes a log entry with a single message and source.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="source">The source of the message.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLogEntry(string message, string source)
        {
            _ = WriteLogEntry([message], false, null, source, null, null, null, null, null);
        }

        /// <summary>
        /// Writes a log entry with a single message, severity, and source.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="severity">The severity level.</param>
        /// <param name="source">The source of the message.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLogEntry(string message, LogSeverity severity, string source)
        {
            _ = WriteLogEntry([message], false, severity, source, null, null, null, null, null);
        }

        /// <summary>
        /// Writes a log entry with a single message and host write option.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <param name="writeHost">Whether to write to the host.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLogEntry(string message, bool writeHost)
        {
            _ = WriteLogEntry([message], false, null, null, null, null, null, null, GetHostLogStreamTypeMode(writeHost));
        }

        /// <summary>
        /// Gets a read-only collection of the current log entries in the log buffer.
        /// </summary>
        /// <returns>A read-only list of <see cref="LogEntry"/> objects representing the current log entries. The list is empty
        /// if no log entries are present.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<LogEntry> GetLogBuffer()
        {
            return LogBuffer.AsReadOnly();
        }

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
            PSObject? history = ModuleDatabase.GetSessionState().InvokeProvider.Property.Get(RegKeyDeferHistory, null).FirstOrDefault();
            if (history is null)
            {
                return null;
            }
            object? deferDeadline = history.Properties["DeferDeadline"]?.Value;
            object? deferTimesRemaining = history.Properties["DeferTimesRemaining"]?.Value;
            object? deferRunIntervalLastTime = history.Properties["DeferRunIntervalLastTime"]?.Value;
            return deferRunIntervalLastTime is null && deferTimesRemaining is null && deferDeadline is null ? null : new
            (
                deferTimesRemaining is not null ? deferTimesRemaining is string deferTimesRemainingString ? (uint)int.Parse(deferTimesRemainingString, CultureInfo.InvariantCulture) : (uint)(int)deferTimesRemaining : null,
                deferDeadline is not null ? DateTime.Parse((string)deferDeadline, CultureInfo.InvariantCulture) : null,
                deferRunIntervalLastTime is not null ? DateTime.Parse((string)deferRunIntervalLastTime, CultureInfo.InvariantCulture) : null
            );
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
            SessionState moduleSessionState = ModuleDatabase.GetSessionState();

            // Internal helper for setting deferral history.
            void SetDeferHistoryImpl(string key, object value, RegistryValueKind kind)
            {
                WriteLogEntry($"Setting deferral history: [{key} = {value}].");
                if (!TestDeferHistoryPath())
                {
                    CreateDeferHistoryPath();
                }
                _ = moduleSessionState.InvokeProvider.Property.New([RegKeyDeferHistory], key, kind.ToString(), value, true, true);
            }

            // Test each property and set it if it exists.
            if (deferTimesRemaining is not null)
            {
                SetDeferHistoryImpl("DeferTimesRemaining", deferTimesRemaining.Value, RegistryValueKind.DWord);
            }
            if (deferDeadline is not null)
            {
                SetDeferHistoryImpl("DeferDeadline", deferDeadline.Value.ToUniversalTime().ToString("O"), RegistryValueKind.String);
            }
            if (deferRunInterval is not null)
            {
                SetDeferHistoryImpl("DeferRunInterval", deferRunInterval.Value.ToString("c"), RegistryValueKind.String);
            }
            if (deferRunIntervalLastTime is not null)
            {
                SetDeferHistoryImpl("DeferRunIntervalLastTime", deferRunIntervalLastTime.Value.ToUniversalTime().ToString("O"), RegistryValueKind.String);
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
        /// Generates a new log file name using the specified discriminator value.
        /// </summary>
        /// <param name="discriminator">A string value used to distinguish the log file name. Typically represents a unique identifier or context
        /// for the log file. Cannot be null.</param>
        /// <returns>A string containing the formatted log file name that incorporates the specified discriminator.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string NewLogFileName(string discriminator)
        {
            return string.Format(CultureInfo.InvariantCulture, DefaultLogName, discriminator);
        }

        /// <summary>
        /// Gets the deployment status.
        /// </summary>
        /// <returns>The deployment status.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public DeploymentStatus GetDeploymentStatus()
        {
            if ((ExitCode == DefaultExitCode) || (ExitCode == DeferExitCode))
            {
                return DeploymentStatus.FastRetry;
            }
            if (AppRebootExitCodes.Contains(ExitCode))
            {
                return DeploymentStatus.RestartRequired;
            }
            if (AppSuccessExitCodes.Contains(ExitCode))
            {
                return DeploymentStatus.Complete;
            }
            return DeploymentStatus.Error;
        }

        /// <summary>
        /// Returns whether this session has been closed out.
        /// </summary>
        /// <returns>True if so; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsClosed()
        {
            return Settings.HasFlag(DeploymentSettings.Disposed);
        }

        /// <summary>
        /// Determines whether the session is allowed to exit PowerShell on close.
        /// </summary>
        /// <returns>True if the session can exit; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanExitOnClose()
        {
            return !Settings.HasFlag(DeploymentSettings.NoExitOnClose);
        }

        /// <summary>
        /// Determines whether the mode is non-interactive.
        /// </summary>
        /// <returns>True if the mode is non-interactive; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNonInteractive()
        {
            return Settings.HasFlag(DeploymentSettings.NonInteractive);
        }

        /// <summary>
        /// Determines whether the mode is silent.
        /// </summary>
        /// <returns>True if the mode is silent; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSilent()
        {
            return Settings.HasFlag(DeploymentSettings.Silent);
        }

        /// <summary>
        /// Gets the exit code.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "I like methods.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetExitCode()
        {
            return ExitCode;
        }

        /// <summary>
        /// Sets the exit code.
        /// </summary>
        /// <param name="exitCode">The exit code to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetExitCode(int exitCode)
        {
            ExitCode = exitCode;
        }

        /// <summary>
        /// Add the mounted WIM files.
        /// </summary>
        /// <param>The WIM file to add to the list for dismounting upon session closure.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddMountedWimFile(FileInfo wimFile)
        {
            MountedWimFiles.Add(wimFile);
        }

        /// <summary>
        /// Writes a log divider.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteLogDivider()
        {
            WriteLogEntry(LogUtilities.LogDivider);
        }

        /// <summary>
        /// Writes a divider if one hasn't been written already.
        /// </summary>
        private void WriteInitialDivider(ref bool written)
        {
            if (written)
            {
                return;
            }
            WriteLogDivider();
            written = true;
        }

        /// <summary>
        /// Determines the type of log stream to use based on the specified host logging settings.
        /// </summary>
        /// <remarks>If <paramref name="writeHost"/> is false, the method returns HostLogStreamType.None.
        /// If host logging is enabled, the method returns HostLogStreamType.Console when standard streams are used;
        /// otherwise, it returns HostLogStreamType.Host.</remarks>
        /// <param name="writeHost">true to enable writing logs to the host; false to disable host logging; or null to use the default behavior.</param>
        /// <returns>A value of type HostLogStreamType that indicates the log stream type to use: Console, Host, or None.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HostLogStreamType GetHostLogStreamTypeMode(bool? writeHost = null)
        {
            return writeHost != false && LogWriteToHost
                ? (LogHostOutputToStdStreams ? HostLogStreamType.Console : HostLogStreamType.Host)
                : HostLogStreamType.None;
        }

        /// <summary>
        /// Tests the deferral history path.
        /// </summary>
        /// <returns>True if the deferral history path exists; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TestDeferHistoryPath()
        {
            return ModuleDatabase.GetSessionState().InvokeProvider.Item.Exists(RegKeyDeferHistory, true, true);
        }

        /// <summary>
        /// Creates the deferral history path.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateDeferHistoryPath()
        {
            _ = ModuleDatabase.GetSessionState().InvokeProvider.Item.New([RegKeyDeferBase], InstallName, null, null, true);
        }

        /// <summary>
        /// Removes the virtual drive mapping created with the SUBST command for the directory specified by
        /// DirFilesSubstDrive.
        /// </summary>
        /// <remarks>This method undoes a previous drive substitution, making the drive letter unavailable
        /// for accessing the substituted directory. If DirFilesSubstDrive is null, empty, or consists only of
        /// white-space characters, no action is taken.</remarks>
        private void RemoveSubstDrive()
        {
            if (DirFilesSubstDrive is null)
            {
                return;
            }
            WriteLogEntry($"Removing substitution drive [{DirFilesSubstDrive}].");
            _ = NativeMethods.DefineDosDevice(DEFINE_DOS_DEVICE_FLAGS.DDD_REMOVE_DEFINITION, DirFilesSubstDrive.RootDirectory.FullName, null);
        }

        /// <summary>
        /// Dismounts all currently mounted Windows Imaging (WIM) files managed by this instance.
        /// </summary>
        /// <remarks>This method processes all mounted WIM files in reverse order and clears the internal
        /// list after dismounting. It should be called when all operations requiring access to the mounted WIM files
        /// are complete.</remarks>
        private void DismountWimFiles()
        {
            if (MountedWimFiles.Count <= 0)
            {
                return;
            }
            MountedWimFiles.Reverse(); _ = ModuleDatabase.InvokeScript(ScriptBlock.Create("& $Script:CommandTable.'Dismount-ADTWimFile' -ImagePath $args[0]"), MountedWimFiles);
            MountedWimFiles.Clear();
        }

        /// <summary>
        /// Gets the value of a flag-based boolean property from the Settings bitfield.
        /// </summary>
        /// <param name="flag">The DeploymentSettings flag to check.</param>
        /// <param name="propertyName">The name of the property (auto-populated by CallerMemberName).</param>
        /// <returns>True if the flag is set; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetFlagValue(DeploymentSettings flag, [CallerMemberName] string propertyName = null!)
        {
            return Settings.HasFlag(DeploymentSettings.CompatibilityMode) && SessionState is not null
                ? (bool)SessionState.PSVariable.GetValue(propertyName)
                : Settings.HasFlag(flag);
        }

        /// <summary>
        /// Gets the value of a property using the compiler-generated backing field.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="backingField">Read-only reference to the backing field.</param>
        /// <param name="propertyName">The name of the property (auto-populated by CallerMemberName).</param>
        /// <returns>The property value from either the backing field or PowerShell session state.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetPropertyValue<T>(ref readonly T backingField, [CallerMemberName] string propertyName = null!)
        {
            return Settings.HasFlag(DeploymentSettings.CompatibilityMode) && SessionState is not null
                ? (T)SessionState.PSVariable.GetValue(propertyName)
                : backingField;
        }

        /// <summary>
        /// Sets the value of a mutable property using the compiler-generated backing field.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="backingField">Reference to the mutable backing field.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="propertyName">The name of the property (auto-populated by CallerMemberName).</param>
        private void SetPropertyValue<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null!)
        {
            if (Settings.HasFlag(DeploymentSettings.CompatibilityMode) && SessionState is not null)
            {
                SessionState.PSVariable.Set(new(propertyName, value));
            }
            backingField = value;
        }


        #endregion
        #region Internal variables.


        /// <summary>
        /// Array of all possible drive letters in reverse order.
        /// </summary>
        private static readonly ReadOnlyCollection<DriveInfo> DriveLetters = new([new(@"Z:"), new(@"Y:"), new(@"X:"), new(@"W:"), new(@"V:"), new(@"U:"), new(@"T:"), new(@"S:"), new(@"R:"), new(@"Q:"), new(@"P:"), new(@"O:"), new(@"N:"), new(@"M:"), new(@"L:"), new(@"K:"), new(@"J:"), new(@"I:"), new(@"H:"), new(@"G:"), new(@"F:"), new(@"E:"), new(@"D:"), new(@"C:"), new(@"B:"), new(@"A:")]);

        /// <summary>
        /// The default exit code to exit out with in the event of an error.
        /// </summary>
        private readonly int DefaultExitCode = 1618;

        /// <summary>
        /// The default exit code used when the user defers a deployment.
        /// </summary>
        private readonly int DeferExitCode = 1602;

        /// <summary>
        /// Indicates whether log files should be compressed upon session closure.
        /// </summary>
        private readonly bool CompressLogs;

        /// <summary>
        /// The log path as specified in the configuration.
        /// </summary>
        private readonly DirectoryInfo ConfigLogPath;

        /// <summary>
        /// Specifies the maximum number of log entries to retain in history.
        /// </summary>
        private readonly int LogMaxHistory;

        /// <summary>
        /// Specifies the style to use for log output.
        /// </summary>
        private readonly LogStyle LogStyle;

        /// <summary>
        /// Specifies whether to write log entries to the host.
        /// </summary>
        private readonly bool LogWriteToHost;

        /// <summary>
        /// Specifies whether to write log entries to the standard streams.
        /// </summary>
        private readonly bool LogHostOutputToStdStreams;

        /// <summary>
        /// Buffer for log entries.
        /// </summary>
        private readonly List<LogEntry> LogBuffer = [];

        /// <summary>
        /// Gets the mounted WIM files within this session.
        /// </summary>
        private readonly List<FileInfo> MountedWimFiles = [];

        /// <summary>
        /// Gets the drive letter used with subst during a Zero-Config WIM file mount operation.
        /// </summary>
        private readonly DriveInfo? DirFilesSubstDrive;

        /// <summary>
        /// Gets the base registry path used for getting/setting deferral information.
        /// </summary>
        private readonly string RegKeyDeferBase;

        /// <summary>
        /// Gets the registry path used for getting/setting deferral information.
        /// </summary>
        private readonly string RegKeyDeferHistory;

        /// <summary>
        /// Gets the default log file name, used when no override is specified.
        /// </summary>
        private readonly string DefaultLogName;

        /// <summary>
        /// Bitfield with settings for this deployment.
        /// </summary>
        private DeploymentSettings Settings;

        /// <summary>
        /// Gets the deployment session's closing exit code.
        /// </summary>
        private int ExitCode;


        #endregion
        #region Frontend parameters.


        /// <summary>
        /// Gets the deployment session's deployment type.
        /// </summary>
        public DeploymentType DeploymentType { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; } = DeploymentType.Install;

        /// <summary>
        /// Gets the deployment session's deployment mode.
        /// </summary>
        public DeployMode DeployMode { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; } = DeployMode.Auto;

        /// <summary>
        /// Gets whether this deployment session is allowed to exit with a reboot exit code.
        /// </summary>
        public bool SuppressRebootPassThru => GetFlagValue(DeploymentSettings.SuppressRebootPassThru);

        /// <summary>
        /// Gets whether this deployment session should enable terminal services install mode.
        /// </summary>
        public bool TerminalServerMode => GetFlagValue(DeploymentSettings.TerminalServerMode);

        /// <summary>
        /// Gets whether this deployment session should disable logging for the operation.
        /// </summary>
        public bool DisableLogging => GetFlagValue(DeploymentSettings.DisableLogging);


        #endregion
        #region Frontend variables.


        /// <summary>
        /// Gets the deployment session's application vendor.
        /// </summary>
        public string? AppVendor { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's application name.
        /// </summary>
        public string? AppName { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's application version.
        /// </summary>
        public string? AppVersion { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's application architecture.
        /// </summary>
        public string? AppArch { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's application language.
        /// </summary>
        public string? AppLang { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's application package revision.
        /// </summary>
        public string? AppRevision { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's exit code(s) to indicate a successful deployment.
        /// </summary>
        public IReadOnlyList<int> AppSuccessExitCodes { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; } = new ReadOnlyCollection<int>([0]);

        /// <summary>
        /// Gets the deployment session's exit code(s) to indicate a reboot is required.
        /// </summary>
        public IReadOnlyList<int> AppRebootExitCodes { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; } = new ReadOnlyCollection<int>([1641, 3010]);

        /// <summary>
        /// Gets the list of application processes that should be closed.
        /// </summary>
        public IReadOnlyList<ProcessDefinition> AppProcessesToClose { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; } = new ReadOnlyCollection<ProcessDefinition>([]);

        /// <summary>
        /// Gets the deployment session's application package version.
        /// </summary>
        public Version? AppScriptVersion { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's application package date.
        /// </summary>
        public DateTime? AppScriptDate { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's application package author.
        /// </summary>
        public string? AppScriptAuthor { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets an override to the deployment session's installation name.
        /// </summary>
        public string InstallName { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets an override to the deployment session's installation title.
        /// </summary>
        public string InstallTitle { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's frontend script name.
        /// </summary>
        public string? DeployAppScriptFriendlyName { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's frontend script version.
        /// </summary>
        public Version? DeployAppScriptVersion { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's frontend script parameters.
        /// </summary>
        public IReadOnlyDictionary<string, object>? DeployAppScriptParameters { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets/sets the deployment session's installation phase.
        /// </summary>
        public string InstallPhase { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] set => SetPropertyValue(ref field, value); } = "Initialization";


        #endregion
        #region Other public variables.


        /// <summary>
        /// Gets the deployment session's starting date and time.
        /// </summary>
        public DateTime CurrentDateTime { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = DateTime.Now;

        /// <summary>
        /// Gets the deployment session's starting date as a string.
        /// </summary>
        public string CurrentDate { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's starting time as a string.
        /// </summary>
        public string CurrentTime { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's UTC offset from GMT 0.
        /// </summary>
        public static readonly TimeSpan CurrentTimeZoneBias = TimeZoneInfo.Local.BaseUtcOffset;

        /// <summary>
        /// Gets the script directory of the caller.
        /// </summary>
        public IReadOnlyList<DirectoryInfo> ScriptDirectory { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; } = new ReadOnlyCollection<DirectoryInfo>([]);

        /// <summary>
        /// Gets the specified or determined path to the Files folder.
        /// </summary>
        public DirectoryInfo? DirFiles { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] set => SetPropertyValue(ref field, value); }

        /// <summary>
        /// Gets the specified or determined path to the SupportFiles folder.
        /// </summary>
        public DirectoryInfo? DirSupportFiles { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] set => SetPropertyValue(ref field, value); }

        /// <summary>
        /// Gets the deployment session's Zero-Config MSI file path.
        /// </summary>
        public FileInfo? DefaultMsiFile { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's Zero-Config MST file path.
        /// </summary>
        public FileInfo? DefaultMstFile { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's Zero-Config MSP file paths.
        /// </summary>
        public IReadOnlyList<FileInfo> DefaultMspFiles { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; } = new ReadOnlyCollection<FileInfo>([]);

        /// <summary>
        /// Gets whether this deployment session found a valid Zero-Config MSI file.
        /// </summary>
        public bool UseDefaultMsi => GetFlagValue(DeploymentSettings.UseDefaultMsi);

        /// <summary>
        /// Gets the deployment session's log path.
        /// </summary>
        public DirectoryInfo LogPath { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets the deployment session's log filename.
        /// </summary>
        public string LogName { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => GetPropertyValue(in field); [MethodImpl(MethodImplOptions.AggressiveInlining)] init; }

        /// <summary>
        /// Gets a value indicating whether administrative privileges are required.
        /// </summary>
        public bool RequireAdmin => GetFlagValue(DeploymentSettings.RequireAdmin);

        /// <summary>
        /// Gets the caller's SessionState from value that was supplied during object instantiation.
        /// </summary>
        public SessionState? SessionState { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }


        #endregion
    }
}

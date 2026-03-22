using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using PSADT.AccountManagement;
using PSADT.DeviceManagement;
using PSADT.Extensions;
using PSADT.Foundation;
using PSADT.Interop;
using PSADT.TerminalServices;
using PSADT.Utilities;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Provides a comprehensive, immutable snapshot of environment-specific information for the current PowerShell
    /// session, including system paths, user and domain context, operating system details, PowerShell and CLR versions,
    /// and other relevant runtime properties.
    /// </summary>
    /// <remarks>The EnvironmentTable record is designed to centralize access to a wide range of environment
    /// data, making it easier for applications and scripts to query system, user, and runtime context in a consistent
    /// manner. It exposes properties for common environment variables, file system locations, user session details,
    /// domain membership, process and OS architecture, and PowerShell execution context. This type is particularly
    /// useful for deployment automation, diagnostics, and scripts that need to adapt their behavior based on the
    /// current environment. All properties are read-only and reflect the state of the environment at the time the
    /// instance is created.</remarks>
    public sealed record EnvironmentTable
    {
        /// <summary>
        /// Initializes a new instance of the EnvironmentTable class, providing access to various environment-related
        /// information.
        /// </summary>
        /// <remarks>This constructor gathers various environment details such as the current language,
        /// operating system information, and user session details. It also validates the input parameters to ensure
        /// they are not null.</remarks>
        /// <param name="cmdlet">The PowerShell cmdlet that is used to retrieve the current execution context. This parameter cannot be null.</param>
        /// <param name="psVersionTable">A hashtable containing version information for the PowerShell environment. This parameter cannot be null.</param>
        /// <param name="psVersion">The version of PowerShell being used, represented as a Version object.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cmdlet"/> or <paramref name="psVersionTable"/> is null.</exception>
        public EnvironmentTable(PSCmdlet cmdlet, Hashtable psVersionTable, Version psVersion)
        {
            // Toolkit info.
            ArgumentNullException.ThrowIfNull(psVersionTable);
            ArgumentNullException.ThrowIfNull(psVersion);
            ArgumentNullException.ThrowIfNull(cmdlet);
            PSModuleInfo moduleInfo = cmdlet.MyInvocation.MyCommand.Module;
            AppDeployToolkitName = moduleInfo.Name;
            AppDeployToolkitPath = moduleInfo.ModuleBase;
            AppDeployMainScriptVersion = moduleInfo.Version;

            // Culture.
            CurrentLanguage = Culture.TwoLetterISOLanguageName.ToUpperInvariant();
            CurrentUILanguage = UICulture.TwoLetterISOLanguageName.ToUpperInvariant();

            // Environment variables.
            EnvHost = cmdlet.Host;
            EnvHostVersion = EnvHost.Version;
            EnvSystemDrive = new(EnvSystem32Directory.Root.FullName);

            // Domain membership.
            DomainStatus domainStatus = DeviceUtilities.GetDomainStatus();
            if (IsMachinePartOfDomain = domainStatus.JoinStatus == NETSETUP_JOIN_STATUS.NetSetupDomainName)
            {
                // Set the domain name.
                if (domainStatus.DomainOrWorkgroupName is string domainName)
                {
                    EnvMachineADDomain = CultureInfo.InvariantCulture.TextInfo.ToLower(domainName);
                }

                // Set the FQDN of the computer name.
                try
                {
                    EnvComputerNameFQDN = Dns.GetHostEntry("localhost").HostName;
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    EnvComputerNameFQDN = $"{EnvComputerName}.{EnvMachineADDomain}";
                }

                // Determine the logon server.
                if (EnvironmentUtilities.GetEnvironmentVariable("LOGONSERVER") is string logonServer && !logonServer.Contains("\\\\MicrosoftAccount"))
                {
                    try
                    {
                        EnvLogonServer = Dns.GetHostEntry(logonServer).HostName;

                    }
                    catch (Exception ex) when (ex.Message is not null)
                    {
                        EnvLogonServer = (string?)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy\\History", "DCName", null);
                    }
                }
                else
                {
                    EnvLogonServer = (string?)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy\\History", "DCName", null);
                }
                if (EnvLogonServer is not null && EnvLogonServer.StartsWith("\\"))
                {
                    EnvLogonServer = EnvLogonServer.TrimStart('\\');
                }
            }
            else
            {
                EnvMachineWorkgroup = domainStatus.DomainOrWorkgroupName?.ToUpperInvariant();
                EnvComputerNameFQDN = Dns.GetHostName();
            }

            // Normalised paths that vary based on process architecture.
            if (Is64Bit)
            {
                if (Is64BitProcess)
                {
                    EnvProgramFiles = GetEnvironmentFolderPath(Environment.SpecialFolder.ProgramFiles);
                    EnvCommonProgramFiles = GetEnvironmentFolderPath(Environment.SpecialFolder.CommonProgramFiles);
                    EnvSysNativeDirectory = EnvSystem32Directory;
                    EnvSysWow64Directory = GetEnvironmentFolderPath(Environment.SpecialFolder.SystemX86);
                }
                else
                {
                    EnvProgramFiles = GetEnvironmentVariableDirectory("ProgramW6432");
                    EnvCommonProgramFiles = GetEnvironmentVariableDirectory("CommonProgramW6432");
                    EnvSysNativeDirectory = EnvWinDir is not null ? new(Path.Combine(EnvWinDir.FullName, "sysnative")) : null;
                    EnvSysWow64Directory = EnvSystem32Directory;
                }
                EnvProgramFilesX86 = GetEnvironmentFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                EnvCommonProgramFilesX86 = GetEnvironmentFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);
                EnvSystemProfile = EnvSysNativeDirectory is not null ? new(Path.Combine(EnvSysNativeDirectory.FullName, "Config", "systemprofile")) : null;
                EnvSystemProfileX86 = EnvSysWow64Directory is not null ? new(Path.Combine(EnvSysWow64Directory.FullName, "Config", "systemprofile")) : null;
            }
            else
            {
                EnvProgramFiles = GetEnvironmentFolderPath(Environment.SpecialFolder.ProgramFiles);
                EnvCommonProgramFiles = GetEnvironmentFolderPath(Environment.SpecialFolder.CommonProgramFiles);
                EnvSysNativeDirectory = EnvSystem32Directory;
                EnvSystemProfile = EnvSysNativeDirectory is not null ? new(Path.Combine(EnvSysNativeDirectory.FullName, "Config", "systemprofile")) : null;
            }

            // Operating system information.
            OperatingSystemInfo osInfo = OperatingSystemInfo.Current;
            EnvOSName = osInfo.Name;
            EnvOSVersion = osInfo.Version;
            EnvOSProductType = (int)osInfo.ProductType;
            IsTerminalServer = osInfo.IsTerminalServer;
            IsMultiSessionOS = osInfo.IsWorkstationEnterpriseMultiSessionOS;

            // Office C2R information.
            using (RegistryKey? officeC2RKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration"))
            {
                if (officeC2RKey is not null)
                {
                    Dictionary<string, object> officeVars = [];
                    foreach (string valueName in officeC2RKey.GetValueNames())
                    {
                        object? value = officeC2RKey.GetValue(valueName);
                        if ((value is string str && !string.IsNullOrWhiteSpace(str)) || value is not string and not null)
                        {
                            officeVars.Add(valueName, value);
                        }
                    }
                    EnvOfficeVars = new ReadOnlyDictionary<string, object>(officeVars);
                }
            }
            if (EnvOfficeVars is not null)
            {
                if (EnvOfficeVars.TryGetValue("VersionToReport", out object? versionToReportObj) && versionToReportObj is string versionToReportStr && Version.TryParse(versionToReportStr, out Version? versionToReport))
                {
                    EnvOfficeVersion = versionToReport;
                }
                if (EnvOfficeVars.TryGetValue("Platform", out object? platformObj) && platformObj is string platformStr)
                {
                    EnvOfficeBitness = platformStr;
                }
                EnvOfficeChannel = (EnvOfficeVars.TryGetValue("UpdateChannel", out object? channelObj) && channelObj is string channelStr ? channelStr.Substring(channelStr.LastIndexOf('/') + 1) : EnvOfficeVars.TryGetValue("CDNBaseUrl", out object? cdnBaseUrlObj) && cdnBaseUrlObj is string cdnBaseUrlStr ? cdnBaseUrlStr.Substring(cdnBaseUrlStr.LastIndexOf('/') + 1) : null) switch
                {
                    "492350f6-3a01-4f97-b9c0-c7c6ddf67d60" => "monthly",
                    "7ffbc6bf-bc32-4f92-8982-f9dd17fd3114" => "semi-annual",
                    "64256afe-f5d9-4f86-8936-8840a6a4f5be" => "monthly targeted",
                    "b8f9b850-328d-4355-9145-c59439a0c4cf" => "semi-annual targeted",
                    "55336b82-a18d-4dd6-b5f6-9e5095c314a6" => "monthly enterprise",
                    _ => null
                };
            }

            // Hardware information.
            if (HardwareInfo.SystemInformation.Manufacturer is string manufacturer && HardwareInfo.SystemInformation.SerialNumber is string serialNumber && HardwareInfo.SystemInformation.ProductName is string productName && HardwareInfo.SystemInformation.Version is string version)
            {
                EnvHardwareType = version.Contains("VRTUAL") || (manufacturer.Contains("Microsoft") && !productName.Contains("Surface"))
                    ? "Virtual:Hyper-V"
                    : version.Contains("A M I")
                    ? "Virtual:Virtual PC"
                    : version.Contains("Xen")
                    ? "Virtual:Xen"
                    : serialNumber.Contains("VMware") || manufacturer.Contains("VMware")
                    ? "Virtual:VMware"
                    : serialNumber.Contains("Parallels") || manufacturer.Contains("Parallels")
                    ? "Virtual:Parallels"
                    : productName.Contains("Virtual")
                    ? "Virtual"
                    : "Physical";
            }

            // PowerShell version information.
            using (Process currentProcess = Process.GetCurrentProcess())
            {
                EnvPSProcessPath = currentProcess.GetFilePath();
            }
            if (psVersionTable["CLRVersion"] is Version clrVersion)
            {
                EnvCLRVersion = clrVersion;
            }
            EnvPSVersionTable = psVersionTable;
            EnvPSVersion = psVersion;

            // Logged on user information.
            if (LoggedOnUserSessions.Count > 0)
            {
                UsersLoggedOn = new ReadOnlyCollection<NTAccount>([.. LoggedOnUserSessions.Select(static s => s.NTAccount)]);
                CurrentLoggedOnUserSession = LoggedOnUserSessions.FirstOrDefault(static s => s.IsCurrentSession);
                CurrentConsoleUserSession = LoggedOnUserSessions.FirstOrDefault(static s => s.IsConsoleSession);
                RunAsActiveUser = RunAsActiveUser.Get(LoggedOnUserSessions);
                if (RunAsActiveUser is not null)
                {
                    RunAsActiveUserLocale = Registry.GetValue($@"HKEY_USERS\{RunAsActiveUser.SID}\Control Panel\International", "LocaleName", null) is string localeName && !string.IsNullOrWhiteSpace(localeName) ? new(localeName) : null;
                    RunAsUserProfile = Registry.GetValue(@$"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{RunAsActiveUser.SID}", "ProfileImagePath", null) is string runAsUserProfile && !string.IsNullOrWhiteSpace(runAsUserProfile) ? new(runAsUserProfile) : null;
                    UserProfileName = RunAsUserProfile?.Name;
                }
            }

            // Miscellaneous environment information.
            InvalidFileNameCharsRegexPattern = new($"[{Regex.Escape(new(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);
        }

        /// <summary>
        /// Gets the name of the application deployment toolkit.
        /// </summary>
        /// <remarks>This property provides the name of the toolkit used for application deployment, which
        /// can be useful for logging or configuration purposes.</remarks>
        public string AppDeployToolkitName { get; }

        /// <summary>
        /// Gets the file system path to the application deployment toolkit.
        /// </summary>
        /// <remarks>This property provides the location of the App Deploy Toolkit, which is used for
        /// managing application deployments. Ensure that the path is correctly set to avoid issues during deployment
        /// operations.</remarks>
        public string AppDeployToolkitPath { get; }

        /// <summary>
        /// Gets the version of the main deployment script used by the application.
        /// </summary>
        /// <remarks>This property is useful for determining compatibility with specific script versions
        /// during deployment or updates. Use this information to ensure that the application is running with the
        /// expected deployment script version.</remarks>
        public Version AppDeployMainScriptVersion { get; }

        /// <summary>
        /// Gets the culture information associated with the current instance.
        /// </summary>
        /// <remarks>This property provides access to the culture-specific information, such as formatting
        /// for dates, numbers, and strings. It is useful for applications that need to present data in a culture-aware
        /// manner.</remarks>
        public CultureInfo Culture { get; } = CultureInfo.CurrentCulture;

        /// <summary>
        /// Gets the culture used by the user interface, which influences formatting of dates, numbers, and strings.
        /// </summary>
        /// <remarks>This property reflects the current UI culture settings of the application, which can
        /// be important for localizing user interfaces and ensuring that content is presented in a culturally
        /// appropriate manner.</remarks>
        public CultureInfo UICulture { get; } = CultureInfo.CurrentUICulture;

        /// <summary>
        /// Gets the name of the programming language that is currently active in the application.
        /// </summary>
        /// <remarks>This property reflects the language setting that is currently active in the
        /// application. It is useful for determining the language context for localization or syntax highlighting
        /// purposes.</remarks>
        public string CurrentLanguage { get; }

        /// <summary>
        /// Gets the current user interface language for the application.
        /// </summary>
        /// <remarks>This property reflects the language that is currently set for the application's user
        /// interface. The value may affect the display of localized resources, such as text and formatting, throughout
        /// the application.</remarks>
        public string CurrentUILanguage { get; }

        /// <summary>
        /// Gets the host interface for the current PowerShell runspace environment.
        /// </summary>
        /// <remarks>The EnvHost property provides access to the PSHost instance that represents the
        /// environment in which the PowerShell session is running. This can be used to interact with the host
        /// application, such as retrieving information about the console or managing user input and output.</remarks>
        public PSHost EnvHost { get; }

        /// <summary>
        /// Gets the version of the environment host.
        /// </summary>
        /// <remarks>This property provides the version information of the environment host, which can be
        /// useful for compatibility checks and debugging purposes.</remarks>
        public Version EnvHostVersion { get; }

        /// <summary>
        /// Gets the major version number of the environment host.
        /// </summary>
        public int EnvHostVersionMajor => EnvHostVersion.Major;

        /// <summary>
        /// Gets the minor version number of the environment host.
        /// </summary>
        public int EnvHostVersionMinor => EnvHostVersion.Minor;

        /// <summary>
        /// Gets the build number component of the environment host version.
        /// </summary>
        public int? EnvHostVersionBuild => EnvHostVersion.Build >= 0 ? EnvHostVersion.Build : null;

        /// <summary>
        /// Gets the revision number of the environment host version.
        /// </summary>
        public int? EnvHostVersionRevision => EnvHostVersion.Revision >= 0 ? EnvHostVersion.Revision : null;

        /// <summary>
        /// Gets the path to the profile directory that is shared by all users on the system.
        /// </summary>
        /// <remarks>This property provides the location where applications can store data or
        /// configuration files that are accessible to every user account on the computer. The exact path may vary
        /// depending on the operating system version and configuration.</remarks>
        public DirectoryInfo? EnvAllUsersProfile { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.CommonApplicationData);

        /// <summary>
        /// Gets the application-specific data directory for the current environment.
        /// </summary>
        /// <remarks>This property provides the path to a directory intended for storing application data
        /// that is specific to the current environment. Use this path to read or write files that should be isolated
        /// from other environments or users.</remarks>
        public DirectoryInfo? EnvAppData { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// Gets the architecture of the environment in which the application is running.
        /// </summary>
        /// <remarks>This property provides information about the system architecture, which can be useful
        /// for determining compatibility and performance characteristics. It may return null if the architecture is not
        /// specified.</remarks>
        public string? EnvArchitecture { get; } = EnvironmentUtilities.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");

        /// <summary>
        /// Gets the full path to the common desktop folder that is shared by all users on the system.
        /// </summary>
        /// <remarks>This property can be used to access or store files that should be available to every
        /// user on the computer. The location is determined by the operating system and may vary between
        /// environments.</remarks>
        public DirectoryInfo? EnvCommonDesktop { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);

        /// <summary>
        /// Gets the full path to the common documents directory for the current environment.
        /// </summary>
        public DirectoryInfo? EnvCommonDocuments { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.CommonDocuments);

        /// <summary>
        /// Gets the full path to the common Start Menu Programs folder shared by all users on the system.
        /// </summary>
        public DirectoryInfo? EnvCommonStartMenuPrograms { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.CommonStartMenu);

        /// <summary>
        /// Gets the full path to the common Start Menu folder for all users on the system.
        /// </summary>
        /// <remarks>This property provides the location where shortcuts and items that appear in the
        /// Start Menu for every user are stored. Applications can use this path to access or manage Start Menu entries
        /// that are shared across all user profiles.</remarks>
        public DirectoryInfo? EnvCommonStartMenu { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.CommonStartMenu);

        /// <summary>
        /// Gets the common startup environment configuration string used for initializing the application.
        /// </summary>
        /// <remarks>This property provides the necessary environment settings that are typically required
        /// during the startup phase of the application. It is essential for ensuring that the application is configured
        /// correctly before execution.</remarks>
        public DirectoryInfo? EnvCommonStartUp { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.CommonStartup);

        /// <summary>
        /// Gets the common templates used in the environment.
        /// </summary>
        /// <remarks>This property provides access to a set of predefined templates that can be utilized
        /// across various components of the application. These templates are designed to standardize and streamline the
        /// development process.</remarks>
        public DirectoryInfo? EnvCommonTemplates { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.CommonTemplates);

        /// <summary>
        /// Gets the home drive of the current environment, which specifies the default location for user files.
        /// </summary>
        /// <remarks>This property may return null if the home drive is not set in the environment. It is
        /// typically used to determine where user-specific files should be stored or accessed.</remarks>
        public DriveInfo? EnvHomeDrive { get; } = EnvironmentUtilities.GetEnvironmentVariable("HOMEDRIVE") is string homeDrive ? new(homeDrive) : null;

        /// <summary>
        /// Gets the environment home path, which specifies the directory used for storing environment-specific files.
        /// </summary>
        /// <remarks>This property may return null if the environment home path is not set. It is
        /// typically used to locate configuration files or other resources specific to the environment.</remarks>
        public DirectoryInfo? EnvHomePath { get; } = GetEnvironmentVariableDirectory("HOMEPATH");

        /// <summary>
        /// Gets the path to the home share as specified by the relevant environment variable.
        /// </summary>
        /// <remarks>This property returns the network path used for accessing shared resources in a
        /// multi-user or networked environment. If the environment variable is not set, the property returns
        /// null.</remarks>
        public DirectoryInfo? EnvHomeShare { get; } = GetEnvironmentVariableDirectory("HOMESHARE");

        /// <summary>
        /// Gets the path to the local application data folder for the current user.
        /// </summary>
        /// <remarks>This property provides the directory where application data specific to the user can
        /// be stored. It is typically used for storing user-specific settings and data that should not be shared with
        /// other users on the same machine.</remarks>
        public DirectoryInfo? EnvLocalAppData { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.LocalApplicationData);

        /// <summary>
        /// Gets a read-only list of the logical drive names available on the current system.
        /// </summary>
        /// <remarks>The collection reflects the logical drives that are accessible in the environment at
        /// the time of retrieval. The list may vary depending on the system configuration and available
        /// drives.</remarks>
        public IReadOnlyList<DriveInfo> EnvLogicalDrives { get; } = new ReadOnlyCollection<DriveInfo>(DriveInfo.GetDrives());

        /// <summary>
        /// Gets the path to the environment-specific program data directory for the application.
        /// </summary>
        /// <remarks>This property provides access to a directory path that may vary depending on the
        /// application's deployment environment or configuration. Use this path to store or retrieve data that should
        /// be isolated per environment, such as configuration files or logs.</remarks>
        public DirectoryInfo? EnvProgramData { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.CommonApplicationData);

        /// <summary>
        /// Gets the value of the public environment variable, if it is set.
        /// </summary>
        /// <remarks>This property returns a string representing the value of the public environment
        /// variable. If the variable is not defined in the environment, the property returns null.</remarks>
        public DirectoryInfo? EnvPublic { get; } = GetEnvironmentVariableDirectory("PUBLIC");

        /// <summary>
        /// Gets the drive letter of the system drive where the operating system is installed.
        /// </summary>
        /// <remarks>This property is useful for determining file paths or configurations that depend on
        /// the location of the operating system. The value typically represents the root drive, such as "C:" on Windows
        /// systems.</remarks>
        public DriveInfo EnvSystemDrive { get; }

        /// <summary>
        /// Gets the full path to the root directory of the system environment.
        /// </summary>
        /// <remarks>This property can be used to locate system-level files and resources that reside in
        /// the environment's root directory. The returned path is typically platform-dependent and reflects the system
        /// configuration at runtime.</remarks>
        public DirectoryInfo? EnvSystemRoot { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.Windows);

        /// <summary>
        /// Gets the current environmental temperature as a string.
        /// </summary>
        /// <remarks>The temperature is represented in degrees Celsius and is updated periodically based
        /// on environmental changes.</remarks>
        public DirectoryInfo EnvTemp { get; } = new(Path.GetTempPath());

        /// <summary>
        /// Gets the cookies associated with the current user's environment.
        /// </summary>
        /// <remarks>This property retrieves the cookies that are set for the user's session, which may be
        /// used for tracking or maintaining user-specific settings.</remarks>
        public DirectoryInfo? EnvUserCookies { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.Cookies);

        /// <summary>
        /// Gets the full path to the current user's desktop directory.
        /// </summary>
        public DirectoryInfo? EnvUserDesktop { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.DesktopDirectory);

        /// <summary>
        /// Gets the user's favorite environment settings.
        /// </summary>
        /// <remarks>The format and content of the returned string may vary depending on user preferences
        /// and application configuration. Use this property to retrieve a representation of the user's preferred
        /// environment settings for further processing or display.</remarks>
        public DirectoryInfo? EnvUserFavorites { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.Favorites);

        /// <summary>
        /// Gets the full path to the current user's Internet cache directory.
        /// </summary>
        /// <remarks>This property provides the location where the user's Internet cache files are stored.
        /// It can be used to access or manage cached web content specific to the user profile.</remarks>
        public DirectoryInfo? EnvUserInternetCache { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.InternetCache);

        /// <summary>
        /// Gets a string representation of the user's internet browsing history.
        /// </summary>
        /// <remarks>The format and content of the returned string may vary depending on the user's
        /// browsing activity and system configuration. Access to this property may be subject to privacy considerations
        /// and user permissions.</remarks>
        public DirectoryInfo? EnvUserInternetHistory { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.History);

        /// <summary>
        /// Gets the full path to the current user's My Documents folder.
        /// </summary>
        public DirectoryInfo? EnvUserMyDocuments { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.MyDocuments);

        /// <summary>
        /// Gets the user name of the current Windows environment.
        /// </summary>
        public string EnvUserName { get; } = Environment.UserName;

        /// <summary>
        /// Gets the full path to the current user's Pictures directory.
        /// </summary>
        public DirectoryInfo? EnvUserPictures { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.MyPictures);

        /// <summary>
        /// Gets the full path to the current user's profile directory as defined by the system environment.
        /// </summary>
        /// <remarks>This property retrieves the user profile path from the environment variables, which
        /// can be used to access user-specific files and settings. The value is typically determined by the operating
        /// system and may vary between platforms or user accounts.</remarks>
        public DirectoryInfo? EnvUserProfile { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.UserProfile);

        /// <summary>
        /// Gets the email address to which user notifications are sent.
        /// </summary>
        public DirectoryInfo? EnvUserSendTo { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.SendTo);

        /// <summary>
        /// Gets the full path to the current user's Start Menu directory.
        /// </summary>
        /// <remarks>This property provides the location where the user's Start Menu shortcuts are stored.
        /// It is typically used to access or manage user-specific application shortcuts.</remarks>
        public DirectoryInfo? EnvUserStartMenu { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.StartMenu);

        /// <summary>
        /// Gets the full path to the current user's Start Menu Programs folder.
        /// </summary>
        /// <remarks>This property provides the location where user-specific program shortcuts are stored.
        /// It is typically used to access or manipulate the Start Menu items for the current user.</remarks>
        public DirectoryInfo? EnvUserStartMenuPrograms { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.Programs);

        /// <summary>
        /// Gets the user-specific startup environment configuration string.
        /// </summary>
        public DirectoryInfo? EnvUserStartUp { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.Startup);

        /// <summary>
        /// Gets the environment user templates for the application.
        /// </summary>
        public DirectoryInfo? EnvUserTemplates { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.Templates);

        /// <summary>
        /// Gets the full path to the System32 directory of the current Windows environment.
        /// </summary>
        /// <remarks>This property provides the location of core system files required by Windows and many
        /// applications. The path may differ depending on the process architecture (32-bit or 64-bit) and the operating
        /// system version.</remarks>
        public DirectoryInfo EnvSystem32Directory { get; } = new(Environment.SystemDirectory);

        /// <summary>
        /// Gets the full path to the Windows directory for the current environment.
        /// </summary>
        public DirectoryInfo? EnvWinDir { get; } = GetEnvironmentFolderPath(Environment.SpecialFolder.Windows);

        /// <summary>
        /// Gets a value indicating whether a Microsoft Endpoint Configuration Manager (SCCM) task sequence is currently
        /// running on the system.
        /// </summary>
        /// <remarks>This property determines the presence of an active task sequence by checking for the
        /// availability of the Microsoft SMS TSEnvironment. Use this property to conditionally execute logic that
        /// should only run during a task sequence deployment.</remarks>
        public bool RunningTaskSequence { get; } = Type.GetTypeFromProgID("Microsoft.SMS.TSEnvironment") is not null;

        /// <summary>
        /// Gets a value indicating whether a DeployR task sequence is currently running.
        /// </summary>
        /// <remarks>This property determines if a Deployr task sequence is active by checking for the
        /// presence of the "DEPLOYR_PROCESS" environment variable. Use this property to detect whether
        /// deployment-related operations are in progress within the current process context.</remarks>
        public bool RunningDeployrTaskSequence { get; } = !string.IsNullOrWhiteSpace(EnvironmentUtilities.GetEnvironmentVariable("DEPLOYR_PROCESS"));

        /// <summary>
        /// Gets a value indicating whether the machine is joined to a domain.
        /// </summary>
        /// <remarks>This property is useful for determining the network context of the machine,
        /// particularly in enterprise or managed environments where domain membership may affect authentication, policy
        /// enforcement, or resource access.</remarks>
        public bool IsMachinePartOfDomain { get; }

        /// <summary>
        /// Gets the name of the workgroup to which the current machine belongs.
        /// </summary>
        /// <remarks>This property returns the workgroup name for the environment machine. If the machine
        /// is not part of a workgroup, the property may return null.</remarks>
        public string? EnvMachineWorkgroup { get; }

        /// <summary>
        /// Gets the Active Directory domain name associated with the current machine.
        /// </summary>
        /// <remarks>This property returns the domain name if the machine is joined to an Active Directory
        /// domain. If the machine is not part of a domain, the property returns null.</remarks>
        public string? EnvMachineADDomain { get; }

        /// <summary>
        /// Gets the name of the server used for environment logon operations.
        /// </summary>
        /// <remarks>This property can be used to determine the server context in which the application is
        /// authenticating. The value may be null if the environment logon server is not set or unavailable.</remarks>
        public string? EnvLogonServer { get; }

        /// <summary>
        /// Gets the DNS domain name of the current machine as reported by the operating system.
        /// </summary>
        /// <remarks>This property retrieves the domain name associated with the local machine's network
        /// configuration. It is useful for applications that need to identify the domain context in which they are
        /// running.</remarks>
        public string EnvMachineDNSDomain { get; } = IPGlobalProperties.GetIPGlobalProperties().DomainName;

        /// <summary>
        /// Gets the DNS domain name associated with the current user environment, if available.
        /// </summary>
        /// <remarks>This property retrieves the value of the USERDNSDOMAIN environment variable and
        /// converts it to lowercase using the invariant culture. It returns null if the environment variable is not set
        /// or is empty.</remarks>
        public string? EnvUserDNSDomain { get; } = EnvironmentUtilities.GetEnvironmentVariable("USERDNSDOMAIN") is string userDnsDomain && !string.IsNullOrWhiteSpace(userDnsDomain) ? CultureInfo.InvariantCulture.TextInfo.ToLower(userDnsDomain) : null;

        /// <summary>
        /// Gets the domain name of the user currently logged into the environment, represented in uppercase.
        /// </summary>
        /// <remarks>Returns null if the user domain name is not available or is empty.</remarks>
        public string? EnvUserDomain { get; } = Environment.UserDomainName is string userDomainName && !string.IsNullOrWhiteSpace(userDomainName) ? userDomainName.ToUpperInvariant() : null;

        /// <summary>
        /// Gets the DNS host name of the local computer.
        /// </summary>
        /// <remarks>This property retrieves the host name as reported by the Domain Name System (DNS) for
        /// the current machine. The value can be used for network identification, logging, or other operations that
        /// require the computer's network name.</remarks>
        public string EnvComputerName { get; } = Dns.GetHostName();

        /// <summary>
        /// Gets the fully qualified domain name (FQDN) of the environment's computer.
        /// </summary>
        /// <remarks>This property provides the FQDN, which includes the hostname and the domain name,
        /// useful for network identification and configuration.</remarks>
        public string EnvComputerNameFQDN { get; }

        /// <summary>
        /// Gets a value indicating whether the current operating system is a 64-bit version.
        /// </summary>
        /// <remarks>This property reflects the architecture of the operating system on which the
        /// application is running. Application behavior or compatibility with certain libraries may depend on whether
        /// the environment is 64-bit.</remarks>
        public bool Is64Bit { get; } = Environment.Is64BitOperatingSystem;

        /// <summary>
        /// Gets the architecture of the operating system on which the application is running.
        /// </summary>
        /// <remarks>This property reflects the OS architecture as reported by the runtime, which can be
        /// useful for determining compatibility with platform-specific features or libraries.</remarks>
        public Architecture EnvOSArchitecture { get; } = RuntimeInformation.OSArchitecture;

        /// <summary>
        /// Gets a value indicating whether the current process is running in a 64-bit environment.
        /// </summary>
        /// <remarks>This property is useful for determining process architecture, which can affect
        /// compatibility with certain libraries and APIs.</remarks>
        public bool Is64BitProcess { get; } = Environment.Is64BitProcess;

        /// <summary>
        /// Gets the architecture of the process running the application.
        /// </summary>
        /// <remarks>This property reflects the architecture of the current process, which can be useful
        /// for determining compatibility with certain libraries or features that depend on the underlying
        /// architecture.</remarks>
        public Architecture PSArchitecture { get; } = RuntimeInformation.ProcessArchitecture;

        /// <summary>
        /// Gets the full path to the Program Files directory for the current environment.
        /// </summary>
        /// <remarks>This property returns the directory path where applications are typically installed.
        /// The exact location may vary depending on the operating system and its configuration.</remarks>
        public DirectoryInfo? EnvProgramFiles { get; }

        /// <summary>
        /// Gets the full path to the Program Files (x86) directory on the current system.
        /// </summary>
        /// <remarks>This property provides the location where 32-bit applications are typically installed
        /// on 64-bit Windows operating systems. It can be used to locate or store files specific to 32-bit
        /// applications.</remarks>
        public DirectoryInfo? EnvProgramFilesX86 { get; }

        /// <summary>
        /// Gets the full path to the directory that contains common program files shared by applications on the system.
        /// </summary>
        /// <remarks>This property is typically used to locate resources or libraries that are shared
        /// among multiple applications. The returned path is specific to the current environment and may vary depending
        /// on the operating system and user context.</remarks>
        public DirectoryInfo? EnvCommonProgramFiles { get; }

        /// <summary>
        /// Gets the full path to the common program files directory for 32-bit applications on a 64-bit operating
        /// system.
        /// </summary>
        /// <remarks>This property is useful for accessing shared resources or libraries that are
        /// installed in the 32-bit common program files directory. It returns null if the corresponding environment
        /// variable is not set.</remarks>
        public DirectoryInfo? EnvCommonProgramFilesX86 { get; }

        /// <summary>
        /// Gets the directory path for the native environment system files.
        /// </summary>
        public DirectoryInfo? EnvSysNativeDirectory { get; }

        /// <summary>
        /// Gets the full path to the SysWow64 directory, which contains 32-bit system files on a 64-bit Windows
        /// operating system.
        /// </summary>
        /// <remarks>This property is useful for applications that need to access or interact with 32-bit
        /// system components on a 64-bit platform. The value may be null if the directory cannot be determined on the
        /// current system.</remarks>
        public DirectoryInfo? EnvSysWow64Directory { get; }

        /// <summary>
        /// Gets the name of the current environment system profile.
        /// </summary>
        /// <remarks>The environment system profile provides information about the system configuration or
        /// context in which the application is running. This value may influence application behavior or configuration
        /// settings.</remarks>
        public DirectoryInfo? EnvSystemProfile { get; }

        /// <summary>
        /// Gets the path to the system profile directory used by 32-bit applications on a 64-bit operating system.
        /// </summary>
        /// <remarks>This property is useful for accessing configuration or data specific to the 32-bit
        /// environment when running on a 64-bit system. The value may be null if the system profile directory cannot be
        /// determined.</remarks>
        public DirectoryInfo? EnvSystemProfileX86 { get; }

        /// <summary>
        /// Gets the name of the operating system environment.
        /// </summary>
        public string EnvOSName { get; }

        /// <summary>
        /// Gets the version of the operating system environment.
        /// </summary>
        /// <remarks>This property provides the OS version information, which can be useful for
        /// determining compatibility or specific features available in the current environment.</remarks>
        public Version EnvOSVersion { get; }

        /// <summary>
        /// Gets the major version number of the operating system environment.
        /// </summary>
        /// <remarks>This property retrieves the major version component of the operating system version,
        /// which can be useful for determining compatibility with specific OS features or behaviors.</remarks>
        public int EnvOSVersionMajor => EnvOSVersion.Major;

        /// <summary>
        /// Gets the minor version number of the operating system on which the application is running.
        /// </summary>
        /// <remarks>Use this property to determine the specific minor version of the operating system,
        /// which can be useful for compatibility checks or enabling features based on OS version.</remarks>
        public int EnvOSVersionMinor => EnvOSVersion.Minor;

        /// <summary>
        /// Gets the build number of the operating system version for the current environment.
        /// </summary>
        /// <remarks>The build number can be used to determine the specific release or update level of the
        /// operating system, which may be relevant for compatibility checks or feature availability.</remarks>
        public int? EnvOSVersionBuild => EnvOSVersion.Build >= 0 ? EnvOSVersion.Build : null;

        /// <summary>
        /// Gets the revision number of the current operating system version.
        /// </summary>
        public int? EnvOSVersionRevision => EnvOSVersion.Revision >= 0 ? EnvOSVersion.Revision : null;

        /// <summary>
        /// Gets the product type of the operating system environment as an integer value.
        /// </summary>
        /// <remarks>This property can be used to determine the specific product type of the operating
        /// system, such as workstation, domain controller, or server. The value corresponds to the underlying operating
        /// system's product type identifier, which may be useful for environment-specific logic or
        /// configuration.</remarks>
        public int EnvOSProductType { get; }

        /// <summary>
        /// Gets a value indicating whether the current operating system is a server edition.
        /// </summary>
        /// <remarks>This property returns <see langword="true"/> if the environment's operating system is
        /// identified as a server version based on its product type. Use this property to distinguish between server
        /// and client operating systems when conditional logic is required.</remarks>
        public bool IsServerOS => EnvOSProductType == 3;

        /// <summary>
        /// Gets a value indicating whether the operating system is configured as a domain controller.
        /// </summary>
        /// <remarks>This property determines if the current operating system is functioning as a domain
        /// controller by evaluating its product type. Use this property to check for domain controller-specific
        /// behavior or requirements in your application.</remarks>
        public bool IsDomainControllerOS => EnvOSProductType == 2;

        /// <summary>
        /// Gets a value indicating whether the operating system is a workstation edition.
        /// </summary>
        /// <remarks>This property can be used to distinguish between workstation and server operating
        /// system environments. It is useful for scenarios where application behavior should differ based on the OS
        /// type.</remarks>
        public bool IsWorkstationOS => EnvOSProductType == 1;

        /// <summary>
        /// Gets a value indicating whether the current environment is a terminal server.
        /// </summary>
        /// <remarks>Use this property to determine if the application is running in a terminal server
        /// environment, which may affect certain functionalities or configurations.</remarks>
        public bool IsTerminalServer { get; }

        /// <summary>
        /// Gets a value indicating whether the operating system supports multiple user sessions.
        /// </summary>
        /// <remarks>This property is useful for determining if the application can leverage multi-session
        /// capabilities, such as in terminal services or remote desktop scenarios.</remarks>
        public bool IsMultiSessionOS { get; }

        /// <summary>
        /// Gets the name of the operating system product type for the current environment.
        /// </summary>
        /// <remarks>The returned value indicates whether the environment is a workstation, domain
        /// controller, server, or an unknown type, based on the value of the EnvOSProductType property.</remarks>
        public string EnvOSProductTypeName => EnvOSProductType switch
        {
            1 => "Workstation",
            2 => "Domain Controller",
            3 => "Server",
            _ => "Unknown"
        };

        /// <summary>
        /// Gets a read-only dictionary that contains environment-specific office variables.
        /// </summary>
        /// <remarks>The returned dictionary provides key-value pairs representing office-related settings
        /// or configurations that are specific to the current environment. The collection is read-only and cannot be
        /// modified through this property.</remarks>
        public IReadOnlyDictionary<string, object>? EnvOfficeVars { get; }

        /// <summary>
        /// Gets the version of Microsoft Office that is currently installed on the environment, if available.
        /// </summary>
        /// <remarks>This property returns a <see cref="Version"/> object representing the installed
        /// Office version. If Microsoft Office is not installed or the version cannot be determined, the property
        /// returns <see langword="null"/>.</remarks>
        public Version? EnvOfficeVersion { get; }

        /// <summary>
        /// Gets the bitness of the Office environment, indicating whether it is 32-bit or 64-bit.
        /// </summary>
        /// <remarks>This property is useful for determining compatibility with Office add-ins or
        /// applications that depend on the Office bitness.</remarks>
        public string? EnvOfficeBitness { get; }

        /// <summary>
        /// Gets the current Office channel configuration for the environment.
        /// </summary>
        /// <remarks>The Office channel configuration may affect feature availability and application
        /// behavior. Use this property to determine which Office update channel is active in the current
        /// environment.</remarks>
        public string? EnvOfficeChannel { get; }

        /// <summary>
        /// Gets the total amount of physical system memory, in megabytes, installed on the machine.
        /// </summary>
        /// <remarks>This property can be used to monitor available system resources or to make decisions
        /// based on the total installed RAM. The value reflects the physical memory detected by the operating system at
        /// the time of retrieval.</remarks>
        public decimal EnvSystemRAM { get; } = DeviceUtilities.GetTotalSystemMemory() / (decimal)1073741824;

        /// <summary>
        /// Gets the type of hardware used in the current environment.
        /// </summary>
        /// <remarks>This property provides information about the hardware configuration, which can be
        /// useful for diagnostics and performance tuning.</remarks>
        public string? EnvHardwareType { get; }

        /// <summary>
        /// Gets the environment PowerShell version information as a hashtable.
        /// </summary>
        /// <remarks>This hashtable contains key-value pairs representing various details about the
        /// PowerShell version, including the version number and other relevant metadata. It is useful for determining
        /// the capabilities and features available in the current PowerShell environment.</remarks>
        public Hashtable EnvPSVersionTable { get; }

        /// <summary>
        /// Gets the path of the PowerShell process for the current environment.
        /// </summary>
        /// <remarks>This property provides the full path to the PowerShell executable being used in the current environment.
        /// It is useful for scenarios where the exact location of the PowerShell process is required.</remarks>
        public FileInfo EnvPSProcessPath { get; }

        /// <summary>
        /// Gets the version of the PowerShell environment currently in use.
        /// </summary>
        /// <remarks>This property provides the version information of the PowerShell environment, which
        /// can be useful for determining compatibility with scripts or modules that depend on specific PowerShell
        /// features.</remarks>
        public Version EnvPSVersion { get; }

        /// <summary>
        /// Gets the major version number of the current PowerShell environment.
        /// </summary>
        public int EnvPSVersionMajor => EnvPSVersion.Major;

        /// <summary>
        /// Gets the minor version number of the current PowerShell environment.
        /// </summary>
        public int EnvPSVersionMinor => EnvPSVersion.Minor;

        /// <summary>
        /// Gets the build version number of the PowerShell environment, or null if the build version is not available.
        /// </summary>
        public int? EnvPSVersionBuild => EnvPSVersion.Build >= 0 ? EnvPSVersion.Build : null;

        /// <summary>
        /// Gets the revision number component of the current PowerShell version, or null if the revision is not
        /// defined.
        /// </summary>
        public int? EnvPSVersionRevision => EnvPSVersion.Revision >= 0 ? EnvPSVersion.Revision : null;

        /// <summary>
        /// Gets the version of the Common Language Runtime (CLR) that the application is running on.
        /// </summary>
        /// <remarks>This property returns a Version object that represents the CLR version. It can be
        /// used to determine compatibility with specific features or libraries that depend on a particular CLR
        /// version.</remarks>
        public Version? EnvCLRVersion { get; }

        /// <summary>
        /// Gets the major version number of the Common Language Runtime (CLR) environment, or null if the CLR version
        /// is not available.
        /// </summary>
        /// <remarks>This property retrieves the major version from the current CLR version in use. It can
        /// be used to determine compatibility with specific CLR features or behaviors.</remarks>
        public int? EnvCLRVersionMajor => EnvCLRVersion?.Major;

        /// <summary>
        /// Gets the minor version number of the Common Language Runtime (CLR) environment, if available.
        /// </summary>
        /// <remarks>This property returns null if the CLR version is not set or cannot be
        /// determined.</remarks>
        public int? EnvCLRVersionMinor => EnvCLRVersion?.Minor;

        /// <summary>
        /// Gets the build number component of the current CLR version, or null if the build number is not available.
        /// </summary>
        /// <remarks>This property returns the build number from the CLR version if it is non-negative. If
        /// the build number is negative or the CLR version is not set, the property returns null.</remarks>
        public int? EnvCLRVersionBuild => EnvCLRVersion?.Build >= 0 ? EnvCLRVersion.Build : null;

        /// <summary>
        /// Gets the revision number component of the current CLR version, or null if the revision is not available.
        /// </summary>
        /// <remarks>The revision number is obtained from the associated CLR version. If the revision
        /// component is negative or the CLR version is not set, this property returns null to indicate that the
        /// revision is unavailable.</remarks>
        public int? EnvCLRVersionRevision => EnvCLRVersion?.Revision >= 0 ? EnvCLRVersion.Revision : null;

        /// <summary>
        /// Gets the security identifier (SID) of the current process, which represents the security context under which
        /// the process is running.
        /// </summary>
        /// <remarks>This property provides the SID for the current process, which can be used for
        /// security-related operations, such as access control and auditing. The SID is retrieved from the caller's
        /// security context.</remarks>
        public SecurityIdentifier CurrentProcessSID { get; } = AccountUtilities.CallerSid;

        /// <summary>
        /// Gets the NT account representing the user identity under which the current process is running.
        /// </summary>
        /// <remarks>This property can be used for auditing, access control, or logging scenarios where
        /// the process's user context is relevant.</remarks>
        public NTAccount ProcessNTAccount { get; } = AccountUtilities.CallerUsername;

        /// <summary>
        /// Gets a value indicating whether the current user has administrative privileges.
        /// </summary>
        /// <remarks>Use this property to determine if the caller has the necessary permissions to perform
        /// actions that require elevated rights. This is useful for enabling or restricting access to features that are
        /// limited to administrators.</remarks>
        public bool IsAdmin { get; } = AccountUtilities.CallerIsAdmin;

        /// <summary>
        /// Gets a value indicating whether the current account is the local system account.
        /// </summary>
        /// <remarks>This property returns <see langword="true"/> if the application is running under the
        /// local system account; otherwise, it returns <see langword="false"/>. This can be useful for determining the
        /// security context of the application, especially when performing operations that require elevated privileges
        /// or when making security-related decisions.</remarks>
        public bool IsLocalSystemAccount { get; } = AccountUtilities.CallerIsLocalSystem;

        /// <summary>
        /// Gets a value indicating whether the current account is a local service account.
        /// </summary>
        /// <remarks>Use this property to determine if the application is running under the built-in Local
        /// Service account. This can be useful for adjusting behavior or permissions based on the account
        /// context.</remarks>
        public bool IsLocalServiceAccount { get; } = AccountUtilities.CallerIsLocalService;

        /// <summary>
        /// Gets a value indicating whether the current account is the network service account.
        /// </summary>
        /// <remarks>This property is useful for determining the context in which the application is
        /// running, particularly in scenarios where specific permissions or behaviors are associated with the network
        /// service account.</remarks>
        public bool IsNetworkServiceAccount { get; } = AccountUtilities.CallerIsNetworkService;

        /// <summary>
        /// Gets a value indicating whether the current account is a service account.
        /// </summary>
        /// <remarks>A service account typically has different permissions and behaviors compared to
        /// standard user accounts. Use this property to determine if the current context is running under a service
        /// account, which may affect access control and operational logic.</remarks>
        public bool IsServiceAccount { get; } = AccountUtilities.CallerIsServiceAccount;

        /// <summary>
        /// Gets a value indicating whether the current process is running in a user-interactive environment.
        /// </summary>
        /// <remarks>This property returns <see langword="true"/> if the process is running in an
        /// environment that supports user interaction, such as a desktop session, and <see langword="false"/> if it is
        /// running in a non-interactive context, such as a service or background process. This can be useful for
        /// determining whether to display user interface elements or prompt the user for input.</remarks>
        public bool IsProcessUserInteractive { get; } = Environment.UserInteractive;

        /// <summary>
        /// Gets the NT account representation of the local system account.
        /// </summary>
        /// <remarks>This property provides a way to access the well-known SID for the local system
        /// account, which is often used in security contexts. The returned NTAccount can be used for identity-related
        /// operations within the system.</remarks>
        public NTAccount LocalSystemNTAccount { get; } = (NTAccount)AccountUtilities.GetWellKnownSid(WellKnownSidType.LocalSystemSid).Translate(typeof(NTAccount));

        /// <summary>
        /// Gets the NTAccount that represents the built-in local users group on the current machine.
        /// </summary>
        /// <remarks>This property retrieves the well-known security identifier (SID) for the built-in
        /// users group and translates it to an NTAccount. It is useful for managing permissions and access control for
        /// all local users.</remarks>
        public NTAccount LocalUsersGroup { get; } = (NTAccount)AccountUtilities.GetWellKnownSid(WellKnownSidType.BuiltinUsersSid).Translate(typeof(NTAccount));

        /// <summary>
        /// Gets the local administrators group account for the current machine.
        /// </summary>
        /// <remarks>This property retrieves the well-known local administrators group by translating the
        /// built-in administrators security identifier (SID) to an NTAccount. It is useful for managing permissions and
        /// access control related to local administrative tasks.</remarks>
        public NTAccount LocalAdministratorsGroup { get; } = (NTAccount)AccountUtilities.GetWellKnownSid(WellKnownSidType.BuiltinAdministratorsSid).Translate(typeof(NTAccount));

        /// <summary>
        /// Gets a value indicating whether the current account is a session zero account, which includes local system,
        /// local service, network service, or service accounts.
        /// </summary>
        /// <remarks>Session zero is a special session in Windows that is used for services and system
        /// processes. Applications running in this session do not interact with the desktop, enhancing
        /// security.</remarks>
        public bool SessionZero => IsLocalSystemAccount || IsLocalServiceAccount || IsNetworkServiceAccount || IsServiceAccount;

        /// <summary>
        /// Gets a list of sessions for all logged-on users.
        /// </summary>
        /// <remarks>This property retrieves information about all user sessions on the system, which can be useful for
        /// managing user interactions and session-specific operations.</remarks>
        public IReadOnlyList<SessionInfo> LoggedOnUserSessions { get; } = SessionManager.GetSessionInfo();

        /// <summary>
        /// Gets a read-only list of users currently logged on to the system.
        /// </summary>
        /// <remarks>The returned collection is updated dynamically to reflect users as they log on and
        /// off. Each user is represented by an NTAccount object. The list is read-only and cannot be modified
        /// directly.</remarks>
        public IReadOnlyList<NTAccount> UsersLoggedOn { get; } = [];

        /// <summary>
        /// Gets the current session information for the logged-on user.
        /// </summary>
        /// <remarks>This property returns null if no user is currently logged on.</remarks>
        public SessionInfo? CurrentLoggedOnUserSession { get; }

        /// <summary>
        /// Gets information about the user session currently logged into the console, if any.
        /// </summary>
        /// <remarks>If no user is actively logged into the console session, this property returns null.
        /// Use this property to determine the session details of the interactive user on the system.</remarks>
        public SessionInfo? CurrentConsoleUserSession { get; }

        /// <summary>
        /// Gets the current user context under which the application is running, if applicable.
        /// </summary>
        /// <remarks>This property returns a nullable RunAsActiveUser instance, which indicates whether
        /// the application is executing with the privileges of the active user. If the property is null, it means the
        /// application is not running as an active user.</remarks>
        public RunAsActiveUser? RunAsActiveUser { get; }

        /// <summary>
        /// Gets the locale information for the active user, which determines the culture-specific formatting for dates,
        /// numbers, and strings.
        /// </summary>
        /// <remarks>This property returns a CultureInfo object that reflects the current user's locale
        /// settings. It is useful for applications that need to present information in a way that is culturally
        /// appropriate for the user.</remarks>
        public CultureInfo? RunAsActiveUserLocale { get; }

        /// <summary>
        /// Gets the name of the user profile under which the application is running.
        /// </summary>
        /// <remarks>This property can be used to determine the security context or environment associated
        /// with the application's execution. Ensure that any operations performed under this user profile have the
        /// necessary permissions.</remarks>
        public DirectoryInfo? RunAsUserProfile { get; }

        /// <summary>
        /// Gets the name of the user profile associated with the current context.
        /// </summary>
        /// <remarks>This property returns the user profile name, which may be null if no profile is set.
        /// It is typically used to identify the user in applications that require user-specific settings or
        /// data.</remarks>
        public string? UserProfileName { get; }

        /// <summary>
        /// Gets the path to the directory where user profiles are stored on the system.
        /// </summary>
        /// <remarks>This property retrieves the location typically used for storing user-specific data
        /// and settings. The returned value may be null if the user profiles directory cannot be determined on the
        /// current system.</remarks>
        public DirectoryInfo DirUserProfile { get; } = ShellUtilities.GetUserProfilesDirectory();

        /// <summary>
        /// Gets the default user profile path configured on the system.
        /// </summary>
        /// <remarks>This property retrieves the default user profile path from the Windows registry at
        /// HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList. If the registry value is not
        /// found, the property returns null.</remarks>
        public DirectoryInfo? DefaultUserProfile { get; } = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList", "Default", null) is string defaultProfilePath && !string.IsNullOrWhiteSpace(defaultProfilePath) ? new(defaultProfilePath) : null;

        /// <summary>
        /// Gets a read-only collection of characters that are invalid in file names.
        /// </summary>
        /// <remarks>This collection is derived from the system-defined invalid file name characters,
        /// which may vary by operating system. It is useful for validating file names before attempting to create or
        /// manipulate files.</remarks>
        public IReadOnlyList<char> InvalidFileNameChars { get; } = new ReadOnlyCollection<char>(Path.GetInvalidFileNameChars());

        /// <summary>
        /// Gets the regular expression pattern used to identify invalid characters in file names.
        /// </summary>
        /// <remarks>This property provides a compiled regular expression that can be used to validate
        /// file names against a set of invalid characters. It is useful for ensuring that file names conform to system
        /// requirements and do not contain characters that are not allowed.</remarks>
        public Regex InvalidFileNameCharsRegexPattern { get; }

        /// <summary>
        /// Gets the regular expression used to validate Windows Installer product codes in GUID format.
        /// </summary>
        /// <remarks>The regular expression matches a GUID enclosed in curly braces, ensuring that the
        /// product code conforms to the standard format required by Windows Installer. This property can be used to
        /// verify whether a given string is a valid product code before performing installation-related
        /// operations.</remarks>
        public Regex MsiProductCodeRegexPattern { get; } = new(@"^\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}$", RegexOptions.Compiled);

        /// <summary>
        /// Gets a regular expression that matches characters not allowed in scheduled task names.
        /// </summary>
        /// <remarks>Use this pattern to validate or sanitize scheduled task names by identifying invalid
        /// characters, such as backslashes, slashes, colons, asterisks, question marks, double quotes, angle brackets,
        /// and vertical bars. This regular expression is compiled for performance.</remarks>
        public Regex InvalidScheduledTaskNameCharsRegexPattern { get; } = new(@"[\\\/\:\*\?\""\<\>\|]", RegexOptions.Compiled);

        /// <summary>
        /// Retrieves the directory information for the specified special folder in the current environment.
        /// </summary>
        /// <param name="folder">A value that identifies the special folder whose path is to be retrieved.</param>
        /// <returns>A DirectoryInfo object representing the specified special folder if the path exists and is not empty;
        /// otherwise, null.</returns>
        private static DirectoryInfo? GetEnvironmentFolderPath(Environment.SpecialFolder folder)
        {
            string folderPath = Environment.GetFolderPath(folder);
            return !string.IsNullOrWhiteSpace(folderPath)
                ? new(folderPath)
                : null;
        }

        /// <summary>
        /// Retrieves a directory specified by the value of an environment variable.
        /// </summary>
        /// <remarks>This method does not validate whether the directory exists. The environment variable
        /// value is used as-is to construct the DirectoryInfo object.</remarks>
        /// <param name="variableName">The name of the environment variable whose value is expected to be a directory path.</param>
        /// <returns>A DirectoryInfo representing the directory if the environment variable is set and not empty; otherwise,
        /// null.</returns>
        private static DirectoryInfo? GetEnvironmentVariableDirectory(string variableName)
        {
            string? variableValue = EnvironmentUtilities.GetEnvironmentVariable(variableName);
            return !string.IsNullOrWhiteSpace(variableValue)
                ? new(variableValue)
                : null;
        }
    }
}

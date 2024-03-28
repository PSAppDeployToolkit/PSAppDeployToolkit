The toolkit has a number of internal variables which can be used in your script. Outlined below are each of them:

| Variable                                 | Description                                                                                   |
| ---------------------------------------- | --------------------------------------------------------------------------------------------- |
| **Toolkit Name**                         |
| $appDeployToolkitName                    | Short-name of toolkit without spaces                                                          |
| $appDeployMainScriptFriendlyName         | Full name of toolkit including spaces                                                         |
|                                          |
| **Script Info**                          |
| $appDeployMainScriptVersion              | Version number of the toolkit                                                                 |
| $appDeployMainScriptMinimumConfigVersion | Minimum version of the config XML file required by the toolkit                                |
| $appDeployMainScriptDate                 | Date toolkit was last modified                                                                |
| $appDeployMainScriptParameters           | Contains all parameters and values specified when toolkit was launched                        |
|                                          |
| **Datetime and Culture**                 |
| $currentDateTime                         | Current date &amp; time when the toolkit was launched                                         |
| $currentTime                             | Current time when toolkit was launched                                                        |
| $currentDate                             | Current date when toolkit was launched                                                        |
| $currentTimeZoneBias                     | TimeZone bias based on the current date/time                                                  |
| $culture                                 | Object which contains all of the current Windows culture settings                             |
| $currentLanguage                         | Current Windows two letter ISO language name (e.g. EN, FR, DE, JA etc)                        |
| $currentUILanguage                       | Current Windows two letter UI ISO language name (e.g. EN, FR, DE, JA etc)                     |
|                                          |
| **Environment Variables**                | *(path examples are for Windows 7 and higher)*                                                |
| $envHost                                 | Object that contains details about the current PowerShell console                             |
| $envShellFolders                         | Object that contains properties from registry path:                                           |
|                                          | `HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders`                  |
| $envAllUsersProfile                      | %ALLUSERSPROFILE% (e.g. C:\ProgramData)                                                       |
| $envAppData                              | %APPDATA% (e.g. C:\Users\{username}\AppData\Roaming)                                          |
| $envArchitecture                         | %PROCESSOR_ARCHITECTURE% (e.g. AMD64/IA64/x86)                                                |
|                                          | (This doesn't tell you the architecture of the processor but only of the current              |
|                                          | process, so it returns "x86" for a 32-bit WOW process running on 64-bit Windows.)             |
| $envCommonProgramFiles                   | %COMMONPROGRAMFILES% (e.g. C:\Program Files\Common Files)                                     |
| $envCommonProgramFilesX86                | %COMMONPROGRAMFILES(x86)% (e.g. C:\Program Files (x86)\Common Files)                          |
| $envCommonDesktop                        | e.g. C:\Users\Public\Desktop                                                                  |
| $envCommonDocuments                      | e.g. C:\Users\Public\Documents                                                                |
| $envCommonStartMenuPrograms              | e.g. C:\ProgramData\Microsoft\Windows\Start Menu\Programs                                     |
| $envCommonStartMenu                      | e.g. C:\ProgramData\Microsoft\Windows\Start Menu                                              |
| $envCommonStartUp                        | e.g. C:\ProgramData\Microsoft\Windows\Start Menu                                              |
| $envCommonTemplates                      | e.g. C:\ProgramData\Microsoft\Windows\Templates                                               |
| $envComputerName                         | $COMPUTERNAME% (e.g. computer1)                                                               |
| $envComputerNameFQDN                     | Fully qualified computer name (e.g. computer1.conto.contoso.com)                              |
| $envHomeDrive                            | %HOMEDRIVE% (e.g. C:)                                                                         |
| $envHomePath                             | %HOMEPATH% (e.g. \Users\{username})                                                           |
| $envHomeShare                            | %HOMESHARE% (Used instead of HOMEDRIVE if the home directory uses UNC paths.)                 |
| $envLocalAppData                         | %LOCALAPPDATA% (e.g. C:\Users\{username}\AppData\Local)                                       |
| $envLogicalDrives                        | An array containing all of the logical drives on the system.                                  |
| $envProgramFiles                         | %PROGRAMFILES% (e.g. C:\Program Files)                                                        |
| $envProgramFilesX86                      | %ProgramFiles(x86)% (e.g. C:\Program Files (x86)                                              |
|                                          | (Only on 64 bit systems, is used to store 32 bit programs.)                                   |
| $envProgramData                          | %PROGRAMDATA% (e.g. C:\ProgramData)                                                           |
| $envPublic                               | %PUBLIC% (e.g. C:\Users\Public)                                                               |
| $envSystemDrive                          | %SYSTEMDRIVE% (e.g. C:)                                                                       |
| $envSystemRAM                            | System RAM as an integer                                                                      |
| $envSystemRoot                           | %SYSTEMROOT% (e.g. C:\Windows)                                                                |
| $envTemp                                 | Checks for the existence of environment variables in the following order                      |
|                                          | and uses the first path found:                                                                |
|                                          | - The path specified by the TMP environment variable.                                         |
|                                          | - The path specified by the TEMP environment variable.                                        |
|                                          | (e.g. C:\Users\{Username}\AppData\Local\Temp)                                                 |
|                                          | - The path specified by the USERPROFILE environment variable.                                 |
|                                          | - The Windows root (C:\Windows) directory.                                                    |
| $envUserCookies                          | C:\Users\{username}\AppData\Local\Microsoft\Windows\INetCookies                               |
| $envUserDesktop                          | C:\Users\{username}\Desktop                                                                   |
| $envUserFavorites                        | C:\Users\{username}\Favorites                                                                 |
| $envUserInternetCache                    | C:\Users\{username}\AppData\Local\Microsoft\Windows\INetCache                                 |
| $envUserInternetHistory                  | C:\Users\{username}\AppData\Local\Microsoft\Windows\History                                   |
| $envUserMyDocuments                      | C:\Users\{username}\Documents                                                                 |
| $envUserName                             | %USERNAME% (e.g. {username})                                                                  |
| $envUserProfile                          | %USERPROFILE% (e.g. %SystemDrive%\Users\{username})                                           |
| $envUserSendTo                           | C:\Users\{username}\AppData\Roaming\Microsoft\Windows\SendTo                                  |
| $envUserStartMenu                        | C:\Users\{username}\AppData\Roaming\Microsoft\Windows\Start Menu                              |
| $envUserStartMenuPrograms                | C:\Users\{username}\AppData\Roaming\Microsoft\Windows\Start Menu\Programs                     |
| $envUserStartUp                          | C:\Users\{username}\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup             |
| $envSystem32Directory                    | C:\WINDOWS\system32                                                                           |
| $envWinDir                               | %WINDIR% (e.g. C:\Windows)                                                                    |
|                                          |
| **Domain Membership**                    |
| $IsMachinePartOfDomain                   | Is machine joined to a domain (e.g. $true/$false)                                             |
| $envMachineWorkgroup                     | If machine not joined to domain, what is the WORKGROUP it belongs to?                         |
| $envMachineADDomain                      | Root AD domain name for machine (e.g. &lt;name&gt;.&lt;suffix&gt;.contoso.com)                |
| $envLogonServer                          | FQDN of %LOGONSERVER% used for authenticating logged in user                                  |
| $MachineDomainController                 | FQDN of an AD domain controller used for authentication                                       |
| $envMachineDNSDomain                     | Full Domain name for machine (e.g. &lt;name&gt;.conto.contoso.com)                            |
| $envUserDNSDomain                        | %USERDNSDOMAIN%. Root AD domain name for user                                                 |
|                                          | (e.g. &lt;name&gt;.&lt;suffix&gt;.contoso.com)                                                |
| $envUserDomain                           | %USERDOMAIN% (e.g. &lt;name&gt;.&lt;suffix&gt;.CONTOSO.&lt;tld&gt;)                           |
|                                          |
| **Operating System**                     |
| $envOS                                   | Object that contains details about the operating system                                       |
| $envOSName                               | Name of the operating system (e.g. Microsoft Windows 8.1 Pro)                                 |
| $envOSServicePack                        | Latest service pack installed on the system (e.g. Service Pack 3)                             |
| $envOSVersion                            | Full version number of the OS (e.g. {major}.{minor}.{build}.{revision})                       |
| $envOSVersionMajor                       | Major portion of the OS version number (e.g. {major}.{minor}.{build}.{revision})              |
| $envOSVersionMinor                       | Minor portion of the OS version number (e.g. {major}.{minor}.{build}.{revision})              |
| $envOSVersionBuild                       | Build portion of the OS version number (e.g. {major}.{minor}.{build}.{revision})              |
| $envOSVersionRevision                    | Revision portion of the OS version number (e.g. {major}.{minor}.{build}.{revision})           |
| $envOSProductType                        | OS product type represented as an integer (e.g. 1/2/3)                                        |
| $IsServerOS                              | Is server OS? (e.g. $true/$false)                                                             |
| $IsDomainControllerOS                    | Is domain controller OS? (e.g. $true/$false)                                                  |
| $IsWorkStationOS                         | Is workstation OS? (e.g. $true/$false)                                                        |
| $IsMultiSessionOS                         | Is Multi-Session OS? (e.g. $true/$false)                                                    |
| $envOSProductTypeName                    | OS product type name (e.g. Server/Domain Controller/Workstation/Unknown)                      |
| $Is64Bit                                 | Is this a 64-bit OS? (e.g. $true/$false)                                                      |
| $envOSArchitecture                       | Represents the OS architecture (e.g. 32-Bit/64-Bit)                                           |
|                                          |
| **Current Process Architecture**         |
| $Is64BitProcess                          | Is the current process 64-bits? (e.g. $true/$false)                                           |
| $psArchitecture                          | Represents the current process architecture (e.g. x86/x64)                                    |
|                                          |
| **PowerShell And CLR (.NET) Versions**   |
| $envPSVersionTable                       | Object containing PowerShell version details from PS variable $PSVersionTable                 |
| $envPSVersion                            | Full version number of PS (e.g. {major}.{minor}.{build}.{revision})                           |
| $envPSVersionMajor                       | Major portion of PS version number (e.g. {major}.{minor}.{build}.{revision})                  |
| $envPSVersionMinor                       | Minor portion of PS version number (e.g. {major}.{minor}.{build}.{revision})                  |
| $envPSVersionBuild                       | Build portion of PS version number (e.g. {major}.{minor}.{build}.{revision})                  |
| $envPSVersionRevision                    | Revision portion of PS version number (e.g. {major}.{minor}.{build}.{revision})               |
| $envCLRVersion                           | Full version number of .NET used by PS (e.g. {major}.{minor}.{build}.{revision})              |
| $envCLRVersionMajor                      | Major portion of PS .NET version number (e.g. {major}.{minor}.{build}.{revision})             |
| $envCLRVersionMinor                      | Minor portion of PS .NET version number (e.g. {major}.{minor}.{build}.{revision})             |
| $envCLRVersionBuild                      | Build portion of PS .NET version number (e.g. {major}.{minor}.{build}.{revision})             |
| $envCLRVersionRevision                   | Revision portion of PS .NET version number (e.g. {major}.{minor}.{build}.{revision})          |
|                                          |
| **Permissions/Accounts**                 |
| $CurrentProcessToken                     | Object that represents the current processes Windows Identity user token.                     |
|                                          | Contains all details regarding user permissions.                                              |
| $CurrentProcessSID                       | Object that represents the current process account SID (e.g. S-1-5-32-544)                    |
| $ProcessNTAccount                        | Current process NT Account (e.g. NT AUTHORITY\SYSTEM)                                         |
| $ProcessNTAccountSID                     | Current process account SID (e.g. S-1-5-32-544)                                               |
| $IsAdmin                                 | Is the current process running with elevated admin privileges? (e.g. $true/$false)            |
| $IsLocalSystemAccount                    | Is the current process running under the SYSTEM account? (e.g. $true/$false)                  |
| $IsLocalServiceAccount                   | Is the current process running under LOCAL SERVICE account? (e.g. $true/$false)               |
| $IsNetworkServiceAccount                 | Is the current process running under the NETWORK SERVICE account? (e.g. $true/$false)         |
| $IsServiceAccount                        | Is the current process running as a service? (e.g. $true/$false)                              |
| $IsProcessUserInteractive                | Is the current process able to display a user interface?                                      |
| $LocalSystemNTAccount                    | Localized NT account name of the SYSTEM account (e.g. NT AUTHORITY\SYSTEM)                    |
| $SessionZero                             | Is the current process currently in session zero?                                             |
|                                          | In session zero isolation, process is not able to display a user interface.                   |
|                                          | (e.g. $true/$false)                                                                           |
|                                          |
| **Script Name and Script Paths**         |
| $scriptPath                              | Fully qualified path of the toolkit                                                           |
|                                          | (e.g. C:\Testing\AppDeployToolkit\AppDeployToolkitMain.ps1)                                   |
| $scriptName                              | Name of toolkit without file extension (e.g. AppDeployToolkitMain)                            |
| $scriptFileName                          | Name of toolkit file (e.g. AppDeployToolkitMain.ps1)                                          |
| $scriptRoot                              | Folder that the toolkit is located in. (e.g. C:\Testing\AppDeployToolkit)                     |
| $invokingScript                          | Fully qualified path of the script that invoked the toolkit                                   |
|                                          | (e.g. C:\Testing\Deploy-Application.ps1)                                                      |
| $scriptParentPath                        | If toolkit was invoked by another script: contains folder that the invoking script is located |
|                                          | in. If toolkit was not invoked by another script: contains parent folder of the toolkit.      |
|                                          |
| **App Deploy Script Dependency Files**   |
| $appDeployLogoIcon                       | Path to the logo icon file for the toolkit (e.g. $scriptRoot\AppDeployToolkitLogo.ico)        |
| $appDeployLogoBanner                     | Path to the logo banner file for the toolkit (e.g. $scriptRoot\AppDeployToolkitBanner.png)    |
| $appDeployConfigFile                     | Path to the config XML file for the toolkit (e.g. $scriptRoot\AppDeployToolkitConfig.xml)     |
| $appDeployToolkitDotSourceExtensions     | Name of the optional extensions file for the toolkit (e.g. AppDeployToolkitExtensions.ps1)    |
| $xmlConfigFile                           | Contains the entire contents of the XML config file                                           |
| $configConfigVersion                     | Version number of the config XML file                                                         |
| $configConfigDate                        | Last modified date of the config XML file                                                     |
|                                          |
| **Script Directories**                   |
| $dirFiles                                | "Files" sub-directory of the toolkit                                                          |
| $dirSupportFiles                         | "SupportFiles" sub-directory of the toolkit                                                   |
| $dirAppDeployTemp                        | Toolkit temp directory. Configured in XML Config file option "Toolkit_TempPath".              |
|                                          | (e.g. Toolkit_TempPath\$appDeployToolkitName)                                                 |
|                                          |
| **Script Naming Convention**             |
| $appVendor                               | Name of the manufacturer that created the package being deployed (e.g. Microsoft)             |
| $appName                                 | Name of the application being packaged (e.g. Office 2010)                                     |
| $appVersion                              | Version number of the application being packaged (e.g. 14.0)                                  |
| $appLang                                 | UI language of the application being packaged (e.g. EN)                                       |
| $appRevision                             | Revision number of the package (e.g. 01)                                                      |
| $appArch                                 | Architecture of the application being packaged (e.g. x86/x64)                                 |
| $installTitle                            | Combination of the most important details about the application being                         |
|                                          | packaged (e.g. "$appVendor $appName $appVersion")                                             |
| $installName                             | Combination of any of the following details which were provided:                              |
|                                          | $appVendor_$appName_$appVersion_$appArch_$appLang_$appRevision                                |
|                                          |
| **Executables**                          |
| $exeWusa                                 | Name of system utility that installs Standalone Windows Updates (e.g. wusa.exe)               |
| $exeMsiexec                              | Name of system utility that install Windows Installer files (e.g. msiexec.exe)                |
| $exeSchTasks                             | Path of system utility that allows management of scheduled tasks                              |
|                                          | (e.g. $envWinDir\System32\schtasks.exe)                                                       |
| **RegEx Patterns**                       |
| $MSIProductCodeRegExPattern              | Contains the regex pattern used to detect a MSI product code.                                 |
|                                          |
| **Registry Keys**                        |
| $regKeyApplications                      | Array containing the path to the 32-bit and 64-bit portions of the registry                   |
|                                          | that contain information about programs installed on the system.                              |
|                                          | `HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`                                    |
|                                          | `HKLM:SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall`                        |
| $regKeyLotusNotes                        | Contains the registry path that stores information about a Lotus Notes installation.          |
|                                          | `HKLM:SOFTWARE\Lotus\Notes','HKLM:SOFTWARE\Wow6432Node\Lotus\Notes`                           |
| $regKeyAppExecution                      | Contains the registry path where application execution can be blocked by                      |
|                                          | configuring the ‘Debugger’ value.                                                             |
|                                          | `HKLM:SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options`              |
| $regKeyDeferHistory                      | The path in the registry where the defer history for the package being installed is stored.   |
|                                          | "$configToolkitRegPath\$appDeployToolkitName\DeferHistory\$installName"                       |
|                                          |
| **COM Objects**                          |
| $Shell                                   | Represents and allows use of the WScript.Shell COM object                                     |
| $ShellApp                                | Represents and allows use of the Shell.Application COM object                                 |
| **Log File**                             |
| $logName                                 | Name of the script log file:                                                                  |
|                                          | $installName + '_' + $appDeployToolkitName + '_' + $deploymentType + '.log'                   |
| $logTempFolder                           | Temporary log file directory used if the option to compress log files was                     |
|                                          | selected in the config XML file: $envTemp\$installName                                        |
| $configToolkitLogDir                     | Path to log directory defined in XML config file                                              |
| $DisableScriptLogging                    | Dot source this ScriptBlock to disable logging messages to the log file.                      |
| $RevertScriptLogging                     | Dot source this ScriptBlock to revert script logging back to its original setting.            |
| **Script Parameters**                    |
| $deployAppScriptParameters               | Non-default parameters that Deploy-Application.ps1 was launched with                          |
| $appDeployMainScriptParameters           | Non-default parameters that AppDeployToolkitMain.ps1 was launched with                        |
| $appDeployExtScriptParameters            | Non-default parameters that AppDeployToolkitExtensions.ps1 was launched with                  |
|                                          |
| **Logged On Users**                      |
| $LoggedOnUserSessions                    | Object that contains account and session details for all users                                |
| $usersLoggedOn                           | Array that contains all of the NTAccount names of logged in users                             |
| $CurrentLoggedOnUserSession              | Object that contains account and session details for the current process if                   |
|                                          | it is running as a logged in user. This is the object from                                    |
|                                          | $LoggedOnUserSessions where the IsCurrentSession property is $true.                           |
| $CurrentConsoleUserSession               | Objects that contains the account and session details of the console user                     |
|                                          | (user with control of the physical monitor, keyboard, and mouse). This is the                 |
|                                          | object from $LoggedOnUserSessions where the IsConsoleSession property is $true.               |
| $RunAsActiveUser                         | The active console user. If no console user exists but users are logged in,                   |
|                                          | such as on terminal servers, then the first logged-in non-console user.                       |
|                                          |
| **Miscellaneous**                        |
| $dpiPixels                               | DPI Scale (property only exists if DPI scaling has been changed on the                        |
|                                          | system at least once)                                                                         |
| $runningTaskSequence                     | Is the current process running in a SCCM task sequence? (e.g. $true/$false)                   |
| $IsTaskSchedulerHealthy                  | Are the task scheduler services in a healthy state? (e.g. $true/$false)                       |
| $invalidFileNameChars                    | Array of all invalid file name characters used to sanitize variables which                    |
|                                          | may be used to create file names.                                                             |
| $useDefaultMsi                           | A Zero-Config MSI installation was detected.                                                  |
| $IsConfigMgr                             | Is the toolkit deployed from ConfigMgr? (e.g. $true/$false)                                   |
| $IsIntune                                | Is the toolkit deployed from Intune? (e.g. $true/$false)                                      |

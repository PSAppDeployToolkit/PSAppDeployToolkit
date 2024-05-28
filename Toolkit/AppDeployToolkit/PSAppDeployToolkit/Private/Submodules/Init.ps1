#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Initialize-ADTVariableDatabase
{
    ## Open new dictionary for storage.
    $variables = [ordered]@{}

    ## Variables: Toolkit Name
    $variables.Add('appDeployToolkitName', $Script:MyInvocation.MyCommand.ScriptBlock.Module.Name)

    ## Variables: Script Info
    $variables.Add('appDeployMainScriptVersion', $Script:MyInvocation.MyCommand.ScriptBlock.Module.Version)
    $variables.Add('appDeployMainScriptMinimumConfigVersion', $Script:MyInvocation.MyCommand.ScriptBlock.Module.Version)

    ## Variables: Culture
    $variables.Add('culture', $Host.CurrentCulture)
    $variables.Add('uiculture', $Host.CurrentUICulture)
    $variables.Add('currentLanguage', $variables.culture.TwoLetterISOLanguageName.ToUpper())
    $variables.Add('currentUILanguage', $variables.uiculture.TwoLetterISOLanguageName.ToUpper())

    ## Variables: Environment Variables
    $variables.Add('envShellFolders', (Get-ItemProperty -Path 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -ErrorAction Ignore))
    $variables.envShellFolders.PSObject.Properties.Remove('PSProvider')
    $variables.Add('envAllUsersProfile', $env:ALLUSERSPROFILE)
    $variables.Add('envAppData', [System.Environment]::GetFolderPath('ApplicationData'))
    $variables.Add('envArchitecture', $env:PROCESSOR_ARCHITECTURE)
    $variables.Add('envCommonDesktop', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Desktop' -ErrorAction Ignore))
    $variables.Add('envCommonDocuments', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Documents' -ErrorAction Ignore))
    $variables.Add('envCommonStartMenuPrograms', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Programs' -ErrorAction Ignore))
    $variables.Add('envCommonStartMenu', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Start Menu' -ErrorAction Ignore))
    $variables.Add('envCommonStartUp', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Startup' -ErrorAction Ignore))
    $variables.Add('envCommonTemplates', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Templates' -ErrorAction Ignore))
    $variables.Add('envComputerName', [System.Environment]::MachineName.ToUpper())
    $variables.Add('envHomeDrive', $env:HOMEDRIVE)
    $variables.Add('envHomePath', $env:HOMEPATH)
    $variables.Add('envHomeShare', $env:HOMESHARE)
    $variables.Add('envLocalAppData', [System.Environment]::GetFolderPath('LocalApplicationData'))
    $variables.Add('envLogicalDrives', [System.Environment]::GetLogicalDrives())
    $variables.Add('envProgramData', [System.Environment]::GetFolderPath('CommonApplicationData'))
    $variables.Add('envPublic', $env:PUBLIC)
    $variables.Add('envSystemDrive', $env:SYSTEMDRIVE)
    $variables.Add('envSystemRoot', $env:SYSTEMROOT)
    $variables.Add('envTemp', [System.IO.Path]::GetTempPath())
    $variables.Add('envUserCookies', [System.Environment]::GetFolderPath('Cookies'))
    $variables.Add('envUserDesktop', [System.Environment]::GetFolderPath('DesktopDirectory'))
    $variables.Add('envUserFavorites', [System.Environment]::GetFolderPath('Favorites'))
    $variables.Add('envUserInternetCache', [System.Environment]::GetFolderPath('InternetCache'))
    $variables.Add('envUserInternetHistory', [System.Environment]::GetFolderPath('History'))
    $variables.Add('envUserMyDocuments', [System.Environment]::GetFolderPath('MyDocuments'))
    $variables.Add('envUserName', [System.Environment]::UserName)
    $variables.Add('envUserPictures', [System.Environment]::GetFolderPath('MyPictures'))
    $variables.Add('envUserProfile', $env:USERPROFILE)
    $variables.Add('envUserSendTo', [System.Environment]::GetFolderPath('SendTo'))
    $variables.Add('envUserStartMenu', [System.Environment]::GetFolderPath('StartMenu'))
    $variables.Add('envUserStartMenuPrograms', [System.Environment]::GetFolderPath('Programs'))
    $variables.Add('envUserStartUp', [System.Environment]::GetFolderPath('StartUp'))
    $variables.Add('envUserTemplates', [System.Environment]::GetFolderPath('Templates'))
    $variables.Add('envSystem32Directory', [System.Environment]::SystemDirectory)
    $variables.Add('envWinDir', $env:WINDIR)

    ## Variables: Running in SCCM Task Sequence.
    $variables.Add('RunningTaskSequence', !![System.Type]::GetTypeFromProgID('Microsoft.SMS.TSEnvironment'))

    ## Variables: Domain Membership
    $w32cs = Get-CimInstance -ClassName Win32_ComputerSystem
    $w32csd = $w32cs.Domain | Where-Object {$_}
    $variables.Add('IsMachinePartOfDomain', $w32cs.PartOfDomain)
    $variables.Add('envMachineWorkgroup', [System.String]::Empty)
    $variables.Add('envMachineADDomain', [System.String]::Empty)
    $variables.Add('envLogonServer', [System.String]::Empty)
    $variables.Add('MachineDomainController', [System.String]::Empty)
    $variables.Add('envMachineDNSDomain', [string]([System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().DomainName | Where-Object {$_} | ForEach-Object {$_.ToLower()}))
    $variables.Add('envUserDNSDomain', [string]($env:USERDNSDOMAIN | Where-Object {$_} | ForEach-Object {$_.ToLower()}))
    $variables.Add('envUserDomain', [string]$(try {[System.Environment]::UserDomainName.ToUpper()} catch {[System.Void]$null}))
    $variables.Add('envComputerNameFQDN', $variables.envComputerName)
    if ($variables.IsMachinePartOfDomain.Equals($true))
    {
        $variables.envMachineADDomain = $w32csd.ToLower()
        $variables.envComputerNameFQDN = try
        {
            [System.Net.Dns]::GetHostEntry('localhost').HostName
        }
        catch
        {
            # Function GetHostEntry failed, but we can construct the FQDN in another way
            $variables.envComputerNameFQDN + '.' + $variables.envMachineADDomain
        }

        # Set the logon server and remove backslashes at the beginning.
        $variables.envLogonServer = [string]$(try
        {
            $env:LOGONSERVER | Where-Object {$_ -and !$_.Contains('\\MicrosoftAccount')} | ForEach-Object {[System.Net.Dns]::GetHostEntry($_.TrimStart('\')).HostName}
        }
        catch
        {
            # If running in system context or if GetHostEntry fails, fall back on the logonserver value stored in the registry
            Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy\History' -ErrorAction Ignore | Select-Object -ExpandProperty DCName -ErrorAction Ignore
        })
        while ($variables.envLogonServer.StartsWith('\'))
        {
            $variables.envLogonServer = $variables.envLogonServer.Substring(1)
        }

        try
        {
            $variables.MachineDomainController = [System.DirectoryServices.ActiveDirectory.Domain]::GetCurrentDomain().FindDomainController().Name
        }
        catch
        {
            [System.Void]$null
        }
    }
    else
    {
        $variables.envMachineWorkgroup = $w32csd.ToUpper()
    }

    # Get the OS Architecture.
    $archArr = @('x86', 'x64')
    $variables.Add('Is64Bit', [System.Environment]::Is64BitOperatingSystem)
    $variables.Add('envOSArchitecture', $archArr[$variables.Is64Bit])

    ## Variables: Current Process Architecture
    $variables.Add('Is64BitProcess', [System.Environment]::Is64BitProcess)
    $variables.Add('psArchitecture', $archArr[$variables.Is64BitProcess])

    ## Variables: Get normalised paths that vary depending on process bitness.
    if ($variables.Is64Bit)
    {
        if ($variables.Is64BitProcess)
        {
            $variables.Add('envProgramFiles', [System.Environment]::GetFolderPath('ProgramFiles'))
            $variables.Add('envCommonProgramFiles', [System.Environment]::GetFolderPath('CommonProgramFiles'))
            $variables.Add('envSysNativeDirectory', [System.Environment]::SystemDirectory)
            $variables.Add('envSYSWOW64Directory', [System.IO.Path]::Combine($Env:windir, 'SysWOW64'))
        }
        else
        {
            $variables.Add('envProgramFiles', [System.Environment]::GetEnvironmentVariable('ProgramW6432'))
            $variables.Add('envCommonProgramFiles', [System.Environment]::GetEnvironmentVariable('CommonProgramW6432'))
            $variables.Add('envSysNativeDirectory', [System.IO.Path]::Combine($Env:windir, 'sysnative'))
            $variables.Add('envSYSWOW64Directory', [System.Environment]::SystemDirectory)
        }
        $variables.Add('envProgramFilesX86', [System.Environment]::GetFolderPath('ProgramFilesX86'))
        $variables.Add('envCommonProgramFilesX86', [System.Environment]::GetFolderPath('CommonProgramFilesX86'))
    }
    else
    {
        $variables.Add('envProgramFiles', [Environment]::GetFolderPath('ProgramFiles'))
        $variables.Add('envProgramFilesX86', [System.String]::Empty)
        $variables.Add('envCommonProgramFiles', [Environment]::GetFolderPath('CommonProgramFiles'))
        $variables.Add('envCommonProgramFilesX86', [System.String]::Empty)
        $variables.Add('envSysNativeDirectory', [System.Environment]::SystemDirectory)
        $variables.Add('envSYSWOW64Directory', [System.String]::Empty)
    }

    ## Variables: Operating System
    $variables.Add('envOS', (Get-CimInstance -ClassName Win32_OperatingSystem))
    $variables.Add('envOSName', $variables.envOS.Caption.Trim())
    $variables.Add('envOSServicePack', $variables.envOS.CSDVersion)
    $variables.Add('envOSVersion', [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo([System.IO.Path]::Combine($variables.envSysNativeDirectory, 'ntoskrnl.exe')).ProductVersion)
    $variables.Add('envOSVersionMajor', $variables.envOSVersion.Major)
    $variables.Add('envOSVersionMinor', $variables.envOSVersion.Minor)
    $variables.Add('envOSVersionBuild', $variables.envOSVersion.Build)
    $variables.Add('envOSVersionRevision', $variables.envOSVersion.Revision)

    # Get the operating system type.
    $variables.Add('envOSProductType', $variables.envOS.ProductType)
    $variables.Add('IsServerOS', $variables.envOSProductType -eq 3)
    $variables.Add('IsDomainControllerOS', $variables.envOSProductType -eq 2)
    $variables.Add('IsWorkstationOS', $variables.envOSProductType -eq 1)
    $variables.Add('IsMultiSessionOS', $variables.envOSName -match '^Microsoft Windows \d+ Enterprise (for Virtual Desktops|Enterprise Multi-Session)$')
    $variables.Add('envOSProductTypeName', $(switch ($variables.envOSProductType) {
        3 { 'Server' }
        2 { 'Domain Controller' }
        1 { 'Workstation' }
        default { 'Unknown' }
    }))

    ## Variables: Office C2R version, bitness and channel
    $variables.Add('envOfficeVars', (Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Office\ClickToRun\Configuration' -ErrorAction Ignore))
    if ($variables.envOfficeVars) {$variables.envOfficeVars.PSObject.Properties.Remove('PSProvider')}
    $variables.Add('envOfficeVersion', [string]($variables.envOfficeVars | Select-Object -ExpandProperty VersionToReport -ErrorAction Ignore))
    $variables.Add('envOfficeBitness', [string]($variables.envOfficeVars | Select-Object -ExpandProperty Platform -ErrorAction Ignore))

    # Channel needs special handling for group policy values.
    $officeChannelProperty = if ($variables.envOfficeVars | Select-Object -ExpandProperty UpdateChannel -ErrorAction Ignore)
    {
        $variables.envOfficeVars.UpdateChannel
    }
    elseif ($variables.envOfficeVars | Select-Object -ExpandProperty CDNBaseURL -ErrorAction Ignore)
    {
        $variables.envOfficeVars.CDNBaseURL
    }
    $variables.Add('envOfficeChannel', [string]$(switch -regex ($officeChannelProperty)
    {
        "492350f6-3a01-4f97-b9c0-c7c6ddf67d60" {"monthly"}
        "7ffbc6bf-bc32-4f92-8982-f9dd17fd3114" {"semi-annual"}
        "64256afe-f5d9-4f86-8936-8840a6a4f5be" {"monthly targeted"}
        "b8f9b850-328d-4355-9145-c59439a0c4cf" {"semi-annual targeted"}
        "55336b82-a18d-4dd6-b5f6-9e5095c314a6" {"monthly enterprise"}
    }))

    ## Variables: Hardware
    $w32b = Get-CimInstance -ClassName Win32_BIOS
    $variables.Add('envSystemRAM', [System.Math]::Round($w32cs.TotalPhysicalMemory / 1GB))
    $variables.Add('envHardwareType', $(if ($w32b.Version -match 'VRTUAL')
    {
        'Virtual:Hyper-V'
    }
    elseif ($w32b.Version -match 'A M I')
    {
        'Virtual:Virtual PC'
    }
    elseif ($w32b.Version -like '*Xen*')
    {
        'Virtual:Xen'
    }
    elseif ($w32b.SerialNumber -like '*VMware*')
    {
        'Virtual:VMware'
    }
    elseif ($w32b.SerialNumber -like '*Parallels*')
    {
        'Virtual:Parallels'
    }
    elseif (($w32cs.Manufacturer -like '*Microsoft*') -and ($w32cs.Model -notlike '*Surface*'))
    {
        'Virtual:Hyper-V'
    }
    elseif ($w32cs.Manufacturer -like '*VMWare*')
    {
        'Virtual:VMware'
    }
    elseif ($w32cs.Manufacturer -like '*Parallels*')
    {
        'Virtual:Parallels'
    }
    elseif ($w32cs.Model -like '*Virtual*')
    {
        'Virtual'
    }
    else
    {
        'Physical'
    }))

    ## Variables: PowerShell And CLR (.NET) Versions
    $variables.Add('envPSVersionTable', $PSVersionTable)
    $variables.Add('envPSProcessPath', (Get-ADTPowerShellProcessPath))

    # PowerShell Version
    $variables.Add('envPSVersion', $variables.envPSVersionTable.PSVersion)
    $variables.Add('envPSVersionMajor', $variables.envPSVersion.Major)
    $variables.Add('envPSVersionMinor', $variables.envPSVersion.Minor)
    $variables.Add('envPSVersionBuild', $(if ($variables.envPSVersion.PSObject.Properties.Name.Contains('Build')) {$variables.envPSVersionTable.PSVersion.Build}))
    $variables.Add('envPSVersionRevision', $(if ($variables.envPSVersion.PSObject.Properties.Name.Contains('Revision')) {$variables.envPSVersionTable.PSVersion.Revision}))

    # CLR (.NET) Version used by Windows PowerShell
    if ($variables.envPSVersionTable.ContainsKey('CLRVersion'))
    {
        $variables.Add('envCLRVersion', $variables.envPSVersionTable.CLRVersion)
        $variables.Add('envCLRVersionMajor', $variables.envCLRVersion.Major)
        $variables.Add('envCLRVersionMinor', $variables.envCLRVersion.Minor)
        $variables.Add('envCLRVersionBuild', $variables.envCLRVersion.Build)
        $variables.Add('envCLRVersionRevision', $variables.envCLRVersion.Revision)
    }
    else
    {
        $variables.Add('envCLRVersion', $null)
        $variables.Add('envCLRVersionMajor', $null)
        $variables.Add('envCLRVersionMinor', $null)
        $variables.Add('envCLRVersionBuild', $null)
        $variables.Add('envCLRVersionRevision', $null)
    }

    ## Variables: Permissions/Accounts
    $currentProcessToken = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $variables.Add('CurrentProcessSID', $currentProcessToken.User.Value)
    $variables.Add('ProcessNTAccount', $currentProcessToken.Name)
    $variables.Add('ProcessNTAccountSID', $currentProcessToken.User.Value)
    $variables.Add('IsAdmin', $currentProcessToken.Groups -contains [System.Security.Principal.SecurityIdentifier]'S-1-5-32-544')
    $variables.Add('IsLocalSystemAccount', $currentProcessToken.User.IsWellKnown([System.Security.Principal.WellKnownSidType]'LocalSystemSid'))
    $variables.Add('IsLocalServiceAccount', $currentProcessToken.User.IsWellKnown([System.Security.Principal.WellKnownSidType]'LocalServiceSid'))
    $variables.Add('IsNetworkServiceAccount', $currentProcessToken.User.IsWellKnown([System.Security.Principal.WellKnownSidType]'NetworkServiceSid'))
    $variables.Add('IsServiceAccount', ($currentProcessToken.Groups -contains [System.Security.Principal.SecurityIdentifier]'S-1-5-6'))
    $variables.Add('IsProcessUserInteractive', [System.Environment]::UserInteractive)
    $variables.Add('LocalSystemNTAccount', (Get-SidTypeAccountName -WellKnownSidType LocalSystemSid))
    $variables.Add('LocalUsersGroup', (Get-SidTypeAccountName -WellKnownSidType BuiltinUsersSid))
    $variables.Add('LocalPowerUsersGroup', (Get-SidTypeAccountName -WellKnownSidType BuiltinPowerUsersSid -ErrorAction Ignore))
    $variables.Add('LocalAdministratorsGroup', (Get-SidTypeAccountName -WellKnownSidType BuiltinAdministratorsSid))
    $variables.Add('SessionZero', $variables.IsLocalSystemAccount -or $variables.IsLocalServiceAccount -or $variables.IsNetworkServiceAccount -or $variables.IsServiceAccount)

    # Variables: Logged on user information
    $variables.Add('LoggedOnUserSessions', [PSADT.QueryUser]::GetUserSessionInfo($env:ComputerName))
    $variables.Add('usersLoggedOn', ($variables.LoggedOnUserSessions | ForEach-Object {$_.NTAccount}))
    $variables.Add('CurrentLoggedOnUserSession', ($variables.LoggedOnUserSessions | Where-Object {$_.IsCurrentSession}))
    $variables.Add('CurrentConsoleUserSession', ($variables.LoggedOnUserSessions | Where-Object {$_.IsConsoleSession}))
    $variables.Add('RunAsActiveUser', $(if ($variables.usersLoggedOn)
    {
        # Determine the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
        # If a console user exists, then that will be the active user session.
        # If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user that is either 'Active' or 'Connected' is the active user.
        if ($variables.IsMultiSessionOS)
        {
            $variables.LoggedOnUserSessions | Where-Object {$_.IsCurrentSession}
        }
        else
        {
            $variables.LoggedOnUserSessions | Where-Object {$_.IsActiveUserSession}
        }
    }))

    # Variables: User profile information.
    $variables.Add('dirUserProfile', (Split-Path -LiteralPath $variables.envPublic))
    $variables.Add('userProfileName', $variables.RunAsActiveUser.UserName)
    $variables.Add('runasUserProfile', (Join-Path -Path $variables.dirUserProfile -ChildPath $variables.userProfileName -Resolve -ErrorAction Ignore))

    ## Variables: Executables
    $variables.Add('exeWusa', "$($variables.envWinDir)\System32\wusa.exe") # Installs Standalone Windows Updates
    $variables.Add('exeMsiexec', "$($variables.envWinDir)\System32\msiexec.exe") # Installs MSI Installers
    $variables.Add('exeSchTasks', "$($variables.envWinDir)\System32\schtasks.exe") # Manages Scheduled Tasks

    ## Variables: Invalid FileName Characters
    $variables.Add('invalidFileNameChars', [System.IO.Path]::GetInvalidFileNameChars())

    ## Variables: RegEx Patterns
    $variables.Add('MSIProductCodeRegExPattern', '^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$')
    $variables.Add('InvalidFileNameCharsRegExPattern', "($([System.String]::Join('|', $variables.invalidFileNameChars.ForEach({[System.Text.RegularExpressions.Regex]::Escape($_)}))))")

    ## Variables: Registry Keys
    # Registry keys for native and WOW64 applications
    $variables.Add('regKeyApplications', ('Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall', 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall'))
    $variables.Add('regKeyAppExecution', 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options')

    # Add in WScript shell variables.
    $variables.Add('Shell', (New-Object -ComObject 'WScript.Shell'))
    $variables.Add('ShellApp', (New-Object -ComObject 'Shell.Application'))

    # Store variables within the module's scope.
    $Script:ADT.Environment = $variables.AsReadOnly()
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Import-ADTLocalizedStrings
{
    # Get the best language identifier.
    $Script:ADT.Language = if (![System.String]::IsNullOrWhiteSpace($Script:ADT.Config.UI.LanguageOverride))
    {
        # The caller has specified a specific language.
        $Script:ADT.Config.UI.LanguageOverride
    }
    else
    {
        # Fall back to PowerShell's.
        $PSUICulture
    }

    # Store the chosen language within this session.
    $Script:ADT.Strings = Import-LocalizedData -BaseDirectory "$Script:PSScriptRoot\Strings" -FileName strings.psd1 -UICulture $Script:ADT.Language
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Read-ADTAssetsIntoMemory
{
    # Grab the bytes of each image asset, store them into a memory stream, then as an image for the form to use.
    $Script:FormData.Assets.Icon = [System.Drawing.Icon]::new([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($Script:ADT.Config.Assets.Icon)))
    $Script:FormData.Assets.Logo = [System.Drawing.Image]::FromStream([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($Script:ADT.Config.Assets.Logo)))
    $Script:FormData.Assets.Banner = [System.Drawing.Image]::FromStream([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($Script:ADT.Config.Assets.Banner)))
    $Script:FormData.BannerHeight = [System.Math]::Ceiling($Script:FormData.Width * ($Script:FormData.Assets.Banner.Height / $Script:FormData.Assets.Banner.Width))
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Import-ADTConfig
{
    # Create variables within this scope from the database, it's needed during the config import.
    $Script:ADT.Environment.GetEnumerator().ForEach({New-Variable -Name $_.Name -Value $_.Value -Option Constant})

    # Read config file and cast the version into an object.
    $config = Import-LocalizedData -BaseDirectory "$Script:PSScriptRoot\Config" -FileName config.psd1
    $config.File.Version = [version]$config.File.Version

    # Confirm the config version meets our minimum requirements.
    if ($config.File.Version -lt $Script:ADT.Environment.appDeployMainScriptMinimumConfigVersion)
    {
        throw [System.Activities.VersionMismatchException]::new("The configuration file version [$($config.File.Version)] is lower than the supported of [$($Script:ADT.Environment.appDeployMainScriptMinimumConfigVersion)]. Please upgrade the configuration file.")
    }

    # Process the config and expand out variables.
    foreach ($section in $($config.Keys))
    {
        foreach ($subsection in $($config[$section].Keys))
        {
            if ($config[$section][$subsection] -is [System.String])
            {
                $config[$section][$subsection] = $ExecutionContext.InvokeCommand.ExpandString($config[$section][$subsection])
            }
        }
    }

    # Expand out asset file paths and test that the files are present.
    foreach ($asset in ('Icon', 'Logo', 'Banner'))
    {
        $config.Assets.$asset = (Get-Item -LiteralPath "$($Script:PSScriptRoot)\$($config.Assets.$asset)").FullName
    }

    # Change paths to user accessible ones if user isn't an admin.
    if (!$Script:ADT.Environment.IsAdmin)
    {
        if ($config.Toolkit.TempPathNoAdminRights)
        {
            $config.Toolkit.TempPath = $config.Toolkit.TempPathNoAdminRights
        }
        if ($config.Toolkit.RegPathNoAdminRights)
        {
            $config.Toolkit.RegPath = $config.Toolkit.RegPathNoAdminRights
        }
        if ($config.Toolkit.LogPathNoAdminRights)
        {
            $config.Toolkit.LogPath = $config.Toolkit.LogPathNoAdminRights
        }
        if ($config.MSI.LogPathNoAdminRights)
        {
            $config.MSI.LogPath = $config.MSI.LogPathNoAdminRights
        }
    }

    # Finally, store the config globally for usage within module.
    $Script:ADT.Config = $config
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Reset-ADTNotifyIcon
{
    $null = if ($Script:FormData.NotifyIcon)
    {
        try
        {
            $Script:FormData.NotifyIcon.Dispose()
            $Script:FormData.NotifyIcon = $null
        }
        catch
        {
            $null
        }
    }
}

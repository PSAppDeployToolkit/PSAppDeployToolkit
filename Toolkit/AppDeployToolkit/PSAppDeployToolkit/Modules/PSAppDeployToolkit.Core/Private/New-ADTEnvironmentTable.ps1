﻿function New-ADTEnvironmentTable
{
    # Internal function for translating SID types to names.
    function Get-SidTypeAccountName
    {
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [Security.Principal.WellKnownSidType]$WellKnownSidType
        )

        # Translate the SidType into its user-readable name.
        return [System.Security.Principal.SecurityIdentifier]::new($WellKnownSidType, $null).Translate([System.Security.Principal.NTAccount]).Value
    }

    # Perform initial setup.
    $adtModule = Get-ADTModuleInfo
    $variables = [ordered]@{}

    ## Variables: Toolkit Info
    $variables.Add('appDeployToolkitName', $adtModule.Name)
    $variables.Add('appDeployMainScriptVersion', $adtModule.Version)

    ## Variables: Culture
    $variables.Add('culture', $Host.CurrentCulture)
    $variables.Add('uiculture', $Host.CurrentUICulture)
    $variables.Add('currentLanguage', $variables.culture.TwoLetterISOLanguageName.ToUpper())
    $variables.Add('currentUILanguage', $variables.uiculture.TwoLetterISOLanguageName.ToUpper())

    ## Variables: Environment Variables
    $variables.Add('envHost', $Host)
    $variables.Add('envShellFolders', (Get-ItemProperty -Path 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -ErrorAction Ignore))
    $variables.Add('envAllUsersProfile', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonApplicationData))
    $variables.Add('envAppData', [System.Environment]::GetFolderPath('ApplicationData'))
    $variables.Add('envArchitecture', [System.Environment]::GetEnvironmentVariable('PROCESSOR_ARCHITECTURE'))
    $variables.Add('envCommonDesktop', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Desktop' -ErrorAction Ignore))
    $variables.Add('envCommonDocuments', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Documents' -ErrorAction Ignore))
    $variables.Add('envCommonStartMenuPrograms', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Programs' -ErrorAction Ignore))
    $variables.Add('envCommonStartMenu', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Start Menu' -ErrorAction Ignore))
    $variables.Add('envCommonStartUp', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Startup' -ErrorAction Ignore))
    $variables.Add('envCommonTemplates', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Templates' -ErrorAction Ignore))
    $variables.Add('envComputerName', [System.Environment]::MachineName.ToUpper())
    $variables.Add('envHomeDrive', [System.Environment]::GetEnvironmentVariable('HOMEDRIVE'))
    $variables.Add('envHomePath', [System.Environment]::GetEnvironmentVariable('HOMEPATH'))
    $variables.Add('envHomeShare', [System.Environment]::GetEnvironmentVariable('HOMESHARE'))
    $variables.Add('envLocalAppData', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::LocalApplicationData))
    $variables.Add('envLogicalDrives', [System.Environment]::GetLogicalDrives())
    $variables.Add('envProgramData', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonApplicationData))
    $variables.Add('envPublic', [System.Environment]::GetEnvironmentVariable('PUBLIC'))
    $variables.Add('envSystemDrive', [System.IO.Path]::GetPathRoot([System.Environment]::SystemDirectory).TrimEnd('\'))
    $variables.Add('envSystemRoot', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows))
    $variables.Add('envTemp', [System.IO.Path]::GetTempPath())
    $variables.Add('envUserCookies', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Cookies))
    $variables.Add('envUserDesktop', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::DesktopDirectory))
    $variables.Add('envUserFavorites', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Favorites))
    $variables.Add('envUserInternetCache', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::InternetCache))
    $variables.Add('envUserInternetHistory', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::History))
    $variables.Add('envUserMyDocuments', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::MyDocuments))
    $variables.Add('envUserName', [System.Environment]::UserName)
    $variables.Add('envUserPictures', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::MyPictures))
    $variables.Add('envUserProfile', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile))
    $variables.Add('envUserSendTo', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::SendTo))
    $variables.Add('envUserStartMenu', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::StartMenu))
    $variables.Add('envUserStartMenuPrograms', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Programs))
    $variables.Add('envUserStartUp', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::StartUp))
    $variables.Add('envUserTemplates', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Templates))
    $variables.Add('envSystem32Directory', [System.Environment]::SystemDirectory)
    $variables.Add('envWinDir', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows))

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
    $variables.Add('envUserDNSDomain', [string]([System.Environment]::GetEnvironmentVariable('USERDNSDOMAIN') | Where-Object {$_} | ForEach-Object {$_.ToLower()}))
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
            [System.Environment]::GetEnvironmentVariable('LOGONSERVER') | Where-Object {$_ -and !$_.Contains('\\MicrosoftAccount')} | ForEach-Object {[System.Net.Dns]::GetHostEntry($_.TrimStart('\')).HostName}
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
            $variables.Add('envSYSWOW64Directory', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::SystemX86))
        }
        else
        {
            $variables.Add('envProgramFiles', [System.Environment]::GetEnvironmentVariable('ProgramW6432'))
            $variables.Add('envCommonProgramFiles', [System.Environment]::GetEnvironmentVariable('CommonProgramW6432'))
            $variables.Add('envSysNativeDirectory', [System.IO.Path]::Combine([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows), 'sysnative'))
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
    $variables.Add('IsMultiSessionOS', (Test-ADTIsMultiSessionOS))
    $variables.Add('envOSProductTypeName', $(switch ($variables.envOSProductType) {
        3 { 'Server' }
        2 { 'Domain Controller' }
        1 { 'Workstation' }
        default { 'Unknown' }
    }))

    ## Variables: Office C2R version, bitness and channel
    $variables.Add('envOfficeVars', (Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Office\ClickToRun\Configuration' -ErrorAction Ignore))
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
    $variables.Add('CurrentProcessToken', [System.Security.Principal.WindowsIdentity]::GetCurrent())
    $variables.Add('CurrentProcessSID', [System.Security.Principal.SecurityIdentifier]$variables.CurrentProcessToken.User)
    $variables.Add('ProcessNTAccount', $variables.CurrentProcessToken.Name)
    $variables.Add('ProcessNTAccountSID', $variables.CurrentProcessSID.Value)
    $variables.Add('IsAdmin', (Test-ADTCallerIsAdmin))
    $variables.Add('IsLocalSystemAccount', $variables.CurrentProcessSID.IsWellKnown([System.Security.Principal.WellKnownSidType]'LocalSystemSid'))
    $variables.Add('IsLocalServiceAccount', $variables.CurrentProcessSID.IsWellKnown([System.Security.Principal.WellKnownSidType]'LocalServiceSid'))
    $variables.Add('IsNetworkServiceAccount', $variables.CurrentProcessSID.IsWellKnown([System.Security.Principal.WellKnownSidType]'NetworkServiceSid'))
    $variables.Add('IsServiceAccount', ($variables.CurrentProcessToken.Groups -contains [System.Security.Principal.SecurityIdentifier]'S-1-5-6'))
    $variables.Add('IsProcessUserInteractive', [System.Environment]::UserInteractive)
    $variables.Add('LocalSystemNTAccount', (Get-SidTypeAccountName -WellKnownSidType LocalSystemSid))
    $variables.Add('LocalUsersGroup', (Get-SidTypeAccountName -WellKnownSidType BuiltinUsersSid))
    $variables.Add('LocalPowerUsersGroup', (Get-SidTypeAccountName -WellKnownSidType BuiltinPowerUsersSid -ErrorAction Ignore))
    $variables.Add('LocalAdministratorsGroup', (Get-SidTypeAccountName -WellKnownSidType BuiltinAdministratorsSid))
    $variables.Add('SessionZero', $variables.IsLocalSystemAccount -or $variables.IsLocalServiceAccount -or $variables.IsNetworkServiceAccount -or $variables.IsServiceAccount)

    ## Variables: Logged on user information
    $variables.Add('LoggedOnUserSessions', [PSADT.QueryUser]::GetUserSessionInfo([System.Environment]::MachineName))
    $variables.Add('usersLoggedOn', ($variables.LoggedOnUserSessions | ForEach-Object {$_.NTAccount}))
    $variables.Add('CurrentLoggedOnUserSession', ($variables.LoggedOnUserSessions | Where-Object {$_.IsCurrentSession}))
    $variables.Add('CurrentConsoleUserSession', ($variables.LoggedOnUserSessions | Where-Object {$_.IsConsoleSession}))
    $variables.Add('RunAsActiveUser', (Get-ADTRunAsActiveUser))

    ## Variables: Task Scheduler service state.
    $variables.Add('IsTaskSchedulerHealthy', $true)
    if ($variables.IsLocalSystemAccount)
    {
        # Check the health of the 'Task Scheduler' service
        try
        {
            if ($svcSchedule = Get-Service -Name Schedule -ErrorAction Ignore)
            {
                if ($svcSchedule.StartType -ne 'Automatic')
                {
                    Set-Service -Name Schedule -StartupType Automatic
                }
                Start-Service -Name Schedule
            }
            else
            {
                $this.Properties.IsTaskSchedulerHealthy = $false
            }
        }
        catch
        {
            $this.Properties.IsTaskSchedulerHealthy = $false
        }
    }

    ## Variables: User profile information.
    $variables.Add('dirUserProfile', (Split-Path -LiteralPath $variables.envPublic))
    $variables.Add('userProfileName', $variables.RunAsActiveUser.UserName)
    $variables.Add('runasUserProfile', (Join-Path -Path $variables.dirUserProfile -ChildPath $variables.userProfileName -Resolve -ErrorAction Ignore))

    ## Variables: Executables
    $variables.Add('exeSchTasks', "$($variables.envWinDir)\System32\schtasks.exe") # Manages Scheduled Tasks

    ## Variables: Invalid FileName Characters
    $variables.Add('invalidFileNameChars', [System.IO.Path]::GetInvalidFileNameChars())

    ## Variables: RegEx Patterns
    $variables.Add('MSIProductCodeRegExPattern', (Get-ADTGuidRegexPattern))
    $variables.Add('InvalidScheduledTaskNameCharsRegExPattern', "($([System.String]::Join('|', ('$', '!', "'", '"', '(', ')', ';', '\', '`', '*', '?', '{', '}', '[', ']', '<', '>', '|', '&', '%', '#', '~', '@', ' ').ForEach({[System.Text.RegularExpressions.Regex]::Escape($_)}))))")

    # Add in WScript shell variables.
    $variables.Add('Shell', (New-Object -ComObject 'WScript.Shell'))
    $variables.Add('ShellApp', (New-Object -ComObject 'Shell.Application'))

    # Return variables for use within the module.
    return $variables.AsReadOnly()
}
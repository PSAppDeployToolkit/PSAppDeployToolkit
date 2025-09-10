#-----------------------------------------------------------------------------
#
# MARK: New-ADTEnvironmentTable
#
#-----------------------------------------------------------------------------

function Private:New-ADTEnvironmentTable
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = "This function does not change system state.")]
    [CmdletBinding()]
    [OutputType([System.Collections.Generic.IReadOnlyDictionary[System.String, System.Object]])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.IDictionary]$AdditionalEnvironmentVariables
    )

    # Perform initial setup.
    $variables = [System.Collections.Generic.Dictionary[System.String, System.Object]]::new()

    ## Variables: Toolkit Info
    $variables.Add('appDeployToolkitName', $MyInvocation.MyCommand.Module.Name)
    $variables.Add('appDeployToolkitPath', $MyInvocation.MyCommand.Module.ModuleBase)
    $variables.Add('appDeployMainScriptVersion', $MyInvocation.MyCommand.Module.Version)

    ## Variables: Culture
    $variables.Add('culture', $Host.CurrentCulture)
    $variables.Add('uiculture', $Host.CurrentUICulture)
    $variables.Add('currentLanguage', $variables.culture.TwoLetterISOLanguageName.ToUpper())
    $variables.Add('currentUILanguage', $variables.uiculture.TwoLetterISOLanguageName.ToUpper())

    ## Variables: Environment Variables
    $variables.Add('envHost', $Host)
    $variables.Add('envHostVersion', [System.Version]$Host.Version)
    $variables.Add('envHostVersionSemantic', $(if ($Host.Version.PSObject.Properties.Name -match '^PSSemVer') { [System.Management.Automation.SemanticVersion]$Host.Version }))
    $variables.Add('envHostVersionMajor', $variables.envHostVersion.Major)
    $variables.Add('envHostVersionMinor', $variables.envHostVersion.Minor)
    $variables.Add('envHostVersionBuild', $(if ($variables.envHostVersion.Build -ge 0) { $variables.envHostVersion.Build }))
    $variables.Add('envHostVersionRevision', $(if ($variables.envHostVersion.Revision -ge 0) { $variables.envHostVersion.Revision }))
    $variables.Add('envHostVersionPreReleaseLabel', $(if ($variables.envHostVersionSemantic -and $variables.envHostVersionSemantic.PreReleaseLabel) { $variables.envHostVersionSemantic.PreReleaseLabel }))
    $variables.Add('envHostVersionBuildLabel', $(if ($variables.envHostVersionSemantic -and $variables.envHostVersionSemantic.BuildLabel) { $variables.envHostVersionSemantic.BuildLabel }))
    $variables.Add('envAllUsersProfile', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonApplicationData))
    $variables.Add('envAppData', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ApplicationData))
    $variables.Add('envArchitecture', [System.Environment]::GetEnvironmentVariable('PROCESSOR_ARCHITECTURE'))
    $variables.Add('envCommonDesktop', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonDesktopDirectory))
    $variables.Add('envCommonDocuments', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonDocuments))
    $variables.Add('envCommonStartMenuPrograms', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonPrograms))
    $variables.Add('envCommonStartMenu', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonStartMenu))
    $variables.Add('envCommonStartUp', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonStartup))
    $variables.Add('envCommonTemplates', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonTemplates))
    $variables.Add('envHomeDrive', [System.Environment]::GetEnvironmentVariable('HOMEDRIVE'))
    $variables.Add('envHomePath', [System.Environment]::GetEnvironmentVariable('HOMEPATH'))
    $variables.Add('envHomeShare', [System.Environment]::GetEnvironmentVariable('HOMESHARE'))
    $variables.Add('envLocalAppData', [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::LocalApplicationData))
    $variables.Add('envLogicalDrives', [System.Collections.Generic.IReadOnlyList[System.String]][System.Collections.ObjectModel.ReadOnlyCollection[System.String]][System.Environment]::GetLogicalDrives())
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
    $w32cs = Get-CimInstance -ClassName Win32_ComputerSystem -Verbose:$false
    $w32csd = $w32cs.Domain | & { process { if ($_) { return $_ } } } | Select-Object -First 1
    $variables.Add('IsMachinePartOfDomain', $w32cs.PartOfDomain)
    $variables.Add('envMachineWorkgroup', $null)
    $variables.Add('envMachineADDomain', $null)
    $variables.Add('envLogonServer', $null)
    $variables.Add('MachineDomainController', $null)
    $variables.Add('envMachineDNSDomain', ([System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().DomainName | & { process { if ($_) { return $_.ToLower() } } } | Select-Object -First 1))
    $variables.Add('envUserDNSDomain', ([System.Environment]::GetEnvironmentVariable('USERDNSDOMAIN') | & { process { if ($_) { return $_.ToLower() } } } | Select-Object -First 1))
    $variables.Add('envUserDomain', $(if ([System.Environment]::UserDomainName) { [System.Environment]::UserDomainName.ToUpper() }))
    $variables.Add('envComputerName', $w32cs.DNSHostName.ToUpper())
    $variables.Add('envComputerNameFQDN', $variables.envComputerName)
    if ($variables.IsMachinePartOfDomain)
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
        $variables.envLogonServer = try
        {
            [System.Environment]::GetEnvironmentVariable('LOGONSERVER') | & { process { if ($_ -and !$_.Contains('\\MicrosoftAccount')) { [System.Net.Dns]::GetHostEntry($_.TrimStart('\')).HostName } } }
        }
        catch
        {
            # If running in system context or if GetHostEntry fails, fall back on the logonserver value stored in the registry
            Get-ItemProperty -LiteralPath 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy\History' -ErrorAction Ignore | Select-Object -ExpandProperty DCName -ErrorAction Ignore
        }
        while ($variables.envLogonServer -and $variables.envLogonServer.StartsWith('\'))
        {
            $variables.envLogonServer = $variables.envLogonServer.Substring(1)
        }

        try
        {
            $variables.MachineDomainController = [System.DirectoryServices.ActiveDirectory.Domain]::GetCurrentDomain().FindDomainController().Name
        }
        catch
        {
            $null = $null
        }
    }
    else
    {
        $variables.envMachineWorkgroup = $w32csd.ToUpper()
    }

    # Get the OS Architecture.
    $variables.Add('Is64Bit', [System.Environment]::Is64BitOperatingSystem)
    $variables.Add('envOSArchitecture', [System.Runtime.InteropServices.RuntimeInformation, mscorlib, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089]::OSArchitecture)

    ## Variables: Current Process Architecture
    $variables.Add('Is64BitProcess', [System.Environment]::Is64BitProcess)
    $variables.Add('psArchitecture', [System.Runtime.InteropServices.RuntimeInformation, mscorlib, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089]::ProcessArchitecture)

    ## Variables: Get normalized paths that vary depending on process bitness.
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
            $variables.Add('envSysNativeDirectory', (Join-Path -Path ([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Windows)) -ChildPath sysnative))
            $variables.Add('envSYSWOW64Directory', [System.Environment]::SystemDirectory)
        }
        $variables.Add('envProgramFilesX86', [System.Environment]::GetFolderPath('ProgramFilesX86'))
        $variables.Add('envCommonProgramFilesX86', [System.Environment]::GetFolderPath('CommonProgramFilesX86'))
    }
    else
    {
        $variables.Add('envProgramFiles', [System.Environment]::GetFolderPath('ProgramFiles'))
        $variables.Add('envProgramFilesX86', $null)
        $variables.Add('envCommonProgramFiles', [System.Environment]::GetFolderPath('CommonProgramFiles'))
        $variables.Add('envCommonProgramFilesX86', $null)
        $variables.Add('envSysNativeDirectory', [System.Environment]::SystemDirectory)
        $variables.Add('envSYSWOW64Directory', $null)
    }

    ## Variables: Operating System
    $osInfo = Get-ADTOperatingSystemInfo
    $variables.Add('envOS', (Get-CimInstance -ClassName Win32_OperatingSystem -Verbose:$false))
    $variables.Add('envOSName', $variables.envOS.Caption.Trim())
    $variables.Add('envOSServicePack', $variables.envOS.CSDVersion)
    $variables.Add('envOSVersion', $osInfo.Version)
    $variables.Add('envOSVersionMajor', $variables.envOSVersion.Major)
    $variables.Add('envOSVersionMinor', $variables.envOSVersion.Minor)
    $variables.Add('envOSVersionBuild', $(if ($variables.envOSVersion.Build -ge 0) { $variables.envOSVersion.Build }))
    $variables.Add('envOSVersionRevision', $(if ($variables.envOSVersion.Revision -ge 0) { $variables.envOSVersion.Revision }))

    # Get the operating system type.
    $variables.Add('envOSProductType', $variables.envOS.ProductType)
    $variables.Add('IsServerOS', $variables.envOSProductType -eq 3)
    $variables.Add('IsDomainControllerOS', $variables.envOSProductType -eq 2)
    $variables.Add('IsWorkstationOS', $variables.envOSProductType -eq 1)
    $variables.Add('IsTerminalServer', $osInfo.IsTerminalServer)
    $variables.Add('IsMultiSessionOS', $osInfo.IsWorkstationEnterpriseMultiSessionOS)
    $variables.Add('envOSProductTypeName', [Microsoft.PowerShell.Commands.ProductType]$variables.envOSProductType)

    ## Variables: Office C2R version, bitness and channel
    $variables.Add('envOfficeVars', (Get-ItemProperty -LiteralPath 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Office\ClickToRun\Configuration' -ErrorAction Ignore))
    $variables.Add('envOfficeVersion', ($variables.envOfficeVars | Select-Object -ExpandProperty VersionToReport -ErrorAction Ignore))
    $variables.Add('envOfficeBitness', ($variables.envOfficeVars | Select-Object -ExpandProperty Platform -ErrorAction Ignore))

    # Channel needs special handling for group policy values.
    $officeChannelProperty = if ($variables.envOfficeVars | Select-Object -ExpandProperty UpdateChannel -ErrorAction Ignore)
    {
        $variables.envOfficeVars.UpdateChannel
    }
    elseif ($variables.envOfficeVars | Select-Object -ExpandProperty CDNBaseURL -ErrorAction Ignore)
    {
        $variables.envOfficeVars.CDNBaseURL
    }
    $variables.Add('envOfficeChannel', $(switch ($officeChannelProperty -replace '^.+/')
            {
                "492350f6-3a01-4f97-b9c0-c7c6ddf67d60" { "monthly"; break }
                "7ffbc6bf-bc32-4f92-8982-f9dd17fd3114" { "semi-annual"; break }
                "64256afe-f5d9-4f86-8936-8840a6a4f5be" { "monthly targeted"; break }
                "b8f9b850-328d-4355-9145-c59439a0c4cf" { "semi-annual targeted"; break }
                "55336b82-a18d-4dd6-b5f6-9e5095c314a6" { "monthly enterprise"; break }
            }))

    ## Variables: Hardware
    $w32b = Get-CimInstance -ClassName Win32_BIOS -Verbose:$false
    $w32bVersion = $w32b | Select-Object -ExpandProperty Version -ErrorAction Ignore
    $w32bSerialNumber = $w32b | Select-Object -ExpandProperty SerialNumber -ErrorAction Ignore
    $variables.Add('envSystemRAM', [System.Math]::Round($w32cs.TotalPhysicalMemory / 1GB))
    $variables.Add('envHardwareType', $(if (($w32bVersion -match 'VRTUAL') -or (($w32cs.Manufacturer -like '*Microsoft*') -and ($w32cs.Model -notlike '*Surface*')))
            {
                'Virtual:Hyper-V'
            }
            elseif ($w32bVersion -match 'A M I')
            {
                'Virtual:Virtual PC'
            }
            elseif ($w32bVersion -like '*Xen*')
            {
                'Virtual:Xen'
            }
            elseif (($w32bSerialNumber -like '*VMware*') -or ($w32cs.Manufacturer -like '*VMWare*'))
            {
                'Virtual:VMware'
            }
            elseif (($w32bSerialNumber -like '*Parallels*') -or ($w32cs.Manufacturer -like '*Parallels*'))
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
    $variables.Add('envPSProcessPath', [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)

    # PowerShell Version
    $variables.Add('envPSVersion', [System.Version]$variables.envPSVersionTable.PSVersion)
    $variables.Add('envPSVersionSemantic', $(if ($variables.envPSVersionTable.PSVersion.GetType().FullName.Equals('System.Management.Automation.SemanticVersion')) { $variables.envPSVersionTable.PSVersion }))
    $variables.Add('envPSVersionMajor', $variables.envPSVersion.Major)
    $variables.Add('envPSVersionMinor', $variables.envPSVersion.Minor)
    $variables.Add('envPSVersionBuild', $(if ($variables.envPSVersion.Build -ge 0) { $variables.envPSVersion.Build }))
    $variables.Add('envPSVersionRevision', $(if ($variables.envPSVersion.Revision -ge 0) { $variables.envPSVersion.Revision }))
    $variables.Add('envPSVersionPreReleaseLabel', $(if ($variables.envPSVersionSemantic -and $variables.envPSVersionSemantic.PreReleaseLabel) { $variables.envPSVersionSemantic.PreReleaseLabel }))
    $variables.Add('envPSVersionBuildLabel', $(if ($variables.envPSVersionSemantic -and $variables.envPSVersionSemantic.BuildLabel) { $variables.envPSVersionSemantic.BuildLabel }))

    # CLR (.NET) Version used by Windows PowerShell
    if ($variables.envPSVersionTable.ContainsKey('CLRVersion'))
    {
        $variables.Add('envCLRVersion', $variables.envPSVersionTable.CLRVersion)
        $variables.Add('envCLRVersionMajor', $variables.envCLRVersion.Major)
        $variables.Add('envCLRVersionMinor', $variables.envCLRVersion.Minor)
        $variables.Add('envCLRVersionBuild', $(if ($variables.envCLRVersion.Build -ge 0) { $variables.envCLRVersion.Build }))
        $variables.Add('envCLRVersionRevision', $(if ($variables.envCLRVersion.Revision -ge 0) { $variables.envCLRVersion.Revision }))
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
    $variables.Add('ProcessNTAccount', [System.Security.Principal.NTAccount]$variables.CurrentProcessToken.Name)
    $variables.Add('ProcessNTAccountSID', $variables.CurrentProcessSID.Value)
    $variables.Add('IsAdmin', (Test-ADTCallerIsAdmin))
    $variables.Add('IsLocalSystemAccount', $variables.CurrentProcessSID.IsWellKnown([System.Security.Principal.WellKnownSidType]::LocalSystemSid))
    $variables.Add('IsLocalServiceAccount', $variables.CurrentProcessSID.IsWellKnown([System.Security.Principal.WellKnownSidType]::LocalServiceSid))
    $variables.Add('IsNetworkServiceAccount', $variables.CurrentProcessSID.IsWellKnown([System.Security.Principal.WellKnownSidType]::NetworkServiceSid))
    $variables.Add('IsServiceAccount', ($variables.CurrentProcessToken.Groups -contains ([System.Security.Principal.SecurityIdentifier]'S-1-5-6')))
    $variables.Add('IsProcessUserInteractive', [System.Environment]::UserInteractive)
    $variables.Add('LocalSystemNTAccount', (ConvertTo-ADTNTAccountOrSID -WellKnownSIDName LocalSystemSid -WellKnownToNTAccount -LocalHost 4>$null).Value)
    $variables.Add('LocalUsersGroup', (ConvertTo-ADTNTAccountOrSID -WellKnownSIDName BuiltinUsersSid -WellKnownToNTAccount -LocalHost 4>$null).Value)
    $variables.Add('LocalAdministratorsGroup', (ConvertTo-ADTNTAccountOrSID -WellKnownSIDName BuiltinAdministratorsSid -WellKnownToNTAccount -LocalHost 4>$null).Value)
    $variables.Add('SessionZero', $variables.IsLocalSystemAccount -or $variables.IsLocalServiceAccount -or $variables.IsNetworkServiceAccount -or $variables.IsServiceAccount)

    ## Variables: Logged on user information
    $variables.Add('LoggedOnUserSessions', [System.Collections.Generic.IReadOnlyList[PSADT.TerminalServices.SessionInfo]][System.Collections.ObjectModel.ReadOnlyCollection[PSADT.TerminalServices.SessionInfo]][PSADT.TerminalServices.SessionInfo[]](Get-ADTLoggedOnUser 4>$null))
    if ($variables.LoggedOnUserSessions)
    {
        $variables.Add('usersLoggedOn', [System.Collections.Generic.IReadOnlyList[System.Security.Principal.NTAccount]][System.Collections.ObjectModel.ReadOnlyCollection[System.Security.Principal.NTAccount]][System.Security.Principal.NTAccount[]]$variables.LoggedOnUserSessions.NTAccount)
        $variables.Add('CurrentLoggedOnUserSession', ($($variables.LoggedOnUserSessions) | & { process { if ($_.IsCurrentSession) { return $_ } } } | Select-Object -First 1))
        $variables.Add('CurrentConsoleUserSession', ($($variables.LoggedOnUserSessions) | & { process { if ($_.IsConsoleSession) { return $_ } } } | Select-Object -First 1))
        $variables.Add('LoggedOnUserSessionsText', ($($variables.LoggedOnUserSessions) | Format-List | Out-String -Width ([System.Int32]::MaxValue)).Trim())
        $variables.Add('RunAsActiveUser', (Get-ADTRunAsActiveUser -UserSessionInfo $variables.LoggedOnUserSessions 4>$null))
    }
    else
    {
        $variables.Add('usersLoggedOn', $null)
        $variables.Add('CurrentLoggedOnUserSession', $null)
        $variables.Add('CurrentConsoleUserSession', $null)
        $variables.Add('LoggedOnUserSessionsText', $null)
        $variables.Add('RunAsActiveUser', $null)
    }

    ## Variables: User profile information.
    $variables.Add('dirUserProfile', [System.IO.Directory]::GetParent($variables.envPublic))
    $variables.Add('userProfileName', $(if ($variables.RunAsActiveUser) { $variables.RunAsActiveUser.UserName }))
    $variables.Add('runasUserProfile', $(if ($variables.userProfileName) { Join-Path -Path $variables.dirUserProfile -ChildPath $variables.userProfileName -Resolve -ErrorAction Ignore }))

    ## Variables: Invalid FileName Characters
    $variables.Add('invalidFileNameChars', [System.Collections.Generic.IReadOnlyList[System.Char]][System.Collections.ObjectModel.ReadOnlyCollection[System.Char]][System.IO.Path]::GetInvalidFileNameChars())
    $variables.Add('invalidFileNameCharsRegExPattern', [System.Text.RegularExpressions.Regex]::new("[$([System.Text.RegularExpressions.Regex]::Escape([System.String]::Join([System.Management.Automation.Language.NullString]::Value, $variables.invalidFileNameChars)))]", [System.Text.RegularExpressions.RegexOptions]::Compiled))

    ## Variables: RegEx Patterns
    $variables.Add('MSIProductCodeRegExPattern', '^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$')
    $variables.Add('InvalidScheduledTaskNameCharsRegExPattern', [System.Text.RegularExpressions.Regex]::new("[$([System.Text.RegularExpressions.Regex]::Escape('\/:*?"<>|'))]", [System.Text.RegularExpressions.RegexOptions]::Compiled))

    # Add in additional environment variables from the caller if they've provided any.
    if ($PSBoundParameters.ContainsKey('AdditionalEnvironmentVariables'))
    {
        foreach ($kvp in $AdditionalEnvironmentVariables.GetEnumerator())
        {
            $variables.Add($kvp.Key, $kvp.Value)
        }
    }

    # Return variables for use within the module.
    return [System.Collections.Generic.IReadOnlyDictionary[System.String, System.Object]][System.Collections.ObjectModel.ReadOnlyDictionary[System.String, System.Object]]$variables
}

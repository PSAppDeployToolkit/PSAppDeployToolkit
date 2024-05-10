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
    $variables.Add('appDeployToolkitName', [string]$Script:MyInvocation.MyCommand.ScriptBlock.Module.Name)

    ## Variables: Script Info
    $variables.Add('appDeployMainScriptVersion', [version]$Script:MyInvocation.MyCommand.ScriptBlock.Module.Version)
    $variables.Add('appDeployMainScriptMinimumConfigVersion', [version]$Script:MyInvocation.MyCommand.ScriptBlock.Module.Version)

    ## Variables: Culture
    $variables.Add('culture', [cultureinfo]$Host.CurrentCulture)
    $variables.Add('currentLanguage', [string]$variables.culture.TwoLetterISOLanguageName.ToUpper())
    $variables.Add('currentUILanguage', [string]$Host.CurrentUICulture.TwoLetterISOLanguageName.ToUpper())

    ## Variables: Environment Variables
    $variables.Add('envShellFolders', [psobject](Get-ItemProperty -Path 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -ErrorAction Ignore))
    $variables.envShellFolders.PSObject.Properties.Remove('PSProvider')
    $variables.Add('envAllUsersProfile', [string]$env:ALLUSERSPROFILE)
    $variables.Add('envAppData', [string][System.Environment]::GetFolderPath('ApplicationData'))
    $variables.Add('envArchitecture', [string]$env:PROCESSOR_ARCHITECTURE)
    $variables.Add('envCommonDesktop', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Desktop' -ErrorAction Ignore))
    $variables.Add('envCommonDocuments', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Documents' -ErrorAction Ignore))
    $variables.Add('envCommonStartMenuPrograms', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Programs' -ErrorAction Ignore))
    $variables.Add('envCommonStartMenu', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Start Menu' -ErrorAction Ignore))
    $variables.Add('envCommonStartUp', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Startup' -ErrorAction Ignore))
    $variables.Add('envCommonTemplates', [string]($variables.envShellFolders | Select-Object -ExpandProperty 'Common Templates' -ErrorAction Ignore))
    $variables.Add('envComputerName', [string][System.Environment]::MachineName.ToUpper())
    $variables.Add('envHomeDrive', [string]$env:HOMEDRIVE)
    $variables.Add('envHomePath', [string]$env:HOMEPATH)
    $variables.Add('envHomeShare', [string]$env:HOMESHARE)
    $variables.Add('envLocalAppData', [string][System.Environment]::GetFolderPath('LocalApplicationData'))
    $variables.Add('envLogicalDrives', [string[]][System.Environment]::GetLogicalDrives())
    $variables.Add('envProgramData', [string][System.Environment]::GetFolderPath('CommonApplicationData'))
    $variables.Add('envPublic', [string]$env:PUBLIC)
    $variables.Add('envSystemDrive', [string]$env:SYSTEMDRIVE)
    $variables.Add('envSystemRoot', [string]$env:SYSTEMROOT)
    $variables.Add('envTemp', [string][System.IO.Path]::GetTempPath())
    $variables.Add('envUserCookies', [string][System.Environment]::GetFolderPath('Cookies'))
    $variables.Add('envUserDesktop', [string][System.Environment]::GetFolderPath('DesktopDirectory'))
    $variables.Add('envUserFavorites', [string][System.Environment]::GetFolderPath('Favorites'))
    $variables.Add('envUserInternetCache', [string][System.Environment]::GetFolderPath('InternetCache'))
    $variables.Add('envUserInternetHistory', [string][System.Environment]::GetFolderPath('History'))
    $variables.Add('envUserMyDocuments', [string][System.Environment]::GetFolderPath('MyDocuments'))
    $variables.Add('envUserName', [string][System.Environment]::UserName)
    $variables.Add('envUserPictures', [string][System.Environment]::GetFolderPath('MyPictures'))
    $variables.Add('envUserProfile', [string]$env:USERPROFILE)
    $variables.Add('envUserSendTo', [string][System.Environment]::GetFolderPath('SendTo'))
    $variables.Add('envUserStartMenu', [string][System.Environment]::GetFolderPath('StartMenu'))
    $variables.Add('envUserStartMenuPrograms', [string][System.Environment]::GetFolderPath('Programs'))
    $variables.Add('envUserStartUp', [string][System.Environment]::GetFolderPath('StartUp'))
    $variables.Add('envUserTemplates', [string][System.Environment]::GetFolderPath('Templates'))
    $variables.Add('envSystem32Directory', [string][System.Environment]::SystemDirectory)
    $variables.Add('envWinDir', [string]$env:WINDIR)

    ## Variables: Running in SCCM Task Sequence.
    $variables.Add('RunningTaskSequence', !![System.Type]::GetTypeFromProgID('Microsoft.SMS.TSEnvironment'))

    ## Variables: Domain Membership
    $w32cs = Get-CimInstance -ClassName Win32_ComputerSystem
    [string]$w32csd = $w32cs.Domain | Where-Object {$_}
    $variables.Add('IsMachinePartOfDomain', [string]$w32cs.PartOfDomain)
    $variables.Add('envMachineWorkgroup', [System.String]::Empty)
    $variables.Add('envMachineADDomain', [System.String]::Empty)
    $variables.Add('envLogonServer', [System.String]::Empty)
    $variables.Add('MachineDomainController', [System.String]::Empty)
    $variables.Add('envMachineDNSDomain', [string]([System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().DomainName | Where-Object {$_} | ForEach-Object {$_.ToLower()}))
    $variables.Add('envUserDNSDomain', [string]($env:USERDNSDOMAIN | Where-Object {$_} | ForEach-Object {$_.ToLower()}))
    $variables.Add('envUserDomain', [string]$(try {[System.Environment]::UserDomainName.ToUpper()} catch {$null}))
    $variables.Add('envComputerNameFQDN', [string]$variables.envComputerName)
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

    ## Variables: Operating System
    $regVer = Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
    $variables.Add('envOS', [ciminstance](Get-CimInstance -ClassName Win32_OperatingSystem))
    $variables.Add('envOSName', [string]$variables.envOS.Caption.Trim())
    $variables.Add('envOSServicePack', [string]$variables.envOS.CSDVersion)
    $variables.Add('envOSVersion', [version]$variables.envOS.Version)
    $variables.Add('envOSVersionMajor', [string]$variables.envOSVersion.Major)
    $variables.Add('envOSVersionMinor', [string]$variables.envOSVersion.Minor)
    $variables.Add('envOSVersionBuild', [string]$variables.envOSVersion.Build)
    $variables.Add('envOSVersionRevision', [string]$(if ($regVer | Get-Member -Name UBR) {$regVer.UBR} elseif ($regVer | Get-Member -Name BuildLabEx) {$regVer.BuildLabEx.Split('.')[1]}))
    $variables.envOSVersion = if ($variables.envOSVersionRevision) {"$($variables.envOSVersion.ToString()).$($variables.envOSVersionRevision)"} else {$variables.envOSVersion.ToString()}

    # Get the operating system type.
    $variables.Add('envOSProductType', [int32]$variables.envOS.ProductType)
    $variables.Add('IsServerOS', [boolean]($variables.envOSProductType -eq 3))
    $variables.Add('IsDomainControllerOS', [boolean]($variables.envOSProductType -eq 2))
    $variables.Add('IsWorkStationOS', [boolean]($variables.envOSProductType -eq 1))
    $variables.Add('IsMultiSessionOS', [boolean]($variables.envOSName -match '^Microsoft Windows \d+ Enterprise (for Virtual Desktops|Enterprise Multi-Session)$'))
    $variables.Add('envOSProductTypeName', [string]$(switch ($variables.envOSProductType) {
        3 { 'Server' }
        2 { 'Domain Controller' }
        1 { 'Workstation' }
        default { 'Unknown' }
    }))

    # Get the OS Architecture.
    $variables.Add('Is64Bit', [boolean]((Get-CimInstance -ClassName Win32_Processor -Filter 'DeviceID = "CPU0"').AddressWidth -eq 64))
    $variables.Add('envOSArchitecture', [string]$(if ($variables.Is64Bit) {'x64'} else {'x86'}))

    ## Variables: Current Process Architecture
    $variables.Add('Is64BitProcess', [boolean]([System.IntPtr]::Size -eq 8))
    $variables.Add('psArchitecture', [string]$(if ($variables.Is64BitProcess) {'x64'} else {'x86'}))

    ## Variables: Get Normalized ProgramFiles and CommonProgramFiles Paths
    if ($variables.Is64Bit)
    {
        if ($variables.Is64BitProcess)
        {
            $variables.Add('envProgramFiles', [string][System.Environment]::GetFolderPath('ProgramFiles'))
            $variables.Add('envCommonProgramFiles', [string][System.Environment]::GetFolderPath('CommonProgramFiles'))
        }
        else
        {
            $variables.Add('envProgramFiles', [string][System.Environment]::GetEnvironmentVariable('ProgramW6432'))
            $variables.Add('envCommonProgramFiles', [string][System.Environment]::GetEnvironmentVariable('CommonProgramW6432'))
        }

        ## PowerShell 2 doesn't support x86 folders so need to use variables instead
        try
        {
            $variables.Add('envProgramFilesX86', [string][System.Environment]::GetFolderPath('ProgramFilesX86'))
            $variables.Add('envCommonProgramFilesX86', [string][System.Environment]::GetFolderPath('CommonProgramFilesX86'))
        }
        catch
        {
            $variables.Add('envProgramFilesX86', [string][System.Environment]::GetEnvironmentVariable('ProgramFiles(x86)'))
            $variables.Add('envCommonProgramFilesX86', [string][System.Environment]::GetEnvironmentVariable('CommonProgramFiles(x86)'))
        }
    }
    else
    {
        $variables.Add('envProgramFiles', [string][Environment]::GetFolderPath('ProgramFiles'))
        $variables.Add('envProgramFilesX86', [System.String]::Empty)
        $variables.Add('envCommonProgramFiles', [string][Environment]::GetFolderPath('CommonProgramFiles'))
        $variables.Add('envCommonProgramFilesX86', [System.String]::Empty)
    }

    ## Variables: Office C2R version, bitness and channel
    $variables.Add('envOfficeVars', [psobject](Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Office\ClickToRun\Configuration' -ErrorAction Ignore))
    $variables.envOfficeVars.PSObject.Properties.Remove('PSProvider')
    $variables.Add('envOfficeVersion', [string]$(if ($variables.envOfficeVars | Select-Object -ExpandProperty VersionToReport -ErrorAction Ignore) {$variables.envOfficeVars.VersionToReport}))
    $variables.Add('envOfficeBitness', [string]$(if ($variables.envOfficeVars | Select-Object -ExpandProperty Platform -ErrorAction Ignore) {$variables.envOfficeVars.Platform}))

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
    $variables.Add('envSystemRAM', [int32](Get-CimInstance -ClassName Win32_PhysicalMemory | Measure-Object -Property Capacity -Sum | ForEach-Object {[System.Math]::Round(($_.Sum / 1GB), 2)}))

    ## Variables: PowerShell And CLR (.NET) Versions
    $variables.Add('envPSVersionTable', [hashtable]$PSVersionTable)

    # PowerShell Version
    $variables.Add('envPSVersion', [string]$variables.envPSVersionTable.PSVersion.ToString())
    $variables.Add('envPSVersionMajor', [string]$variables.envPSVersionTable.PSVersion.Major)
    $variables.Add('envPSVersionMinor', [string]$variables.envPSVersionTable.PSVersion.Minor)
    $variables.Add('envPSVersionBuild', [string]$(if ($variables.envPSVersionTable.PSVersion.PSObject.Properties.Name.Contains('Build')) {$variables.envPSVersionTable.PSVersion.Build}))
    $variables.Add('envPSVersionRevision', [string]$(if ($variables.envPSVersionTable.PSVersion.PSObject.Properties.Name.Contains('Revision')) {$variables.envPSVersionTable.PSVersion.Revision}))

    # CLR (.NET) Version used by Windows PowerShell
    if ($variables.envPSVersionTable.ContainsKey('CLRVersion'))
    {
        $variables.Add('envCLRVersion', [string]$variables.envPSVersionTable.CLRVersion.ToString())
        $variables.Add('envCLRVersionMajor', [string]$variables.envPSVersionTable.CLRVersion.Major)
        $variables.Add('envCLRVersionMinor', [string]$variables.envPSVersionTable.CLRVersion.Minor)
        $variables.Add('envCLRVersionBuild', [string]$variables.envPSVersionTable.CLRVersion.Build)
        $variables.Add('envCLRVersionRevision', [string]$variables.envPSVersionTable.CLRVersion.Revision)
    }
    else
    {
        $variables.Add('envCLRVersion', [System.String]::Empty)
        $variables.Add('envCLRVersionMajor', [System.String]::Empty)
        $variables.Add('envCLRVersionMinor', [System.String]::Empty)
        $variables.Add('envCLRVersionBuild', [System.String]::Empty)
        $variables.Add('envCLRVersionRevision', [System.String]::Empty)
    }

    ## Variables: Permissions/Accounts
    $currentProcessToken = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $variables.Add('CurrentProcessSID', [string]$currentProcessToken.User.Value)
    $variables.Add('ProcessNTAccount', [string]$currentProcessToken.Name)
    $variables.Add('ProcessNTAccountSID', [string]$currentProcessToken.User.Value)
    $variables.Add('IsAdmin', [boolean]($currentProcessToken.Groups -contains [System.Security.Principal.SecurityIdentifier]'S-1-5-32-544'))
    $variables.Add('IsLocalSystemAccount', [boolean]$currentProcessToken.User.IsWellKnown([System.Security.Principal.WellKnownSidType]'LocalSystemSid'))
    $variables.Add('IsLocalServiceAccount', [boolean]$currentProcessToken.User.IsWellKnown([System.Security.Principal.WellKnownSidType]'LocalServiceSid'))
    $variables.Add('IsNetworkServiceAccount', [boolean]$currentProcessToken.User.IsWellKnown([System.Security.Principal.WellKnownSidType]'NetworkServiceSid'))
    $variables.Add('IsServiceAccount', [boolean]($currentProcessToken.Groups -contains [System.Security.Principal.SecurityIdentifier]'S-1-5-6'))
    $variables.Add('IsProcessUserInteractive', [boolean][System.Environment]::UserInteractive)
    $variables.Add('LocalSystemNTAccount', [string](Get-SidTypeAccountName -WellKnownSidType LocalSystemSid))
    $variables.Add('LocalUsersGroup', [string](Get-SidTypeAccountName -WellKnownSidType BuiltinUsersSid))
    $variables.Add('LocalPowerUsersGroup', [string](Get-SidTypeAccountName -WellKnownSidType BuiltinPowerUsersSid -ErrorAction Ignore))
    $variables.Add('LocalAdministratorsGroup', [string](Get-SidTypeAccountName -WellKnownSidType BuiltinAdministratorsSid))
    $variables.Add('SessionZero', [boolean]($variables.IsLocalSystemAccount -or $variables.IsLocalServiceAccount -or $variables.IsNetworkServiceAccount -or $variables.IsServiceAccount))

    # Variables: Logged on user information
    $variables.Add('LoggedOnUserSessions', [PSADT.QueryUser]::GetUserSessionInfo($env:ComputerName))
    $variables.Add('usersLoggedOn', [string[]]($variables.LoggedOnUserSessions | ForEach-Object {$_.NTAccount}))
    $variables.Add('CurrentLoggedOnUserSession', [psobject]($variables.LoggedOnUserSessions | Where-Object {$_.IsCurrentSession}))
    $variables.Add('CurrentConsoleUserSession', [psobject]($variables.LoggedOnUserSessions | Where-Object {$_.IsConsoleSession}))
    $variables.Add('RunAsActiveUser', [psobject]$(if ($variables.usersLoggedOn)
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

    ## Variables: Executables
    $variables.Add('exeWusa', [string]"$($variables.envWinDir)\System32\wusa.exe") # Installs Standalone Windows Updates
    $variables.Add('exeMsiexec', [string]"$($variables.envWinDir)\System32\msiexec.exe") # Installs MSI Installers
    $variables.Add('exeSchTasks', [string]"$($variables.envWinDir)\System32\schtasks.exe") # Manages Scheduled Tasks

    ## Variables: RegEx Patterns
    $variables.Add('MSIProductCodeRegExPattern', [string]'^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$')

    ## Variables: Invalid FileName Characters
    $variables.Add('invalidFileNameChars', [char[]][System.IO.Path]::GetInvalidFileNameChars())

    ## Variables: Registry Keys
    # Registry keys for native and WOW64 applications
    $variables.Add('regKeyApplications', [string[]]('Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall', 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall'))
    $variables.Add('regKeyLotusNotes', [string]"Registry::HKEY_LOCAL_MACHINE\SOFTWARE\$(if ($variables.Is64BitProcess) {'Wow6432Node\'})Lotus\Notes")
    $variables.Add('regKeyAppExecution', [string]'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options')

    ## Variables: System DPI Scale Factor (Requires PSADT.UiAutomation loaded)
    [System.Drawing.Graphics]$GraphicsObject = $null
    [System.IntPtr]$DeviceContextHandle = [IntPtr]::Zero
    $variables.Add('UserDisplayScaleFactor', [boolean]$false)
    $variables.Add('dpiScale', [int32]0)
    $variables.Add('dpiPixels', [int32]0)

    # If a user is logged on, then get display scale factor for logged on user (even if running in session 0).
    try
    {
        # Get Graphics Object from the current Window Handle.
        [System.Drawing.Graphics]$GraphicsObject = [System.Drawing.Graphics]::FromHwnd([IntPtr]::Zero)

        # Get Device Context Handle.
        [System.IntPtr]$DeviceContextHandle = $GraphicsObject.GetHdc()

        # Get Logical and Physical screen height.
        [int32]$LogicalScreenHeight = [PSADT.UiAutomation]::GetDeviceCaps($DeviceContextHandle, [int32][PSADT.UiAutomation+DeviceCap]::VERTRES)
        [int32]$PhysicalScreenHeight = [PSADT.UiAutomation]::GetDeviceCaps($DeviceContextHandle, [int32][PSADT.UiAutomation+DeviceCap]::DESKTOPVERTRES)

        # Calculate DPI scale and pixels.
        $variables.dpiScale = [System.Math]::Round([double]$PhysicalScreenHeight / [double]$LogicalScreenHeight, 2) * 100
        $variables.dpiPixels = [System.Math]::Round(($variables.dpiScale / 100) * 96, 0)
    }
    catch
    {
        $variables.dpiScale = 0
        $variables.dpiPixels = 0
    }
    finally
    {
        # Release the device context handle and dispose of the graphics object.
        if ($null -ne $GraphicsObject)
        {
            if ($DeviceContextHandle -ne [IntPtr]::Zero)
            {
                $GraphicsObject.ReleaseHdc($DeviceContextHandle)
            }
            $GraphicsObject.Dispose()
        }
    }

    # Failed to get dpi, try to read them from registry - Might not be accurate.
    if ($variables.RunAsActiveUser)
    {
        if ($variables.dpiPixels -lt 1)
        {
            $variables.dpiPixels = Get-ItemProperty -LiteralPath "Registry::HKEY_USERS\$($variables.RunAsActiveUser)\Control Panel\Desktop\WindowMetrics" -ErrorAction Ignore | Select-Object -ExpandProperty AppliedDPI -ErrorAction Ignore
        }
        if ($variables.dpiPixels -lt 1)
        {
            $variables.dpiPixels = Get-ItemProperty -LiteralPath "Registry::HKEY_USERS\$($variables.RunAsActiveUser)\Control Panel\Desktop" -ErrorAction Ignore | Select-Object -ExpandProperty LogPixels -ErrorAction Ignore
        }
        $variables.UserDisplayScaleFactor = $true
    }

    # Failed to get dpi from first two registry entries, try to read FontDPI - Usually inaccurate.
    if ($variables.dpiPixels -lt 1)
    {
        #  This registry setting only exists if system scale factor has been changed at least once.
        $variables.dpiPixels = Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontDPI' -ErrorAction Ignore | Select-Object -ExpandProperty LogPixels -ErrorAction Ignore
        $variables.UserDisplayScaleFactor = $false
    }

    # Calculate DPI scale if its empty and we have DPI pixels.
    if (($variables.dpiScale -lt 1) -and ($variables.dpiPixels -gt 0))
    {
        $variables.dpiScale = [System.Math]::Round(($variables.dpiPixels * 100) / 96)
    }

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
        throw [System.InvalidOperationException]::new("The configuration file version [$($config.File.Version)] is lower than the supported version required by the Toolkit [$($Script:ADT.Environment.appDeployMainScriptMinimumConfigVersion)]. Please upgrade the configuration file.")
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
    if (![System.IO.File]::Exists(($config.Assets.Icon = (Resolve-Path -LiteralPath "$($Script:PSScriptRoot)\$($config.Assets.Icon)").Path)))
    {
        throw [System.InvalidOperationException]::new("$($Script:ADT.Environment.appDeployToolkitName) icon file not found.")
    }
    if (![System.IO.File]::Exists(($config.Assets.Logo = (Resolve-Path -LiteralPath "$($Script:PSScriptRoot)\$($config.Assets.Logo)").Path)))
    {
        throw [System.InvalidOperationException]::new("$($Script:ADT.Environment.appDeployToolkitName) logo file not found.")
    }
    if (![System.IO.File]::Exists(($config.Assets.Banner = (Resolve-Path -LiteralPath "$($Script:PSScriptRoot)\$($config.Assets.Banner)").Path)))
    {
        throw [System.InvalidOperationException]::new("$($Script:ADT.Environment.appDeployToolkitName) banner file not found.")
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

    # Calculate banner height.
    $banner = [System.Drawing.Bitmap]::new($config.Assets.Banner)
    $height = [System.Math]::Ceiling(450 * ($banner.Height / $banner.Width))
    $banner.Dispose()
    $Script:ADT.BannerHeight = $height

    # Finally, store the config globally for usage within module.
    $Script:ADT.Config = $config
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Invoke-ScriptBlockInSessionState
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$ScriptBlock,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Object[]]$Arguments
    )

    # Get unbound scriptblock from the provided scriptblock's AST, then invoke it within the provided session.
    return $ExecutionContext.InvokeCommand.InvokeScript($SessionState, $ScriptBlock.Ast.GetScriptBlock(), $Arguments).Where({$null -ne $_})
}

#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

class ADTSession
{
    # Private variables (don't change once initialised).
    hidden [System.Boolean]$LegacyMode = (Get-PSCallStack).Command.Contains('AppDeployToolkitMain.ps1')
    hidden [System.String]$OldPSWindowTitle = $Host.UI.RawUI.WindowTitle
    hidden [System.String]$LoggedOnUserTempPath = [System.String]::Empty
    hidden [System.String]$DefaultMsiExecutablesList = [System.String]::Empty
    hidden [System.String]$DeploymentTypeName = [System.String]::Empty
    hidden [System.Boolean]$DeployModeNonInteractive = $false
    hidden [System.Boolean]$DeployModeSilent = $false
    hidden [System.Boolean]$Initialised = $false

    # State values (can change mid-flight).
    hidden [System.Collections.Hashtable]$State = @{
        LogFileInitialized = $false
        BlockExecution = $false
        MsiRebootDetected = $false
    }

    # Variables we export publically for compatibility.
    hidden [System.Collections.Specialized.OrderedDictionary]$Properties = [ordered]@{
        # Deploy-Application.ps1 variables.
        DeploymentType = 'Install'
        DeployMode = 'Interactive'
        AppVendor = [System.String]::Empty
        AppName = [System.String]::Empty
        AppVersion = [System.String]::Empty
        AppArch = [System.String]::Empty
        AppLang = [System.String]::Empty
        AppRevision = [System.String]::Empty
        AppScriptVersion = [System.String]::Empty
        AppScriptDate = [System.String]::Empty
        AppScriptAuthor = [System.String]::Empty
        InstallName = [System.String]::Empty
        InstallTitle = [System.String]::Empty
        DeployAppScriptFriendlyName = [System.String]::Empty
        DeployAppScriptVersion = [System.String]::Empty
        DeployAppScriptDate = [System.String]::Empty
        DeployAppScriptParameters = @{}
        InstallPhase = 'Initialization'

        # Deploy-Application.ps1 parameters.
        AllowRebootPassThru = $false
        TerminalServerMode = $false
        DisableLogging = $false

        # Calculated variables we publicise.
        CurrentDateTime = [System.DateTime]::Now
        CurrentTime = [System.String]::Empty
        CurrentDate = [System.String]::Empty
        CurrentTimeZoneBias = $null
        DefaultMsiFile = [System.String]::Empty
        DefaultMstFile = [System.String]::Empty
        DefaultMspFiles = [System.String]::Empty
        UseDefaultMsi = $false
        LogName = [System.String]::Empty
        ScriptParentPath = [System.String]::Empty
        DirFiles = [System.String]::Empty
        DirSupportFiles = [System.String]::Empty
        DirAppDeployTemp = [System.String]::Empty
        RegKeyDeferHistory = [System.String]::Empty
        LogTempFolder = [System.String]::Empty
        IsTaskSchedulerHealthy = $true
    }

    # Constructors.
    ADTSession([System.Management.Automation.PSCmdlet]$Cmdlet)
    {
        $this.Init(@{Cmdlet = $Cmdlet})
    }
    ADTSession([System.Collections.Hashtable]$Parameters)
    {
        $this.Init($Parameters)
    }

    # Private methods.
    hidden [System.Void] Init([System.Collections.Hashtable]$Parameters)
    {
        # Establish start date/time first so we can accurately mark the start of execution.
        $this.Properties.CurrentTime = Get-Date -Date $this.Properties.CurrentDateTime -UFormat '%T'
        $this.Properties.CurrentDate = Get-Date -Date $this.Properties.CurrentDateTime -UFormat '%d-%m-%Y'
        $this.Properties.CurrentTimeZoneBias = [System.TimeZone]::CurrentTimeZone.GetUtcOffset($this.Properties.CurrentDateTime)

        # Process provided parameters.
        $Script:SessionCallers.Add($this, $Parameters.Cmdlet)
        $Parameters.GetEnumerator().Where({!$_.Name.Equals('Cmdlet')}).ForEach({$this.Properties[$_.Name] = $_.Value})

        # Ensure the deployment type is always title-cased for log aesthetics.
        $this.Properties.DeploymentType = $Global:Host.CurrentCulture.TextInfo.ToTitleCase($this.Properties.DeploymentType)

        # Establish script directories.
        $this.Properties.ScriptParentPath = [System.IO.Path]::GetDirectoryName($Parameters.Cmdlet.MyInvocation.MyCommand.Path)
        $this.Properties.DirFiles = "$($this.Properties.ScriptParentPath)\Files"
        $this.Properties.DirSupportFiles = "$($this.Properties.ScriptParentPath)\SupportFiles"
        $this.Properties.DirAppDeployTemp = [System.IO.Directory]::CreateDirectory("$($Script:ADT.Config.Toolkit.TempPath)\$($Script:ADT.Environment.appDeployToolkitName)").FullName

        # Set up the user temp path. When running in system context we can derive the native "C:\Users" base path from the Public environment variable.
        # This needs to be performed within the session code as we need the config up before we can process this, but the config depends on the environment being up first.
        $this.LoggedOnUserTempPath = if (($null -ne $Script:ADT.Environment.RunAsActiveUser.NTAccount) -and [System.IO.Directory]::Exists("$(Split-Path -LiteralPath $env:PUBLIC)\$($Script:ADT.Environment.RunAsActiveUser.UserName)"))
        {
            "$(Split-Path -LiteralPath $env:PUBLIC)\$($Script:ADT.Environment.RunAsActiveUser.UserName)\ExecuteAsUser"
        }
        else
        {
            "$($this.Properties.DirAppDeployTemp)\ExecuteAsUser"
        }
    }

    hidden [System.String] GetLogSource()
    {
        # Get the first command in the callstack and consider it the log source.
        return (Get-PSCallStack).Command.Where({![System.String]::IsNullOrWhiteSpace($_)})[0]
    }

    hidden [System.Void] DetectDefaultMsi()
    {
        # If the default Deploy-Application.ps1 hasn't been modified, and the main script was not called by a referring script, check for MSI / MST and modify the install accordingly.
        if (![System.String]::IsNullOrWhiteSpace($this.Properties.AppName))
        {
            return
        }

        # Find the first MSI file in the Files folder and use that as our install.
        $logSrc = $this.GetLogSource()
        if (!$this.Properties.DefaultMsiFile)
        {
            # Get all MSI files.
            $msiFiles = Get-ChildItem -Path "$($this.Properties.DirFiles)\*.msi" -ErrorAction Ignore

            if ($this.Properties.DefaultMsiFile = $msiFiles | Where-Object {$_.Name.EndsWith(".$($Script:ADT.Environment.envOSArchitecture).msi")} | Select-Object -ExpandProperty FullName -First 1)
            {
                Write-Log -Message "Discovered $($Script:ADT.Environment.envOSArchitecture) Zero-Config MSI under $($this.Properties.DefaultMsiFile)" -Source $logSrc
            }
            elseif ($this.Properties.DefaultMsiFile = $msiFiles | Select-Object -ExpandProperty FullName -First 1)
            {
                Write-Log -Message "Discovered Arch-Independent Zero-Config MSI under $($this.Properties.DefaultMsiFile)" -Source $logSrc
            }
            else
            {
                # Return early if we haven't found anything.
                return
            }
        }
        else
        {
            Write-Log -Message "Discovered Zero-Config MSI installation file [$($this.Properties.DefaultMsiFile)]." -Source $logSrc
        }

        try
        {
            # Discover if there is a zero-config MST file
            if ([System.String]::IsNullOrWhiteSpace($this.Properties.DefaultMstFile))
            {
                $this.Properties.DefaultMstFile = [System.IO.Path]::ChangeExtension($this.Properties.DefaultMsiFile, 'mst')
            }
            if ([System.IO.File]::Exists($this.Properties.DefaultMstFile))
            {
                Write-Log -Message "Discovered Zero-Config MST installation file [$($this.Properties.DefaultMstFile)]." -Source $logSrc
            }
            else
            {
                $this.Properties.DefaultMstFile = [System.String]::Empty
            }

            # Discover if there are zero-config MSP files. Name multiple MSP files in alphabetical order to control order in which they are installed.
            if (!$this.Properties.DefaultMspFiles)
            {
                $this.Properties.DefaultMspFiles = Get-ChildItem -Path "$($this.Properties.DirFiles)\*.msp" | Select-Object -ExpandProperty FullName
            }
            if ($this.Properties.DefaultMspFiles)
            {
                Write-Log -Message "Discovered Zero-Config MSP installation file(s) [$($this.Properties.DefaultMspFiles -join ',')]." -Source $logSrc
            }

            # Read the MSI and get the installation details.
            $gmtpParams = @{Path = $this.Properties.DefaultMsiFile; Table = 'File'; ContinueOnError = $false}
            if ($this.Properties.DefaultMstFile) {$gmtpParams.Add('TransformPath', $this.Properties.DefaultMstFile)}
            $msiProps = Get-MsiTableProperty @gmtpParams

            # Generate list of MSI executables for testing later on.
            if ($this.DefaultMsiExecutablesList = (Get-Member -InputObject $msiProps | Where-Object {[System.IO.Path]::GetExtension($_.Name) -eq '.exe'} | ForEach-Object {[System.IO.Path]::GetFileNameWithoutExtension($_.Name)}) -join ',')
            {
                Write-Log -Message "MSI Executable List [$($this.DefaultMsiExecutablesList)]." -Source $logSrc
            }

            # Change table and get properties from it.
            $gmtpParams.Set_Item('Table', 'Property')
            $msiProps = Get-MsiTableProperty @gmtpParams

            # Update our app variables with new values.
            Write-Log -Message "App Vendor [$(($this.Properties.AppVendor = $msiProps.Manufacturer))]." -Source $logSrc
            Write-Log -Message "App Name [$(($this.Properties.AppName = $msiProps.ProductName))]." -Source $logSrc
            Write-Log -Message "App Version [$(($this.Properties.AppVersion = $msiProps.ProductVersion))]." -Source $logSrc
            $this.Properties.UseDefaultMsi = $true
        }
        catch
        {
            Write-Log -Message "Failed to process Zero-Config MSI Deployment.`n$(Resolve-Error)" -Source $logSrc
        }
    }

    hidden [System.Void] SetAppProperties()
    {
        # Set up sample variables if Dot Sourcing the script, app details have not been specified
        if ([System.String]::IsNullOrWhiteSpace($this.Properties.AppName))
        {
            $this.Properties.AppName = $Script:ADT.Environment.appDeployToolkitName

            if (![System.String]::IsNullOrWhiteSpace($this.Properties.AppVendor))
            {
                $this.Properties.AppVendor = [System.String]::Empty
            }
            if ([System.String]::IsNullOrWhiteSpace($this.Properties.AppVersion))
            {
                $this.Properties.AppVersion = $Script:ADT.Environment.appDeployMainScriptVersion.ToString()
            }
            if ([System.String]::IsNullOrWhiteSpace($this.Properties.AppLang))
            {
                $this.Properties.AppLang = $Script:ADT.Environment.currentLanguage
            }
            if ([System.String]::IsNullOrWhiteSpace($this.Properties.AppRevision))
            {
                $this.Properties.AppRevision = '01'
            }
        }

        # Sanitize the application details, as they can cause issues in the script.
        $invalidChars = "($([regex]::Escape([System.IO.Path]::GetInvalidFileNameChars() -join '|')))"
        $this.Properties.AppVendor = $this.Properties.AppVendor.Trim() -replace $invalidChars
        $this.Properties.AppName = $this.Properties.AppName.Trim() -replace $invalidChars
        $this.Properties.AppVersion = $this.Properties.AppVersion.Trim() -replace $invalidChars
        $this.Properties.AppArch = $this.Properties.AppArch.Trim() -replace $invalidChars
        $this.Properties.AppLang = $this.Properties.AppLang.Trim() -replace $invalidChars
        $this.Properties.AppRevision = $this.Properties.AppRevision.Trim() -replace $invalidChars
    }

    hidden [System.Void] SetInstallProperties()
    {
        # Build the Installation Title.
        if ([System.String]::IsNullOrWhiteSpace($this.Properties.InstallTitle))
        {
            $this.Properties.InstallTitle = "$($this.Properties.AppVendor) $($this.Properties.AppName) $($this.Properties.AppVersion)".Trim() -replace '\s{2,}',' '
        }

        # Build the Installation Name.
        if ([System.String]::IsNullOrWhiteSpace($this.Properties.InstallName))
        {
            $this.Properties.InstallName = "$($this.Properties.AppVendor)_$($this.Properties.AppName)_$($this.Properties.AppVersion)_$($this.Properties.AppArch)_$($this.Properties.AppLang)_$($this.Properties.AppRevision)"
        }
        $this.Properties.InstallName = ($this.Properties.InstallName -replace '\s').Trim('_') -replace '[_]+', '_'

        # Set PowerShell window title, in case the window is visible.
        $Global:Host.UI.RawUI.WindowTitle = "$($this.Properties.InstallTitle) - $($this.Properties.DeploymentType)" -replace '\s{2,}',' '

        # Set the Defer History registry path.
        $this.Properties.RegKeyDeferHistory = "$($Script:ADT.Config.Toolkit.RegPath)\$($Script:ADT.Environment.appDeployToolkitName)\DeferHistory\$($this.Properties.InstallName)"
    }

    hidden [System.Void] SetLogName()
    {
        # Generate a log name from our installation properties.
        $this.Properties.LogName = "$($this.Properties.InstallName)_$($Script:ADT.Environment.appDeployToolkitName)_$($this.Properties.DeploymentType).log"

        # If option to compress logs is selected, then log will be created in temp log folder and then copied to actual log folder ($Script:ADT.Config.Toolkit.LogPath) after being zipped.
        if ($Script:ADT.Config.Toolkit.CompressLogs)
        {
            # If the temp log folder already exists from a previous ZIP operation, then delete all files in it to avoid issues.
            if ([System.IO.Directory]::Exists(($this.Properties.LogTempFolder = "$([System.IO.Path]::GetTempPath())$($this.Properties.InstallName)_$($this.Properties.DeploymentType)")))
            {
                [System.IO.Directory]::Remove($this.Properties.LogTempFolder, $true)
            }
        }
    }

    hidden [System.Void] WriteLogDivider()
    {
        # Write divider as requested.
        Write-Log -Message ('*' * 79) -Source $this.GetLogSource()
    }

    hidden [System.Void] OpenLogFile()
    {
        # Initialize logging.
        $this.WriteLogDivider()
        $this.WriteLogDivider()
        Write-Log -Message "[$($this.Properties.InstallName)] setup started." -Source $this.GetLogSource()
    }

    hidden [System.Void] LogScriptInfo()
    {
        $logSrc = $this.GetLogSource()
        if ($this.Properties.AppScriptVersion)
        {
            Write-Log -Message "[$($this.Properties.InstallName)] script version is [$($this.Properties.AppScriptVersion)]" -Source $logSrc
        }
        if ($this.Properties.AppScriptDate)
        {
            Write-Log -Message "[$($this.Properties.InstallName)] script date is [$($this.Properties.AppScriptDate)]" -Source $logSrc
        }
        if ($this.Properties.AppScriptAuthor)
        {
            Write-Log -Message "[$($this.Properties.InstallName)] script author is [$($this.Properties.AppScriptAuthor)]" -Source $logSrc
        }
        if ($this.Properties.DeployAppScriptFriendlyName)
        {
            Write-Log -Message "[$($this.Properties.DeployAppScriptFriendlyName)] script version is [$($this.Properties.DeployAppScriptVersion)]" -Source $logSrc
        }
        if ($this.Properties.DeployAppScriptParameters -and $this.Properties.DeployAppScriptParameters.Count)
        {
            Write-Log -Message "The following parameters were passed to [$($this.Properties.DeployAppScriptFriendlyName)]: [$($this.Properties.deployAppScriptParameters | Resolve-Parameters)]" -Source $logSrc
        }
        Write-Log -Message "[$($Script:ADT.Environment.appDeployToolkitName)] module version is [$($Script:MyInvocation.MyCommand.ScriptBlock.Module.Version)]" -Source $logSrc
        Write-Log -Message "[$($Script:ADT.Environment.appDeployToolkitName)] session in compatibility mode is [$($this.LegacyMode)]" -Source $logSrc
    }

    hidden [System.Void] LogSystemInfo()
    {
        Write-Log -Message "Computer Name is [$($Script:ADT.Environment.envComputerNameFQDN)]" -Source ($logSrc = $this.GetLogSource())
        Write-Log -Message "Current User is [$($Script:ADT.Environment.ProcessNTAccount)]" -Source $logSrc
        Write-Log -Message "OS Version is [$($Script:ADT.Environment.envOSName)$(if ($Script:ADT.Environment.envOSServicePack) {" $($Script:ADT.Environment.envOSServicePack)"}) $($Script:ADT.Environment.envOSArchitecture) $($Script:ADT.Environment.envOSVersion)]" -Source $logSrc
        Write-Log -Message "OS Type is [$($Script:ADT.Environment.envOSProductTypeName)]" -Source $logSrc
        Write-Log -Message "Current Culture is [$($($Script:ADT.Environment.culture).Name)], language is [$($Script:ADT.Environment.currentLanguage)] and UI language is [$($Script:ADT.Environment.currentUILanguage)]" -Source $logSrc
        Write-Log -Message "Hardware Platform is [$(Get-HardwarePlatform)]" -Source $logSrc
        Write-Log -Message "PowerShell Host is [$($Global:Host.Name)] with version [$($Global:Host.Version)]" -Source $logSrc
        Write-Log -Message "PowerShell Version is [$($Script:ADT.Environment.envPSVersion) $($Script:ADT.Environment.psArchitecture)]" -Source $logSrc
        if ($Script:ADT.Environment.envCLRVersion)
        {
            Write-Log -Message "PowerShell CLR (.NET) version is [$($Script:ADT.Environment.envCLRVersion)]" -Source $logSrc
        }
    }

    hidden [System.Void] InstallToastDependencies()
    {
        # Install required assemblies for toast notifications if conditions are right.
        if (!$Script:ADT.Config.Toast.Disable -and $Script:PSVersionTable.PSEdition.Equals('Core') -and !(Get-Package -Name Microsoft.Windows.SDK.NET.Ref -ErrorAction Ignore))
        {
            try
            {
                Write-Log -Message "Installing WinRT assemblies for PowerShell 7 toast notification support. This will take at least 5 minutes, please wait..." -Source $this.GetLogSource()
                Install-Package -Name Microsoft.Windows.SDK.NET.Ref -ProviderName NuGet -Force -Confirm:$false | Out-Null
            }
            catch
            {
                Write-Log -Message "An error occurred while preparing WinRT assemblies for usage. Toast notifications will not be available for this execution." -Severity 2 -Source $this.GetLogSource()
            }
        }
    }

    hidden [System.Void] LogUserInfo()
    {
        # Log details for all currently logged in users.
        Write-Log -Message "Display session information for all logged on users:`n$($Script:ADT.Environment.LoggedOnUserSessions | Format-List | Out-String)" -Source ($logSrc = $this.GetLogSource()) -DebugMessage
        if ($Script:ADT.Environment.usersLoggedOn)
        {
            Write-Log -Message "The following users are logged on to the system: [$($Script:ADT.Environment.usersLoggedOn -join ', ')]." -Source $logSrc

            # Check if the current process is running in the context of one of the logged in users
            if ($Script:ADT.Environment.CurrentLoggedOnUserSession)
            {
                Write-Log -Message "Current process is running with user account [$($Script:ADT.Environment.ProcessNTAccount)] under logged in user session for [$($Script:ADT.Environment.CurrentLoggedOnUserSession.NTAccount)]." -Source $logSrc
            }
            else
            {
                Write-Log -Message "Current process is running under a system account [$($Script:ADT.Environment.ProcessNTAccount)]." -Source $logSrc
            }

            # Guard Intune detection code behind a variable.
            if ($Script:ADT.Config.Toolkit.OobeDetection -and ![PSADT.Utilities]::OobeCompleted())
            {
                Write-Log -Message "Detected OOBE in progress, changing deployment mode to silent." -Source $logSrc
                $this.Properties.DeployMode = 'Silent'
            }

            # Display account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
            if ($Script:ADT.Environment.CurrentConsoleUserSession)
            {
                Write-Log -Message "The following user is the console user [$($Script:ADT.Environment.CurrentConsoleUserSession.NTAccount)] (user with control of physical monitor, keyboard, and mouse)." -Source $logSrc
            }
            else
            {
                Write-Log -Message 'There is no console user logged in (user with control of physical monitor, keyboard, and mouse).' -Source $logSrc
            }

            # Display the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
            if ($Script:ADT.Environment.RunAsActiveUser)
            {
                Write-Log -Message "The active logged on user is [$($Script:ADT.Environment.RunAsActiveUser.NTAccount)]." -Source $logSrc
            }
        }
        else
        {
            Write-Log -Message 'No users are logged on to the system.' -Source $logSrc
        }

        # Log which language's UI messages are loaded from the config file
        Write-Log -Message "The current execution context has a primary UI language of [$($Script:ADT.Environment.currentLanguage)]." -Source $logSrc

        # Advise whether the UI language was overridden.
        if ($Script:ADT.Config.UI.LanguageOverride)
        {
            Write-Log -Message "The config file was configured to override the detected primary UI language with the following UI language: [$($Script:ADT.Config.UI.LanguageOverride)]." -Source $logSrc
        }
        Write-Log -Message "The following UI messages were imported from the config file: [$($Script:ADT.Language)]." -Source $logSrc

        # Log system DPI scale factor of active logged on user
        if ($Script:ADT.Environment.UserDisplayScaleFactor)
        {
            Write-Log -Message "The active logged on user [$($Script:ADT.Environment.RunAsActiveUser.NTAccount)] has a DPI scale factor of [$($Script:ADT.Environment.dpiScale)] with DPI pixels [$($Script:ADT.Environment.dpiPixels)]." -Source $logSrc
        }
        else
        {
            Write-Log -Message "The system has a DPI scale factor of [$($Script:ADT.Environment.dpiScale)] with DPI pixels [$($Script:ADT.Environment.dpiPixels)]." -Source $logSrc
        }
    }

    hidden [System.Void] PerformSCCMTests()
    {
        # Check if script is running from a SCCM Task Sequence.
        if ($Script:ADT.Environment.RunningTaskSequence)
        {
            Write-Log -Message 'Successfully found COM object [Microsoft.SMS.TSEnvironment]. Therefore, script is currently running from a SCCM Task Sequence.' -Source $this.GetLogSource()
        }
        else
        {
            Write-Log -Message 'Unable to find COM object [Microsoft.SMS.TSEnvironment]. Therefore, script is not currently running from a SCCM Task Sequence.' -Source $this.GetLogSource()
        }
    }

    hidden [System.Void] PerformSystemAccountTests()
    {
        # Check to see if the Task Scheduler service is in a healthy state by checking its services to see if they exist, are currently running, and have a start mode of 'Automatic'.
        # The task scheduler service and the services it is dependent on can/should only be started/stopped/modified when running in the SYSTEM context.
        $logSrc = $this.GetLogSource()
        if ($Script:ADT.Environment.IsLocalSystemAccount)
        {
            # Check the health of the 'Task Scheduler' service
            try
            {
                if (Test-ServiceExists -Name 'Schedule' -ContinueOnError $false)
                {
                    if ((Get-ServiceStartMode -Name 'Schedule' -ContinueOnError $false) -ne 'Automatic')
                    {
                        Set-ServiceStartMode -Name 'Schedule' -StartMode 'Automatic' -ContinueOnError $false
                    }
                    Start-ServiceAndDependencies -Name 'Schedule' -SkipServiceExistsTest -ContinueOnError $false
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

            # Log the health of the 'Task Scheduler' service.
            Write-Log -Message "The task scheduler service is in a healthy state: $($this.Properties.IsTaskSchedulerHealthy)." -Source $logSrc
        }
        else
        {
            Write-Log -Message "Skipping attempt to check for and make the task scheduler services healthy, because $($Script:ADT.Environment.appDeployToolkitName) is not running under the [$($Script:ADT.Environment.LocalSystemNTAccount)] account." -Source $logSrc
        }

        # If script is running in session zero.
        if ($Script:ADT.Environment.SessionZero)
        {
            # If the script was launched with deployment mode set to NonInteractive, then continue
            if ($this.Properties.DeployMode -eq 'NonInteractive')
            {
                Write-Log -Message "Session 0 detected but deployment mode was manually set to [$($this.Properties.DeployMode)]." -Source $logSrc
            }
            elseif ($Script:ADT.Config.Toolkit.SessionDetection)
            {
                # If the process is not able to display a UI, enable NonInteractive mode
                if (!$Script:ADT.Environment.IsProcessUserInteractive)
                {
                    $this.Properties.DeployMode = 'NonInteractive'
                    Write-Log -Message "Session 0 detected, process not running in user interactive mode; deployment mode set to [$($this.Properties.DeployMode)]." -Source $logSrc
                }
                elseif (!$Script:ADT.Environment.usersLoggedOn)
                {
                    $this.Properties.DeployMode = 'NonInteractive'
                    Write-Log -Message "Session 0 detected, process running in user interactive mode, no users logged in; deployment mode set to [$($this.Properties.DeployMode)]." -Source $logSrc
                }
                else
                {
                    Write-Log -Message 'Session 0 detected, process running in user interactive mode, user(s) logged in.' -Source $logSrc
                }
            }
            else
            {
                Write-Log -Message "Session 0 detected but toolkit configured to not adjust deployment mode." -Source $logSrc
            }
        }
        else
        {
            Write-Log -Message 'Session 0 not detected.' -Source $logSrc
        }
    }

    hidden [System.Void] SetDeploymentProperties()
    {
        # Set Deploy Mode switches.
        Write-Log -Message "Installation is running in [$($this.Properties.DeployMode)] mode." -Source ($logSrc = $this.GetLogSource())
        switch ($this.Properties.DeployMode)
        {
            'Silent' {
                $this.DeployModeNonInteractive = $true; $this.DeployModeSilent = $true
            }
            'NonInteractive' {
                $this.DeployModeNonInteractive = $true; $this.DeployModeSilent = $false
            }
        }

        # Check deployment type (install/uninstall).
        $this.DeploymentTypeName = switch ($this.Properties.DeploymentType)
        {
            'Install' {
                $Script:ADT.Strings.DeploymentType.Install
            }
            'Uninstall' {
                $Script:ADT.Strings.DeploymentType.UnInstall
            }
            'Repair' {
                $Script:ADT.Strings.DeploymentType.Repair
            }
            default {
                $Script:ADT.Strings.DeploymentType.Install
            }
        }
        Write-Log -Message "Deployment type is [$($this.DeploymentTypeName)]." -Source $logSrc
    }

    hidden [System.Void] TestDefaultMsi()
    {
        # Advise the caller if a zero-config MSI was found.
        if ($this.Properties.UseDefaultMsi)
        {
            Write-Log -Message "Discovered Zero-Config MSI installation file [$($this.Properties.DefaultMsiFile)]." -Source $this.GetLogSource()
        }
    }

    hidden [System.Void] TestAdminRequired()
    {
        # Check current permissions and exit if not running with Administrator rights
        if ($Script:ADT.Config.Toolkit.RequireAdmin -and !$Script:ADT.Environment.IsAdmin)# -and !$ShowBlockedAppDialog)
        {
            $adminErr = "[$($Script:ADT.Environment.appDeployToolkitName)] has a config file option [Toolkit_RequireAdmin] set to [True] so as to require Administrator rights for the toolkit to function. Please re-run the deployment script as an Administrator or change the option in the config file to not require Administrator rights."
            Write-Log -Message $adminErr -Severity 3 -Source $this.GetLogSource()
            Show-DialogBox -Text $adminErr -Icon Stop
            throw [System.InvalidOperationException]::new($adminErr)
        }
    }

    hidden [System.Void] PerformTerminalServerTests()
    {
        # If terminal server mode was specified, change the installation mode to support it
        if ($this.Properties.TerminalServerMode)
        {
            Enable-TerminalServerInstallMode
        }
    }

    # Public methods.
    [System.Object] GetPropertyValue([System.String]$Name)
    {
        # This getter exists as once the script is initialised, we need to read the variable from the caller's scope.
        # We must get the variable every time as syntax like `$var = 'val'` always constructs a new PSVariable...
        if ($this.LegacyMode -and $this.Initialised)
        {
            return Invoke-ScriptBlockInSessionState -SessionState $Script:SessionCallers[$this].SessionState -Arguments $Name -ScriptBlock {
                Get-Variable -Name $args[0] -ValueOnly
            }
        }
        else
        {
            return $this.Properties.$Name
        }
    }

    [System.Void] SetPropertyValue([System.String]$Name, [System.Object]$Value)
    {
        # This getter exists as once the script is initialised, we need to read the variable from the caller's scope.
        # We must get the variable every time as syntax like `$var = 'val'` always constructs a new PSVariable...
        if ($this.LegacyMode -and $this.Initialised)
        {
            Invoke-ScriptBlockInSessionState -SessionState $Script:SessionCallers[$this].SessionState -Arguments $Name, $Value -ScriptBlock {
                Set-Variable -Name $args[0] -Value $args[1]
            }
        }
        else
        {
            $this.Properties[$Name] = $Value
        }
    }

    [System.Void] SyncPropertyValues()
    {
        # This is ran ahead of an async operation for legacy mode operations to ensure the module has the current state.
        if (!$this.LegacyMode -or !$this.Initialised)
        {
            return
        }

        # Pass through the session's property table. Because objects are passed by reference, this works fine.
        Invoke-ScriptBlockInSessionState -SessionState $Script:SessionCallers[$this].SessionState -Arguments $this.Properties -ScriptBlock {
            Set-Variable -Name $($args[0].Keys) | ForEach-Object {$args[0][$_.Name] = $_.Value}
        }
    }

    [System.Void] Open()
    {
        # Ensure this session isn't being opened twice.
        if ($this.Initialised)
        {
            throw [System.InvalidOperationException]::new("The current $($Script:ADT.Environment.appDeployToolkitName) session has already been opened.")
        }

        # Initialise PSADT session.
        $this.DetectDefaultMsi()
        $this.SetAppProperties()
        $this.SetInstallProperties()
        $this.SetLogName()
        $this.OpenLogFile()
        $this.LogScriptInfo()
        $this.LogSystemInfo()
        $this.WriteLogDivider()
        $this.InstallToastDependencies()
        $this.LogUserInfo()
        $this.PerformSCCMTests()
        $this.PerformSystemAccountTests()
        $this.SetDeploymentProperties()
        $this.TestDefaultMsi()
        $this.TestAdminRequired()
        $this.PerformTerminalServerTests()

        # Change the install phase since we've finished initialising. This should get overwritten shortly.
        $this.Properties.InstallPhase = 'Execution'

        # Export session's public variables to the user's scope. For these, we can't capture the Set-Variable
        # PassThru data as syntax like `$var = 'val'` constructs a new PSVariable every time.
        if ($this.LegacyMode)
        {
            Invoke-ScriptBlockInSessionState -SessionState $Script:SessionCallers[$this].SessionState -Arguments $this.Properties -ScriptBlock {
                $args[0].GetEnumerator().ForEach({Set-Variable -Name $_.Name -Value $_.Value -Force})
            }
        }

        # Reflect that we've completed initialisation. This is important for variable retrieval.
        $this.Initialised = $true
    }

    [System.Void] Close()
    {
        # Migrate `Exit-Script` into here.
    }
}

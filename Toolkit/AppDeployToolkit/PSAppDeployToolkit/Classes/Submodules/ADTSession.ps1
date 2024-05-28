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
    hidden [System.String]$DefaultMsiExecutablesList = $null
    hidden [System.String]$DeploymentTypeName = [System.String]::Empty
    hidden [System.Boolean]$DeployModeNonInteractive = $false
    hidden [System.Boolean]$DeployModeSilent = $false
    hidden [System.Boolean]$Initialised = $false

    # State values (can change mid-flight).
    hidden [System.Collections.Hashtable]$State = @{
        BlockExecution = $false
        WelcomeTimer = $null
        FormWelcomeStartPosition = $null
        CloseAppsCountdownGlobal = $null
    }

    # Variables we export publicly for compatibility.
    hidden $Properties = [ordered]@{
        # Deploy-Application.ps1 parameters.
        DeploymentType = 'Install'
        DeployMode = 'Interactive'
        AllowRebootPassThru = $false
        TerminalServerMode = $false
        DisableLogging = $false

        # Deploy-Application.ps1 variables.
        AppVendor = [System.String]::Empty
        AppName = [System.String]::Empty
        AppVersion = [System.String]::Empty
        AppArch = [System.String]::Empty
        AppLang = [System.String]::Empty
        AppRevision = [System.String]::Empty
        AppExitCodes = @(0)
        AppRebootCodes = @(1641, 3010)
        AppScriptVersion = [System.String]::Empty
        AppScriptDate = [System.String]::Empty
        AppScriptAuthor = [System.String]::Empty
        InstallName = [System.String]::Empty
        InstallTitle = [System.String]::Empty
        DeployAppScriptFriendlyName = [System.String]::Empty
        DeployAppScriptVersion = [System.String]::Empty
        DeployAppScriptDate = [System.String]::Empty
        DeployAppScriptParameters = $null
        InstallPhase = 'Initialization'

        # Calculated variables we publicise.
        CurrentDateTime = [System.DateTime]::Now
        CurrentTime = [System.String]::Empty
        CurrentDate = [System.String]::Empty
        CurrentTimeZoneBias = $null
        DefaultMsiFile = [System.String]::Empty
        DefaultMstFile = [System.String]::Empty
        DefaultMspFiles = [System.String]::Empty
        UseDefaultMsi = $false
        LogPath = [System.String]::Empty
        LogName = [System.String]::Empty
        LogFile = [System.String]::Empty
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
    ADTSession([System.Collections.Generic.Dictionary[System.String, System.Object]]$Parameters)
    {
        $this.Init($Parameters)
    }
    hidden ADTSession([System.Management.Automation.PSObject]$DeserialisedSession)
    {
        $DeserialisedSession.PSObject.Properties.ForEach({$this.($_.Name) = $_.Value})
    }

    # Private methods.
    hidden [System.Void] Init([System.Collections.IDictionary]$Parameters)
    {
        # Establish start date/time first so we can accurately mark the start of execution.
        $this.Properties.CurrentTime = Get-Date -Date $this.Properties.CurrentDateTime -UFormat '%T'
        $this.Properties.CurrentDate = Get-Date -Date $this.Properties.CurrentDateTime -UFormat '%d-%m-%Y'
        $this.Properties.CurrentTimeZoneBias = [System.TimeZone]::CurrentTimeZone.GetUtcOffset($this.Properties.CurrentDateTime)

        # Process provided parameters and amend some incoming values.
        $Parameters.GetEnumerator().Where({!$_.Key.Equals('Cmdlet')}).ForEach({$this.Properties[$_.Key] = $_.Value})
        $this.Properties.DeploymentType = $Global:Host.CurrentCulture.TextInfo.ToTitleCase($this.Properties.DeploymentType)
        $this.Properties.DeployAppScriptParameters = $Parameters.Cmdlet.MyInvocation.BoundParameters

        # Establish script directories.
        $this.Properties.ScriptParentPath = [System.IO.Path]::GetDirectoryName($Parameters.Cmdlet.MyInvocation.MyCommand.Path)
        $this.Properties.DirFiles = "$($this.Properties.ScriptParentPath)\Files"
        $this.Properties.DirSupportFiles = "$($this.Properties.ScriptParentPath)\SupportFiles"
        $this.Properties.DirAppDeployTemp = [System.IO.Directory]::CreateDirectory("$($Script:ADT.Config.Toolkit.TempPath)\$($Script:ADT.Environment.appDeployToolkitName)").FullName

        # Set up the user temp path. When running in system context we can derive the native "C:\Users" base path from the Public environment variable.
        # This needs to be performed within the session code as we need the config up before we can process this, but the config depends on the environment being up first.
        $this.LoggedOnUserTempPath = [System.IO.Directory]::CreateDirectory($(if (($null -ne $Script:ADT.Environment.RunAsActiveUser.NTAccount) -and [System.IO.Directory]::Exists($Script:ADT.Environment.runasUserProfile))
        {
            "$($Script:ADT.Environment.runasUserProfile)\ExecuteAsUser"
        }
        else
        {
            "$($this.Properties.DirAppDeployTemp)\ExecuteAsUser"
        })).FullName

        # Lastly, store the caller's $PSCmdlet in our global table. Do this last in case any of the above fails.
        $Script:SessionCallers.Add($this, $Parameters.Cmdlet)
    }

    hidden [System.Void] DetectDefaultMsi()
    {
        # If the default Deploy-Application.ps1 hasn't been modified, and the main script was not called by a referring script, check for MSI / MST and modify the install accordingly.
        if (![System.String]::IsNullOrWhiteSpace($this.Properties.AppName))
        {
            return
        }

        # Find the first MSI file in the Files folder and use that as our install.
        if (!$this.Properties.DefaultMsiFile)
        {
            # Get all MSI files.
            $msiFiles = Get-ChildItem -Path "$($this.Properties.DirFiles)\*.msi" -ErrorAction Ignore

            if ($this.Properties.DefaultMsiFile = $msiFiles | Where-Object {$_.Name.EndsWith(".$($Script:ADT.Environment.envOSArchitecture).msi")} | Select-Object -ExpandProperty FullName -First 1)
            {
                $this.WriteLogEntry("Discovered $($Script:ADT.Environment.envOSArchitecture) Zero-Config MSI under $($this.Properties.DefaultMsiFile)")
            }
            elseif ($this.Properties.DefaultMsiFile = $msiFiles | Select-Object -ExpandProperty FullName -First 1)
            {
                $this.WriteLogEntry("Discovered Arch-Independent Zero-Config MSI under $($this.Properties.DefaultMsiFile)")
            }
            else
            {
                # Return early if we haven't found anything.
                return
            }
        }
        else
        {
            $this.WriteLogEntry("Discovered Zero-Config MSI installation file [$($this.Properties.DefaultMsiFile)].")
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
                $this.WriteLogEntry("Discovered Zero-Config MST installation file [$($this.Properties.DefaultMstFile)].")
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
                $this.WriteLogEntry("Discovered Zero-Config MSP installation file(s) [$($this.Properties.DefaultMspFiles -join ',')].")
            }

            # Read the MSI and get the installation details.
            $gmtpParams = @{Path = $this.Properties.DefaultMsiFile; Table = 'File'; ContinueOnError = $false}
            if ($this.Properties.DefaultMstFile) {$gmtpParams.Add('TransformPath', $this.Properties.DefaultMstFile)}
            $msiProps = Get-MsiTableProperty @gmtpParams

            # Generate list of MSI executables for testing later on.
            if ($this.DefaultMsiExecutablesList = Get-Member -InputObject $msiProps | Where-Object {[System.IO.Path]::GetExtension($_.Name) -eq '.exe'} | ForEach-Object {@{ProcessName = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)}})
            {
                $this.WriteLogEntry("MSI Executable List [$($this.DefaultMsiExecutablesList.ProcessName)].")
            }

            # Change table and get properties from it.
            $gmtpParams.Set_Item('Table', 'Property')
            $msiProps = Get-MsiTableProperty @gmtpParams

            # Update our app variables with new values.
            $this.WriteLogEntry("App Vendor [$(($this.Properties.AppVendor = $msiProps.Manufacturer))].")
            $this.WriteLogEntry("App Name [$(($this.Properties.AppName = $msiProps.ProductName))].")
            $this.WriteLogEntry("App Version [$(($this.Properties.AppVersion = $msiProps.ProductVersion))].")
            $this.Properties.UseDefaultMsi = $true
        }
        catch
        {
            $this.WriteLogEntry("Failed to process Zero-Config MSI Deployment.`n$(Resolve-Error)")
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
        $this.Properties.AppVendor = Remove-ADTInvalidFileNameChars -Name $this.Properties.AppVendor
        $this.Properties.AppName = Remove-ADTInvalidFileNameChars -Name $this.Properties.AppName
        $this.Properties.AppVersion = Remove-ADTInvalidFileNameChars -Name $this.Properties.AppVersion
        $this.Properties.AppArch = Remove-ADTInvalidFileNameChars -Name $this.Properties.AppArch
        $this.Properties.AppLang = Remove-ADTInvalidFileNameChars -Name $this.Properties.AppLang
        $this.Properties.AppRevision = Remove-ADTInvalidFileNameChars -Name $this.Properties.AppRevision
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

    hidden [System.Void] WriteLogDivider([System.UInt32]$Count)
    {
        # Write divider as requested.
        $this.WriteLogEntry((1..$Count).ForEach({'*' * 79}))
    }

    hidden [System.Void] WriteLogDivider()
    {
        # Write divider as requested.
        $this.WriteLogDivider(1)
    }

    hidden [System.Void] InitLogging()
    {
        # Generate log paths from our installation properties.
        $this.Properties.LogTempFolder = Join-Path -Path $Script:ADT.Environment.envTemp -ChildPath "$($this.Properties.InstallName)_$($this.Properties.DeploymentType)"

        # Generate the log directory to use.
        $this.Properties.LogPath = [System.IO.Directory]::CreateDirectory($(if ($Script:ADT.Config.Toolkit.CompressLogs)
        {
            # If the temp log folder already exists from a previous ZIP operation, then delete all files in it to avoid issues.
            if ([System.IO.Directory]::Exists($this.Properties.LogTempFolder))
            {
                [System.IO.Directory]::Remove($this.Properties.LogTempFolder, $true)
            }
            $this.Properties.LogTempFolder
        }
        else
        {
            $Script:ADT.Config.Toolkit.LogPath
        })).FullName

        # Generate the log filename to use.
        $this.Properties.LogName = "$($this.Properties.InstallName)_$($Script:ADT.Environment.appDeployToolkitName)_$($this.Properties.DeploymentType).log"
        $this.Properties.LogFile = Join-Path -Path $this.Properties.LogPath -ChildPath $this.Properties.LogName

        # Check if log file needs to be rotated.
        if ([System.IO.File]::Exists($this.Properties.LogFile) -and !$Script:ADT.Config.Toolkit.LogAppend)
        {
            $logFile = [System.IO.FileInfo]$this.Properties.LogFile
            $logFileSizeMB = $logFile.Length / 1MB

            # Rotate if we've exceeded the size already.
            if (($Script:ADT.Config.Toolkit.LogMaxSize -gt 0) -and ($logFileSizeMB -gt $Script:ADT.Config.Toolkit.LogMaxSize))
            {
                try
                {
                    # Get new log file path.
                    $logFileNameWithoutExtension = [IO.Path]::GetFileNameWithoutExtension($this.Properties.LogFile)
                    $logFileExtension = [IO.Path]::GetExtension($this.Properties.LogFile)
                    $Timestamp = $logFile.LastWriteTime.ToString('yyyy-MM-dd-HH-mm-ss')
                    $ArchiveLogFileName = "{0}_{1}{2}" -f $logFileNameWithoutExtension, $Timestamp, $logFileExtension
                    [String]$ArchiveLogFilePath = Join-Path -Path $this.Properties.LogPath -ChildPath $ArchiveLogFileName

                    # Log message about archiving the log file.
                    $this.WriteLogEntry("Maximum log file size [$($Script:ADT.Config.Toolkit.LogMaxSize) MB] reached. Rename log file to [$ArchiveLogFileName].", 2)

                    # Rename the file
                    Move-Item -LiteralPath $logFile -Destination $ArchiveLogFilePath -Force

                    # Start new log file and log message about archiving the old log file.
                    $this.WriteLogEntry("Previous log file was renamed to [$ArchiveLogFileName] because maximum log file size of [$($Script:ADT.Config.Toolkit.LogMaxSize) MB] was reached.", 2)

                    # Get all log files (including any .lo_ files that may have been created by previous toolkit versions) sorted by last write time
                    $logFiles = $(Get-ChildItem -LiteralPath $this.Properties.LogPath -Filter ("{0}_*{1}" -f $logFileNameWithoutExtension, $logFileExtension); Get-Item -LiteralPath ([IO.Path]::ChangeExtension($this.Properties.LogFile, 'lo_')) -ErrorAction Ignore) | Sort-Object -Property LastWriteTime

                    # Keep only the max number of log files
                    if ($logFiles.Count -gt $Script:ADT.Config.Toolkit.LogMaxHistory)
                    {
                        $logFiles | Select-Object -First ($logFiles.Count - $Script:ADT.Config.Toolkit.LogMaxHistory) | Remove-Item
                    }
                }
                catch
                {
                    Write-Host -Object "[$([System.DateTime]::Now.ToString('O'))] $($this.Properties.InstallPhase) :: Failed to rotate the log file [$($this.Properties.LogFile)].`n$(Resolve-Error)" -ForegroundColor Red
                }
            }
        }

        # Open log file with commencement message.
        $this.WriteLogDivider(2)
        $this.WriteLogEntry("[$($this.Properties.InstallName)] setup started.")
    }

    hidden [System.Void] LogScriptInfo()
    {
        # Announce provided deployment script info.
        if ($this.Properties.AppScriptVersion)
        {
            $this.WriteLogEntry("[$($this.Properties.InstallName)] script version is [$($this.Properties.AppScriptVersion)]")
        }
        if ($this.Properties.AppScriptDate)
        {
            $this.WriteLogEntry("[$($this.Properties.InstallName)] script date is [$($this.Properties.AppScriptDate)]")
        }
        if ($this.Properties.AppScriptAuthor)
        {
            $this.WriteLogEntry("[$($this.Properties.InstallName)] script author is [$($this.Properties.AppScriptAuthor)]")
        }
        if ($this.Properties.DeployAppScriptFriendlyName)
        {
            $this.WriteLogEntry("[$($this.Properties.DeployAppScriptFriendlyName)] script version is [$($this.Properties.DeployAppScriptVersion)]")
        }
        if ($this.Properties.DeployAppScriptParameters.Count)
        {
            $this.WriteLogEntry("The following parameters were passed to [$($this.Properties.DeployAppScriptFriendlyName)]: [$($this.Properties.deployAppScriptParameters | Resolve-ADTBoundParameters)]")
        }
        $this.WriteLogEntry("[$($Script:ADT.Environment.appDeployToolkitName)] module version is [$($Script:MyInvocation.MyCommand.ScriptBlock.Module.Version)]")

        # Announce session instantiation mode.
        if ($this.LegacyMode)
        {
            $this.WriteLogEntry("[$($Script:ADT.Environment.appDeployToolkitName)] session mode is [Legacy]. This mode is deprecated and will be removed in a future release.", 2)
            $this.WriteLogEntry("Information on how to migrate this script to Native mode is available at [https://psappdeploytoolkit.com/].", 2)
            return
        }
        $this.WriteLogEntry("[$($Script:ADT.Environment.appDeployToolkitName)] session mode is [Native].")
    }

    hidden [System.Void] LogSystemInfo()
    {
        $this.WriteLogEntry("Computer Name is [$($Script:ADT.Environment.envComputerNameFQDN)]")
        $this.WriteLogEntry("Current User is [$($Script:ADT.Environment.ProcessNTAccount)]")
        $this.WriteLogEntry("OS Version is [$($Script:ADT.Environment.envOSName)$(if ($Script:ADT.Environment.envOSServicePack) {" $($Script:ADT.Environment.envOSServicePack)"}) $($Script:ADT.Environment.envOSArchitecture) $($Script:ADT.Environment.envOSVersion)]")
        $this.WriteLogEntry("OS Type is [$($Script:ADT.Environment.envOSProductTypeName)]")
        $this.WriteLogEntry("Current Culture is [$($Script:ADT.Environment.culture.Name)], language is [$($Script:ADT.Environment.currentLanguage)] and UI language is [$($Script:ADT.Environment.currentUILanguage)]")
        $this.WriteLogEntry("Hardware Platform is [$($Script:ADT.Environment.envHardwareType)]")
        $this.WriteLogEntry("PowerShell Host is [$($Global:Host.Name)] with version [$($Global:Host.Version)]")
        $this.WriteLogEntry("PowerShell Version is [$($Script:ADT.Environment.envPSVersion) $($Script:ADT.Environment.psArchitecture)]")
        if ($Script:ADT.Environment.envCLRVersion)
        {
            $this.WriteLogEntry("PowerShell CLR (.NET) version is [$($Script:ADT.Environment.envCLRVersion)]")
        }
    }

    hidden [System.Void] LogUserInfo()
    {
        # Log details for all currently logged in users.
        $this.WriteLogEntry("Display session information for all logged on users:`n$($Script:ADT.Environment.LoggedOnUserSessions | Format-List | Out-String)", $true)
        if ($Script:ADT.Environment.usersLoggedOn)
        {
            $this.WriteLogEntry("The following users are logged on to the system: [$($Script:ADT.Environment.usersLoggedOn -join ', ')].")

            # Check if the current process is running in the context of one of the logged in users
            if ($Script:ADT.Environment.CurrentLoggedOnUserSession)
            {
                $this.WriteLogEntry("Current process is running with user account [$($Script:ADT.Environment.ProcessNTAccount)] under logged in user session for [$($Script:ADT.Environment.CurrentLoggedOnUserSession.NTAccount)].")
            }
            else
            {
                $this.WriteLogEntry("Current process is running under a system account [$($Script:ADT.Environment.ProcessNTAccount)].")
            }

            # Guard Intune detection code behind a variable.
            if ($Script:ADT.Config.Toolkit.OobeDetection -and ![PSADT.Utilities]::OobeCompleted())
            {
                $this.WriteLogEntry("Detected OOBE in progress, changing deployment mode to silent.")
                $this.Properties.DeployMode = 'Silent'
            }

            # Display account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
            if ($Script:ADT.Environment.CurrentConsoleUserSession)
            {
                $this.WriteLogEntry("The following user is the console user [$($Script:ADT.Environment.CurrentConsoleUserSession.NTAccount)] (user with control of physical monitor, keyboard, and mouse).")
            }
            else
            {
                $this.WriteLogEntry('There is no console user logged in (user with control of physical monitor, keyboard, and mouse).')
            }

            # Display the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
            if ($Script:ADT.Environment.RunAsActiveUser)
            {
                $this.WriteLogEntry("The active logged on user is [$($Script:ADT.Environment.RunAsActiveUser.NTAccount)].")
            }
        }
        else
        {
            $this.WriteLogEntry('No users are logged on to the system.')
        }

        # Log which language's UI messages are loaded from the config file
        $this.WriteLogEntry("The current execution context has a primary UI language of [$($Script:ADT.Environment.currentLanguage)].")

        # Advise whether the UI language was overridden.
        if ($Script:ADT.Config.UI.LanguageOverride)
        {
            $this.WriteLogEntry("The config file was configured to override the detected primary UI language with the following UI language: [$($Script:ADT.Config.UI.LanguageOverride)].")
        }
        $this.WriteLogEntry("The following UI messages were imported from the config file: [$($Script:ADT.Language)].")
    }

    hidden [System.Void] PerformSCCMTests()
    {
        # Check if script is running from a SCCM Task Sequence.
        if ($Script:ADT.Environment.RunningTaskSequence)
        {
            $this.WriteLogEntry('Successfully found COM object [Microsoft.SMS.TSEnvironment]. Therefore, script is currently running from a SCCM Task Sequence.')
        }
        else
        {
            $this.WriteLogEntry('Unable to find COM object [Microsoft.SMS.TSEnvironment]. Therefore, script is not currently running from a SCCM Task Sequence.')
        }
    }

    hidden [System.Void] PerformSystemAccountTests()
    {
        # Check to see if the Task Scheduler service is in a healthy state by checking its services to see if they exist, are currently running, and have a start mode of 'Automatic'.
        # The task scheduler service and the services it is dependent on can/should only be started/stopped/modified when running in the SYSTEM context.
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
            $this.WriteLogEntry("The task scheduler service is in a healthy state: $($this.Properties.IsTaskSchedulerHealthy).")
        }
        else
        {
            $this.WriteLogEntry("Skipping attempt to check for and make the task scheduler services healthy, because $($Script:ADT.Environment.appDeployToolkitName) is not running under the [$($Script:ADT.Environment.LocalSystemNTAccount)] account.")
        }

        # If script is running in session zero.
        if ($Script:ADT.Environment.SessionZero)
        {
            # If the script was launched with deployment mode set to NonInteractive, then continue
            if ($this.Properties.DeployMode -eq 'NonInteractive')
            {
                $this.WriteLogEntry("Session 0 detected but deployment mode was manually set to [$($this.Properties.DeployMode)].")
            }
            elseif ($Script:ADT.Config.Toolkit.SessionDetection)
            {
                # If the process is not able to display a UI, enable NonInteractive mode
                if (!$Script:ADT.Environment.IsProcessUserInteractive)
                {
                    $this.Properties.DeployMode = 'NonInteractive'
                    $this.WriteLogEntry("Session 0 detected, process not running in user interactive mode; deployment mode set to [$($this.Properties.DeployMode)].")
                }
                elseif (!$Script:ADT.Environment.usersLoggedOn)
                {
                    $this.Properties.DeployMode = 'NonInteractive'
                    $this.WriteLogEntry("Session 0 detected, process running in user interactive mode, no users logged in; deployment mode set to [$($this.Properties.DeployMode)].")
                }
                else
                {
                    $this.WriteLogEntry('Session 0 detected, process running in user interactive mode, user(s) logged in.')
                }
            }
            else
            {
                $this.WriteLogEntry("Session 0 detected but toolkit configured to not adjust deployment mode.")
            }
        }
        else
        {
            $this.WriteLogEntry('Session 0 not detected.')
        }
    }

    hidden [System.Void] SetDeploymentProperties()
    {
        # Set Deploy Mode switches.
        $this.WriteLogEntry("Installation is running in [$($this.Properties.DeployMode)] mode.")
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
        $this.WriteLogEntry("Deployment type is [$($this.DeploymentTypeName)].")
    }

    hidden [System.Void] TestDefaultMsi()
    {
        # Advise the caller if a zero-config MSI was found.
        if ($this.Properties.UseDefaultMsi)
        {
            $this.WriteLogEntry("Discovered Zero-Config MSI installation file [$($this.Properties.DefaultMsiFile)].")
        }
    }

    hidden [System.Void] TestAdminRequired()
    {
        # Check current permissions and exit if not running with Administrator rights
        if ($Script:ADT.Config.Toolkit.RequireAdmin -and !$Script:ADT.Environment.IsAdmin)
        {
            $adminErr = "[$($Script:ADT.Environment.appDeployToolkitName)] has a toolkit config option [RequireAdmin] set to [True] and the current user is not an Administrator, or PowerShell is not elevated. Please re-run the deployment script as an Administrator or change the option in the config file to not require Administrator rights."
            $this.WriteLogEntry($adminErr, 3)
            Show-ADTDialogBox -Text $adminErr -Icon Stop
            throw [System.UnauthorizedAccessException]::new($adminErr)
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
            return $Script:SessionCallers[$this].SessionState.PSVariable.Get($Name).Value
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
            $Script:SessionCallers[$this].SessionState.PSVariable.Set($Name, $Value)
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
        $($this.Properties.Keys).ForEach({$this.Properties.$_ = $this.GetPropertyValue($_)})
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
        $this.InitLogging()
        $this.LogScriptInfo()
        $this.LogSystemInfo()
        $this.WriteLogDivider()
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
            $callerSession = $Script:SessionCallers[$this].SessionState
            $this.Properties.GetEnumerator().ForEach({$callerSession.PSVariable.Set($_.Name, $_.Value)})
        }

        # Reflect that we've completed initialisation. This is important for variable retrieval.
        $this.Initialised = $true
    }

    [System.Void] Close([System.Int32]$ExitCode)
    {
        # If block execution variable is true, call the function to unblock execution.
        if ($this.State.BlockExecution)
        {
            Unblock-AppExecution
        }

        # If Terminal Server mode was set, turn it off.
        if ($this.GetPropertyValue('TerminalServerMode'))
        {
            Disable-TerminalServerInstallMode
        }

        # Process resulting exit code.
        if ($this.GetPropertyValue('AppExitCodes').Contains($ExitCode) -or $this.GetPropertyValue('AppRebootCodes').Contains($ExitCode))
        {
            # Clean up app deferral history.
            if (Test-Path -LiteralPath $this.GetPropertyValue('RegKeyDeferHistory'))
            {
                $this.WriteLogEntry('Removing deferral history...')
                Remove-RegistryKey -Key $this.GetPropertyValue('RegKeyDeferHistory') -Recurse
            }

            # Handle reboot prompts on successful script completion.
            if ($this.GetPropertyValue('AllowRebootPassThru') -and $this.GetPropertyValue('AppRebootCodes').Contains($ExitCode))
            {
                $this.WriteLogEntry('A restart has been flagged as required.')
                $balloonText = "$($this.DeploymentTypeName) $($Script:ADT.Strings.BalloonText.RestartRequired)"
            }
            $balloonText = "$($this.DeploymentTypeName) $($Script:ADT.Strings.BalloonText.Complete)"
            $balloonIcon = 'Info'
            $logSeverity = 0
        }
        elseif (($ExitCode -eq $Script:ADT.Config.UI.DefaultExitCode) -or ($ExitCode -eq $Script:ADT.Config.UI.DeferExitCode))
        {
            $balloonText = "$($this.DeploymentTypeName) $($Script:ADT.Strings.BalloonText.FastRetry)"
            $balloonIcon = 'Warning'
            $logSeverity = 2
        }
        else
        {
            $balloonText = "$($this.DeploymentTypeName) $($Script:ADT.Strings.BalloonText.Error)"
            $balloonIcon = 'Error'
            $logSeverity = 3
        }

        # Update the module's last tracked exit code.
        if ($ExitCode)
        {
            $Script:ADT.LastExitCode = $ExitCode
        }

        # Annouce session success/failure.
        $this.WriteLogEntry("$($this.GetPropertyValue('InstallName')) $($this.DeploymentTypeName.ToLower()) completed with exit code [$ExitCode].", $logSeverity)
        Show-ADTBalloonTip -BalloonTipIcon $balloonIcon -BalloonTipText $balloonText -NoWait

        # Write out a log divider to indicate the end of logging.
        $this.WriteLogEntry('-' * 79)
        $this.SetPropertyValue('DisableLogging', $true)

        # Archive the log files to zip format and then delete the temporary logs folder.
        if ($Script:ADT.Config.Toolkit.CompressLogs)
        {
            $DestinationArchiveFileName = "$($this.GetPropertyValue('InstallName'))_$($this.GetPropertyValue('DeploymentType'))_{0}.zip"
            try
            {
                # Get all archive files sorted by last write time
                $ArchiveFiles = Get-ChildItem -LiteralPath $Script:ADT.Config.Toolkit.LogPath -Filter ([System.String]::Format($DestinationArchiveFileName, '*')) | Sort-Object LastWriteTime
                $DestinationArchiveFileName = [System.String]::Format($DestinationArchiveFileName, [System.DateTime]::Now.ToString('yyyy-MM-dd-HH-mm-ss'))

                # Keep only the max number of archive files
                if ($ArchiveFiles.Count -gt $Script:ADT.Config.Toolkit.LogMaxHistory)
                {
                    $ArchiveFiles | Select-Object -First ($ArchiveFiles.Count - $Script:ADT.Config.Toolkit.LogMaxHistory) | Remove-Item
                }
                Compress-Archive -LiteralPath $this.GetPropertyValue('LogTempFolder') -DestinationPath $($Script:ADT.Config.Toolkit.LogPath)\$DestinationArchiveFileName -Force
                [System.IO.Directory]::Delete($this.GetPropertyValue('LogTempFolder'), $true)
            }
            catch
            {
                Write-Host -Object "[$([System.DateTime]::Now.ToString('O'))] $($this.GetPropertyValue('InstallPhase')) :: Failed to manage archive file [$DestinationArchiveFileName].`n$(Resolve-Error)" -ForegroundColor Red
            }
        }
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.Int32]]$Severity, [System.String]$Source, [System.String]$ScriptSection, [System.Boolean]$DebugMessage)
    {
        # Perform early return checks before wasting time.
        if (($this.GetPropertyValue('DisableLogging') -and !$Script:ADT.Config.Toolkit.LogWriteToHost) -or ($DebugMessage -and !$Script:ADT.Config.Toolkit.LogDebugMessage))
        {
            return
        }

        # Establish logging date/time vars.
        $dateTimeNow = [System.DateTime]::Now
        $logTime = $dateTimeNow.ToString('HH\:mm\:ss.fff')
        $logDate = $dateTimeNow.ToString('MM-dd-yyyy')
        $logTimePlusBias = $logTime + $this.GetPropertyValue('CurrentTimeZoneBias').TotalMinutes
        $writeLogRegex = '^Write-(Log|ADTLogEntry)$'

        # Get caller's invocation info, we'll need it for some variables.
        $i = 1; while (!($invoker = Get-Variable -Name MyInvocation -Scope $i -ValueOnly).MyCommand -or ($invoker.MyCommand.Name -match $writeLogRegex))
        {
            $i++
        }

        # Set up default values if not specified.
        if ($null -eq $Severity)
        {
            $Severity = 1
        }
        if ([System.String]::IsNullOrWhiteSpace($Source))
        {
            $Source = if ($invoker.MyCommand.Name.Equals($Script:MyInvocation.MyCommand.Name))
            {
                (Get-PSCallStack).Command.Where({![System.String]::IsNullOrWhiteSpace($_) -and ($_ -notmatch $writeLogRegex)})[0]
            }
            else
            {
                $invoker.MyCommand.Name
            }
        }
        if ([System.String]::IsNullOrWhiteSpace($ScriptSection))
        {
            $ScriptSection = $this.GetPropertyValue('InstallPhase')
        }

        # Store log string to format with message.
        $logFormats = @(
            [System.String]::Format($Script:Logging.Formats.Legacy, '{0}', $logDate, $logTime, $ScriptSection, $Source, $Script:Logging.SeverityNames[$Severity])
            [System.String]::Format($Script:Logging.Formats.CMTrace, '{0}', $ScriptSection, $logTimePlusBias, $logDate, $Source, $Severity, $invoker.PSCommandPath)
        )

        # Store the colours we'll use against Write-Host.
        $whParams = $Script:Logging.SeverityColours[$Severity]
        $logLine = $logFormats[$Script:ADT.Config.Toolkit.LogStyle -ieq 'CMTrace']
        $conLine = $logFormats[0]
        $logFile = $this.GetPropertyValue('LogFile')
        $canLog = !$this.GetPropertyValue('DisableLogging') -and ![System.String]::IsNullOrWhiteSpace($logFile)

        # If the message is not $null or empty, create the log entry for the different logging methods.
        $Message.Where({![System.String]::IsNullOrWhiteSpace($_)}).ForEach({
            # Write the log entry to the log file if logging is not currently disabled.
            if ($canLog)
            {
                [System.String]::Format($logLine, $_) | Out-File -LiteralPath $logFile -Append -NoClobber -Force -Encoding UTF8
            }

            # Return early if we're not configured to write to the host.
            if (!$Script:ADT.Config.Toolkit.LogWriteToHost)
            {
                return
            }

            # Only output using color options if running in a host which supports colors.
            if ($Global:Host.UI.RawUI.ForegroundColor)
            {
                [System.String]::Format($conLine, $_) | Write-Host @whParams
            }
            else
            {
                # If executing "powershell.exe -File <filename>.ps1 > log.txt", then all the Write-Host calls are sent to stdout so that they are included in the text log.
                [System.Console]::WriteLine([System.String]::Format($conLine, $_))
            }
        })
    }

    [System.Void] WriteLogEntry([System.String[]]$Message)
    {
        $this.WriteLogEntry($Message, $null, $null, $null, $false)
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.Int32]]$Severity)
    {
        $this.WriteLogEntry($Message, $Severity, $null, $null, $false)
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Boolean]$DebugMessage)
    {
        $this.WriteLogEntry($Message, $null, $null, $null, $DebugMessage)
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.Int32]]$Severity, [System.Boolean]$DebugMessage)
    {
        $this.WriteLogEntry($Message, $Severity, $null, $null, $DebugMessage)
    }
}

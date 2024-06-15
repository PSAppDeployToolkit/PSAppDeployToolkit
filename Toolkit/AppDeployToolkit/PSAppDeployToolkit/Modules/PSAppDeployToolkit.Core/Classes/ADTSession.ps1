#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

class ADTSession
{
    # Internal variables that aren't for public access.
    hidden [System.Management.Automation.PSObject]$Internal = [pscustomobject]@{
        LegacyMode = (Get-PSCallStack).Command.Contains('AppDeployToolkitMain.ps1')
        OldPSWindowTitle = $Host.UI.RawUI.WindowTitle
        DefaultMsiExecutablesList = $null
        CallerVariableIntrinsics = $null
        LoggedOnUserTempPath = [System.String]::Empty
        RegKeyDeferHistory = [System.String]::Empty
        DeploymentTypeName = [System.String]::Empty
        DeployModeNonInteractive = $false
        DeployModeSilent = $false
        BlockExecution = $false
        Initialised = $false
    }

    # Private variables for modules to use that aren't for public access.
    hidden [System.Collections.Hashtable]$ExtensionData = @{}

    # Deploy-Application.ps1 parameters.
    [System.String]$DeploymentType = 'Install'
    [System.String]$DeployMode = 'Interactive'
    [System.Boolean]$AllowRebootPassThru
    [System.Boolean]$TerminalServerMode
    [System.Boolean]$DisableLogging

    # Deploy-Application.ps1 variables.
    [System.String]$AppVendor
    [System.String]$AppName
    [System.String]$AppVersion
    [System.String]$AppArch
    [System.String]$AppLang
    [System.String]$AppRevision
    [System.Int32[]]$AppExitCodes = 0
    [System.Int32[]]$AppRebootCodes = 1641, 3010
    [System.String]$AppScriptVersion
    [System.String]$AppScriptDate
    [System.String]$AppScriptAuthor
    [System.String]$InstallName
    [System.String]$InstallTitle
    [System.String]$DeployAppScriptFriendlyName
    [System.String]$DeployAppScriptVersion
    [System.String]$DeployAppScriptDate
    [System.Collections.Generic.Dictionary[System.String, System.Object]]$DeployAppScriptParameters
    [System.String]$InstallPhase = 'Initialization'

    # Calculated variables we publicise.
    [System.DateTime]$CurrentDateTime = [System.DateTime]::Now
    [System.String]$CurrentTime
    [System.String]$CurrentDate
    [System.TimeSpan]$CurrentTimeZoneBias
    [System.String]$ScriptParentPath
    [System.String]$DirFiles
    [System.String]$DirSupportFiles
    [System.String]$DirAppDeployTemp
    [System.String]$DefaultMsiFile
    [System.String]$DefaultMstFile
    [System.String]$DefaultMspFiles
    [System.Boolean]$UseDefaultMsi
    [System.String]$LogTempFolder
    [System.String]$LogPath
    [System.String]$LogName
    [System.String]$LogFile

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
        # Confirm the main system automation params are present.
        foreach ($param in @('Cmdlet').Where({!$Parameters.ContainsKey($_)}))
        {
            throw [System.Management.Automation.ErrorRecord]::new(
                [System.ArgumentException]::new('One or more mandatory parameters are missing.', $param),
                'MandatoryParameterMissing',
                [System.Management.Automation.ErrorCategory]::InvalidArgument,
                $Parameters
            )
        }

        # Confirm the main system automation params aren't null.
        foreach ($param in @('Cmdlet').Where({!$Parameters[$_]}))
        {
            throw [System.Management.Automation.ErrorRecord]::new(
                [System.ArgumentNullException]::new($param, 'One or more mandatory parameters are null.'),
                'MandatoryParameterNullOrEmpty',
                [System.Management.Automation.ErrorCategory]::InvalidData,
                $Parameters
            )
        }

        # Get the current environment.
        $adtEnv = Get-ADTEnvironment

        # Establish start date/time first so we can accurately mark the start of execution.
        $this.CurrentTime = Get-Date -Date $this.CurrentDateTime -UFormat '%T'
        $this.CurrentDate = Get-Date -Date $this.CurrentDateTime -UFormat '%d-%m-%Y'
        $this.CurrentTimeZoneBias = [System.TimeZone]::CurrentTimeZone.GetUtcOffset($this.CurrentDateTime)

        # Process provided parameters and amend some incoming values.
        $Parameters.GetEnumerator().Where({!$_.Key.Equals('Cmdlet')}).ForEach({$this.($_.Key) = $_.Value})
        $this.DeploymentType = $Global:Host.CurrentCulture.TextInfo.ToTitleCase($this.DeploymentType.ToLower())
        $this.DeployAppScriptParameters = $Parameters.Cmdlet.MyInvocation.BoundParameters
        $this.Internal.CallerVariableIntrinsics = $Parameters.Cmdlet.SessionState.PSVariable

        # Establish script directories.
        $this.ScriptParentPath = [System.IO.Path]::GetDirectoryName($Parameters.Cmdlet.MyInvocation.MyCommand.Path)
        $this.DirFiles = "$($this.ScriptParentPath)\Files"
        $this.DirSupportFiles = "$($this.ScriptParentPath)\SupportFiles"
        $this.DirAppDeployTemp = [System.IO.Directory]::CreateDirectory("$((Get-ADTConfig).Toolkit.TempPath)\$($adtEnv.appDeployToolkitName)").FullName

        # Set up the user temp path. When running in system context we can derive the native "C:\Users" base path from the Public environment variable.
        # This needs to be performed within the session code as we need the config up before we can process this, but the config depends on the environment being up first.
        if (($null -ne $adtEnv.RunAsActiveUser.NTAccount) -and [System.IO.Directory]::Exists($adtEnv.runasUserProfile))
        {
            $this.Internal.LoggedOnUserTempPath = [System.IO.Directory]::CreateDirectory("$($adtEnv.runasUserProfile)\ExecuteAsUser").FullName
        }
        else
        {
            $this.Internal.LoggedOnUserTempPath = [System.IO.Directory]::CreateDirectory("$($this.DirAppDeployTemp)\ExecuteAsUser").FullName
        }
    }

    hidden [System.Void] DetectDefaultMsi()
    {
        # If the default Deploy-Application.ps1 hasn't been modified, and the main script was not called by a referring script, check for MSI / MST and modify the install accordingly.
        if (![System.String]::IsNullOrWhiteSpace($this.AppName))
        {
            return
        }

        # Get the current environment.
        $adtEnv = Get-ADTEnvironment

        # Find the first MSI file in the Files folder and use that as our install.
        if (!$this.DefaultMsiFile)
        {
            # Get all MSI files.
            $msiFiles = Get-ChildItem -Path "$($this.DirFiles)\*.msi" -ErrorAction Ignore

            if ($this.DefaultMsiFile = $msiFiles | Where-Object {$_.Name.EndsWith(".$($adtEnv.envOSArchitecture).msi")} | Select-Object -ExpandProperty FullName -First 1)
            {
                $this.WriteLogEntry("Discovered $($adtEnv.envOSArchitecture) Zero-Config MSI under $($this.DefaultMsiFile)")
            }
            elseif ($this.DefaultMsiFile = $msiFiles | Select-Object -ExpandProperty FullName -First 1)
            {
                $this.WriteLogEntry("Discovered Arch-Independent Zero-Config MSI under $($this.DefaultMsiFile)")
            }
            else
            {
                # Return early if we haven't found anything.
                return
            }
        }
        else
        {
            $this.WriteLogEntry("Discovered Zero-Config MSI installation file [$($this.DefaultMsiFile)].")
        }

        try
        {
            # Discover if there is a zero-config MST file
            if ([System.String]::IsNullOrWhiteSpace($this.DefaultMstFile))
            {
                $this.DefaultMstFile = [System.IO.Path]::ChangeExtension($this.DefaultMsiFile, 'mst')
            }
            if ([System.IO.File]::Exists($this.DefaultMstFile))
            {
                $this.WriteLogEntry("Discovered Zero-Config MST installation file [$($this.DefaultMstFile)].")
            }
            else
            {
                $this.DefaultMstFile = [System.String]::Empty
            }

            # Discover if there are zero-config MSP files. Name multiple MSP files in alphabetical order to control order in which they are installed.
            if (!$this.DefaultMspFiles)
            {
                $this.DefaultMspFiles = Get-ChildItem -Path "$($this.DirFiles)\*.msp" | Select-Object -ExpandProperty FullName
            }
            if ($this.DefaultMspFiles)
            {
                $this.WriteLogEntry("Discovered Zero-Config MSP installation file(s) [$($this.DefaultMspFiles -join ',')].")
            }

            # Read the MSI and get the installation details.
            $gmtpParams = @{Path = $this.DefaultMsiFile; Table = 'File'; ContinueOnError = $false}
            if ($this.DefaultMstFile) {$gmtpParams.Add('TransformPath', $this.DefaultMstFile)}
            $msiProps = Get-MsiTableProperty @gmtpParams

            # Generate list of MSI executables for testing later on.
            if ($this.Internal.DefaultMsiExecutablesList = Get-Member -InputObject $msiProps | Where-Object {[System.IO.Path]::GetExtension($_.Name) -eq '.exe'} | ForEach-Object {[pscustomobject]@{ProcessName = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)}})
            {
                $this.WriteLogEntry("MSI Executable List [$($this.Internal.DefaultMsiExecutablesList.ProcessName)].")
            }

            # Change table and get properties from it.
            $gmtpParams.Set_Item('Table', 'Property')
            $msiProps = Get-MsiTableProperty @gmtpParams

            # Update our app variables with new values.
            $this.WriteLogEntry("App Vendor [$(($this.AppVendor = $msiProps.Manufacturer))].")
            $this.WriteLogEntry("App Name [$(($this.AppName = $msiProps.ProductName))].")
            $this.WriteLogEntry("App Version [$(($this.AppVersion = $msiProps.ProductVersion))].")
            $this.UseDefaultMsi = $true
        }
        catch
        {
            $this.WriteLogEntry("Failed to process Zero-Config MSI Deployment.`n$(Resolve-ADTError)")
        }
    }

    hidden [System.Void] SetAppProperties()
    {
        # Set up sample variables if Dot Sourcing the script, app details have not been specified
        if ([System.String]::IsNullOrWhiteSpace($this.AppName))
        {
            $this.AppName = ($adtEnv = Get-ADTEnvironment).appDeployToolkitName

            if (![System.String]::IsNullOrWhiteSpace($this.AppVendor))
            {
                $this.AppVendor = [System.String]::Empty
            }
            if ([System.String]::IsNullOrWhiteSpace($this.AppVersion))
            {
                $this.AppVersion = $adtEnv.appDeployMainScriptVersion.ToString()
            }
            if ([System.String]::IsNullOrWhiteSpace($this.AppLang))
            {
                $this.AppLang = $adtEnv.currentLanguage
            }
            if ([System.String]::IsNullOrWhiteSpace($this.AppRevision))
            {
                $this.AppRevision = '01'
            }
        }

        # Sanitize the application details, as they can cause issues in the script.
        $this.AppVendor = Remove-ADTInvalidFileNameChars -Name $this.AppVendor
        $this.AppName = Remove-ADTInvalidFileNameChars -Name $this.AppName
        $this.AppVersion = Remove-ADTInvalidFileNameChars -Name $this.AppVersion
        $this.AppArch = Remove-ADTInvalidFileNameChars -Name $this.AppArch
        $this.AppLang = Remove-ADTInvalidFileNameChars -Name $this.AppLang
        $this.AppRevision = Remove-ADTInvalidFileNameChars -Name $this.AppRevision
    }

    hidden [System.Void] SetInstallProperties()
    {
        # Build the Installation Title.
        if ([System.String]::IsNullOrWhiteSpace($this.InstallTitle))
        {
            $this.InstallTitle = "$($this.AppVendor) $($this.AppName) $($this.AppVersion)".Trim() -replace '\s{2,}',' '
        }

        # Build the Installation Name.
        if ([System.String]::IsNullOrWhiteSpace($this.InstallName))
        {
            $this.InstallName = "$($this.AppVendor)_$($this.AppName)_$($this.AppVersion)_$($this.AppArch)_$($this.AppLang)_$($this.AppRevision)"
        }
        $this.InstallName = ($this.InstallName -replace '\s').Trim('_') -replace '[_]+', '_'

        # Set the Defer History registry path.
        $this.Internal.RegKeyDeferHistory = "$((Get-ADTConfig).Toolkit.RegPath)\$((Get-ADTEnvironment).appDeployToolkitName)\DeferHistory\$($this.InstallName)"
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
        # Get the current environment and config.
        $adtEnv = Get-ADTEnvironment
        $adtConfig = Get-ADTConfig

        # Generate log paths from our installation properties.
        $this.LogTempFolder = Join-Path -Path $adtEnv.envTemp -ChildPath "$($this.InstallName)_$($this.DeploymentType)"
        if ($adtConfig.Toolkit.CompressLogs)
        {
            # If the temp log folder already exists from a previous ZIP operation, then delete all files in it to avoid issues.
            if ([System.IO.Directory]::Exists($this.LogTempFolder))
            {
                [System.IO.Directory]::Remove($this.LogTempFolder, $true)
            }
            $this.LogPath = [System.IO.Directory]::CreateDirectory($this.LogTempFolder).FullName
            
        }
        else
        {
            $this.LogPath = [System.IO.Directory]::CreateDirectory($adtConfig.Toolkit.LogPath).FullName
        }

        # Generate the log filename to use.
        $this.LogName = "$($this.InstallName)_$($adtEnv.appDeployToolkitName)_$($this.DeploymentType).log"
        $this.LogFile = Join-Path -Path $this.LogPath -ChildPath $this.LogName

        # Check if log file needs to be rotated.
        if ([System.IO.File]::Exists($this.LogFile) -and !$adtConfig.Toolkit.LogAppend)
        {
            $logFileInfo = [System.IO.FileInfo]$this.LogFile
            $logFileSizeMB = $logFileInfo.Length / 1MB

            # Rotate if we've exceeded the size already.
            if (($adtConfig.Toolkit.LogMaxSize -gt 0) -and ($logFileSizeMB -gt $adtConfig.Toolkit.LogMaxSize))
            {
                try
                {
                    # Get new log file path.
                    $logFileNameWithoutExtension = [IO.Path]::GetFileNameWithoutExtension($this.LogFile)
                    $logFileExtension = [IO.Path]::GetExtension($this.LogFile)
                    $Timestamp = $logFileInfo.LastWriteTime.ToString('yyyy-MM-dd-HH-mm-ss')
                    $ArchiveLogFileName = "{0}_{1}{2}" -f $logFileNameWithoutExtension, $Timestamp, $logFileExtension
                    [String]$ArchiveLogFilePath = Join-Path -Path $this.LogPath -ChildPath $ArchiveLogFileName

                    # Log message about archiving the log file.
                    $this.WriteLogEntry("Maximum log file size [$($adtConfig.Toolkit.LogMaxSize) MB] reached. Rename log file to [$ArchiveLogFileName].", 2)

                    # Rename the file
                    Move-Item -LiteralPath $logFileInfo.FullName -Destination $ArchiveLogFilePath -Force

                    # Start new log file and log message about archiving the old log file.
                    $this.WriteLogEntry("Previous log file was renamed to [$ArchiveLogFileName] because maximum log file size of [$($adtConfig.Toolkit.LogMaxSize) MB] was reached.", 2)

                    # Get all log files (including any .lo_ files that may have been created by previous toolkit versions) sorted by last write time
                    $logFiles = $(Get-ChildItem -LiteralPath $this.LogPath -Filter ("{0}_*{1}" -f $logFileNameWithoutExtension, $logFileExtension); Get-Item -LiteralPath ([IO.Path]::ChangeExtension($this.LogFile, 'lo_')) -ErrorAction Ignore) | Sort-Object -Property LastWriteTime

                    # Keep only the max number of log files
                    if ($logFiles.Count -gt $adtConfig.Toolkit.LogMaxHistory)
                    {
                        $logFiles | Select-Object -First ($logFiles.Count - $adtConfig.Toolkit.LogMaxHistory) | Remove-Item
                    }
                }
                catch
                {
                    Write-Host -Object "[$([System.DateTime]::Now.ToString('O'))] $($this.InstallPhase) :: Failed to rotate the log file [$($this.LogFile)].`n$(Resolve-ADTError)" -ForegroundColor Red
                }
            }
        }

        # Open log file with commencement message.
        $this.WriteLogDivider(2)
        $this.WriteLogEntry("[$($this.InstallName)] setup started.")
    }

    hidden [System.Void] LogScriptInfo()
    {
        # Get the current environment.
        $adtEnv = Get-ADTEnvironment

        # Announce provided deployment script info.
        if ($this.AppScriptVersion)
        {
            $this.WriteLogEntry("[$($this.InstallName)] script version is [$($this.AppScriptVersion)]")
        }
        if ($this.AppScriptDate)
        {
            $this.WriteLogEntry("[$($this.InstallName)] script date is [$($this.AppScriptDate)]")
        }
        if ($this.AppScriptAuthor)
        {
            $this.WriteLogEntry("[$($this.InstallName)] script author is [$($this.AppScriptAuthor)]")
        }
        if ($this.DeployAppScriptFriendlyName)
        {
            $this.WriteLogEntry("[$($this.DeployAppScriptFriendlyName)] script version is [$($this.DeployAppScriptVersion)]")
        }
        if ($this.DeployAppScriptParameters.Count)
        {
            $this.WriteLogEntry("The following parameters were passed to [$($this.DeployAppScriptFriendlyName)]: [$($this.deployAppScriptParameters | Resolve-ADTBoundParameters)]")
        }
        $this.WriteLogEntry("[$($adtEnv.appDeployToolkitName)] module version is [$((Get-ADTModuleInfo).Version)]")

        # Announce session instantiation mode.
        if ($this.Internal.LegacyMode)
        {
            $this.WriteLogEntry("[$($adtEnv.appDeployToolkitName)] session mode is [Legacy]. This mode is deprecated and will be removed in a future release.", 2)
            $this.WriteLogEntry("Information on how to migrate this script to Native mode is available at [https://psappdeploytoolkit.com/].", 2)
            return
        }
        $this.WriteLogEntry("[$($adtEnv.appDeployToolkitName)] session mode is [Native].")
    }

    hidden [System.Void] LogSystemInfo()
    {
        # Get the current environment.
        $adtEnv = Get-ADTEnvironment

        # Report on all determined system info.
        $this.WriteLogEntry("Computer Name is [$($adtEnv.envComputerNameFQDN)]")
        $this.WriteLogEntry("Current User is [$($adtEnv.ProcessNTAccount)]")
        $this.WriteLogEntry("OS Version is [$($adtEnv.envOSName)$(if ($adtEnv.envOSServicePack) {" $($adtEnv.envOSServicePack)"}) $($adtEnv.envOSArchitecture) $($adtEnv.envOSVersion)]")
        $this.WriteLogEntry("OS Type is [$($adtEnv.envOSProductTypeName)]")
        $this.WriteLogEntry("Current Culture is [$($adtEnv.culture.Name)], language is [$($adtEnv.currentLanguage)] and UI language is [$($adtEnv.currentUILanguage)]")
        $this.WriteLogEntry("Hardware Platform is [$($adtEnv.envHardwareType)]")
        $this.WriteLogEntry("PowerShell Host is [$($Global:Host.Name)] with version [$($Global:Host.Version)]")
        $this.WriteLogEntry("PowerShell Version is [$($adtEnv.envPSVersion) $($adtEnv.psArchitecture)]")
        if ($adtEnv.envCLRVersion)
        {
            $this.WriteLogEntry("PowerShell CLR (.NET) version is [$($adtEnv.envCLRVersion)]")
        }
    }

    hidden [System.Void] LogUserInfo()
    {
        # Get the current environment and config.
        $adtEnv = Get-ADTEnvironment
        $adtConfig = Get-ADTConfig

        # Log details for all currently logged in users.
        $this.WriteLogEntry("Display session information for all logged on users:`n$($adtEnv.LoggedOnUserSessions | Format-List | Out-String)", $true)

        # Provide detailed info about current process state.
        if ($adtEnv.usersLoggedOn)
        {
            $this.WriteLogEntry("The following users are logged on to the system: [$($adtEnv.usersLoggedOn -join ', ')].")

            # Check if the current process is running in the context of one of the logged in users
            if ($adtEnv.CurrentLoggedOnUserSession)
            {
                $this.WriteLogEntry("Current process is running with user account [$($adtEnv.ProcessNTAccount)] under logged in user session for [$($adtEnv.CurrentLoggedOnUserSession.NTAccount)].")
            }
            else
            {
                $this.WriteLogEntry("Current process is running under a system account [$($adtEnv.ProcessNTAccount)].")
            }

            # Guard Intune detection code behind a variable.
            if ($adtConfig.Toolkit.OobeDetection -and ![PSADT.Utilities]::OobeCompleted())
            {
                $this.WriteLogEntry("Detected OOBE in progress, changing deployment mode to silent.")
                $this.DeployMode = 'Silent'
            }

            # Display account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
            if ($adtEnv.CurrentConsoleUserSession)
            {
                $this.WriteLogEntry("The following user is the console user [$($adtEnv.CurrentConsoleUserSession.NTAccount)] (user with control of physical monitor, keyboard, and mouse).")
            }
            else
            {
                $this.WriteLogEntry('There is no console user logged in (user with control of physical monitor, keyboard, and mouse).')
            }

            # Display the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
            if ($adtEnv.RunAsActiveUser)
            {
                $this.WriteLogEntry("The active logged on user is [$($adtEnv.RunAsActiveUser.NTAccount)].")
            }
        }
        else
        {
            $this.WriteLogEntry('No users are logged on to the system.')
        }

        # Log which language's UI messages are loaded from the config file
        $this.WriteLogEntry("The current execution context has a primary UI language of [$($adtEnv.currentLanguage)].")

        # Advise whether the UI language was overridden.
        if ($adtConfig.UI.LanguageOverride)
        {
            $this.WriteLogEntry("The config file was configured to override the detected primary UI language with the following UI language: [$($adtConfig.UI.LanguageOverride)].")
        }
        $this.WriteLogEntry("The following UI messages were imported from the config file: [$((Get-ADT).Language)].")
    }

    hidden [System.Void] PerformSCCMTests()
    {
        # Check if script is running from a SCCM Task Sequence.
        if ((Get-ADTEnvironment).RunningTaskSequence)
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
        # Get the current environment.
        $adtEnv = Get-ADTEnvironment

        # Check to see if the Task Scheduler service is in a healthy state by checking its services to see if they exist, are currently running, and have a start mode of 'Automatic'.
        # The task scheduler service and the services it is dependent on can/should only be started/stopped/modified when running in the SYSTEM context.
        if ($adtEnv.IsLocalSystemAccount)
        {
            $this.WriteLogEntry("The task scheduler service is in a healthy state: $($this.IsTaskSchedulerHealthy).")
        }
        else
        {
            $this.WriteLogEntry("Skipping attempt to check for and make the task scheduler services healthy, because $($adtEnv.appDeployToolkitName) is not running under the [$($adtEnv.LocalSystemNTAccount)] account.")
        }

        # If script is running in session zero.
        if ($adtEnv.SessionZero)
        {
            # If the script was launched with deployment mode set to NonInteractive, then continue
            if ($this.DeployMode -eq 'NonInteractive')
            {
                $this.WriteLogEntry("Session 0 detected but deployment mode was manually set to [$($this.DeployMode)].")
            }
            elseif ((Get-ADTConfig).Toolkit.SessionDetection)
            {
                # If the process is not able to display a UI, enable NonInteractive mode
                if (!$adtEnv.IsProcessUserInteractive)
                {
                    $this.DeployMode = 'NonInteractive'
                    $this.WriteLogEntry("Session 0 detected, process not running in user interactive mode; deployment mode set to [$($this.DeployMode)].")
                }
                elseif (!$adtEnv.usersLoggedOn)
                {
                    $this.DeployMode = 'NonInteractive'
                    $this.WriteLogEntry("Session 0 detected, process running in user interactive mode, no users logged in; deployment mode set to [$($this.DeployMode)].")
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
        $this.WriteLogEntry("Installation is running in [$($this.DeployMode)] mode.")
        switch ($this.DeployMode)
        {
            Silent {
                $this.DeployModeNonInteractive = $true
                $this.DeployModeSilent = $true
                break
            }
            NonInteractive {
                $this.DeployModeNonInteractive = $true
                break
            }
        }

        # Check deployment type (install/uninstall).
        $this.WriteLogEntry("Deployment type is [$(($this.Internal.DeploymentTypeName = (Get-ADTStrings).DeploymentType.($this.DeploymentType)))].")
    }

    hidden [System.Void] TestDefaultMsi()
    {
        # Advise the caller if a zero-config MSI was found.
        if ($this.UseDefaultMsi)
        {
            $this.WriteLogEntry("Discovered Zero-Config MSI installation file [$($this.DefaultMsiFile)].")
        }
    }

    hidden [System.Void] TestAdminRequired()
    {
        # Get the current environment and config.
        $adtEnv = Get-ADTEnvironment
        $adtConfig = Get-ADTConfig

        # Check current permissions and exit if not running with Administrator rights
        if ($adtConfig.Toolkit.RequireAdmin -and !$adtEnv.IsAdmin)
        {
            $adminErr = "[$($adtEnv.appDeployToolkitName)] has a toolkit config option [RequireAdmin] set to [True] and the current user is not an Administrator, or PowerShell is not elevated. Please re-run the deployment script as an Administrator or change the option in the config file to not require Administrator rights."
            $this.WriteLogEntry($adminErr, 3)
            Show-ADTDialogBox -Text $adminErr -Icon Stop
            throw [System.UnauthorizedAccessException]::new($adminErr)
        }
    }

    hidden [System.Void] PerformTerminalServerTests()
    {
        # If terminal server mode was specified, change the installation mode to support it
        if ($this.TerminalServerMode)
        {
            Enable-TerminalServerInstallMode
        }
    }

    # Public methods.
    [System.Object] GetPropertyValue([System.String]$Name)
    {
        # This getter exists as once the script is initialised, we need to read the variable from the caller's scope.
        # We must get the variable every time as syntax like `$var = 'val'` always constructs a new PSVariable...
        if ($this.Internal.LegacyMode -and $this.Internal.Initialised)
        {
            return $this.Internal.CallerVariableIntrinsics.Get($Name).Value
        }
        else
        {
            return $this.$Name
        }
    }

    [System.Void] SetPropertyValue([System.String]$Name, [System.Object]$Value)
    {
        # This getter exists as once the script is initialised, we need to read the variable from the caller's scope.
        # We must get the variable every time as syntax like `$var = 'val'` always constructs a new PSVariable...
        if ($this.Internal.LegacyMode -and $this.Internal.Initialised)
        {
            $this.Internal.CallerVariableIntrinsics.Set($Name, $Value)
        }
        else
        {
            $this.$Name = $Value
        }
    }

    [System.Void] SyncPropertyValues()
    {
        # This is ran ahead of an async operation for legacy mode operations to ensure the module has the current state.
        if (!$this.Internal.LegacyMode -or !$this.Internal.Initialised)
        {
            return
        }

        # Pass through the session's property table. Because objects are passed by reference, this works fine.
        $this.PSObject.Properties.Name.ForEach({$this.$_ = $this.GetPropertyValue($_)})
    }

    [System.Void] Open()
    {
        # Ensure this session isn't being opened twice.
        if ($this.Internal.Initialised)
        {
            throw [System.InvalidOperationException]::new("The current $((Get-ADTEnvironment).appDeployToolkitName) session has already been opened.")
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
        $this.InstallPhase = 'Execution'

        # Export session's public variables to the user's scope. For these, we can't capture the Set-Variable
        # PassThru data as syntax like `$var = 'val'` constructs a new PSVariable every time.
        if ($this.Internal.LegacyMode)
        {
            $this.PSObject.Properties.ForEach({$this.Internal.CallerVariableIntrinsics.Set($_.Name, $_.Value)})
        }

        # Set PowerShell window title, in case the window is visible.
        $Global:Host.UI.RawUI.WindowTitle = "$($this.InstallTitle) - $($this.DeploymentType)" -replace '\s{2,}',' '

        # Reflect that we've completed initialisation. This is important for variable retrieval.
        $this.Internal.Initialised = $true
    }

    [System.Void] Close([System.Int32]$ExitCode)
    {
        # Get the current config and strings.
        $adtConfig = Get-ADTConfig
        $adtStrings = Get-ADTStrings

        # If block execution variable is true, call the function to unblock execution.
        if ($this.Internal.BlockExecution)
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
            if (Test-Path -LiteralPath $this.GetRegKeyDeferHistory())
            {
                $this.WriteLogEntry('Removing deferral history...')
                Remove-RegistryKey -Key $this.GetRegKeyDeferHistory() -Recurse
            }

            # Handle reboot prompts on successful script completion.
            $balloonText = if ($this.GetPropertyValue('AllowRebootPassThru') -and $this.GetPropertyValue('AppRebootCodes').Contains($ExitCode))
            {
                $this.WriteLogEntry('A restart has been flagged as required.')
                "$($this.GetDeploymentTypeName()) $($adtStrings.BalloonText.RestartRequired)"
            }
            else
            {
                "$($this.GetDeploymentTypeName()) $($adtStrings.BalloonText.Complete)"
            }
            $balloonIcon = 'Info'
            $logSeverity = 0
        }
        elseif (($ExitCode -eq $adtConfig.UI.DefaultExitCode) -or ($ExitCode -eq $adtConfig.UI.DeferExitCode))
        {
            $balloonText = "$($this.GetDeploymentTypeName()) $($adtStrings.BalloonText.FastRetry)"
            $balloonIcon = 'Warning'
            $logSeverity = 2
        }
        else
        {
            $balloonText = "$($this.GetDeploymentTypeName()) $($adtStrings.BalloonText.Error)"
            $balloonIcon = 'Error'
            $logSeverity = 3
        }

        # Update the module's last tracked exit code.
        if ($ExitCode)
        {
            (Get-ADT).LastExitCode = $ExitCode
        }

        # Annouce session success/failure.
        $this.WriteLogEntry("$($this.GetPropertyValue('InstallName')) $($this.GetDeploymentTypeName().ToLower()) completed with exit code [$ExitCode].", $logSeverity)
        if (Get-Module -Name PSAppDeployToolkit.Dialogs)
        {
            Show-ADTBalloonTip -BalloonTipIcon $balloonIcon -BalloonTipText $balloonText -NoWait
        }

        # Write out a log divider to indicate the end of logging.
        $this.WriteLogEntry('-' * 79)
        $this.SetPropertyValue('DisableLogging', $true)

        # Archive the log files to zip format and then delete the temporary logs folder.
        if ($adtConfig.Toolkit.CompressLogs)
        {
            $DestinationArchiveFileName = "$($this.GetPropertyValue('InstallName'))_$($this.GetPropertyValue('DeploymentType'))_{0}.zip"
            try
            {
                # Get all archive files sorted by last write time
                $ArchiveFiles = Get-ChildItem -LiteralPath $adtConfig.Toolkit.LogPath -Filter ([System.String]::Format($DestinationArchiveFileName, '*')) | Sort-Object LastWriteTime
                $DestinationArchiveFileName = [System.String]::Format($DestinationArchiveFileName, [System.DateTime]::Now.ToString('yyyy-MM-dd-HH-mm-ss'))

                # Keep only the max number of archive files
                if ($ArchiveFiles.Count -gt $adtConfig.Toolkit.LogMaxHistory)
                {
                    $ArchiveFiles | Select-Object -First ($ArchiveFiles.Count - $adtConfig.Toolkit.LogMaxHistory) | Remove-Item
                }
                Compress-Archive -LiteralPath $this.GetPropertyValue('LogTempFolder') -DestinationPath $($adtConfig.Toolkit.LogPath)\$DestinationArchiveFileName -Force
                [System.IO.Directory]::Delete($this.GetPropertyValue('LogTempFolder'), $true)
            }
            catch
            {
                Write-Host -Object "[$([System.DateTime]::Now.ToString('O'))] $($this.GetPropertyValue('InstallPhase')) :: Failed to manage archive file [$DestinationArchiveFileName].`n$(Resolve-ADTError)" -ForegroundColor Red
            }
        }

        # Reset powershell window title to its previous title.
        $Global:Host.UI.RawUI.WindowTitle = $this.Internal.OldPSWindowTitle
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.UInt32]]$Severity, [System.String]$Source, [System.String]$ScriptSection, [System.Boolean]$DebugMessage)
    {
        # Get the current config.
        $adtConfig = Get-ADTConfig

        # Perform early return checks before wasting time.
        if (($this.GetPropertyValue('DisableLogging') -and !$adtConfig.Toolkit.LogWriteToHost) -or ($DebugMessage -and !$adtConfig.Toolkit.LogDebugMessage))
        {
            return
        }

        # Establish logging date/time vars.
        $dateTimeNow = [System.DateTime]::Now
        $logTime = $dateTimeNow.ToString('HH\:mm\:ss.fff')
        $logDate = $dateTimeNow.ToString('MM-dd-yyyy')
        $logTimePlusBias = $logTime + $this.GetPropertyValue('CurrentTimeZoneBias').TotalMinutes

        # Get caller's invocation info, we'll need it for some variables.
        $caller = (Get-PSCallStack).Where({![System.String]::IsNullOrWhiteSpace($_.Command) -and ($_.Command -notmatch '^Write-(Log|ADTLogEntry)$')}, 'First')

        # Set up default values if not specified.
        if ($null -eq $Severity)
        {
            $Severity = 1
        }
        if ([System.String]::IsNullOrWhiteSpace($Source))
        {
            $Source = $caller.Command
        }
        if ([System.String]::IsNullOrWhiteSpace($ScriptSection))
        {
            $ScriptSection = $this.GetPropertyValue('InstallPhase')
        }

        # Store log string to format with message.
        $logFormats = @(
            [System.String]::Format($Script:Logging.Formats.Legacy, '{0}', $logDate, $logTime, $ScriptSection, $Source, $Script:Logging.SeverityNames[$Severity])
            [System.String]::Format($Script:Logging.Formats.CMTrace, '{0}', $ScriptSection, $logTimePlusBias, $logDate, $Source, $Severity, $caller.ScriptName)
        )

        # Store the colours we'll use against Write-Host.
        $whParams = $Script:Logging.SeverityColours[$Severity]
        $logLine = $logFormats[$adtConfig.Toolkit.LogStyle -ieq 'CMTrace']
        $conLine = $logFormats[0]
        $outFile = $this.GetPropertyValue('LogFile')
        $canLog = !$this.GetPropertyValue('DisableLogging') -and ![System.String]::IsNullOrWhiteSpace($outFile)

        # If the message is not $null or empty, create the log entry for the different logging methods.
        $Message.Where({![System.String]::IsNullOrWhiteSpace($_)}).ForEach({
            # Write the log entry to the log file if logging is not currently disabled.
            if ($canLog)
            {
                [System.String]::Format($logLine, $_) | Out-File -LiteralPath $outFile -Append -NoClobber -Force -Encoding UTF8
            }

            # Return early if we're not configured to write to the host.
            if (!$adtConfig.Toolkit.LogWriteToHost)
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

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.UInt32]]$Severity)
    {
        $this.WriteLogEntry($Message, $Severity, $null, $null, $false)
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Boolean]$DebugMessage)
    {
        $this.WriteLogEntry($Message, $null, $null, $null, $DebugMessage)
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.UInt32]]$Severity, [System.Boolean]$DebugMessage)
    {
        $this.WriteLogEntry($Message, $Severity, $null, $null, $DebugMessage)
    }

    [System.Management.Automation.PSObject[]] GetDefaultMsiExecutablesList()
    {
        return $this.Internal.DefaultMsiExecutablesList
    }

    [System.String] GetLoggedOnUserTempPath()
    {
        return $this.Internal.LoggedOnUserTempPath
    }

    [System.String] GetRegKeyDeferHistory()
    {
        return $this.Internal.RegKeyDeferHistory
    }

    [System.String] GetDeploymentTypeName()
    {
        return $this.Internal.DeploymentTypeName
    }

    [System.Void] SetBlockExecution([System.Boolean]$value)
    {
        $this.Internal.BlockExecution = $value
    }

    [System.Boolean] IsNonInteractive()
    {
        return $this.Internal.DeployModeNonInteractive
    }

    [System.Boolean] IsSilent()
    {
        return $this.Internal.DeployModeSilent
    }
}

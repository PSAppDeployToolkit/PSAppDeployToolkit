class ADTSession
{
    # Private variables for modules to use that aren't for public access.
    hidden [AllowEmptyCollection()][System.Collections.Hashtable]$ExtensionData = @{}

    # Internal variables that aren't for public access.
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$CompatibilityMode = (Test-ADTNonNativeCaller)
    hidden [ValidateNotNullOrEmpty()][System.String]$OldPSWindowTitle = $Host.UI.RawUI.WindowTitle
    hidden [ValidateNotNullOrEmpty()][PSADT.Types.ProcessObject[]]$DefaultMsiExecutablesList
    hidden [ValidateNotNullOrEmpty()][System.Management.Automation.PSVariableIntrinsics]$CallerVariables
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$RunspaceOrigin
    hidden [ValidateNotNullOrEmpty()][System.String]$LoggedOnUserTempPath
    hidden [ValidateNotNullOrEmpty()][System.String]$RegKeyDeferHistory
    hidden [ValidateNotNullOrEmpty()][System.String]$DeploymentTypeName
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$DeployModeNonInteractive
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$DeployModeSilent
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$Instantiated
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$Opened
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$Closed
    hidden [ValidateNotNullOrEmpty()][System.String]$LogPath
    hidden [ValidateNotNullOrEmpty()][System.Int32]$ExitCode

    # Deploy-Application.ps1 parameters.
    [ValidateNotNullOrEmpty()][System.String]$DeploymentType = 'Install'
    [ValidateNotNullOrEmpty()][System.String]$DeployMode = 'Interactive'
    [ValidateNotNullOrEmpty()][System.Boolean]$AllowRebootPassThru
    [ValidateNotNullOrEmpty()][System.Boolean]$DisableLogging

    # Deploy-Application.ps1 variables.
    [AllowEmptyString()][System.String]$AppVendor
    [AllowEmptyString()][System.String]$AppName
    [AllowEmptyString()][System.String]$AppVersion
    [AllowEmptyString()][System.String]$AppArch
    [AllowEmptyString()][System.String]$AppLang
    [AllowEmptyString()][System.String]$AppRevision
    [ValidateNotNullOrEmpty()][System.Int32[]]$AppExitCodes = 0
    [ValidateNotNullOrEmpty()][System.Int32[]]$AppRebootCodes = 1641, 3010
    [ValidateNotNullOrEmpty()][System.Version]$AppScriptVersion
    [ValidateNotNullOrEmpty()][System.String]$AppScriptDate
    [ValidateNotNullOrEmpty()][System.String]$AppScriptAuthor
    [ValidateNotNullOrEmpty()][System.String]$InstallName
    [ValidateNotNullOrEmpty()][System.String]$InstallTitle
    [ValidateNotNullOrEmpty()][System.String]$DeployAppScriptFriendlyName
    [ValidateNotNullOrEmpty()][System.Version]$DeployAppScriptVersion
    [ValidateNotNullOrEmpty()][System.String]$DeployAppScriptDate
    [AllowEmptyCollection()][System.Collections.Generic.Dictionary[System.String, System.Object]]$DeployAppScriptParameters
    [ValidateNotNullOrEmpty()][System.String]$InstallPhase = 'Initialization'

    # Calculated variables we publicise.
    [ValidateNotNullOrEmpty()][System.DateTime]$CurrentDateTime = [System.DateTime]::Now
    [ValidateNotNullOrEmpty()][System.String]$CurrentTime
    [ValidateNotNullOrEmpty()][System.String]$CurrentDate
    [ValidateNotNullOrEmpty()][System.TimeSpan]$CurrentTimeZoneBias
    [ValidateNotNullOrEmpty()][System.String]$ScriptDirectory
    [ValidateNotNullOrEmpty()][System.String]$DirFiles
    [ValidateNotNullOrEmpty()][System.String]$DirSupportFiles
    [AllowEmptyString()][System.String]$DefaultMsiFile
    [AllowEmptyString()][System.String]$DefaultMstFile
    [AllowEmptyCollection()][System.String[]]$DefaultMspFiles
    [ValidateNotNullOrEmpty()][System.Boolean]$UseDefaultMsi
    [ValidateNotNullOrEmpty()][System.String]$LogTempFolder
    [ValidateNotNullOrEmpty()][System.String]$LogName

    # Constructors.
    ADTSession([System.Management.Automation.SessionState]$SessionState)
    {
        $this.Init(@{SessionState = $SessionState})
    }
    ADTSession([System.Collections.Generic.Dictionary[System.String, System.Object]]$Parameters)
    {
        $this.Init($Parameters)
    }
    hidden ADTSession([System.Management.Automation.PSObject]$DeserialisedSession)
    {
        $DeserialisedSession.PSObject.Properties.Where({$_.Value -and !$_.TypeNameOfValue.EndsWith('PSVariableIntrinsics')}).ForEach({$this.($_.Name) = $_.Value})
    }

    # Private methods.
    hidden [System.Void] Init([System.Collections.IDictionary]$Parameters)
    {
        # Get the current environment.
        $adtEnv = Get-ADTEnvironment

        # Ensure this session isn't being re-instantiated.
        if ($this.Instantiated)
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The current $($adtEnv.appDeployToolkitName) session has already been instantiated.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'ADTSessionAlreadyInstantiated'
                TargetObject = $this
                TargetName = '[ADTSession]'
                TargetType = 'Init()'
                RecommendedAction = "Please review your setup to ensure this ADTSession object isn't being instantiated twice."
            }
            throw (New-ADTErrorRecord @naerParams)
        }

        # Confirm the main system automation params are present.
        foreach ($param in @('SessionState').Where({!$Parameters.ContainsKey($_)}))
        {
            $naerParams = @{
                Exception = [System.ArgumentException]::new('One or more mandatory parameters are missing.', $param)
                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                ErrorId = 'MandatoryParameterMissing'
                TargetObject = $Parameters
                TargetName = '[ADTSession]'
                TargetType = 'Init()'
                RecommendedAction = "Please review the supplied paramters to this object's constructor and try again."
            }
            throw (New-ADTErrorRecord @naerParams)
        }

        # Confirm the main system automation params aren't null.
        foreach ($param in @('SessionState').Where({!$Parameters[$_]}))
        {
            $naerParams = @{
                Exception = [System.ArgumentNullException]::new($param, 'One or more mandatory parameters are null.')
                Category = [System.Management.Automation.ErrorCategory]::InvalidData
                ErrorId = 'MandatoryParameterNullOrEmpty'
                TargetObject = $Parameters
                TargetName = '[ADTSession]'
                TargetType = 'Init()'
                RecommendedAction = "Please review the supplied paramters to this object's constructor and try again."
            }
            throw (New-ADTErrorRecord @naerParams)
        }

        # Establish start date/time first so we can accurately mark the start of execution.
        $this.CurrentTime = Get-Date -Date $this.CurrentDateTime -UFormat '%T'
        $this.CurrentDate = Get-Date -Date $this.CurrentDateTime -UFormat '%d-%m-%Y'
        $this.CurrentTimeZoneBias = [System.TimeZone]::CurrentTimeZone.GetUtcOffset($this.CurrentDateTime)

        # Process provided parameters and amend some incoming values.
        $Properties = (Get-Member -InputObject $this -MemberType Property -Force).Name
        $Parameters.GetEnumerator().Where({$Properties.Contains($_.Key) -and ![System.String]::IsNullOrWhiteSpace((Out-String -InputObject $_.Value))}).ForEach({$this.($_.Key) = $_.Value})
        $this.DeploymentType = $Global:Host.CurrentCulture.TextInfo.ToTitleCase($this.DeploymentType.ToLower())
        $this.CallerVariables = $Parameters.SessionState.PSVariable

        # Establish script directories.
        $this.ScriptDirectory = if ($rootLocation = $Parameters.SessionState.PSVariable.GetValue('PSScriptRoot', $null)) {$rootLocation} else {$PWD.Path}
        $this.DirFiles = "$($this.ScriptDirectory)\Files"
        $this.DirSupportFiles = "$($this.ScriptDirectory)\SupportFiles"

        # Set up the user temp path. When running in system context we can derive the native "C:\Users" base path from the Public environment variable.
        # This needs to be performed within the session code as we need the config up before we can process this, but the config depends on the environment being up first.
        if (($null -ne $adtEnv.RunAsActiveUser.NTAccount) -and [System.IO.Directory]::Exists($adtEnv.runasUserProfile))
        {
            $this.LoggedOnUserTempPath = [System.IO.Directory]::CreateDirectory("$($adtEnv.runasUserProfile)\ExecuteAsUser").FullName
        }
        else
        {
            $this.LoggedOnUserTempPath = [System.IO.Directory]::CreateDirectory("$((Get-ADTConfig).Toolkit.TempPath)\ExecuteAsUser").FullName
        }

        # Reflect that we've completed instantiation.
        $this.Instantiated = $true
    }

    hidden [System.Void] DetectDefaultMsi([System.Collections.Specialized.OrderedDictionary]$ADTEnv)
    {
        # If the default Deploy-Application.ps1 hasn't been modified, and the main script was not called by a referring script, check for MSI / MST and modify the install accordingly.
        if (![System.String]::IsNullOrWhiteSpace($this.AppName))
        {
            return
        }

        # Find the first MSI file in the Files folder and use that as our install.
        if (!$this.DefaultMsiFile)
        {
            # Get all MSI files and return early if we haven't found anything.
            if ($this.DefaultMsiFile = ($msiFiles = Get-ChildItem -Path "$($this.DirFiles)\*.msi" -ErrorAction Ignore) | Where-Object {$_.Name.EndsWith(".$($ADTEnv.envOSArchitecture).msi")} | Select-Object -ExpandProperty FullName -First 1)
            {
                $this.WriteLogEntry("Discovered $($ADTEnv.envOSArchitecture) Zero-Config MSI under $($this.DefaultMsiFile)")
            }
            elseif ($this.DefaultMsiFile = $msiFiles | Select-Object -ExpandProperty FullName -First 1)
            {
                $this.WriteLogEntry("Discovered Arch-Independent Zero-Config MSI under $($this.DefaultMsiFile)")
            }
            else
            {
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
            $gmtpParams = @{Path = $this.DefaultMsiFile; Table = 'File'; ContinueOnError = $false}; if ($this.DefaultMstFile) {$gmtpParams.Add('TransformPath', $this.DefaultMstFile)}
            $msiProps = Get-ADTMsiTableProperty @gmtpParams

            # Generate list of MSI executables for testing later on.
            if ($this.DefaultMsiExecutablesList = Get-Member -InputObject $msiProps | Where-Object {[System.IO.Path]::GetExtension($_.Name) -eq '.exe'} | ForEach-Object {@{Name = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)}})
            {
                $this.WriteLogEntry("MSI Executable List [$($this.DefaultMsiExecutablesList.Name)].")
            }

            # Change table and get properties from it.
            $gmtpParams.set_Item('Table', 'Property')
            $msiProps = Get-ADTMsiTableProperty @gmtpParams

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

    hidden [System.Void] SetAppProperties([System.Collections.Specialized.OrderedDictionary]$ADTEnv)
    {
        # Set up sample variables if Dot Sourcing the script, app details have not been specified
        if ([System.String]::IsNullOrWhiteSpace($this.AppName))
        {
            $this.AppName = $ADTEnv.appDeployToolkitName

            if (![System.String]::IsNullOrWhiteSpace($this.AppVendor))
            {
                $this.AppVendor = [System.String]::Empty
            }
            if ([System.String]::IsNullOrWhiteSpace($this.AppVersion))
            {
                $this.AppVersion = $ADTEnv.appDeployMainScriptVersion.ToString()
            }
            if ([System.String]::IsNullOrWhiteSpace($this.AppLang))
            {
                $this.AppLang = $ADTEnv.currentLanguage
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

    hidden [System.Void] SetInstallProperties([System.Collections.Specialized.OrderedDictionary]$ADTEnv, [System.Collections.Hashtable]$ADTConfig)
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
        $this.RegKeyDeferHistory = "$($ADTConfig.Toolkit.RegPath)\$($ADTEnv.appDeployToolkitName)\DeferHistory\$($this.InstallName)"
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

    hidden [System.Void] InitLogging([System.Collections.Specialized.OrderedDictionary]$ADTEnv, [System.Collections.Hashtable]$ADTConfig)
    {
        # Generate log paths from our installation properties.
        $this.LogTempFolder = Join-Path -Path $ADTEnv.envTemp -ChildPath "$($this.InstallName)_$($this.DeploymentType)"
        if ($ADTConfig.Toolkit.CompressLogs)
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
            $this.LogPath = [System.IO.Directory]::CreateDirectory($ADTConfig.Toolkit.LogPath).FullName
        }

        # Generate the log filename to use.
        $this.LogName = "$($this.InstallName)_$($ADTEnv.appDeployToolkitName)_$($this.DeploymentType).log"
        $logFile = [System.IO.Path]::Combine($this.LogPath, $this.LogName)

        # Check if log file needs to be rotated.
        if ([System.IO.File]::Exists($logFile) -and !$ADTConfig.Toolkit.LogAppend)
        {
            $logFileInfo = [System.IO.FileInfo]$logFile
            $logFileSizeMB = $logFileInfo.Length / 1MB

            # Rotate if we've exceeded the size already.
            if (($ADTConfig.Toolkit.LogMaxSize -gt 0) -and ($logFileSizeMB -gt $ADTConfig.Toolkit.LogMaxSize))
            {
                try
                {
                    # Get new log file path.
                    $logFileNameWithoutExtension = [IO.Path]::GetFileNameWithoutExtension($logFile)
                    $logFileExtension = [IO.Path]::GetExtension($logFile)
                    $Timestamp = $logFileInfo.LastWriteTime.ToString('yyyy-MM-dd-HH-mm-ss')
                    $ArchiveLogFileName = "{0}_{1}{2}" -f $logFileNameWithoutExtension, $Timestamp, $logFileExtension
                    [String]$ArchiveLogFilePath = Join-Path -Path $this.LogPath -ChildPath $ArchiveLogFileName

                    # Log message about archiving the log file.
                    $this.WriteLogEntry("Maximum log file size [$($ADTConfig.Toolkit.LogMaxSize) MB] reached. Rename log file to [$ArchiveLogFileName].", 2)

                    # Rename the file
                    Move-Item -LiteralPath $logFileInfo.FullName -Destination $ArchiveLogFilePath -Force

                    # Start new log file and log message about archiving the old log file.
                    $this.WriteLogEntry("Previous log file was renamed to [$ArchiveLogFileName] because maximum log file size of [$($ADTConfig.Toolkit.LogMaxSize) MB] was reached.", 2)

                    # Get all log files (including any .lo_ files that may have been created by previous toolkit versions) sorted by last write time
                    $logFiles = $(Get-ChildItem -LiteralPath $this.LogPath -Filter ("{0}_*{1}" -f $logFileNameWithoutExtension, $logFileExtension); Get-Item -LiteralPath ([IO.Path]::ChangeExtension($logFile, 'lo_')) -ErrorAction Ignore) | Sort-Object -Property LastWriteTime

                    # Keep only the max number of log files
                    if ($logFiles.Count -gt $ADTConfig.Toolkit.LogMaxHistory)
                    {
                        $logFiles | Select-Object -First ($logFiles.Count - $ADTConfig.Toolkit.LogMaxHistory) | Remove-Item
                    }
                }
                catch
                {
                    Write-Host -Object "[$([System.DateTime]::Now.ToString('O'))] $($this.InstallPhase) :: Failed to rotate the log file [$($logFile)].`n$(Resolve-ADTError)" -ForegroundColor Red
                }
            }
        }

        # Open log file with commencement message.
        $this.WriteLogDivider(2)
        $this.WriteLogEntry("[$($this.InstallName)] setup started.")
    }

    hidden [System.Void] LogScriptInfo([System.Collections.Specialized.OrderedDictionary]$ADTEnv)
    {
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
        if ($this.DeployAppScriptParameters -and $this.DeployAppScriptParameters.Count)
        {
            $this.WriteLogEntry("The following parameters were passed to [$($this.DeployAppScriptFriendlyName)]: [$($this.DeployAppScriptParameters | Resolve-ADTBoundParameters)]")
        }
        $this.WriteLogEntry("[$($ADTEnv.appDeployToolkitName)] module version is [$($ADTEnv.appDeployMainScriptVersion)]")

        # Announce session instantiation mode.
        if ($this.CompatibilityMode)
        {
            $this.WriteLogEntry("[$($ADTEnv.appDeployToolkitName)] session mode is [Compatibility]. This mode is for the transition of v3.x scripts and is not for new development.", 2)
            $this.WriteLogEntry("Information on how to migrate this script to Native mode is available at [https://psappdeploytoolkit.com/].", 2)
            return
        }
        $this.WriteLogEntry("[$($ADTEnv.appDeployToolkitName)] session mode is [Native].")
    }

    hidden [System.Void] LogSystemInfo([System.Collections.Specialized.OrderedDictionary]$ADTEnv)
    {
        # Report on all determined system info.
        $this.WriteLogEntry("Computer Name is [$($ADTEnv.envComputerNameFQDN)]")
        $this.WriteLogEntry("Current User is [$($ADTEnv.ProcessNTAccount)]")
        $this.WriteLogEntry("OS Version is [$($ADTEnv.envOSName)$(if ($ADTEnv.envOSServicePack) {" $($ADTEnv.envOSServicePack)"}) $($ADTEnv.envOSArchitecture) $($ADTEnv.envOSVersion)]")
        $this.WriteLogEntry("OS Type is [$($ADTEnv.envOSProductTypeName)]")
        $this.WriteLogEntry("Current Culture is [$($ADTEnv.culture.Name)], language is [$($ADTEnv.currentLanguage)] and UI language is [$($ADTEnv.currentUILanguage)]")
        $this.WriteLogEntry("Hardware Platform is [$($ADTEnv.envHardwareType)]")
        $this.WriteLogEntry("PowerShell Host is [$($Global:Host.Name)] with version [$($Global:Host.Version)]")
        $this.WriteLogEntry("PowerShell Version is [$($ADTEnv.envPSVersion) $($ADTEnv.psArchitecture)]")
        if ($ADTEnv.envCLRVersion)
        {
            $this.WriteLogEntry("PowerShell CLR (.NET) version is [$($ADTEnv.envCLRVersion)]")
        }
    }

    hidden [System.Void] LogUserInfo([System.Collections.Specialized.OrderedDictionary]$ADTEnv, [System.Collections.Hashtable]$ADTConfig)
    {
        # Log details for all currently logged in users.
        $this.WriteLogEntry("Display session information for all logged on users:`n$($ADTEnv.LoggedOnUserSessions | Format-List | Out-String)", $true)

        # Provide detailed info about current process state.
        if ($ADTEnv.usersLoggedOn)
        {
            $this.WriteLogEntry("The following users are logged on to the system: [$($ADTEnv.usersLoggedOn -join ', ')].")

            # Check if the current process is running in the context of one of the logged in users
            if ($ADTEnv.CurrentLoggedOnUserSession)
            {
                $this.WriteLogEntry("Current process is running with user account [$($ADTEnv.ProcessNTAccount)] under logged in user session for [$($ADTEnv.CurrentLoggedOnUserSession.NTAccount)].")
            }
            else
            {
                $this.WriteLogEntry("Current process is running under a system account [$($ADTEnv.ProcessNTAccount)].")
            }

            # Guard Intune detection code behind a variable.
            if ($ADTConfig.Toolkit.OobeDetection -and ![PSADT.Utilities]::OobeCompleted())
            {
                $this.WriteLogEntry("Detected OOBE in progress, changing deployment mode to silent.")
                $this.DeployMode = 'Silent'
            }

            # Display account and session details for the account running as the console user (user with control of the physical monitor, keyboard, and mouse)
            if ($ADTEnv.CurrentConsoleUserSession)
            {
                $this.WriteLogEntry("The following user is the console user [$($ADTEnv.CurrentConsoleUserSession.NTAccount)] (user with control of physical monitor, keyboard, and mouse).")
            }
            else
            {
                $this.WriteLogEntry('There is no console user logged in (user with control of physical monitor, keyboard, and mouse).')
            }

            # Display the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account
            if ($ADTEnv.RunAsActiveUser)
            {
                $this.WriteLogEntry("The active logged on user is [$($ADTEnv.RunAsActiveUser.NTAccount)].")
            }
        }
        else
        {
            $this.WriteLogEntry('No users are logged on to the system.')
        }

        # Log which language's UI messages are loaded from the config file
        $this.WriteLogEntry("The current execution context has a primary UI language of [$($ADTEnv.currentLanguage)].")

        # Advise whether the UI language was overridden.
        if ($ADTConfig.UI.LanguageOverride)
        {
            $this.WriteLogEntry("The config file was configured to override the detected primary UI language with the following UI language: [$($ADTConfig.UI.LanguageOverride)].")
        }
        $this.WriteLogEntry("The following UI messages were imported from the config file: [$((Get-ADTModuleData).Language)].")
    }

    hidden [System.Void] PerformSCCMTests([System.Collections.Specialized.OrderedDictionary]$ADTEnv)
    {
        # Check if script is running from a SCCM Task Sequence.
        if ($ADTEnv.RunningTaskSequence)
        {
            $this.WriteLogEntry('Successfully found COM object [Microsoft.SMS.TSEnvironment]. Therefore, script is currently running from a SCCM Task Sequence.')
        }
        else
        {
            $this.WriteLogEntry('Unable to find COM object [Microsoft.SMS.TSEnvironment]. Therefore, script is not currently running from a SCCM Task Sequence.')
        }
    }

    hidden [System.Void] PerformSystemAccountTests([System.Collections.Specialized.OrderedDictionary]$ADTEnv, [System.Collections.Hashtable]$ADTConfig)
    {
        # Check to see if the Task Scheduler service is in a healthy state by checking its services to see if they exist, are currently running, and have a start mode of 'Automatic'.
        # The task scheduler service and the services it is dependent on can/should only be started/stopped/modified when running in the SYSTEM context.
        if ($ADTEnv.IsLocalSystemAccount)
        {
            $this.WriteLogEntry("The task scheduler service is in a healthy state: $($ADTEnv.IsTaskSchedulerHealthy).")
        }
        else
        {
            $this.WriteLogEntry("Skipping attempt to check for and make the task scheduler services healthy, because $($ADTEnv.appDeployToolkitName) is not running under the [$($ADTEnv.LocalSystemNTAccount)] account.")
        }

        # If script is running in session zero.
        if ($ADTEnv.SessionZero)
        {
            # If the script was launched with deployment mode set to NonInteractive, then continue
            if ($this.DeployMode -eq 'NonInteractive')
            {
                $this.WriteLogEntry("Session 0 detected but deployment mode was manually set to [$($this.DeployMode)].")
            }
            elseif ($ADTConfig.Toolkit.SessionDetection)
            {
                # If the process is not able to display a UI, enable NonInteractive mode
                if (!$ADTEnv.IsProcessUserInteractive)
                {
                    $this.DeployMode = 'NonInteractive'
                    $this.WriteLogEntry("Session 0 detected, process not running in user interactive mode; deployment mode set to [$($this.DeployMode)].")
                }
                elseif (!$ADTEnv.usersLoggedOn)
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
        $this.WriteLogEntry("Deployment type is [$(($this.DeploymentTypeName = (Get-ADTStrings).DeploymentType.($this.DeploymentType)))].")
    }

    hidden [System.Void] TestDefaultMsi()
    {
        # Advise the caller if a zero-config MSI was found.
        if ($this.UseDefaultMsi)
        {
            $this.WriteLogEntry("Discovered Zero-Config MSI installation file [$($this.DefaultMsiFile)].")
        }
    }

    hidden [System.Void] TestAdminRequired([System.Collections.Specialized.OrderedDictionary]$ADTEnv, [System.Collections.Hashtable]$ADTConfig)
    {
        # Check current permissions and exit if not running with Administrator rights.
        if ($ADTConfig.Toolkit.RequireAdmin -and !$ADTEnv.IsAdmin)
        {
            $naerParams = @{
                Exception = [System.UnauthorizedAccessException]::new("[$($ADTEnv.appDeployToolkitName)] has a toolkit config option [RequireAdmin] set to [True] and the current user is not an Administrator, or PowerShell is not elevated. Please re-run the deployment script as an Administrator or change the option in the config file to not require Administrator rights.")
                Category = [System.Management.Automation.ErrorCategory]::PermissionDenied
                ErrorId = 'CallerNotLocalAdmin'
                TargetObject = $this
                TargetName = '[ADTSession]'
                TargetType = 'TestAdminRequired()'
                RecommendedAction = "Please review the executing user's permissions or the supplied config and try again."
            }
            $this.WriteLogEntry($naerParams.Exception.Message, 3)
            Show-ADTDialogBox -Text $naerParams.Exception.Message -Icon Stop
            throw (New-ADTErrorRecord @naerParams)
        }
    }

    # Public methods.
    [System.Object] GetPropertyValue([System.String]$Name)
    {
        # This getter exists as once the object is opened, we need to read the variable from the caller's scope.
        # We must get the variable every time as syntax like `$var = 'val'` always constructs a new PSVariable...
        if ($this.CompatibilityMode -and $this.Opened)
        {
            return $this.CallerVariables.Get($Name).Value
        }
        else
        {
            return $this.$Name
        }
    }

    [System.Void] SetPropertyValue([System.String]$Name, [System.Object]$Value)
    {
        # This getter exists as once the object is opened, we need to read the variable from the caller's scope.
        # We must get the variable every time as syntax like `$var = 'val'` always constructs a new PSVariable...
        if ($this.CompatibilityMode -and $this.Opened)
        {
            $this.CallerVariables.Set($Name, $Value)
        }
        else
        {
            $this.$Name = $Value
        }
    }

    [System.Void] SyncPropertyValues()
    {
        # This is ran ahead of an async operation for compatibility mode operations to ensure the module has the current state.
        if (!$this.CompatibilityMode -or !$this.Opened)
        {
            return
        }
        $this.PSObject.Properties.Name.ForEach({if ($value = $this.CallerVariables.Get($_).Value) {$this.$_ = $value}})
    }

    [System.String] GetDeploymentStatus()
    {
        if (($this.ExitCode -eq ($adtConfig = Get-ADTConfig).UI.DefaultExitCode) -or ($this.ExitCode -eq $adtConfig.UI.DeferExitCode))
        {
            return 'FastRetry'
        }
        elseif ($this.GetPropertyValue('AppRebootCodes').Contains($this.ExitCode))
        {
            return 'RestartRequired'
        }
        elseif ($this.GetPropertyValue('AppExitCodes').Contains($this.ExitCode))
        {
            return 'Complete'
        }
        else
        {
            return 'Error'
        }
    }

    hidden [System.Void] Open()
    {
        # Get the current environment.
        $adtEnv = Get-ADTEnvironment

        # Ensure this session isn't being opened twice.
        if ($this.Opened)
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The current $($adtEnv.appDeployToolkitName) session has already been opened.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'ADTSessionAlreadyOpened'
                TargetObject = $this
                TargetName = '[ADTSession]'
                TargetType = 'Open()'
                RecommendedAction = "Please review your setup to ensure this ADTSession object isn't being opened again."
            }
            throw (New-ADTErrorRecord @naerParams)
        }

        # Get the current config.
        $adtConfig = Get-ADTConfig

        # Initialise PSADT session.
        $this.DetectDefaultMsi($adtEnv)
        $this.SetAppProperties($adtEnv)
        $this.SetInstallProperties($adtEnv, $adtConfig)
        $this.InitLogging($adtEnv, $adtConfig)
        $this.LogScriptInfo($adtEnv)
        $this.LogSystemInfo($adtEnv)
        $this.WriteLogDivider()
        $this.LogUserInfo($adtEnv, $adtConfig)
        $this.PerformSCCMTests($adtEnv)
        $this.PerformSystemAccountTests($adtEnv, $adtConfig)
        $this.SetDeploymentProperties()
        $this.TestDefaultMsi()
        $this.TestAdminRequired($adtEnv, $adtConfig)

        # Change the install phase since we've finished initialising. This should get overwritten shortly.
        $this.InstallPhase = 'Execution'

        # Export session's public variables to the user's scope. For these, we can't capture the Set-Variable
        # PassThru data as syntax like `$var = 'val'` constructs a new PSVariable every time.
        if ($this.CompatibilityMode)
        {
            $this.PSObject.Properties.ForEach({$this.CallerVariables.Set($_.Name, $_.Value)})
        }

        # Set PowerShell window title and reflect that we've completed initialisation. This is important for variable retrieval.
        $Global:Host.UI.RawUI.WindowTitle = "$($this.InstallTitle) - $($this.DeploymentType)" -replace '\s{2,}',' '
        $this.Opened = $true
    }

    hidden [System.Void] Close()
    {
        # Get the current environment.
        $adtEnv = Get-ADTEnvironment

        # Ensure this session isn't being closed twice.
        if ($this.Closed)
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The current $($adtEnv.appDeployToolkitName) session has already been closed.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'ADTSessionAlreadyClosed'
                TargetObject = $this
                TargetName = '[ADTSession]'
                TargetType = 'Close()'
                RecommendedAction = "Please review your setup to ensure this ADTSession object isn't being closed again."
            }
            throw (New-ADTErrorRecord @naerParams)
        }

        # Get the current config and strings.
        $adtConfig = Get-ADTConfig
        $adtStrings = Get-ADTStrings

        # Process resulting exit code.
        switch ($this.GetDeploymentStatus())
        {
            FastRetry {
                # Just advise of the exit code with the appropriate severity.
                $this.WriteLogEntry("$($this.GetPropertyValue('InstallName')) $($this.GetDeploymentTypeName().ToLower()) completed with exit code [$($this.ExitCode)].", 2)
                break
            }
            Error {
                # Just advise of the exit code with the appropriate severity.
                $this.WriteLogEntry("$($this.GetPropertyValue('InstallName')) $($this.GetDeploymentTypeName().ToLower()) completed with exit code [$($this.ExitCode)].", 3)
                break
            }
            default {
                # Clean up app deferral history.
                if (Test-Path -LiteralPath $this.RegKeyDeferHistory)
                {
                    $this.WriteLogEntry('Removing deferral history...')
                    Remove-ADTRegistryKey -Key $this.RegKeyDeferHistory -Recurse
                }

                # Handle reboot prompts on successful script completion.
                if ($_.Equals('RestartRequired') -and $this.GetPropertyValue('AllowRebootPassThru'))
                {
                    $this.WriteLogEntry('A restart has been flagged as required.')
                }
                else
                {
                    $this.ExitCode = 0
                }
                $this.WriteLogEntry("$($this.GetPropertyValue('InstallName')) $($this.GetDeploymentTypeName().ToLower()) completed with exit code [$($this.ExitCode)].", 0)
                break
            }
        }

        # Update the module's last tracked exit code.
        if ($this.ExitCode)
        {
            (Get-ADTModuleData).LastExitCode = $this.ExitCode
        }

        # Write out a log divider to indicate the end of logging.
        $this.WriteLogEntry('-' * 79)
        $this.SetPropertyValue('DisableLogging', $true)
        $Global:Host.UI.RawUI.WindowTitle = $this.OldPSWindowTitle
        $this.Closed = $true

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
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.UInt32]]$Severity, [System.String]$Source, [System.String]$ScriptSection, [System.Boolean]$DebugMessage, [System.String]$LogType, [System.String]$LogFileDirectory, [System.String]$LogFileName)
    {
        # Get the current config.
        $adtConfig = Get-ADTConfig

        # Perform early return checks before wasting time.
        if (($this.GetPropertyValue('DisableLogging') -and !$adtConfig.Toolkit.LogWriteToHost) -or ($DebugMessage -and !$adtConfig.Toolkit.LogDebugMessage))
        {
            return
        }

        # Establish logging date/time vars.
        $dateNow = [System.DateTime]::Now
        $logTime = $dateNow.ToString('HH\:mm\:ss.fff')
        $logDate = $dateNow.ToString('MM-dd-yyyy')
        $logTimePlusBias = $logTime + $this.GetPropertyValue('CurrentTimeZoneBias').TotalMinutes

        # Get caller's invocation info, we'll need it for some variables.
        $caller = Get-PSCallStack | Where-Object {![System.String]::IsNullOrWhiteSpace($_.Command) -and ($_.Command -notmatch '^Write-(Log|ADTLogEntry)$')} | Select-Object -First 1

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
        if ([System.String]::IsNullOrWhiteSpace($LogType))
        {
            $LogType = $adtConfig.Toolkit.LogStyle
        }
        if ([System.String]::IsNullOrWhiteSpace($LogFileDirectory))
        {
            $LogFileDirectory = $this.LogPath
        }
        elseif (!(Test-Path -LiteralPath $LogFileDirectory -PathType Container))
        {
            [System.Void](New-Item -Path $LogFileDirectory -Type Directory -Force)
        }
        if ([System.String]::IsNullOrWhiteSpace($LogFileName))
        {
            $LogFileName = $this.GetPropertyValue('LogName')
        }

        # Store log string to format with message.
        $logFormats = @(
            [System.String]::Format($Script:Logging.Formats.Legacy, '{0}', $logDate, $logTime, $ScriptSection, $Source, $Script:Logging.SeverityNames[$Severity])
            [System.String]::Format($Script:Logging.Formats.CMTrace, '{0}', $ScriptSection, $logTimePlusBias, $logDate, $Source, $Severity, $caller.ScriptName)
        )

        # Store the colours we'll use against Write-Host.
        $whParams = $Script:Logging.SeverityColours[$Severity]
        $logLine = $logFormats[$LogType -ieq 'CMTrace']
        $conLine = $logFormats[0]
        $outFile = [System.IO.Path]::Combine($LogFileDirectory, $LogFileName)
        $canLog = !$this.GetPropertyValue('DisableLogging') -and ![System.String]::IsNullOrWhiteSpace($outFile)

        # If the message is not $null or empty, create the log entry for the different logging methods.
        foreach ($msg in $Message.Where({![System.String]::IsNullOrWhiteSpace($_)}))
        {
            # Write the log entry to the log file if logging is not currently disabled.
            if ($canLog)
            {
                Out-File -InputObject ([System.String]::Format($logLine, $msg)) -LiteralPath $outFile -Append -NoClobber -Force -Encoding UTF8
            }

            # Only write to host if we're configured to do so.
            if ($adtConfig.Toolkit.LogWriteToHost)
            {
                # Only output using color options if running in a host which supports colors.
                if ($Global:Host.UI.RawUI.ForegroundColor)
                {
                    Write-Host -Object ([System.String]::Format($conLine, $msg)) @whParams
                }
                else
                {
                    # If executing "powershell.exe -File <filename>.ps1 > log.txt", then all the Write-Host calls are sent to stdout so that they are included in the text log.
                    [System.Console]::WriteLine([System.String]::Format($conLine, $msg))
                }
            }
        }
    }

    [System.Void] WriteLogEntry([System.String[]]$Message)
    {
        $this.WriteLogEntry($Message, $null, $null, $null, $false, $null, $null, $null)
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.UInt32]]$Severity)
    {
        $this.WriteLogEntry($Message, $Severity, $null, $null, $false, $null, $null, $null)
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Boolean]$DebugMessage)
    {
        $this.WriteLogEntry($Message, $null, $null, $null, $DebugMessage, $null, $null, $null)
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.UInt32]]$Severity, [System.Boolean]$DebugMessage)
    {
        $this.WriteLogEntry($Message, $Severity, $null, $null, $DebugMessage, $null, $null, $null)
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.UInt32]]$Severity, [System.String]$Source, [System.String]$ScriptSection, [System.Boolean]$DebugMessage)
    {
        $this.WriteLogEntry($Message, $Severity, $Source, $ScriptSection, $DebugMessage, $null, $null, $null)
    }

    [PSADT.Types.ProcessObject[]] GetDefaultMsiExecutablesList()
    {
        return $this.DefaultMsiExecutablesList
    }

    [System.String] GetLoggedOnUserTempPath()
    {
        return $this.LoggedOnUserTempPath
    }

    [System.String] GetDeploymentTypeName()
    {
        return $this.DeploymentTypeName
    }

    [System.Boolean] IsNonInteractive()
    {
        return $this.DeployModeNonInteractive
    }

    [System.Boolean] IsSilent()
    {
        return $this.DeployModeSilent
    }

    [System.Void] SetExitCode([System.Int32]$Value)
    {
        $this.ExitCode = $Value
    }
}

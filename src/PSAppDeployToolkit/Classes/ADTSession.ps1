#-----------------------------------------------------------------------------
#
# MARK: ADTSession
#
#-----------------------------------------------------------------------------

class ADTSession
{
    # Internal variables that aren't for public access.
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$CompatibilityMode
    hidden [ValidateNotNullOrEmpty()][System.Management.Automation.PSVariableIntrinsics]$CallerVariables
    hidden [AllowEmptyCollection()][System.Collections.Generic.List[System.IO.FileInfo]]$MountedWimFiles = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    hidden [AllowEmptyCollection()][System.Collections.Generic.List[PSADT.Types.LogEntry]]$LogBuffer = [System.Collections.Generic.List[PSADT.Types.LogEntry]]::new()
    hidden [ValidateNotNullOrEmpty()][PSADT.Types.ProcessObject[]]$DefaultMsiExecutablesList
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$ZeroConfigInitiated
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$RunspaceOrigin
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$ForceWimDetection
    hidden [ValidateNotNullOrEmpty()][System.String]$DirFilesSubstDrive
    hidden [ValidateNotNullOrEmpty()][System.String]$RegKeyDeferHistory
    hidden [ValidateNotNullOrEmpty()][System.String]$DeploymentTypeName
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$DeployModeNonInteractive
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$DeployModeSilent
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$Instantiated
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$Opened
    hidden [ValidateNotNullOrEmpty()][System.Boolean]$Closed
    hidden [ValidateNotNullOrEmpty()][System.String]$LogPath
    hidden [ValidateNotNullOrEmpty()][System.Int32]$ExitCode

    # Frontend parameters.
    [ValidateSet('Install', 'Uninstall', 'Repair')][System.String]$DeploymentType = 'Install'
    [ValidateSet('Interactive', 'NonInteractive', 'Silent')][System.String]$DeployMode = 'Interactive'
    [ValidateNotNullOrEmpty()][System.Boolean]$AllowRebootPassThru
    [ValidateNotNullOrEmpty()][System.Boolean]$TerminalServerMode
    [ValidateNotNullOrEmpty()][System.Boolean]$DisableLogging

    # Frontend variables.
    [AllowEmptyString()][System.String]$AppVendor
    [AllowEmptyString()][System.String]$AppName
    [AllowEmptyString()][System.String]$AppVersion
    [AllowEmptyString()][System.String]$AppArch
    [AllowEmptyString()][System.String]$AppLang
    [AllowEmptyString()][System.String]$AppRevision
    [ValidateNotNullOrEmpty()][System.Int32[]]$AppSuccessExitCodes = 0
    [ValidateNotNullOrEmpty()][System.Int32[]]$AppRebootExitCodes = 1641, 3010
    [ValidateNotNullOrEmpty()][System.Version]$AppScriptVersion
    [ValidateNotNullOrEmpty()][System.DateTime]$AppScriptDate
    [ValidateNotNullOrEmpty()][System.String]$AppScriptAuthor
    [ValidateNotNullOrEmpty()][System.String]$InstallName
    [ValidateNotNullOrEmpty()][System.String]$InstallTitle
    [ValidateNotNullOrEmpty()][System.String]$DeployAppScriptFriendlyName
    [ValidateNotNullOrEmpty()][System.Version]$DeployAppScriptVersion
    [ValidateNotNullOrEmpty()][System.DateTime]$DeployAppScriptDate
    [ValidateNotNullOrEmpty()][System.Collections.IDictionary]$DeployAppScriptParameters
    [ValidateNotNullOrEmpty()][System.String]$InstallPhase = 'Initialization'

    # Calculated variables we publicize.
    [ValidateNotNullOrEmpty()][System.DateTime]$CurrentDateTime = [System.DateTime]::Now
    [ValidateNotNullOrEmpty()][System.String]$CurrentTime
    [ValidateNotNullOrEmpty()][System.String]$CurrentDate
    [ValidateNotNullOrEmpty()][System.TimeSpan]$CurrentTimeZoneBias
    [ValidateNotNullOrEmpty()][System.String]$ScriptDirectory
    [ValidateNotNullOrEmpty()][System.String]$DirFiles
    [ValidateNotNullOrEmpty()][System.String]$DirSupportFiles
    [ValidateNotNullOrEmpty()][System.String]$DefaultMsiFile
    [ValidateNotNullOrEmpty()][System.String]$DefaultMstFile
    [ValidateNotNullOrEmpty()][System.String[]]$DefaultMspFiles
    [ValidateNotNullOrEmpty()][System.Boolean]$UseDefaultMsi
    [ValidateNotNullOrEmpty()][System.String]$LogTempFolder
    [ValidateNotNullOrEmpty()][System.String]$LogName

    # Constructors.
    ADTSession([System.Management.Automation.SessionState]$SessionState)
    {
        $this.Init(@{ SessionState = $SessionState })
    }
    ADTSession([System.Collections.Generic.Dictionary[System.String, System.Object]]$Parameters)
    {
        $this.Init($Parameters)
    }

    # Private methods.
    hidden [System.Void] TestClassState([System.String]$State)
    {
        # Throw if the specified state is true.
        if ($this.$State)
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("The current $($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name) session has already been $($State.ToLower()).")
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = "ADTSessionAlready$State"
                TargetObject = $this
                TargetName = '[ADTSession]'
                TargetType = $State
                RecommendedAction = "Please review your setup to ensure this ADTSession object isn't being $($State.ToLower()) twice."
            }
            throw (New-ADTErrorRecord @naerParams)
        }
    }

    hidden [System.Void] Init([System.Collections.IDictionary]$Parameters)
    {
        # Ensure this session isn't being re-instantiated.
        $this.TestClassState('Instantiated')
        $this.TestClassState('Opened')
        $this.TestClassState('Closed')

        # Confirm the main system automation params are present.
        'SessionState' | & {
            process
            {
                if (!$Parameters.ContainsKey($_))
                {
                    $naerParams = @{
                        Exception = [System.ArgumentException]::new('One or more mandatory parameters are missing.', $_)
                        Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                        ErrorId = 'MandatoryParameterMissing'
                        TargetObject = $Parameters
                        TargetName = '[ADTSession]'
                        TargetType = 'Init()'
                        RecommendedAction = "Please review the supplied parameters to this object's constructor and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }
                if (!$Parameters.$_)
                {
                    $naerParams = @{
                        Exception = [System.ArgumentNullException]::new($_, 'One or more mandatory parameters are null.')
                        Category = [System.Management.Automation.ErrorCategory]::InvalidData
                        ErrorId = 'MandatoryParameterNullOrEmpty'
                        TargetObject = $Parameters
                        TargetName = '[ADTSession]'
                        TargetType = 'Init()'
                        RecommendedAction = "Please review the supplied parameters to this object's constructor and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }
            }
        }

        # Establish start date/time first so we can accurately mark the start of execution.
        $this.CurrentTime = Get-Date -Date $this.CurrentDateTime -UFormat '%T'
        $this.CurrentDate = Get-Date -Date $this.CurrentDateTime -UFormat '%d-%m-%Y'
        $this.CurrentTimeZoneBias = [System.TimeZone]::CurrentTimeZone.GetUtcOffset($this.CurrentDateTime)

        # Process provided parameters and amend some incoming values.
        $Properties = (Get-Member -InputObject $this -MemberType Property -Force).Name
        $Parameters.GetEnumerator() | & { process { if ($Properties.Contains($_.Key) -and ![System.String]::IsNullOrWhiteSpace((Out-String -InputObject $_.Value))) { $this.($_.Key) = $_.Value } } }
        $this.DeploymentType = $Global:Host.CurrentCulture.TextInfo.ToTitleCase($this.DeploymentType.ToLower())
        $this.CallerVariables = $Parameters.SessionState.PSVariable

        # Establish script directories before returning.
        'DirFiles', 'DirSupportFiles' | & {
            process
            {
                if ([System.String]::IsNullOrWhiteSpace($this.$_))
                {
                    $dir = "$($this.ScriptDirectory)\$($_ -replace '^Dir')"
                    if ([System.IO.Directory]::Exists($dir))
                    {
                        $this.$_ = $dir
                    }
                }
            }
        }
        $this.Instantiated = $true
    }

    hidden [System.Void] WriteZeroConfigDivider()
    {
        # Print an extra divider when we process a Zero-Config setup before the main logging starts.
        if (!$this.ZeroConfigInitiated)
        {
            $this.WriteLogDivider(2)
            $this.ZeroConfigInitiated = $true
        }
    }

    hidden [System.Void] DetectDefaultWimFile()
    {
        # If the default frontend hasn't been modified, and there's not already a mounted WIM file, check for WIM files and modify the install accordingly.
        if (![System.String]::IsNullOrWhiteSpace($this.AppName) -and !$this.ForceWimDetection)
        {
            return
        }

        # If there's already a mounted WIM file, return early.
        if ($this.MountedWimFiles.Count)
        {
            return
        }

        # Find the first WIM file in the Files folder and use that as our install.
        if (!($wimFile = Get-ChildItem -Path "$($this.DirFiles)\*.wim" -ErrorAction Ignore | Select-Object -ExpandProperty FullName -First 1))
        {
            return
        }

        # Mount the WIM file and reset DirFiles to the mount point.
        $this.WriteZeroConfigDivider()
        $this.WriteLogEntry("Discovered Zero-Config WIM file [$wimFile].")
        $mountPath = [System.IO.Path]::Combine($this.DirFiles, [System.IO.Path]::GetRandomFileName())
        Mount-ADTWimFile -ImagePath $wimFile -Path $mountPath -Index 1 6>$null
        $this.WriteLogEntry("Successfully mounted WIM file to [$(($this.DirFiles = $mountPath))].")

        # Subst the new DirFiles path to eliminate any potential path length issues.
        if (($availLetter = [System.String[]][System.Char[]](90..65) | & { begin { $usedLetters = (Get-PSDrive -PSProvider FileSystem).Name } process { if ($usedLetters -notcontains $_) { return $_ } } } | Select-Object -First 1))
        {
            $this.WriteLogEntry("Creating substitution drive [$(($substDrive = "${availLetter}:"))] for [$($this.DirFiles)].")
            Invoke-ADTSubstOperation -Drive $substDrive -Path $this.DirFiles 6>$null
            $this.DirFiles = $this.DirFilesSubstDrive = $substDrive
        }

        $this.WriteLogEntry("Using [$($this.DirFiles)] as the base DirFiles directory.")
    }

    hidden [System.Void] DetectDefaultMsi([System.Collections.Specialized.OrderedDictionary]$ADTEnv)
    {
        # If the default frontend hasn't been modified, check for MSI / MST and modify the install accordingly.
        if (![System.String]::IsNullOrWhiteSpace($this.AppName))
        {
            return
        }

        # Find the first MSI file in the Files folder and use that as our install.
        if ([System.String]::IsNullOrWhiteSpace($this.DefaultMsiFile))
        {
            # Get all MSI files and return early if we haven't found anything.
            if (($msiFile = ($msiFiles = Get-ChildItem -Path "$($this.DirFiles)\*.msi" -ErrorAction Ignore) | & { process { if ($_.Name.EndsWith(".$($ADTEnv.envOSArchitecture).msi")) { return $_ } } } | Select-Object -ExpandProperty FullName -First 1))
            {
                $this.WriteZeroConfigDivider()
                $this.WriteLogEntry("Discovered $($ADTEnv.envOSArchitecture) Zero-Config MSI under $(($this.DefaultMsiFile = $msiFile))")
            }
            elseif (($msiFile = $msiFiles | Select-Object -ExpandProperty FullName -First 1))
            {
                $this.WriteZeroConfigDivider()
                $this.WriteLogEntry("Discovered Arch-Independent Zero-Config MSI under $(($this.DefaultMsiFile = $msiFile))")
            }
            else
            {
                return
            }
        }
        elseif (![System.IO.Path]::IsPathRooted($this.DefaultMsiFile))
        {
            $this.WriteZeroConfigDivider()
            $this.WriteLogEntry("Discovered Zero-Config MSI installation file [$(($this.DefaultMsiFile = [System.IO.Path]::Combine($this.DirFiles, $this.DefaultMsiFile)))].")
        }
        else
        {
            $this.WriteZeroConfigDivider()
            $this.WriteLogEntry("Discovered Zero-Config MSI installation file [$($this.DefaultMsiFile)].")
        }

        # Discover if there is a zero-config MST file.
        if ([System.String]::IsNullOrWhiteSpace($this.DefaultMstFile))
        {
            if ([System.IO.File]::Exists(($mstFile = [System.IO.Path]::ChangeExtension($this.DefaultMsiFile, 'mst'))))
            {
                $this.DefaultMstFile = $mstFile
            }
        }
        elseif (![System.IO.Path]::IsPathRooted($this.DefaultMstFile))
        {
            $this.DefaultMstFile = [System.IO.Path]::Combine($this.DirFiles, $this.DefaultMstFile)
        }
        if (![System.String]::IsNullOrWhiteSpace($this.DefaultMstFile))
        {
            $this.WriteLogEntry("Discovered Zero-Config MST installation file [$($this.DefaultMstFile)].")
        }

        # Discover if there are zero-config MSP files. Name multiple MSP files in alphabetical order to control order in which they are installed.
        if (!$this.DefaultMspFiles)
        {
            if (($mspFiles = Get-ChildItem -Path "$($this.DirFiles)\*.msp" | Select-Object -ExpandProperty FullName))
            {
                $this.DefaultMspFiles = $mspFiles
            }
        }
        elseif ($this.DefaultMspFiles | & { process { if (![System.IO.Path]::IsPathRooted($_)) { return $_ } } } | Select-Object -First 1)
        {
            $this.DefaultMspFiles = $this.DefaultMspFiles | & { process { if (![System.IO.Path]::IsPathRooted($_)) { return [System.IO.Path]::Combine($this.DirFiles, $_) } else { return $_ } } }
        }
        if ($this.DefaultMspFiles)
        {
            $this.WriteLogEntry("Discovered Zero-Config MSP installation file(s) [$($this.DefaultMspFiles -join ',')].")
        }

        # Read the MSI and get the installation details.
        $gmtpParams = @{ Path = $this.DefaultMsiFile }; if ($this.DefaultMstFile) { $gmtpParams.Add('TransformPath', $this.DefaultMstFile) }
        $msiProps = Get-ADTMsiTableProperty @gmtpParams -Table File 6>$null

        # Generate list of MSI executables for testing later on.
        if (($msiProcs = $msiProps | Get-Member -MemberType NoteProperty | & { process { if ([System.IO.Path]::GetExtension($_.Name) -eq '.exe') { [PSADT.Types.ProcessObject]::new([System.IO.Path]::GetFileNameWithoutExtension($_.Name) -replace '^_') } } }))
        {
            $this.WriteLogEntry("MSI Executable List [$(($this.DefaultMsiExecutablesList = $msiProcs).Name)].")
        }

        # Update our app variables with new values.
        $msiProps = Get-ADTMsiTableProperty @gmtpParams -Table Property 6>$null
        $this.WriteLogEntry("App Vendor [$($msiProps.Manufacturer)].")
        $this.WriteLogEntry("App Name [$(($this.AppName = $msiProps.ProductName))].")
        $this.WriteLogEntry("App Version [$(($this.AppVersion = $msiProps.ProductVersion))].")
        $this.UseDefaultMsi = $true
    }

    hidden [System.Void] SetAppProperties([System.Collections.Specialized.OrderedDictionary]$ADTEnv)
    {
        # Archive off the current AppName value so we can use it if we have to throw.
        $initialAppName = $this.AppName

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
        $this.AppVendor = $this.AppVendor -replace $ADTEnv.invalidFileNameCharsRegExPattern
        $this.AppName = $this.AppName -replace $ADTEnv.invalidFileNameCharsRegExPattern
        $this.AppVersion = $this.AppVersion -replace $ADTEnv.invalidFileNameCharsRegExPattern
        $this.AppArch = $this.AppArch -replace $ADTEnv.invalidFileNameCharsRegExPattern
        $this.AppLang = $this.AppLang -replace $ADTEnv.invalidFileNameCharsRegExPattern
        $this.AppRevision = $this.AppRevision -replace $ADTEnv.invalidFileNameCharsRegExPattern

        # If we're left with a null AppName, throw a terminating error.
        if ([System.String]::IsNullOrWhiteSpace($this.AppName))
        {
            $naerParams = @{
                Exception = [System.ArgumentException]::new('The specified AppName contains only invalid filename characters.', 'AppName')
                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                ErrorId = 'AppNameAllCharactersInvalid'
                TargetObject = $initialAppName
                TargetName = '[ADTSession]'
                TargetType = 'Init()'
                RecommendedAction = "Please review the supplied AppName value and try again."
            }
            throw (New-ADTErrorRecord @naerParams)
        }
    }

    hidden [System.Void] SetInstallProperties([System.Collections.Specialized.OrderedDictionary]$ADTEnv, [System.Collections.Hashtable]$ADTConfig)
    {
        # Build the Installation Title.
        if ([System.String]::IsNullOrWhiteSpace($this.InstallTitle))
        {
            $this.InstallTitle = "$($this.AppVendor) $($this.AppName) $($this.AppVersion)".Trim() -replace '\s{2,}', ' '
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
        $this.WriteLogEntry((1..$Count | & { process { '*' * 79 } }))
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
                [System.IO.Directory]::Delete($this.LogTempFolder, $true)
            }
            $this.LogPath = [System.IO.Directory]::CreateDirectory($this.LogTempFolder).FullName
        }
        else
        {
            $this.LogPath = [System.IO.Directory]::CreateDirectory($ADTConfig.Toolkit.LogPath).FullName
        }

        # Generate the log filename to use. Append the username to the log file name if the toolkit is not running as an administrator, since users do not have the rights to modify files in the ProgramData folder that belong to other users.
        $this.LogName = if ($ADTEnv.IsAdmin)
        {
            "$($this.InstallName)_$($ADTEnv.appDeployToolkitName)_$($this.DeploymentType).log"
        }
        else
        {
            "$($this.InstallName)_$($ADTEnv.appDeployToolkitName)_$($this.DeploymentType)_$($ADTEnv.envUserName).log"
        }
        $this.LogName = Remove-ADTInvalidFileNameChars -Name $this.LogName
        $logFile = [System.IO.Path]::Combine($this.LogPath, $this.LogName)
        $logFileInfo = [System.IO.FileInfo]$logFile
        $logFileSizeExceeded = ($ADTConfig.Toolkit.LogMaxSize -gt 0) -and (($logFileInfo.Length / 1MB) -gt $ADTConfig.Toolkit.LogMaxSize)

        # Check if log file needs to be rotated.
        if (([System.IO.File]::Exists($logFile) -and !$ADTConfig.Toolkit.LogAppend) -or $logFileSizeExceeded)
        {
            try
            {
                # Get new log file path.
                $logFileNameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($logFile)
                $logFileExtension = [System.IO.Path]::GetExtension($logFile)
                $Timestamp = $logFileInfo.LastWriteTime.ToString('O').Split('.')[0].Replace(':', $null)
                $ArchiveLogFileName = [System.String]::Format("{0}_{1}{2}", $logFileNameWithoutExtension, $Timestamp, $logFileExtension)
                $ArchiveLogFilePath = Join-Path -Path $this.LogPath -ChildPath $ArchiveLogFileName

                # Log message about archiving the log file.
                if ($logFileSizeExceeded)
                {
                    $this.WriteLogEntry("Maximum log file size [$($ADTConfig.Toolkit.LogMaxSize) MB] reached. Rename log file to [$ArchiveLogFileName].", 2)
                }

                # Rename the file.
                Move-Item -LiteralPath $logFileInfo.FullName -Destination $ArchiveLogFilePath -Force

                # Start new log file and log message about archiving the old log file.
                if ($logFileSizeExceeded)
                {
                    $this.WriteLogEntry("Previous log file was renamed to [$ArchiveLogFileName] because maximum log file size of [$($ADTConfig.Toolkit.LogMaxSize) MB] was reached.", 2)
                }

                # Get all log files (including any .lo_ files that may have been created by previous toolkit versions) sorted by last write time.
                $logFiles = $(Get-ChildItem -LiteralPath $this.LogPath -Filter ("{0}_*{1}" -f $logFileNameWithoutExtension, $logFileExtension); Get-Item -LiteralPath ([IO.Path]::ChangeExtension($logFile, 'lo_')) -ErrorAction Ignore) | Sort-Object -Property LastWriteTime

                # Keep only the max number of log files.
                if ($logFiles.Count -gt $ADTConfig.Toolkit.LogMaxHistory)
                {
                    $logFiles | Select-Object -First ($logFiles.Count - $ADTConfig.Toolkit.LogMaxHistory) | Remove-Item
                }
            }
            catch
            {
                $this.WriteLogEntry("Failed to rotate the log file [$($logFile)].`n$(Resolve-ADTErrorRecord -ErrorRecord $_)", 3)
            }
        }

        # Open log file with commencement message.
        $this.WriteLogDivider(2)
        $this.WriteLogEntry("[$($this.InstallName)] $(($this.DeploymentTypeName = (Get-ADTStringTable).DeploymentType.($this.DeploymentType)).ToLower()) started.")
    }

    hidden [System.Void] LogScriptInfo([System.Management.Automation.PSObject]$ADTData, [System.Collections.Specialized.OrderedDictionary]$ADTEnv)
    {
        # Announce provided deployment script info.
        if ($this.AppScriptVersion)
        {
            $this.WriteLogEntry("[$($this.InstallName)] script version is [$($this.AppScriptVersion)].")
        }
        if ($this.AppScriptDate)
        {
            $this.WriteLogEntry("[$($this.InstallName)] script date is [$($this.AppScriptDate.ToString('O').Split('T')[0])].")
        }
        if ($this.AppScriptAuthor)
        {
            $this.WriteLogEntry("[$($this.InstallName)] script author is [$($this.AppScriptAuthor)].")
        }
        if ($this.DeployAppScriptFriendlyName)
        {
            if ($this.DeployAppScriptVersion)
            {
                $this.WriteLogEntry("[$($this.DeployAppScriptFriendlyName)] script version is [$($this.DeployAppScriptVersion)].")
            }
            if ($this.DeployAppScriptParameters -and $this.DeployAppScriptParameters.Count)
            {
                $this.WriteLogEntry("The following parameters were passed to [$($this.DeployAppScriptFriendlyName)]: [$($this.DeployAppScriptParameters | Resolve-ADTBoundParameters)].")
            }
        }
        $this.WriteLogEntry("[$($ADTEnv.appDeployToolkitName)] module version is [$($ADTEnv.appDeployMainScriptVersion)].")
        $this.WriteLogEntry("[$($ADTEnv.appDeployToolkitName)] module imported in [$($ADTData.Durations.ModuleImport.TotalSeconds)] seconds.")
        $this.WriteLogEntry("[$($ADTEnv.appDeployToolkitName)] module initialized in [$($ADTData.Durations.ModuleInit.TotalSeconds)] seconds.")
        $this.WriteLogEntry("[$($ADTEnv.appDeployToolkitName)] module path is [$($Script:MyInvocation.MyCommand.ScriptBlock.Module.ModuleBase)].")
        $this.WriteLogEntry("[$($ADTEnv.appDeployToolkitName)] config path is [$($ADTData.Directories.Config)].")
        $this.WriteLogEntry("[$($ADTEnv.appDeployToolkitName)] string path is [$($ADTData.Directories.Strings)].")

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
        $this.WriteLogEntry("Computer Name is [$($ADTEnv.envComputerNameFQDN)].")
        $this.WriteLogEntry("Current User is [$($ADTEnv.ProcessNTAccount)].")
        $this.WriteLogEntry("OS Version is [$($ADTEnv.envOSName)$(if ($ADTEnv.envOSServicePack) {" $($ADTEnv.envOSServicePack)"}) $($ADTEnv.envOSArchitecture) $($ADTEnv.envOSVersion)].")
        $this.WriteLogEntry("OS Type is [$($ADTEnv.envOSProductTypeName)].")
        $this.WriteLogEntry("Hardware Platform is [$($ADTEnv.envHardwareType)].")
        $this.WriteLogEntry("Current Culture is [$($ADTEnv.culture.Name)], language is [$($ADTEnv.currentLanguage)] and UI language is [$($ADTEnv.currentUILanguage)].")
        $this.WriteLogEntry("PowerShell Host is [$($ADTEnv.envHost.Name)] with version [$($ADTEnv.envHost.Version)].")
        $this.WriteLogEntry("PowerShell Version is [$($ADTEnv.envPSVersion) $($ADTEnv.psArchitecture)].")
        if ($ADTEnv.envCLRVersion)
        {
            $this.WriteLogEntry("PowerShell CLR (.NET) version is [$($ADTEnv.envCLRVersion)].")
        }
    }

    hidden [System.Void] LogUserInfo([System.Management.Automation.PSObject]$ADTData, [System.Collections.Specialized.OrderedDictionary]$ADTEnv, [System.Collections.Hashtable]$ADTConfig)
    {
        # Log details for all currently logged on users.
        $this.WriteLogEntry("Display session information for all logged on users:`n$($ADTEnv.LoggedOnUserSessions | Format-List | Out-String -Width ([System.Int32]::MaxValue))", $false)

        # Provide detailed info about current process state.
        if ($ADTEnv.usersLoggedOn)
        {
            $this.WriteLogEntry("The following users are logged on to the system: [$($ADTEnv.usersLoggedOn -join ', ')].")

            # Check if the current process is running in the context of one of the logged on users
            if ($ADTEnv.CurrentLoggedOnUserSession)
            {
                $this.WriteLogEntry("Current process is running with user account [$($ADTEnv.ProcessNTAccount)] under logged on user session for [$($ADTEnv.CurrentLoggedOnUserSession.NTAccount)].")
            }
            else
            {
                $this.WriteLogEntry("Current process is running under a system account [$($ADTEnv.ProcessNTAccount)].")
            }

            # Guard Intune detection code behind a variable.
            if ($ADTConfig.Toolkit.OobeDetection -and ([System.Environment]::OSVersion.Version -ge '10.0.16299.0') -and !(Test-ADTOobeCompleted))
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
                $this.WriteLogEntry('There is no console user logged on (user with control of physical monitor, keyboard, and mouse).')
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
        $this.WriteLogEntry("The following UI messages were imported from the config file: [$($ADTData.Language)].")
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
        # Return early if we're not in session 0.
        if (!$ADTEnv.SessionZero)
        {
            $this.WriteLogEntry('Session 0 not detected.')
            return
        }

        # If the script was launched with deployment mode set to NonInteractive, then continue
        if ($this.DeployMode -ne 'Interactive')
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
                $this.WriteLogEntry("Session 0 detected, process running in user interactive mode, no users logged on; deployment mode set to [$($this.DeployMode)].")
            }
            else
            {
                $this.WriteLogEntry('Session 0 detected, process running in user interactive mode, user(s) logged on.')
            }
        }
        else
        {
            $this.WriteLogEntry("Session 0 detected but toolkit is configured to not adjust deployment mode.")
        }
    }

    hidden [System.Void] SetDeploymentProperties()
    {
        # Set Deploy Mode switches.
        $this.WriteLogEntry("Installation is running in [$($this.DeployMode)] mode.")
        switch ($this.DeployMode)
        {
            Silent
            {
                $this.DeployModeNonInteractive = $true
                $this.DeployModeSilent = $true
                break
            }
            NonInteractive
            {
                $this.DeployModeNonInteractive = $true
                break
            }
        }

        # Check deployment type (install/uninstall).
        $this.WriteLogEntry("Deployment type is [$($this.GetDeploymentTypeName())].")
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
        if (!$this.CompatibilityMode -or !$this.Opened)
        {
            return $this.$Name
        }
        else
        {
            return $this.CallerVariables.Get($Name).Value
        }
    }

    [System.Void] SetPropertyValue([System.String]$Name, [System.Object]$Value)
    {
        # This getter exists as once the object is opened, we need to read the variable from the caller's scope.
        # We must get the variable every time as syntax like `$var = 'val'` always constructs a new PSVariable...
        if (!$this.CompatibilityMode -or !$this.Opened)
        {
            $this.$Name = $Value
        }
        else
        {
            $this.CallerVariables.Set($Name, $Value)
        }
    }

    [System.String] GetDeploymentStatus()
    {
        if (($this.ExitCode -eq ($adtConfig = Get-ADTConfig).UI.DefaultExitCode) -or ($this.ExitCode -eq $adtConfig.UI.DeferExitCode))
        {
            return 'FastRetry'
        }
        elseif ($this.GetPropertyValue('AppRebootExitCodes').Contains($this.ExitCode))
        {
            return 'RestartRequired'
        }
        elseif ($this.GetPropertyValue('AppSuccessExitCodes').Contains($this.ExitCode))
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
        # Get the current environment and config.
        $adtData = Get-ADTModuleData
        $adtEnv = Get-ADTEnvironment
        $adtConfig = Get-ADTConfig

        # Ensure this session isn't being opened twice.
        $this.TestClassState('Opened')
        $this.TestClassState('Closed')

        # Initialize PSADT session.
        $this.DetectDefaultWimFile()
        $this.DetectDefaultMsi($adtEnv)
        $this.SetAppProperties($adtEnv)
        $this.SetInstallProperties($adtEnv, $adtConfig)
        $this.InitLogging($adtEnv, $adtConfig)
        $this.LogScriptInfo($adtData, $adtEnv)
        $this.LogSystemInfo($adtEnv)
        $this.WriteLogDivider()
        $this.LogUserInfo($adtData, $adtEnv, $adtConfig)
        $this.PerformSCCMTests($adtEnv)
        $this.PerformSystemAccountTests($adtEnv, $adtConfig)
        $this.SetDeploymentProperties()
        $this.TestDefaultMsi()
        $this.TestAdminRequired($adtEnv, $adtConfig)

        # If terminal server mode was specified, change the installation mode to support it.
        if ($this.TerminalServerMode)
        {
            Enable-ADTTerminalServerInstallMode
        }

        # Export session's public variables to the user's scope. For these, we can't capture the Set-Variable
        # PassThru data as syntax like `$var = 'val'` constructs a new PSVariable every time.
        if ($this.CompatibilityMode)
        {
            $this.PSObject.Properties | & { process { $this.CallerVariables.Set($_.Name, $_.Value) } }
        }
        $this.Opened = $true
    }

    hidden [System.Int32] Close()
    {
        # Ensure this session isn't being closed twice.
        $this.TestClassState('Closed')
        $this.SetPropertyValue('InstallPhase', 'Finalization')

        # Set up initial variables.
        $adtData = Get-ADTModuleData
        $adtConfig = Get-ADTConfig

        # Store app/deployment details string. If we're exiting before properties are set, use a generic string.
        if ([System.String]::IsNullOrWhiteSpace(($deployString = "[$($this.GetPropertyValue('InstallName'))] $($this.GetDeploymentTypeName().ToLower())".Trim())))
        {
            $deployString = "$($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name) deployment"
        }

        # Process resulting exit code.
        switch ($this.GetDeploymentStatus())
        {
            FastRetry
            {
                # Just advise of the exit code with the appropriate severity.
                $this.WriteLogEntry("$deployString completed with exit code [$($this.ExitCode)].", 2)
                break
            }
            Error
            {
                # Just advise of the exit code with the appropriate severity.
                $this.WriteLogEntry("$deployString completed with exit code [$($this.ExitCode)].", 3)
                break
            }
            default
            {
                # Clean up app deferral history.
                $this.ResetDeferHistory()

                # Handle reboot prompts on successful script completion.
                if ($_.Equals('RestartRequired') -and $this.GetPropertyValue('AllowRebootPassThru'))
                {
                    $this.WriteLogEntry('A restart has been flagged as required.')
                }
                else
                {
                    $this.ExitCode = 0
                }
                $this.WriteLogEntry("$deployString completed with exit code [$($this.ExitCode)].", 0)
                break
            }
        }

        # Update the module's last tracked exit code.
        if ($this.ExitCode)
        {
            $adtData.LastExitCode = $this.ExitCode
        }

        # Remove any subst paths if created in the zero-config WIM code.
        if ($this.DirFilesSubstDrive)
        {
            Invoke-ADTSubstOperation -Drive $this.DirFilesSubstDrive -Delete
        }

        # Unmount any stored WIM file entries.
        if ($this.MountedWimFiles.Count)
        {
            $this.MountedWimFiles.Reverse(); Dismount-ADTWimFile -ImagePath $this.MountedWimFiles
            $this.MountedWimFiles.Clear()
        }

        # Write out a log divider to indicate the end of logging.
        $this.WriteLogDivider()
        $this.SetPropertyValue('DisableLogging', $true)
        $this.Closed = $true

        # Return early if we're not archiving log files.
        if ($adtConfig.Toolkit.CompressLogs)
        {
            # Archive the log files to zip format and then delete the temporary logs folder.
            $DestinationArchiveFileName = "$($this.GetPropertyValue('InstallName'))_$($this.GetPropertyValue('DeploymentType'))_{0}.zip"
            try
            {
                # Get all archive files sorted by last write time
                $ArchiveFiles = Get-ChildItem -LiteralPath $adtConfig.Toolkit.LogPath -Filter ([System.String]::Format($DestinationArchiveFileName, '*')) | Sort-Object LastWriteTime
                $DestinationArchiveFileName = [System.String]::Format($DestinationArchiveFileName, [System.DateTime]::Now.ToString('O').Split('.')[0].Replace(':', $null))

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
                $this.WriteLogEntry("Failed to manage archive file [$DestinationArchiveFileName].`n$(Resolve-ADTErrorRecord -ErrorRecord $_)", 3)
            }
        }

        # Return the module's cached exit code to the caller.
        return $adtData.LastExitCode
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.UInt32]]$Severity, [System.String]$Source, [System.String]$ScriptSection, [System.Nullable[System.Boolean]]$WriteHost, [System.Boolean]$DebugMessage, [System.String]$LogType, [System.String]$LogFileDirectory, [System.String]$LogFileName)
    {
        # Get the current config.
        $adtConfig = Get-ADTConfig

        # Determine whether we can write to the console.
        if ($null -eq $WriteHost)
        {
            $WriteHost = $adtConfig.Toolkit.LogWriteToHost
        }

        # Perform early return checks before wasting time.
        if (($this.GetPropertyValue('DisableLogging') -and !$WriteHost) -or ($DebugMessage -and !$adtConfig.Toolkit.LogDebugMessage))
        {
            return
        }

        # Establish logging date/time vars.
        $dateNow = [System.DateTime]::Now
        $logTime = $dateNow.ToString('HH\:mm\:ss.fff')
        $invoker = Get-ADTLogEntryCaller
        $logFile = if (![System.String]::IsNullOrWhiteSpace($invoker.ScriptName))
        {
            # A proper script or function.
            $invoker.ScriptName
        }
        else
        {
            # A call to Write-ADTLogEntry directly from the console.
            $invoker.Location
        }

        # Set up default values if not specified.
        if ($null -eq $Severity)
        {
            $Severity = 1
        }
        if ([System.String]::IsNullOrWhiteSpace($Source))
        {
            $Source = $invoker.Command
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
            $null = New-Item -Path $LogFileDirectory -Type Directory -Force
        }
        if ([System.String]::IsNullOrWhiteSpace($LogFileName))
        {
            $LogFileName = $this.GetPropertyValue('LogName')
        }

        # Cache all data pertaining to current severity.
        $sevData = ($logData = (Get-ADTModuleData).Logging).Severities[$Severity]

        # Store log string to format with message.
        $logFormats = @{
            Legacy = [System.String]::Format($logData.Formats.Legacy, '{0}', $dateNow.ToString('O').Split('T')[0], $logTime, $ScriptSection, $Source, $sevData.Name)
            CMTrace = [System.String]::Format($logData.Formats.CMTrace, '{0}', $ScriptSection, "$($logTime)+$($this.GetPropertyValue('CurrentTimeZoneBias').TotalMinutes)", $dateNow.ToString('M-dd-yyyy'), $Source, $Severity, $logFile)
        }

        # Add this log message to the session's buffer.
        $Message | & {
            process
            {
                $this.LogBuffer.Add([PSADT.Types.LogEntry]::new($dateNow, $invoker, $_, $Severity, $Source, $ScriptSection))
            }
        }

        # Write out all non-null messages to disk or host if configured/permitted to do so.
        if (![System.String]::IsNullOrWhiteSpace(($outFile = [System.IO.Path]::Combine($LogFileDirectory, $LogFileName))) -and !$this.GetPropertyValue('DisableLogging'))
        {
            # For CMTrace logging, sanitize the message for OneTrace's benefit before writing to disk.
            if ($LogType -eq 'CMTrace')
            {
                $Message | & {
                    begin
                    {
                        $logLine = $logFormats.$LogType
                    }

                    process
                    {
                        # Processing for if the message contains line feeds.
                        if ($_.Contains("`n"))
                        {
                            # Replace all empty lines with a space so OneTrace doesn't trim them.
                            # When splitting the message, we want to trim all lines but not replace genuine
                            # spaces. As such, replace all spaces and empty lines with a punctuation space.
                            # C# identifies this character as whitespace but OneTrace does not so it works.
                            # The empty line feed at the end is required by OneTrace to format correctly.
                            return [System.String]::Format($logLine, [System.String]::Join("`n", ($_.Replace("`r", $null).Trim().Replace(' ', [System.Char]0x2008).Split("`n") -replace '^$', [System.Char]0x2008)).Replace("`n", "`r`n") + "`r`n")
                        }
                        else
                        {
                            # Otherwise, just return a formatted string.
                            return [System.String]::Format($logLine, $_)
                        }
                    }
                } | Out-File -LiteralPath $outFile -Append -NoClobber -Force -Encoding UTF8
            }
            else
            {
                $Message | & {
                    begin
                    {
                        $logLine = $logFormats.$LogType
                    }

                    process
                    {
                        return [System.String]::Format($logLine, $_)
                    }
                } | Out-File -LiteralPath $outFile -Append -NoClobber -Force -Encoding UTF8
            }
        }
        if ($WriteHost)
        {
            $colours = $sevData.Colours
            $Message | Write-ADTLogEntryToInformationStream @colours -Source $Source -Format $logFormats.Legacy
        }
    }

    [System.Void] WriteLogEntry([System.String[]]$Message)
    {
        $this.WriteLogEntry($Message, $null, $null, $null, $null, $false, $null, $null, $null)
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Nullable[System.UInt32]]$Severity)
    {
        $this.WriteLogEntry($Message, $Severity, $null, $null, $null, $false, $null, $null, $null)
    }

    [System.Void] WriteLogEntry([System.String[]]$Message, [System.Boolean]$WriteHost)
    {
        $this.WriteLogEntry($Message, $null, $null, $null, $WriteHost, $false, $null, $null, $null)
    }

    [System.Object] GetDeferHistory()
    {
        if (!$this.RegKeyDeferHistory -or !(Test-Path -LiteralPath $this.RegKeyDeferHistory))
        {
            return $null
        }
        $this.WriteLogEntry('Getting deferral history...')
        return (Get-ADTRegistryKey -Key $this.RegKeyDeferHistory -InformationAction SilentlyContinue)
    }

    [System.Void] SetDeferHistory([System.Nullable[System.Int32]]$DeferTimesRemaining, [System.String]$DeferDeadline)
    {
        if ($null -ne $DeferTimesRemaining)
        {
            $this.WriteLogEntry("Setting deferral history: [DeferTimesRemaining = $DeferTimesRemaining].")
            Set-ADTRegistryKey -Key $this.RegKeyDeferHistory -Name 'DeferTimesRemaining' -Value $DeferTimesRemaining
        }
        if (![System.String]::IsNullOrWhiteSpace($DeferDeadline))
        {
            $this.WriteLogEntry("Setting deferral history: [DeferDeadline = $DeferDeadline].")
            Set-ADTRegistryKey -Key $this.RegKeyDeferHistory -Name 'DeferDeadline' -Value $DeferDeadline
        }
    }

    [System.Void] ResetDeferHistory()
    {
        if ($this.RegKeyDeferHistory -and (Test-Path -LiteralPath $this.RegKeyDeferHistory))
        {
            $this.WriteLogEntry('Removing deferral history...')
            Remove-ADTRegistryKey -Key $this.RegKeyDeferHistory -Recurse
        }
    }

    [System.Collections.Generic.List[System.IO.FileInfo]] GetMountedWimFiles()
    {
        return $this.MountedWimFiles
    }

    [PSADT.Types.ProcessObject[]] GetDefaultMsiExecutablesList()
    {
        return $this.DefaultMsiExecutablesList
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

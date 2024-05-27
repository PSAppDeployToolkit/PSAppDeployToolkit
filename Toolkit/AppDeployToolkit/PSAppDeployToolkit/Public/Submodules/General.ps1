#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Write-Log {
    <#
.SYNOPSIS

Write messages to a log file in CMTrace.exe compatible format or Legacy text file format.

.DESCRIPTION

Write messages to a log file in CMTrace.exe compatible format or Legacy text file format and optionally display in the console.

.PARAMETER Message

The message to write to the log file or output to the console.

.PARAMETER Severity

Defines message type. When writing to console or CMTrace.exe log format, it allows highlighting of message type.
Options: 0 = Success (highlighted in green), 1 = Information (default), 2 = Warning (highlighted in yellow), 3 = Error (highlighted in red)

.PARAMETER Source

The source of the message being logged.

.PARAMETER ScriptSection

The heading for the portion of the script that is being executed. Default is: $script:installPhase.

.PARAMETER LogType

Choose whether to write a CMTrace.exe compatible log file or a Legacy text log file.

.PARAMETER LogFileDirectory

Set the directory where the log file will be saved.

.PARAMETER LogFileName

Set the name of the log file.

.PARAMETER AppendToLogFile

Append to existing log file rather than creating a new one upon toolkit initialization. Default value is defined in AppDeployToolkitConfig.xml.

.PARAMETER MaxLogHistory

Maximum number of previous log files to retain. Default value is defined in AppDeployToolkitConfig.xml.

.PARAMETER MaxLogFileSizeMB

Maximum file size limit for log file in megabytes (MB). Default value is defined in AppDeployToolkitConfig.xml.

.PARAMETER ContinueOnError

Suppress writing log message to console on failure to write message to log file. Default is: $true.

.PARAMETER WriteHost

Write the log message to the console.

.PARAMETER PassThru

Return the message that was passed to the function

.PARAMETER DebugMessage

Specifies that the message is a debug message. Debug messages only get logged if -LogDebugMessage is set to $true.

.PARAMETER LogDebugMessage

Debug messages only get logged if this parameter is set to $true in the config XML file.

.INPUTS

System.String

The message to write to the log file or output to the console.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Write-Log -Message "Installing patch MS15-031" -Source 'Add-Patch' -LogType 'CMTrace'

.EXAMPLE

Write-Log -Message "Script is running on Windows 8" -Source 'Test-ValidOS' -LogType 'Legacy'

.EXAMPLE

Write-Log -Message "Log only message" -WriteHost $false

.NOTES

.LINK
https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyCollection()]
        [Alias('Text')]
        [String[]]$Message,
        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateRange(0, 3)]
        [Int16]$Severity = 1,
        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNull()]
        [String]$Source = $([String]$parentFunctionName = [IO.Path]::GetFileNameWithoutExtension((Get-Variable -Name 'MyInvocation' -Scope 1 -ErrorAction 'SilentlyContinue').Value.MyCommand.Name); If ($parentFunctionName) {
                $parentFunctionName
            }
            Else {
                'Unknown'
            }),
        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullorEmpty()]
        [String]$ScriptSection = $Script:ADT.CurrentSession.GetPropertyValue('InstallPhase'),
        [Parameter(Mandatory = $false, Position = 4)]
        [ValidateSet('CMTrace', 'Legacy')]
        [String]$LogType = $Script:ADT.Config.Toolkit_Options.Toolkit_LogStyle,
        [Parameter(Mandatory = $false, Position = 5)]
        [ValidateNotNullorEmpty()]
        [String]$LogFileDirectory = $(If ($Script:ADT.Config.Toolkit_Options.Toolkit_CompressLogs) {
                $logTempFolder
            }
            Else {
                $Script:ADT.Config.Toolkit_Options.Toolkit_LogPath
            }),
        [Parameter(Mandatory = $false, Position = 6)]
        [ValidateNotNullorEmpty()]
        [String]$LogFileName = $Script:ADT.CurrentSession.GetPropertyValue('LogName'),
        [Parameter(Mandatory=$false,Position=7)]
        [ValidateNotNullorEmpty()]
        [Boolean]$AppendToLogFile = $Script:ADT.Config.Toolkit_Options.Toolkit_LogAppend,
        [Parameter(Mandatory=$false,Position=8)]
        [ValidateNotNullorEmpty()]
        [Int]$MaxLogHistory = $Script:ADT.Config.Toolkit_Options.Toolkit_LogMaxHistory,
        [Parameter(Mandatory = $false, Position = 9)]
        [ValidateNotNullorEmpty()]
        [Decimal]$MaxLogFileSizeMB = $Script:ADT.Config.Toolkit_Options.Toolkit_LogMaxSize,
        [Parameter(Mandatory=$false,Position=10)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true,
        [Parameter(Mandatory = $false, Position = 11)]
        [ValidateNotNullorEmpty()]
        [Boolean]$WriteHost = $Script:ADT.Config.Toolkit_Options.Toolkit_LogWriteToHost,
        [Parameter(Mandatory=$false,Position=12)]
        [Switch]$PassThru = $false,
        [Parameter(Mandatory=$false,Position=13)]
        [Switch]$DebugMessage = $false,
        [Parameter(Mandatory=$false,Position=14)]
        [Boolean]$LogDebugMessage = $Script:ADT.Config.Toolkit_Options.Toolkit_LogDebugMessage
    )

    Begin {
        ## Get the name of this function
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

        ## Logging Variables
        #  Log file date/time
        [DateTime]$DateTimeNow = Get-Date
        [String]$LogTime = $DateTimeNow.ToString('HH\:mm\:ss.fff')
        [String]$LogDate = $DateTimeNow.ToString('MM-dd-yyyy')
        If (-not (Test-Path -LiteralPath 'variable:LogTimeZoneBias')) {
            [Int32]$script:LogTimeZoneBias = [TimeZone]::CurrentTimeZone.GetUtcOffset($DateTimeNow).TotalMinutes
        }
        [String]$LogTimePlusBias = $LogTime + $script:LogTimeZoneBias
        #  Initialize variables
        [Boolean]$ExitLoggingFunction = $false
        If (-not (Test-Path -LiteralPath 'variable:DisableLogging')) {
            $Script:ADT.CurrentSession.SetPropertyValue('DisableLogging', $false)
        }
        If ([System.String]::IsNullOrEmpty($LogFileName) -or $LogFileName.Trim().Length -eq 0) {
            $Script:ADT.CurrentSession.SetPropertyValue('DisableLogging', $true)
        }
        #  Check if the script section is defined
        [Boolean]$ScriptSectionDefined = [Boolean](-not [String]::IsNullOrEmpty($ScriptSection))
        #  Get the file name of the source script
        $ScriptSource = If (![System.String]::IsNullOrEmpty($script:MyInvocation.ScriptName)) {
            Split-Path -Path $script:MyInvocation.ScriptName -Leaf -ErrorAction SilentlyContinue
        }
        Else {
            Split-Path -Path $script:MyInvocation.MyCommand.Definition -Leaf -ErrorAction SilentlyContinue
        }

        ## Create script block for generating CMTrace.exe compatible log entry
        [ScriptBlock]$CMTraceLogString = {
            Param (
                [String]$lMessage,
                [String]$lSource,
                [Int16]$lSeverity
            )
            "<![LOG[$lMessage]LOG]!>" + "<time=`"$LogTimePlusBias`" " + "date=`"$LogDate`" " + "component=`"$lSource`" " + "context=`"$([Security.Principal.WindowsIdentity]::GetCurrent().Name)`" " + "type=`"$lSeverity`" " + "thread=`"$PID`" " + "file=`"$ScriptSource`">"
        }

        ## Create script block for writing log entry to the console
        [ScriptBlock]$WriteLogLineToHost = {
            Param (
                [String]$lTextLogLine,
                [Int16]$lSeverity
            )
            If ($WriteHost) {
                #  Only output using color options if running in a host which supports colors.
                If ($Host.UI.RawUI.ForegroundColor) {
                    Switch ($lSeverity) {
                        3 {
                            Write-Host -Object $lTextLogLine -ForegroundColor 'Red' -BackgroundColor 'Black'
                        }
                        2 {
                            Write-Host -Object $lTextLogLine -ForegroundColor 'Yellow' -BackgroundColor 'Black'
                        }
                        1 {
                            Write-Host -Object $lTextLogLine
                        }
                        0 {
                            Write-Host -Object $lTextLogLine -ForegroundColor 'Green' -BackgroundColor 'Black'
                        }
                    }
                }
                #  If executing "powershell.exe -File <filename>.ps1 > log.txt", then all the Write-Host calls are converted to Write-Output calls so that they are included in the text log.
                Else {
                    Write-Output -InputObject ($lTextLogLine)
                }
            }
        }

        ## Exit function if it is a debug message and logging debug messages is not enabled in the config XML file
        If (($DebugMessage) -and (-not $LogDebugMessage)) {
            [Boolean]$ExitLoggingFunction = $true; Return
        }
        ## Exit function if logging to file is disabled and logging to console host is disabled
        If ($Script:ADT.CurrentSession.GetPropertyValue('DisableLogging') -and !$WriteHost) {
            [Boolean]$ExitLoggingFunction = $true; Return
        }
        ## Exit Begin block if logging is disabled
        If ($Script:ADT.CurrentSession.GetPropertyValue('DisableLogging')) {
            Return
        }
        ## Exit function function if it is an [Initialization] message and the toolkit has been relaunched
        If ((Test-Path -LiteralPath 'Variable:AsyncToolkitLaunch') -and ($AsyncToolkitLaunch) -and ($ScriptSection -eq 'Initialization')) {
            [Boolean]$ExitLoggingFunction = $true; Return
        }

        ## Create the directory where the log file will be saved
        If (-not (Test-Path -LiteralPath $LogFileDirectory -PathType 'Container')) {
            Try {
                $null = New-Item -Path $LogFileDirectory -Type 'Directory' -Force -ErrorAction 'Stop'
            }
            Catch {
                [Boolean]$ExitLoggingFunction = $true
                #  If error creating directory, write message to console
                If (-not $ContinueOnError) {
                    Write-Host -Object "[$LogDate $LogTime] [${CmdletName}] $ScriptSection :: Failed to create the log directory [$LogFileDirectory]. `r`n$(Resolve-Error)" -ForegroundColor 'Red'
                }
                Return
            }
        }

        ## Assemble the fully qualified path to the log file
        [String]$LogFilePath = Join-Path -Path $LogFileDirectory -ChildPath $LogFileName

        if (Test-Path -Path $LogFilePath -PathType Leaf) {
            Try {
                $LogFile = Get-Item $LogFilePath
                [Decimal]$LogFileSizeMB = $LogFile.Length / 1MB

                # Check if log file needs to be rotated
                if ((!$script:LogFileInitialized -and !$AppendToLogFile) -or ($MaxLogFileSizeMB -gt 0 -and $LogFileSizeMB -gt $MaxLogFileSizeMB)) {

                    # Get new log file path
                    $LogFileNameWithoutExtension = [IO.Path]::GetFileNameWithoutExtension($LogFileName)
                    $LogFileExtension = [IO.Path]::GetExtension($LogFileName)
                    $Timestamp = $LogFile.LastWriteTime.ToString('yyyy-MM-dd-HH-mm-ss')
                    $ArchiveLogFileName = "{0}_{1}{2}" -f $LogFileNameWithoutExtension, $Timestamp, $LogFileExtension
                    [String]$ArchiveLogFilePath = Join-Path -Path $LogFileDirectory -ChildPath $ArchiveLogFileName

                    if ($MaxLogFileSizeMB -gt 0 -and $LogFileSizeMB -gt $MaxLogFileSizeMB) {
                        [Hashtable]$ArchiveLogParams = @{ ScriptSection = $ScriptSection; Source = ${CmdletName}; Severity = 2; LogFileDirectory = $LogFileDirectory; LogFileName = $LogFileName; LogType = $LogType; MaxLogFileSizeMB = 0; AppendToLogFile = $true; WriteHost = $WriteHost; ContinueOnError = $ContinueOnError; PassThru = $false }

                        ## Log message about archiving the log file
                        $ArchiveLogMessage = "Maximum log file size [$MaxLogFileSizeMB MB] reached. Rename log file to [$ArchiveLogFileName]."
                        Write-Log -Message $ArchiveLogMessage @ArchiveLogParams
                    }

                    # Rename the file
                    Move-Item -Path $LogFilePath -Destination $ArchiveLogFilePath -Force -ErrorAction 'Stop'

                    if ($MaxLogFileSizeMB -gt 0 -and $LogFileSizeMB -gt $MaxLogFileSizeMB) {
                        ## Start new log file and Log message about archiving the old log file
                        $NewLogMessage = "Previous log file was renamed to [$ArchiveLogFileName] because maximum log file size of [$MaxLogFileSizeMB MB] was reached."
                        Write-Log -Message $NewLogMessage @ArchiveLogParams
                    }

                    # Get all log files (including any .lo_ files that may have been created by previous toolkit versions) sorted by last write time
                    $LogFiles = @(Get-ChildItem -LiteralPath $LogFileDirectory -Filter ("{0}_*{1}" -f $LogFileNameWithoutExtension, $LogFileExtension)) + @(Get-Item -LiteralPath ([IO.Path]::ChangeExtension($LogFilePath, 'lo_')) -ErrorAction Ignore) | Sort-Object LastWriteTime

                    # Keep only the max number of log files
                    if ($LogFiles.Count -gt $MaxLogHistory) {
                        $LogFiles | Select-Object -First ($LogFiles.Count - $MaxLogHistory) | Remove-Item -ErrorAction 'Stop'
                    }
                }
            }
            Catch {
                Write-Host -Object "[$LogDate $LogTime] [${CmdletName}] $ScriptSection :: Failed to rotate the log file [$LogFilePath]. `r`n$(Resolve-Error)" -ForegroundColor 'Red'
                # Treat log rotation errors as non-terminating by default
                If (-not $ContinueOnError) {
                    [Boolean]$ExitLoggingFunction = $true
                    Return
                }
            }
        }

        $script:LogFileInitialized = $true
    }
    Process {
        ## Exit function if logging is disabled
        If ($ExitLoggingFunction) {
            Return
        }

        ForEach ($Msg in $Message) {
            ## If the message is not $null or empty, create the log entry for the different logging methods
            [String]$CMTraceMsg = ''
            [String]$ConsoleLogLine = ''
            [String]$LegacyTextLogLine = ''
            If ($Msg) {
                #  Create the CMTrace log message
                If ($ScriptSectionDefined) {
                    [String]$CMTraceMsg = "[$ScriptSection] :: $Msg"
                }

                #  Create a Console and Legacy "text" log entry
                [String]$LegacyMsg = "[$LogDate $LogTime]"
                If ($ScriptSectionDefined) {
                    [String]$LegacyMsg += " [$ScriptSection]"
                }
                If ($Source) {
                    [String]$ConsoleLogLine = "$LegacyMsg [$Source] :: $Msg"
                    Switch ($Severity) {
                        3 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [$Source] [Error] :: $Msg"
                        }
                        2 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [$Source] [Warning] :: $Msg"
                        }
                        1 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [$Source] [Info] :: $Msg"
                        }
                        0 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [$Source] [Success] :: $Msg"
                        }
                    }
                }
                Else {
                    [String]$ConsoleLogLine = "$LegacyMsg :: $Msg"
                    Switch ($Severity) {
                        3 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [Error] :: $Msg"
                        }
                        2 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [Warning] :: $Msg"
                        }
                        1 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [Info] :: $Msg"
                        }
                        0 {
                            [String]$LegacyTextLogLine = "$LegacyMsg [Success] :: $Msg"
                        }
                    }
                }
            }

            ## Execute script block to create the CMTrace.exe compatible log entry
            [String]$CMTraceLogLine = & $CMTraceLogString -lMessage $CMTraceMsg -lSource $Source -lSeverity $Severity

            ## Choose which log type to write to file
            If ($LogType -ieq 'CMTrace') {
                [String]$LogLine = $CMTraceLogLine
            }
            Else {
                [String]$LogLine = $LegacyTextLogLine
            }

            ## Write the log entry to the log file if logging is not currently disabled
            If (!$Script:ADT.CurrentSession.GetPropertyValue('DisableLogging')) {
                Try {
                    $LogLine | Out-File -FilePath $LogFilePath -Append -NoClobber -Force -Encoding 'UTF8' -ErrorAction 'Stop'
                }
                Catch {
                    If (-not $ContinueOnError) {
                        Write-Host -Object "[$LogDate $LogTime] [$ScriptSection] [${CmdletName}] :: Failed to write message [$Msg] to the log file [$LogFilePath]. `r`n$(Resolve-Error)" -ForegroundColor 'Red'
                    }
                }
            }

            ## Execute script block to write the log entry to the console if $WriteHost is $true
            & $WriteLogLineToHost -lTextLogLine $ConsoleLogLine -lSeverity $Severity
        }
    }
    End {
        If ($PassThru) {
            Write-Output -InputObject ($Message)
        }
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Exit-Script {
    <#
.SYNOPSIS

Exit the script, perform cleanup actions, and pass an exit code to the parent process.

.DESCRIPTION

Always use when exiting the script to ensure cleanup actions are performed.

.PARAMETER ExitCode

The exit code to be passed from the script to the parent process, e.g. SCCM

.PARAMETER ValidExitCodes

An optional parameter to specify what exit codes are considered valid. Default are msiexec success codes (0, 1641, and 3010).

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Exit-Script

.EXAMPLE

Exit-Script -ExitCode 1618

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$ExitCode = 0,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32[]]$ValidExitCodes = @(0, 1641, 3010)
    )

    ## Get the name of this function
    [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

    ## Close the Installation Progress Dialog if running
    Close-InstallationProgress

    ## If block execution variable is true, call the function to unblock execution
    If ($Script:ADT.CurrentSession.Session.State.BlockExecution) {
        Unblock-AppExecution
    }

    ## If Terminal Server mode was set, turn it off
    If ($Script:ADT.CurrentSession.GetPropertyValue('TerminalServerMode')) {
        Disable-TerminalServerInstallMode
    }

    ## Determine action based on exit code
    Switch ($exitCode) {
        $Script:ADT.Config.UI_Options.InstallationUI_ExitCode {
            $installSuccess = $false
        }
        $Script:ADT.Config.UI_Options.InstallationDefer_ExitCode {
            $installSuccess = $false
        }
        {$ValidExitCodes -contains $_} {
            $installSuccess = $true
        }
        Default {
            $installSuccess = $false
        }
    }

    ## Determine if balloon notification should be shown
    If ($Script:ADT.CurrentSession.Session.State.DeployModeSilent) {
        [Boolean]$Script:ADT.Config.UI_Options.ShowBalloonNotifications = $false
    }

    If ($installSuccess) {
        If (Test-Path -LiteralPath $Script:ADT.CurrentSession.Session.RegKeyDeferHistory -ErrorAction 'Ignore') {
            Write-Log -Message 'Removing deferral history...' -Source ${CmdletName}
            Remove-RegistryKey -Key $Script:ADT.CurrentSession.Session.RegKeyDeferHistory -Recurse
        }

        [String]$balloonText = "$($Script:ADT.CurrentSession.Session.State.DeploymentTypeName) $($Script:ADT.Strings.BalloonText_Complete)"
        ## Handle reboot prompts on successful script completion
        If ($Script:ADT.CurrentSession.GetPropertyValue('AllowRebootPassThru') -and ((($msiRebootDetected) -or ($exitCode -eq 3010)) -or ($exitCode -eq 1641))) {
            Write-Log -Message 'A restart has been flagged as required.' -Source ${CmdletName}
            [String]$balloonText = "$($Script:ADT.CurrentSession.Session.State.DeploymentTypeName) $($Script:ADT.Strings.BalloonText_RestartRequired)"
            If (($msiRebootDetected) -and ($exitCode -ne 1641)) {
                [Int32]$exitCode = 3010
            }
        }
        Else {
            [Int32]$exitCode = 0
        }

        Write-Log -Message "$($Script:ADT.CurrentSession.GetPropertyValue('installName')) $($Script:ADT.CurrentSession.Session.State.DeploymentTypeName.ToLower()) completed with exit code [$exitcode]." -Source ${CmdletName} -Severity 0
        If ($Script:ADT.Config.UI_Options.ShowBalloonNotifications) {
            Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText $balloonText -NoWait
        }
    }
    ElseIf (($exitCode -eq $Script:ADT.Config.UI_Options.InstallationUI_ExitCode) -or ($exitCode -eq $Script:ADT.Config.UI_Options.InstallationDefer_ExitCode)) {
        Write-Log -Message "$($Script:ADT.CurrentSession.GetPropertyValue('installName')) $($Script:ADT.CurrentSession.Session.State.DeploymentTypeName.ToLower()) completed with exit code [$exitcode]." -Source ${CmdletName} -Severity 2
        [String]$balloonText = "$($Script:ADT.CurrentSession.Session.State.DeploymentTypeName) $($Script:ADT.Strings.BalloonText_FastRetry)"
        If ($Script:ADT.Config.UI_Options.ShowBalloonNotifications) {
            Show-BalloonTip -BalloonTipIcon 'Warning' -BalloonTipText $balloonText -NoWait
        }
    }
    Else {
        Write-Log -Message "$($Script:ADT.CurrentSession.GetPropertyValue('installName')) $($Script:ADT.CurrentSession.Session.State.DeploymentTypeName.ToLower()) completed with exit code [$exitcode]." -Source ${CmdletName} -Severity 3
        [String]$balloonText = "$($Script:ADT.CurrentSession.Session.State.DeploymentTypeName) $($Script:ADT.Strings.BalloonText_Error)"
        If ($Script:ADT.Config.UI_Options.ShowBalloonNotifications) {
            Show-BalloonTip -BalloonTipIcon 'Error' -BalloonTipText $balloonText -NoWait
        }
    }

    [String]$LogDash = '-' * 79
    Write-Log -Message $LogDash -Source ${CmdletName}

    ## Archive the log files to zip format and then delete the temporary logs folder
    If ($Script:ADT.Config.Toolkit_Options.Toolkit_CompressLogs) {
        ## Disable logging to file so that we can archive the log files
        . $DisableScriptLogging

        Try {
            # Get all archive files sorted by last write time
            $ArchiveFiles = Get-ChildItem -LiteralPath $Script:ADT.Config.Toolkit_Options.Toolkit_LogPath -Filter ($Script:ADT.CurrentSession.GetPropertyValue('installName') + '_' + $Script:ADT.CurrentSession.GetPropertyValue('deploymentType') + '_*.zip') | Sort-Object LastWriteTime

            # Keep only the max number of archive files
            if ($ArchiveFiles.Count -gt $Script:ADT.Config.Toolkit_Options.Toolkit_LogMaxHistory) {
                $ArchiveFiles | Select-Object -First ($ArchiveFiles.Count - $Script:ADT.Config.Toolkit_Options.Toolkit_LogMaxHistory) | Remove-Item -ErrorAction 'Stop'
            }

            [String]$DestinationArchiveFileName = $Script:ADT.CurrentSession.GetPropertyValue('installName') + '_' + $Script:ADT.CurrentSession.GetPropertyValue('deploymentType') + '_' + (Get-Date -Format 'yyyy-MM-dd-HH-mm-ss').ToString() + '.zip'
            New-ZipFile -DestinationArchiveDirectoryPath $Script:ADT.Config.Toolkit_Options.Toolkit_LogPath -DestinationArchiveFileName $DestinationArchiveFileName -SourceDirectory $logTempFolder -RemoveSourceAfterArchiving
        }
        Catch {
            Write-Host -Object "[$LogDate $LogTime] [${CmdletName}] $ScriptSection :: Failed to manage archive file [$DestinationArchiveFileName]. `r`n$(Resolve-Error)" -ForegroundColor 'Red'
        }

    }

    If (Test-Path -LiteralPath 'variable:notifyIcon') {
        Try {
            $script:notifyIcon.Dispose()
        }
        Catch {
        }
    }
    ## Reset powershell window title to its previous title
    $Host.UI.RawUI.WindowTitle = $Script:ADT.CurrentSession.Session.State.OldPSWindowTitle
    [System.Void]$Script:ADT.Sessions.Remove($Script:ADT.CurrentSession)
    ## Exit the script, returning the exit code to SCCM
    If (Test-Path -LiteralPath 'variable:HostInvocation') {
        $script:ExitCode = $exitCode; Exit
    }
    Else {
        Exit $exitCode
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Resolve-Error {
    <#
.SYNOPSIS

Enumerate error record details.

.DESCRIPTION

Enumerate an error record, or a collection of error record, properties. By default, the details for the last error will be enumerated.

.PARAMETER ErrorRecord

The error record to resolve. The default error record is the latest one: $global:Error(0). This parameter will also accept an array of error records.

.PARAMETER Property

The list of properties to display from the error record. Use "*" to display all properties.

Default list of error properties is: Message, FullyQualifiedErrorId, ScriptStackTrace, PositionMessage, InnerException

.PARAMETER GetErrorRecord

Get error record details as represented by $_.

.PARAMETER GetErrorInvocation

Get error record invocation information as represented by $_.InvocationInfo.

.PARAMETER GetErrorException

Get error record exception details as represented by $_.Exception.

.PARAMETER GetErrorInnerException

Get error record inner exception details as represented by $_.Exception.InnerException. Will retrieve all inner exceptions if there is more than one.

.INPUTS

System.Array.

Accepts an array of error records.

.OUTPUTS

System.String

Displays the error record details.

.EXAMPLE

Resolve-Error

.EXAMPLE

Resolve-Error -Property *

.EXAMPLE

Resolve-Error -Property InnerException

.EXAMPLE

Resolve-Error -GetErrorInvocation:$false

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyCollection()]
        [Array]$ErrorRecord,
        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String[]]$Property = ('Message', 'InnerException', 'FullyQualifiedErrorId', 'ScriptStackTrace', 'PositionMessage'),
        [Parameter(Mandatory = $false, Position = 2)]
        [Switch]$GetErrorRecord = $true,
        [Parameter(Mandatory = $false, Position = 3)]
        [Switch]$GetErrorInvocation = $true,
        [Parameter(Mandatory = $false, Position = 4)]
        [Switch]$GetErrorException = $true,
        [Parameter(Mandatory = $false, Position = 5)]
        [Switch]$GetErrorInnerException = $true
    )

    Begin {
        ## If function was called without specifying an error record, then choose the latest error that occurred
        If (-not $ErrorRecord) {
            If ($global:Error.Count -eq 0) {
                #Write-Warning -Message "The `$Error collection is empty"
                Return
            }
            Else {
                [Array]$ErrorRecord = $global:Error[0]
            }
        }

        ## Allows selecting and filtering the properties on the error object if they exist
        [ScriptBlock]$SelectProperty = {
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                $InputObject,
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [String[]]$Property
            )

            [String[]]$ObjectProperty = $InputObject | Get-Member -MemberType '*Property' | Select-Object -ExpandProperty 'Name'
            ForEach ($Prop in $Property) {
                If ($Prop -eq '*') {
                    [String[]]$PropertySelection = $ObjectProperty
                    Break
                }
                ElseIf ($ObjectProperty -contains $Prop) {
                    [String[]]$PropertySelection += $Prop
                }
            }
            Write-Output -InputObject ($PropertySelection)
        }

        #  Initialize variables to avoid error if 'Set-StrictMode' is set
        $LogErrorRecordMsg = $null
        $LogErrorInvocationMsg = $null
        $LogErrorExceptionMsg = $null
        $LogErrorMessageTmp = $null
        $LogInnerMessage = $null
    }
    Process {
        If (-not $ErrorRecord) {
            Return
        }
        ForEach ($ErrRecord in $ErrorRecord) {
            ## Capture Error Record
            If ($GetErrorRecord) {
                [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord -Property $Property
                $LogErrorRecordMsg = $ErrRecord | Select-Object -Property $SelectedProperties
            }

            ## Error Invocation Information
            If ($GetErrorInvocation) {
                If ($ErrRecord.InvocationInfo) {
                    [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord.InvocationInfo -Property $Property
                    $LogErrorInvocationMsg = $ErrRecord.InvocationInfo | Select-Object -Property $SelectedProperties
                }
            }

            ## Capture Error Exception
            If ($GetErrorException) {
                If ($ErrRecord.Exception) {
                    [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord.Exception -Property $Property
                    $LogErrorExceptionMsg = $ErrRecord.Exception | Select-Object -Property $SelectedProperties
                }
            }

            ## Display properties in the correct order
            If ($Property -eq '*') {
                #  If all properties were chosen for display, then arrange them in the order the error object displays them by default.
                If ($LogErrorRecordMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorRecordMsg
                }
                If ($LogErrorInvocationMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorInvocationMsg
                }
                If ($LogErrorExceptionMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorExceptionMsg
                }
            }
            Else {
                #  Display selected properties in our custom order
                If ($LogErrorExceptionMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorExceptionMsg
                }
                If ($LogErrorRecordMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorRecordMsg
                }
                If ($LogErrorInvocationMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorInvocationMsg
                }
            }

            If ($LogErrorMessageTmp) {
                $LogErrorMessage = 'Error Record:'
                $LogErrorMessage += "`n-------------"
                $LogErrorMsg = $LogErrorMessageTmp | Format-List | Out-String
                $LogErrorMessage += $LogErrorMsg
            }

            ## Capture Error Inner Exception(s)
            If ($GetErrorInnerException) {
                If ($ErrRecord.Exception -and $ErrRecord.Exception.InnerException) {
                    $LogInnerMessage = 'Error Inner Exception(s):'
                    $LogInnerMessage += "`n-------------------------"

                    $ErrorInnerException = $ErrRecord.Exception.InnerException
                    $Count = 0

                    While ($ErrorInnerException) {
                        [String]$InnerExceptionSeperator = '~' * 40

                        [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrorInnerException -Property $Property
                        $LogErrorInnerExceptionMsg = $ErrorInnerException | Select-Object -Property $SelectedProperties | Format-List | Out-String

                        If ($Count -gt 0) {
                            $LogInnerMessage += $InnerExceptionSeperator
                        }
                        $LogInnerMessage += $LogErrorInnerExceptionMsg

                        $Count++
                        $ErrorInnerException = $ErrorInnerException.InnerException
                    }
                }
            }

            If ($LogErrorMessage) {
                $Output = $LogErrorMessage
            }
            If ($LogInnerMessage) {
                $Output += $LogInnerMessage
            }

            Write-Output -InputObject $Output

            If (Test-Path -LiteralPath 'variable:Output') {
                Clear-Variable -Name 'Output'
            }
            If (Test-Path -LiteralPath 'variable:LogErrorMessage') {
                Clear-Variable -Name 'LogErrorMessage'
            }
            If (Test-Path -LiteralPath 'variable:LogInnerMessage') {
                Clear-Variable -Name 'LogInnerMessage'
            }
            If (Test-Path -LiteralPath 'variable:LogErrorMessageTmp') {
                Clear-Variable -Name 'LogErrorMessageTmp'
            }
        }
    }
    End {
    }
}

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
			} Else {
				'Unknown'
			}),
		[Parameter(Mandatory = $false, Position = 3)]
		[ValidateNotNullorEmpty()]
		[String]$ScriptSection = $script:installPhase,
		[Parameter(Mandatory = $false, Position = 4)]
		[ValidateSet('CMTrace', 'Legacy')]
		[String]$LogType = $configToolkitLogStyle,
		[Parameter(Mandatory = $false, Position = 5)]
		[ValidateNotNullorEmpty()]
		[String]$LogFileDirectory = $(If ($configToolkitCompressLogs) {
				$logTempFolder
			} Else {
				$configToolkitLogDir
			}),
		[Parameter(Mandatory = $false, Position = 6)]
		[ValidateNotNullorEmpty()]
		[String]$LogFileName = $logName,
		[Parameter(Mandatory = $false, Position = 7)]
		[ValidateNotNullorEmpty()]
		[Boolean]$AppendToLogFile = $configToolkitLogAppend,
		[Parameter(Mandatory = $false, Position = 8)]
		[ValidateNotNullorEmpty()]
		[Int]$MaxLogHistory = $configToolkitLogMaxHistory,
		[Parameter(Mandatory = $false, Position = 9)]
		[ValidateNotNullorEmpty()]
		[Decimal]$MaxLogFileSizeMB = $configToolkitLogMaxSize,
		[Parameter(Mandatory = $false, Position = 10)]
		[ValidateNotNullorEmpty()]
		[Boolean]$ContinueOnError = $true,
		[Parameter(Mandatory = $false, Position = 11)]
		[ValidateNotNullorEmpty()]
		[Boolean]$WriteHost = $configToolkitLogWriteToHost,
		[Parameter(Mandatory = $false, Position = 12)]
		[Switch]$PassThru = $false,
		[Parameter(Mandatory = $false, Position = 13)]
		[Switch]$DebugMessage = $false,
		[Parameter(Mandatory = $false, Position = 14)]
		[Boolean]$LogDebugMessage = $configToolkitLogDebugMessage
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
			$DisableLogging = $false
		}
		If ([System.String]::IsNullOrWhiteSpace($LogFileName)) {
			$DisableLogging = $true
		}
		#  Check if the script section is defined
		[Boolean]$ScriptSectionDefined = [Boolean](-not [String]::IsNullOrEmpty($ScriptSection))
		#  Get the file name of the source script
		$ScriptSource = If (![System.String]::IsNullOrWhiteSpace($script:MyInvocation.ScriptName)) {
			Split-Path -Path $script:MyInvocation.ScriptName -Leaf -ErrorAction SilentlyContinue
		} Else {
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
		If (($DisableLogging) -and (-not $WriteHost)) {
			[Boolean]$ExitLoggingFunction = $true; Return
		}
		## Exit Begin block if logging is disabled
		If ($DisableLogging) {
			Return
		}
		## Exit function function if it is an [Initialization] message and the toolkit has been relaunched
		If (($AsyncToolkitLaunch) -and ($ScriptSection -eq 'Initialization')) {
			[Boolean]$ExitLoggingFunction = $true; Return
		}

		## Create the directory where the log file will be saved
		If (-not (Test-Path -LiteralPath $LogFileDirectory -PathType 'Container')) {
			Try {
				$null = New-Item -Path $LogFileDirectory -Type 'Directory' -Force -ErrorAction 'Stop'
			} Catch {
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
					$ArchiveLogFileName = '{0}_{1}{2}' -f $LogFileNameWithoutExtension, $Timestamp, $LogFileExtension
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
					$LogFiles = @(Get-ChildItem -LiteralPath $LogFileDirectory -Filter ('{0}_*{1}' -f $LogFileNameWithoutExtension, $LogFileExtension)) + @(Get-Item -LiteralPath ([IO.Path]::ChangeExtension($LogFilePath, 'lo_')) -ErrorAction Ignore) | Sort-Object LastWriteTime

					# Keep only the max number of log files
					if ($LogFiles.Count -gt $MaxLogHistory) {
						$LogFiles | Select-Object -First ($LogFiles.Count - $MaxLogHistory) | Remove-Item -ErrorAction 'Stop'
					}
				}
			} Catch {
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
				} Else {
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
			} Else {
				[String]$LogLine = $LegacyTextLogLine
			}

			## Write the log entry to the log file if logging is not currently disabled
			If (-not $DisableLogging) {
				Try {
					$LogLine | Out-File -FilePath $LogFilePath -Append -NoClobber -Force -Encoding 'UTF8' -ErrorAction 'Stop'
				} Catch {
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

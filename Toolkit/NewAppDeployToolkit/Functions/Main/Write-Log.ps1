function Write-Log {
<#
.SYNOPSIS
	Write messages to a log file in CMTrace.exe compatible format or Legacy text file format.
.DESCRIPTION
	Write messages to a log file in CMTrace.exe compatible format or Legacy text file format and optionally display in the console.
.PARAMETER Message
	The message to write to the log file or output to the console.
.PARAMETER Severity
	Defines message type. When writing to console or CMTrace.exe log format, it allows highlighting of message type.
	Options: 1 = Information (default), 2 = Warning (highlighted in yellow), 3 = Error (highlighted in red)
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
.PARAMETER MaxLogFileSizeMB
	Maximum file size limit for log file in megabytes (MB). Default is 10 MB.
.PARAMETER WriteHost
	Write the log message to the console.
.PARAMETER ContinueOnError
	Suppress writing log message to console on failure to write message to log file. Default is: $true.
.PARAMETER PassThru
	Return the message that was passed to the function
.PARAMETER DebugMessage
	Specifies that the message is a debug message. Debug messages only get logged if -LogDebugMessage is set to $true.
.PARAMETER LogDebugMessage
	Debug messages only get logged if this parameter is set to $true in the config XML file.
.EXAMPLE
	Write-Log -Message "Installing patch MS15-031" -Source 'Add-Patch' -LogType 'CMTrace'
.EXAMPLE
	Write-Log -Message "Script is running on Windows 8" -Source 'Test-ValidOS' -LogType 'Legacy'
.EXAMPLE
	Write-Log -Message "Log only message" -WriteHost $false
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	param (
		[Parameter(
			Mandatory=$true,
			ValueFromPipeline=$true,
			ValueFromPipelineByPropertyName=$true
		)]
		[AllowEmptyCollection()]
		[Alias('Text')]
		[string[]]$Message,

		[ValidateRange(1,3)]
		[int]$Severity = 1,

		[ValidateNotNull()]
		[string]$Source,

		[ValidateNotNullorEmpty()]
		[string]$ScriptSection = $script:installPhase,

		[ValidateSet('CMTrace','Legacy')]
		[string]$LogType = $configToolkitLogStyle,

		[ValidateNotNullorEmpty()]
		[string]$LogFileDirectory = $(if ($configToolkitCompressLogs) { $logTempFolder } Else { $configToolkitLogDir }),

		[ValidateNotNullorEmpty()]
		[string]$LogFileName = $logName,

		[ValidateNotNullorEmpty()]
		[decimal]$MaxLogFileSizeMB = $configToolkitLogMaxSize,

		[ValidateNotNullorEmpty()]
		[boolean]$WriteHost = $configToolkitLogWriteToHost,

		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true,

		[switch]$PassThru,

		[switch]$DebugMessage,

		[boolean]$LogDebugMessage = $configToolkitLogDebugMessage
	)

	begin {
		## Get the name of this function
		$CmdletName = ($PSCmdlet.MyInvocation.MyCommand.Name)

		if(-not $Source){
			if($CmdletName) {
				$Source = $CmdletName
			} else {
				$Source = "Unknown"
			}
		}

		$DateTimeNow = (Get-Date)

		$LogTime = $DateTimeNow.ToString('HH\:mm\:ss.fff')
		$LogDate = $DateTimeNow.ToString('MM-dd-yyyy')

		if (-not ($LogTimeZoneBias)) {
			$LogTimeZoneBias = ([timezone]::CurrentTimeZone.GetUtcOffset($DateTimeNow).TotalMinutes)
		}

		$LogTimePlusBias = $LogTime + $script:LogTimeZoneBias

		# Initialize variables
		if($ExitLoggingFunction){
			$ExitLoggingFunction = $false
		}

		# Check if the script section is defined
		$ScriptSectionDefined = (-not [string]::IsNullOrEmpty($ScriptSection))

		# Get the file name of the source script
		try {
			if ($script:MyInvocation.Value.ScriptName) {
				$ScriptSource = Split-Path -Path $script:MyInvocation.Value.ScriptName -Leaf -ErrorAction 'Stop'
			} Else {
				$ScriptSource = Split-Path -Path $script:MyInvocation.MyCommand.Definition -Leaf -ErrorAction 'Stop'
			}
		} catch {
			$ScriptSource = ''
		}

		# Create script block for generating CMTrace.exe compatible log entry
		# These annoy me
		$CMTraceLogString = {
			Param (
				$lMessage,
				$lSource,
				$lSeverity
			)
			"<![LOG[$lMessage]LOG]!>" + "<time=`"$LogTimePlusBias`" " + "date=`"$LogDate`" " + "component=`"$lSource`" " + "context=`"$([Security.Principal.WindowsIdentity]::GetCurrent().Name)`" " + "type=`"$lSeverity`" " + "thread=`"$PID`" " + "file=`"$ScriptSource`">"
		}

		# Create script block for writing log entry to the console
		# These annoy me
		$WriteLogLineToHost = {
			Param (
				$lTextLogLine,
				$lSeverity
			)
			if ($WriteHost) {
				#  Only output using color options if running in a host which supports colors.
				if ($Host.UI.RawUI.ForegroundColor) {
					Switch ($lSeverity) {
						3 { Write-Host -Object $lTextLogLine -ForegroundColor 'Red' -BackgroundColor 'Black' }
						2 { Write-Host -Object $lTextLogLine -ForegroundColor 'Yellow' -BackgroundColor 'Black' }
						1 { Write-Host -Object $lTextLogLine }
					}
				}
				#  if executing "powershell.exe -File <filename>.ps1 > log.txt", then all the Write-Host calls are converted to Write-Output calls so that they are included in the text log.
				Else {
					Write-Output -InputObject $lTextLogLine
				}
			}
		}


		# Exit function if it is a debug message and logging debug messages is not enabled in the config XML file
		if (($DebugMessage) -and (-not $LogDebugMessage)) {
			$ExitLoggingFunction = $true
			Return
		}

		# Exit function if logging to file is disabled and logging to console host is disabled
		if (($DisableLogging) -and (-not $WriteHost)) {
			$ExitLoggingFunction = $true
			Return
		}

		# Exit Begin block if logging is disabled
		if ($DisableLogging){
			Return
		}

		# Exit function function if it is an [Initialization] message and the toolkit has been relaunched
		if (($AsyncToolkitLaunch) -and ($ScriptSection -eq 'Initialization')) {
			$ExitLoggingFunction = $true
			Return
		}

		# Create the directory where the log file will be saved
		if (-not (Test-Path -LiteralPath $LogFileDirectory -PathType 'Container')) {
			try {
				$null = New-Item -Path $LogFileDirectory -Type 'Directory' -Force -ErrorAction 'Stop'
			} catch {
				$ExitLoggingFunction = $true
				# if error creating directory, write message to console
				if (-not $ContinueOnError) {
					Write-Host -Object "[$LogDate $LogTime] [${CmdletName}] $ScriptSection :: Failed to create the log directory [$LogFileDirectory]. `r`n$(Resolve-Error)" -ForegroundColor 'Red'
				}
				Return
			}
		}

		# Assemble the fully qualified path to the log file
		$LogFilePath = Join-Path -Path $LogFileDirectory -ChildPath $LogFileName
	}

	process {
		# Exit function if logging is disabled
		if ($ExitLoggingFunction) {
			Return
		}

		ForEach ($Msg in $Message) {
			# if the message is not $null or empty, create the log entry for the different logging methods
			$CMTraceMsg = ''
			$ConsoleLogLine = ''
			$LegacyTextLogLine = ''
			if ($Msg) {

				# Create the CMTrace log message
				if ($ScriptSectionDefined) {
					$CMTraceMsg = "[$ScriptSection] :: $Msg"
				}

				# Create a Console and Legacy "text" log entry
				$LegacyMsg = "[$LogDate $LogTime]"
				if ($ScriptSectionDefined) {
					$LegacyMsg += " [$ScriptSection]"
				}

				if ($Source) {
					$ConsoleLogLine = "$LegacyMsg [$Source] :: $Msg"

					Switch ($Severity) {
						3 {
							$LegacyTextLogLine = "$LegacyMsg [$Source] [Error] :: $Msg"
						}

						2 {
							$LegacyTextLogLine = "$LegacyMsg [$Source] [Warning] :: $Msg"
						}

						1 {
							$LegacyTextLogLine = "$LegacyMsg [$Source] [Info] :: $Msg"
						}
					}
				} Else {
					$ConsoleLogLine = "$LegacyMsg :: $Msg"

					Switch ($Severity) {
						3 {
							$LegacyTextLogLine = "$LegacyMsg [Error] :: $Msg" 
						}

						2 {
							$LegacyTextLogLine = "$LegacyMsg [Warning] :: $Msg" 
						}

						1 {
							$LegacyTextLogLine = "$LegacyMsg [Info] :: $Msg" 
						}
					}
				}
			}

			# Execute script block to create the CMTrace.exe compatible log entry
			# Aha, this is where that annoying thing comes from.
			$CMTraceLogLine = (& $CMTraceLogString -lMessage $CMTraceMsg -lSource $Source -lSeverity $Severity)

			# Choose which log type to write to file
			if ($LogType -ieq 'CMTrace') {
				$LogLine = $CMTraceLogLine
			} Else {
				$LogLine = $LegacyTextLogLine
			}

			# Write the log entry to the log file if logging is not currently disabled
			if (-not $DisableLogging) {
				try {
					$LogLine | Out-File -FilePath $LogFilePath -Append -NoClobber -Force -Encoding 'UTF8' -ErrorAction 'Stop'
				}
				catch {
					if (-not $ContinueOnError) {
						Write-Host -Object "[$LogDate $LogTime] [$ScriptSection] [${CmdletName}] :: Failed to write message [$Msg] to the log file [$LogFilePath]. `r`n$(Resolve-Error)" -ForegroundColor 'Red'
					}
				}
			}

			# Execute script block to write the log entry to the console if $WriteHost is $true
			# And again.
			& $WriteLogLineToHost -lTextLogLine $ConsoleLogLine -lSeverity $Severity
		}
	}

	end {
		# Archive log file if size is greater than $MaxLogFileSizeMB and $MaxLogFileSizeMB > 0
		try {
			if ((-not $ExitLoggingFunction) -and (-not $DisableLogging)) {

				$LogFile = (Get-ChildItem -LiteralPath $LogFilePath -ErrorAction 'Stop')
				$LogFileSizeMB = $LogFile.Length/1MB

				if (($LogFileSizeMB -gt $MaxLogFileSizeMB) -and ($MaxLogFileSizeMB -gt 0)) {
					## Change the file extension to "lo_"
					$ArchivedOutLogFile = [IO.Path]::ChangeExtension($LogFilePath, 'lo_')

					$ArchiveLogParams = @{
						ScriptSection = $ScriptSection
						Source = ${CmdletName}
						Severity = 2
						LogFileDirectory = $LogFileDirectory
						LogFileName = $LogFileName
						LogType = $LogType
						MaxLogFileSizeMB = 0
						WriteHost = $WriteHost
						ContinueOnError = $ContinueOnError
						PassThru = $false
					}

					# Log message about archiving the log file
					$ArchiveLogMessage = "Maximum log file size [$MaxLogFileSizeMB MB] reached. Rename log file to [$ArchivedOutLogFile]."
					Write-Log -Message $ArchiveLogMessage @ArchiveLogParams

					# Archive existing log file from <filename>.log to <filename>.lo_. Overwrites any existing <filename>.lo_ file. This is the same method SCCM uses for log files.
					Move-Item -LiteralPath $LogFilePath -Destination $ArchivedOutLogFile -Force -ErrorAction 'Stop'

					# Start new log file and Log message about archiving the old log file
					$NewLogMessage = "Previous log file was renamed to [$ArchivedOutLogFile] because maximum log file size of [$MaxLogFileSizeMB MB] was reached."
					Write-Log -Message $NewLogMessage @ArchiveLogParams
				}
			}
		} catch {
			# if renaming of file fails, script will continue writing to log file even if size goes over the max file size
		} finally {
			if ($PassThru) {
				Write-Output -InputObject $Message
			}
		}
	}
}
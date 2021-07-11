#region Function Show-InstallationWelcome
Function Show-InstallationWelcome {
<#
.SYNOPSIS
	Show a welcome dialog prompting the user with information about the installation and actions to be performed before the installation can begin.
.DESCRIPTION
	The following prompts can be included in the welcome dialog:
	 a) Close the specified running applications, or optionally close the applications without showing a prompt (using the -Silent switch).
	 b) Defer the installation a certain number of times, for a certain number of days or until a deadline is reached.
	 c) Countdown until applications are automatically closed.
	 d) Prevent users from launching the specified applications while the installation is in progress.
	Notes:
	 The process descriptions are retrieved from WMI, with a fall back on the process name if no description is available. Alternatively, you can specify the description yourself with a '=' symbol - see examples.
	 The dialog box will timeout after the timeout specified in the XML configuration file (default 1 hour and 55 minutes) to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).
.PARAMETER CloseApps
	Name of the process to stop (do not include the .exe). Specify multiple processes separated by a comma. Specify custom descriptions like this: "winword=Microsoft Office Word,excel=Microsoft Office Excel"
.PARAMETER Silent
	Stop processes without prompting the user.
.PARAMETER CloseAppsCountdown
	Option to provide a countdown in seconds until the specified applications are automatically closed. This only takes effect if deferral is not allowed or has expired.
.PARAMETER ForceCloseAppsCountdown
	Option to provide a countdown in seconds until the specified applications are automatically closed regardless of whether deferral is allowed.
.PARAMETER PromptToSave
	Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button. Option does not work in SYSTEM context unless toolkit launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.
.PARAMETER PersistPrompt
	Specify whether to make the Show-InstallationWelcome prompt persist in the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml. The user will have no option but to respond to the prompt. This only takes effect if deferral is not allowed or has expired.
.PARAMETER BlockExecution
	Option to prevent the user from launching processes/applications, specified in -CloseApps, during the installation.
.PARAMETER AllowDefer
	Enables an optional defer button to allow the user to defer the installation.
.PARAMETER AllowDeferCloseApps
	Enables an optional defer button to allow the user to defer the installation only if there are running applications that need to be closed. This parameter automatically enables -AllowDefer
.PARAMETER DeferTimes
	Specify the number of times the installation can be deferred.
.PARAMETER DeferDays
	Specify the number of days since first run that the installation can be deferred. This is converted to a deadline.
.PARAMETER DeferDeadline
	Specify the deadline date until which the installation can be deferred.
	Specify the date in the local culture if the script is intended for that same culture.
	If the script is intended to run on EN-US machines, specify the date in the format: "08/25/2013" or "08-25-2013" or "08-25-2013 18:00:00"
	If the script is intended for multiple cultures, specify the date in the universal sortable date/time format: "2013-08-22 11:51:52Z"
	The deadline date will be displayed to the user in the format of their culture.
.PARAMETER CheckDiskSpace
	Specify whether to check if there is enough disk space for the installation to proceed.
	If this parameter is specified without the RequiredDiskSpace parameter, the required disk space is calculated automatically based on the size of the script source and associated files.
.PARAMETER RequiredDiskSpace
	Specify required disk space in MB, used in combination with CheckDiskSpace.
.PARAMETER MinimizeWindows
	Specifies whether to minimize other windows when displaying prompt. Default: $true.
.PARAMETER TopMost
	Specifies whether the windows is the topmost window. Default: $true.
.PARAMETER ForceCountdown
	Specify a countdown to display before automatically proceeding with the installation when a deferral is enabled.
.PARAMETER CustomText
	Specify whether to display a custom message specified in the XML file. Custom message must be populated for each language section in the XML.
.EXAMPLE
	Show-InstallationWelcome -CloseApps 'iexplore,winword,excel'
	Prompt the user to close Internet Explorer, Word and Excel.
.EXAMPLE
	Show-InstallationWelcome -CloseApps 'winword,excel' -Silent
	Close Word and Excel without prompting the user.
.EXAMPLE
	Show-InstallationWelcome -CloseApps 'winword,excel' -BlockExecution
	Close Word and Excel and prevent the user from launching the applications while the installation is in progress.
.EXAMPLE
	Show-InstallationWelcome -CloseApps 'winword=Microsoft Office Word,excel=Microsoft Office Excel' -CloseAppsCountdown 600
	Prompt the user to close Word and Excel, with customized descriptions for the applications and automatically close the applications after 10 minutes.
.EXAMPLE
	Show-InstallationWelcome -CloseApps 'winword,msaccess,excel' -PersistPrompt
	Prompt the user to close Word, MSAccess and Excel.
	By using the PersistPrompt switch, the dialog will return to the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml, so the user cannot ignore it by dragging it aside.
.EXAMPLE
	Show-InstallationWelcome -AllowDefer -DeferDeadline '25/08/2013'
	Allow the user to defer the installation until the deadline is reached.
.EXAMPLE
	Show-InstallationWelcome -CloseApps 'winword,excel' -BlockExecution -AllowDefer -DeferTimes 10 -DeferDeadline '25/08/2013' -CloseAppsCountdown 600
	Close Word and Excel and prevent the user from launching the applications while the installation is in progress.
	Allow the user to defer the installation a maximum of 10 times or until the deadline is reached, whichever happens first.
	When deferral expires, prompt the user to close the applications and automatically close them after 10 minutes.
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding(DefaultParametersetName='None')]

	Param (
		## Specify process names separated by commas. Optionally specify a process description with an equals symbol, e.g. "winword=Microsoft Office Word"
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$CloseApps,
		## Specify whether to prompt user or force close the applications
		[Parameter(Mandatory=$false)]
		[switch]$Silent = $false,
		## Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$CloseAppsCountdown = 0,
		## Specify a countdown to display before automatically closing applications whether or not deferral is allowed
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$ForceCloseAppsCountdown = 0,
		## Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button
		[Parameter(Mandatory=$false)]
		[switch]$PromptToSave = $false,
		## Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml.
		[Parameter(Mandatory=$false)]
		[switch]$PersistPrompt = $false,
		## Specify whether to block execution of the processes during installation
		[Parameter(Mandatory=$false)]
		[switch]$BlockExecution = $false,
		## Specify whether to enable the optional defer button on the dialog box
		[Parameter(Mandatory=$false)]
		[switch]$AllowDefer = $false,
		## Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed
		[Parameter(Mandatory=$false)]
		[switch]$AllowDeferCloseApps = $false,
		## Specify the number of times the deferral is allowed
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$DeferTimes = 0,
		## Specify the number of days since first run that the deferral is allowed
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$DeferDays = 0,
		## Specify the deadline (in format dd/mm/yyyy) for which deferral will expire as an option
		[Parameter(Mandatory=$false)]
		[string]$DeferDeadline = '',
		## Specify whether to check if there is enough disk space for the installation to proceed. If this parameter is specified without the RequiredDiskSpace parameter, the required disk space is calculated automatically based on the size of the script source and associated files.
		[Parameter(ParameterSetName = "CheckDiskSpaceParameterSet",Mandatory=$true)]
		[ValidateScript({$_.IsPresent -eq ($true -or $false)})]
		[switch]$CheckDiskSpace,
		## Specify required disk space in MB, used in combination with $CheckDiskSpace.
		[Parameter(ParameterSetName = "CheckDiskSpaceParameterSet",Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$RequiredDiskSpace = 0,
		## Specify whether to minimize other windows when displaying prompt
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$MinimizeWindows = $true,
		## Specifies whether the window is the topmost window
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$TopMost = $true,
		## Specify a countdown to display before automatically proceeding with the installation when a deferral is enabled
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$ForceCountdown = 0,
		## Specify whether to display a custom message specified in the XML file. Custom message must be populated for each language section in the XML.
		[Parameter(Mandatory=$false)]
		[switch]$CustomText = $false
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## If running in NonInteractive mode, force the processes to close silently
		If ($deployModeNonInteractive) { $Silent = $true }

		## If using Zero-Config MSI Deployment, append any executables found in the MSI to the CloseApps list
		If ($useDefaultMsi) { $CloseApps = "$CloseApps,$defaultMsiExecutablesList" }

		## Check disk space requirements if specified
		If ($CheckDiskSpace) {
			Write-Log -Message 'Evaluating disk space requirements.' -Source ${CmdletName}
			[double]$freeDiskSpace = Get-FreeDiskSpace
			If ($RequiredDiskSpace -eq 0) {
				Try {
					#  Determine the size of the Files folder
					$fso = New-Object -ComObject 'Scripting.FileSystemObject' -ErrorAction 'Stop'
					$RequiredDiskSpace = [math]::Round((($fso.GetFolder($scriptParentPath).Size) / 1MB))
				}
				Catch {
					Write-Log -Message "Failed to calculate disk space requirement from source files. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				}
			}
			If ($freeDiskSpace -lt $RequiredDiskSpace) {
				Write-Log -Message "Failed to meet minimum disk space requirement. Space Required [$RequiredDiskSpace MB], Space Available [$freeDiskSpace MB]." -Severity 3 -Source ${CmdletName}
				If (-not $Silent) {
					Show-InstallationPrompt -Message ($configDiskSpaceMessage -f $installTitle, $RequiredDiskSpace, ($freeDiskSpace)) -ButtonRightText 'OK' -Icon 'Error'
				}
				Exit-Script -ExitCode $configInstallationUIExitCode
			}
			Else {
				Write-Log -Message 'Successfully passed minimum disk space requirement check.' -Source ${CmdletName}
			}
		}

		If ($CloseApps) {
			## Create a Process object with custom descriptions where they are provided (split on an '=' sign)
			[psobject[]]$processObjects = @()
			#  Split multiple processes on a comma, then split on equal sign, then create custom object with process name and description
			ForEach ($process in ($CloseApps -split ',' | Where-Object { $_ })) {
				If ($process.Contains('=')) {
					[string[]]$ProcessSplit = $process -split '='
					$processObjects += New-Object -TypeName 'PSObject' -Property @{
						ProcessName = $ProcessSplit[0]
						ProcessDescription = $ProcessSplit[1]
					}
				}
				Else {
					[string]$ProcessInfo = $process
					$processObjects += New-Object -TypeName 'PSObject' -Property @{
						ProcessName = $process
						ProcessDescription = ''
					}
				}
			}
		}

		## Check Deferral history and calculate remaining deferrals
		If (($allowDefer) -or ($AllowDeferCloseApps)) {
			#  Set $allowDefer to true if $AllowDeferCloseApps is true
			$allowDefer = $true

			#  Get the deferral history from the registry
			$deferHistory = Get-DeferHistory
			$deferHistoryTimes = $deferHistory.DeferTimesRemaining
			$deferHistoryDeadline = $deferHistory.DeferDeadline

			#  Reset Switches
			$checkDeferDays = $false
			$checkDeferDeadline = $false
			If ($DeferDays -ne 0) { $checkDeferDays = $true }
			If ($DeferDeadline) { $checkDeferDeadline = $true }
			If ($DeferTimes -ne 0) {
				If ($deferHistoryTimes -ge 0) {
					Write-Log -Message "Defer history shows [$($deferHistory.DeferTimesRemaining)] deferrals remaining." -Source ${CmdletName}
					$DeferTimes = $deferHistory.DeferTimesRemaining - 1
				}
				Else {
					$DeferTimes = $DeferTimes - 1
				}
				Write-Log -Message "The user has [$deferTimes] deferrals remaining." -Source ${CmdletName}
				If ($DeferTimes -lt 0) {
					Write-Log -Message 'Deferral has expired.' -Source ${CmdletName}
					$AllowDefer = $false
				}
			}
			Else {
				If (Test-Path -LiteralPath 'variable:deferTimes') { Remove-Variable -Name 'deferTimes' }
				$DeferTimes = $null
			}
			If ($checkDeferDays -and $allowDefer) {
				If ($deferHistoryDeadline) {
					Write-Log -Message "Defer history shows a deadline date of [$deferHistoryDeadline]." -Source ${CmdletName}
					[string]$deferDeadlineUniversal = Get-UniversalDate -DateTime $deferHistoryDeadline
				}
				Else {
					[string]$deferDeadlineUniversal = Get-UniversalDate -DateTime (Get-Date -Date ((Get-Date).AddDays($deferDays)) -Format ($culture).DateTimeFormat.UniversalDateTimePattern).ToString()
				}
				Write-Log -Message "The user has until [$deferDeadlineUniversal] before deferral expires." -Source ${CmdletName}
				If ((Get-UniversalDate) -gt $deferDeadlineUniversal) {
					Write-Log -Message 'Deferral has expired.' -Source ${CmdletName}
					$AllowDefer = $false
				}
			}
			If ($checkDeferDeadline -and $allowDefer) {
				#  Validate Date
				Try {
					[string]$deferDeadlineUniversal = Get-UniversalDate -DateTime $deferDeadline -ErrorAction 'Stop'
				}
				Catch {
					Write-Log -Message "Date is not in the correct format for the current culture. Type the date in the current locale format, such as 20/08/2014 (Europe) or 08/20/2014 (United States). If the script is intended for multiple cultures, specify the date in the universal sortable date/time format, e.g. '2013-08-22 11:51:52Z'. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
					Throw "Date is not in the correct format for the current culture. Type the date in the current locale format, such as 20/08/2014 (Europe) or 08/20/2014 (United States). If the script is intended for multiple cultures, specify the date in the universal sortable date/time format, e.g. '2013-08-22 11:51:52Z': $($_.Exception.Message)"
				}
				Write-Log -Message "The user has until [$deferDeadlineUniversal] remaining." -Source ${CmdletName}
				If ((Get-UniversalDate) -gt $deferDeadlineUniversal) {
					Write-Log -Message 'Deferral has expired.' -Source ${CmdletName}
					$AllowDefer = $false
				}
			}
		}
		If (($deferTimes -lt 0) -and (-not ($deferDeadlineUniversal))) { $AllowDefer = $false }

		## Prompt the user to close running applications and optionally defer if enabled
		If (-not ($deployModeSilent) -and (-not ($silent))) {
			If ($forceCloseAppsCountdown -gt 0) {
				#  Keep the same variable for countdown to simplify the code:
				$closeAppsCountdown = $forceCloseAppsCountdown
				#  Change this variable to a boolean now to switch the countdown on even with deferral
				[boolean]$forceCloseAppsCountdown = $true
			}
			ElseIf ($forceCountdown -gt 0){
				#  Keep the same variable for countdown to simplify the code:
				$closeAppsCountdown = $forceCountdown
				#  Change this variable to a boolean now to switch the countdown on
				[boolean]$forceCountdown = $true
			}
			Set-Variable -Name 'closeAppsCountdownGlobal' -Value $closeAppsCountdown -Scope 'Script'

			While (($runningProcesses = Get-RunningProcesses -ProcessObjects $processObjects) -or (($promptResult -ne 'Defer') -and ($promptResult -ne 'Close'))) {
				[string]$runningProcessDescriptions = ($runningProcesses.ProcessDescription | Sort-Object -Unique) -join ','
				#  Check if we need to prompt the user to defer, to defer and close apps, or not to prompt them at all
				If ($allowDefer) {
					#  If there is deferral and closing apps is allowed but there are no apps to be closed, break the while loop
					If ($AllowDeferCloseApps -and (-not $runningProcessDescriptions)) {
						Break
					}
					#  Otherwise, as long as the user has not selected to close the apps or the processes are still running and the user has not selected to continue, prompt user to close running processes with deferral
					ElseIf (($promptResult -ne 'Close') -or (($runningProcessDescriptions) -and ($promptResult -ne 'Continue'))) {
						[string]$promptResult = Show-WelcomePrompt -ProcessDescriptions $runningProcessDescriptions -CloseAppsCountdown $closeAppsCountdownGlobal -ForceCloseAppsCountdown $forceCloseAppsCountdown -ForceCountdown $forceCountdown -PersistPrompt $PersistPrompt -AllowDefer -DeferTimes $deferTimes -DeferDeadline $deferDeadlineUniversal -MinimizeWindows $MinimizeWindows -CustomText:$CustomText -TopMost $TopMost
					}
				}
				#  If there is no deferral and processes are running, prompt the user to close running processes with no deferral option
				ElseIf (($runningProcessDescriptions) -or ($forceCountdown)) {
					[string]$promptResult = Show-WelcomePrompt -ProcessDescriptions $runningProcessDescriptions -CloseAppsCountdown $closeAppsCountdownGlobal -ForceCloseAppsCountdown $forceCloseAppsCountdown -ForceCountdown $forceCountdown -PersistPrompt $PersistPrompt -MinimizeWindows $minimizeWindows -CustomText:$CustomText -TopMost $TopMost
				}
				#  If there is no deferral and no processes running, break the while loop
				Else {
					Break
				}

				#  If the user has clicked OK, wait a few seconds for the process to terminate before evaluating the running processes again
				If ($promptResult -eq 'Continue') {
					Write-Log -Message 'The user selected to continue...' -Source ${CmdletName}
					Start-Sleep -Seconds 2

					#  Break the while loop if there are no processes to close and the user has clicked OK to continue
					If (-not $runningProcesses) { Break }
				}
				#  Force the applications to close
				ElseIf ($promptResult -eq 'Close') {
					Write-Log -Message 'The user selected to force the application(s) to close...' -Source ${CmdletName}
					If (($PromptToSave) -and ($SessionZero -and (-not $IsProcessUserInteractive))) {
						Write-Log -Message 'Specified [-PromptToSave] option will not be available, because current process is running in session zero and is not interactive.' -Severity 2 -Source ${CmdletName}
					}
					# Update the process list right before closing, in case it changed
					$runningProcesses = Get-RunningProcesses -ProcessObjects $processObjects
					# Close running processes
					ForEach ($runningProcess in $runningProcesses) {
						[psobject[]]$AllOpenWindowsForRunningProcess = Get-WindowTitle -GetAllWindowTitles -DisableFunctionLogging | Where-Object { $_.ParentProcess -eq $runningProcess.ProcessName }
						#  If the PromptToSave parameter was specified and the process has a window open, then prompt the user to save work if there is work to be saved when closing window
						If (($PromptToSave) -and (-not ($SessionZero -and (-not $IsProcessUserInteractive))) -and ($AllOpenWindowsForRunningProcess) -and ($runningProcess.MainWindowHandle -ne [IntPtr]::Zero)) {
							[timespan]$PromptToSaveTimeout = New-TimeSpan -Seconds $configInstallationPromptToSave
							[Diagnostics.StopWatch]$PromptToSaveStopWatch = [Diagnostics.StopWatch]::StartNew()
							$PromptToSaveStopWatch.Reset()
							ForEach ($OpenWindow in $AllOpenWindowsForRunningProcess) {
								Try {
									Write-Log -Message "Stopping process [$($runningProcess.ProcessName)] with window title [$($OpenWindow.WindowTitle)] and prompt to save if there is work to be saved (timeout in [$configInstallationPromptToSave] seconds)..." -Source ${CmdletName}
									[boolean]$IsBringWindowToFrontSuccess = [PSADT.UiAutomation]::BringWindowToFront($OpenWindow.WindowHandle)
									[boolean]$IsCloseWindowCallSuccess = $runningProcess.CloseMainWindow()
									If (-not $IsCloseWindowCallSuccess) {
										Write-Log -Message "Failed to call the CloseMainWindow() method on process [$($runningProcess.ProcessName)] with window title [$($OpenWindow.WindowTitle)] because the main window may be disabled due to a modal dialog being shown." -Severity 3 -Source ${CmdletName}
									}
									Else {
										$PromptToSaveStopWatch.Start()
										Do {
											[boolean]$IsWindowOpen = [boolean](Get-WindowTitle -GetAllWindowTitles -DisableFunctionLogging | Where-Object { $_.WindowHandle -eq $OpenWindow.WindowHandle })
											If (-not $IsWindowOpen) { Break }
											Start-Sleep -Seconds 3
										} While (($IsWindowOpen) -and ($PromptToSaveStopWatch.Elapsed -lt $PromptToSaveTimeout))
										$PromptToSaveStopWatch.Reset()
										If ($IsWindowOpen) {
											Write-Log -Message "Exceeded the [$configInstallationPromptToSave] seconds timeout value for the user to save work associated with process [$($runningProcess.ProcessName)] with window title [$($OpenWindow.WindowTitle)]." -Severity 2 -Source ${CmdletName}
										}
										Else {
											Write-Log -Message "Window [$($OpenWindow.WindowTitle)] for process [$($runningProcess.ProcessName)] was successfully closed." -Source ${CmdletName}
										}
									}
								}
								Catch {
									Write-Log -Message "Failed to close window [$($OpenWindow.WindowTitle)] for process [$($runningProcess.ProcessName)]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
									Continue
								}
								Finally {
									$runningProcess.Refresh()
								}
							}
						}
						Else {
							Write-Log -Message "Stopping process $($runningProcess.ProcessName)..." -Source ${CmdletName}
							Stop-Process -Name $runningProcess.ProcessName -Force -ErrorAction 'SilentlyContinue'
						}
					}

					if ($runningProcesses = Get-RunningProcesses -ProcessObjects $processObjects -DisableLogging) {
						# Apps are still running, give them 2s to close. If they are still running, the Welcome Window will be displayed again
						Write-Log -Message 'Sleeping for 2 seconds because the processes are still not closed...' -Source ${CmdletName}
						Start-Sleep -Seconds 2
					}
				}
				#  Stop the script (if not actioned before the timeout value)
				ElseIf ($promptResult -eq 'Timeout') {
					Write-Log -Message 'Installation not actioned before the timeout value.' -Source ${CmdletName}
					$BlockExecution = $false

					If (($deferTimes -ge 0) -or ($deferDeadlineUniversal)) {
						Set-DeferHistory -DeferTimesRemaining $DeferTimes -DeferDeadline $deferDeadlineUniversal
					}
					## Dispose the welcome prompt timer here because if we dispose it within the Show-WelcomePrompt function we risk resetting the timer and missing the specified timeout period
					If ($script:welcomeTimer) {
						Try {
							$script:welcomeTimer.Dispose()
							$script:welcomeTimer = $null
						}
						Catch { }
					}

					#  Restore minimized windows
					$null = $shellApp.UndoMinimizeAll()

					Exit-Script -ExitCode $configInstallationUIExitCode
				}
				#  Stop the script (user chose to defer)
				ElseIf ($promptResult -eq 'Defer') {
					Write-Log -Message 'Installation deferred by the user.' -Source ${CmdletName}
					$BlockExecution = $false

					Set-DeferHistory -DeferTimesRemaining $DeferTimes -DeferDeadline $deferDeadlineUniversal

					#  Restore minimized windows
					$null = $shellApp.UndoMinimizeAll()

					Exit-Script -ExitCode $configInstallationDeferExitCode
				}
			}
		}

		## Force the processes to close silently, without prompting the user
		If (($Silent -or $deployModeSilent) -and $CloseApps) {
			[array]$runningProcesses = $null
			[array]$runningProcesses = Get-RunningProcesses $processObjects
			If ($runningProcesses) {
				[string]$runningProcessDescriptions = ($runningProcesses.ProcessDescription | Sort-Object -Unique) -join ','
				Write-Log -Message "Force closing application(s) [$($runningProcessDescriptions)] without prompting user." -Source ${CmdletName}
				$runningProcesses.ProcessName | ForEach-Object -Process {Stop-Process -Name $_ -Force -ErrorAction 'SilentlyContinue'}
				Start-Sleep -Seconds 2
			}
		}

		## Force nsd.exe to stop if Notes is one of the required applications to close
		If (($processObjects).ProcessName -contains 'notes') {
			## Get the path where Notes is installed
			[string]$notesPath = (Get-Item -LiteralPath $regKeyLotusNotes -ErrorAction 'SilentlyContinue' | Get-ItemProperty).Path

			## Ensure we aren't running as a Local System Account and Notes install directory was found
			If ((-not $IsLocalSystemAccount) -and ($notesPath)) {
				#  Get a list of all the executables in the Notes folder
				[string[]]$notesPathExes = (Get-ChildItem -LiteralPath $notesPath -Filter '*.exe' -Recurse).BaseName | Sort-Object
				## Check for running Notes executables and run NSD if any are found
				$notesPathExes | ForEach-Object {
					If ((Get-Process).ProcessName -contains $_) {
						[string]$notesNSDExecutable = Join-Path -Path $notesPath -ChildPath 'NSD.exe'
						Try {
							If (Test-Path -LiteralPath $notesNSDExecutable -PathType 'Leaf' -ErrorAction 'Stop') {
								Write-Log -Message "Executing [$notesNSDExecutable] with the -kill argument..." -Source ${CmdletName}
								[Diagnostics.Process]$notesNSDProcess = Start-Process -FilePath $notesNSDExecutable -ArgumentList '-kill' -WindowStyle 'Hidden' -PassThru -ErrorAction 'SilentlyContinue'

								If (-not ($notesNSDProcess.WaitForExit(10000))) {
									Write-Log -Message "[$notesNSDExecutable] did not end in a timely manner. Force terminate process." -Source ${CmdletName}
									Stop-Process -Name 'NSD' -Force -ErrorAction 'SilentlyContinue'
								}
							}
						}
						Catch {
							Write-Log -Message "Failed to launch [$notesNSDExecutable]. `r`n$(Resolve-Error)" -Source ${CmdletName}
						}

						Write-Log -Message "[$notesNSDExecutable] returned exit code [$($notesNSDProcess.ExitCode)]." -Source ${CmdletName}

						#  Force NSD process to stop in case the previous command was not successful
						Stop-Process -Name 'NSD' -Force -ErrorAction 'SilentlyContinue'
					}
				}
			}

			#  Strip all Notes processes from the process list except notes.exe, because the other notes processes (e.g. notes2.exe) may be invoked by the Notes installation, so we don't want to block their execution.
			If ($notesPathExes) {
				[array]$processesIgnoringNotesExceptions = Compare-Object -ReferenceObject ($processObjects.ProcessName | Sort-Object) -DifferenceObject $notesPathExes -IncludeEqual | ForEach-Object { if (($_.SideIndicator -eq '<=') -or ($_.InputObject -eq 'notes')) {$_.InputObject} }
				[array]$processObjects = $processObjects | Where-Object { $processesIgnoringNotesExceptions -contains $_.ProcessName }
			}
		}

		## If block execution switch is true, call the function to block execution of these processes
		If ($BlockExecution) {
			#  Make this variable globally available so we can check whether we need to call Unblock-AppExecution
			Set-Variable -Name 'BlockExecution' -Value $BlockExecution -Scope 'Script'
			Write-Log -Message '[-BlockExecution] parameter specified.' -Source ${CmdletName}
			Block-AppExecution -ProcessNames ($processObjects.ProcessName)
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion

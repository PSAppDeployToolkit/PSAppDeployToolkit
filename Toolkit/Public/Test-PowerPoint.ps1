Function Test-PowerPoint {
	<#
.SYNOPSIS

Tests whether PowerPoint is running in either fullscreen slideshow mode or presentation mode.

.DESCRIPTION

Tests whether someone is presenting using PowerPoint in either fullscreen slideshow mode or presentation mode.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if PowerPoint is running in either fullscreen slideshow mode or presentation mode, otherwise returns $false.

.EXAMPLE

Test-PowerPoint

.NOTES

This function can only execute detection logic if the process is in interactive mode.

There is a possiblity of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show".

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message 'Checking if PowerPoint is in either fullscreen slideshow mode or presentation mode...' -Source ${CmdletName}
			Try {
				[Diagnostics.Process[]]$PowerPointProcess = Get-Process -ErrorAction 'Stop' | Where-Object { $_.ProcessName -eq 'POWERPNT' }
				If ($PowerPointProcess) {
					[Boolean]$IsPowerPointRunning = $true
					Write-Log -Message 'PowerPoint application is running.' -Source ${CmdletName}
				} Else {
					[Boolean]$IsPowerPointRunning = $false
					Write-Log -Message 'PowerPoint application is not running.' -Source ${CmdletName}
				}
			} Catch {
				Throw
			}

			[Nullable[Boolean]]$IsPowerPointFullScreen = $false
			If ($IsPowerPointRunning) {
				## Detect if PowerPoint is in fullscreen mode or Presentation Mode, detection method only works if process is interactive
				If ([Environment]::UserInteractive) {
					#  Check if "POWERPNT" process has a window with a title that begins with "PowerPoint Slide Show" or "Powerpoint-" for non-English language systems.
					#  There is a possiblity of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show"
					[PSObject]$PowerPointWindow = Get-WindowTitle -GetAllWindowTitles | Where-Object { $_.WindowTitle -match '^PowerPoint Slide Show' -or $_.WindowTitle -match '^PowerPoint-' } | Where-Object { $_.ParentProcess -eq 'POWERPNT' } | Select-Object -First 1
					If ($PowerPointWindow) {
						[Nullable[Boolean]]$IsPowerPointFullScreen = $true
						Write-Log -Message 'Detected that PowerPoint process [POWERPNT] has a window with a title that beings with [PowerPoint Slide Show] or [PowerPoint-].' -Source ${CmdletName}
					} Else {
						Write-Log -Message 'Detected that PowerPoint process [POWERPNT] does not have a window with a title that beings with [PowerPoint Slide Show] or [PowerPoint-].' -Source ${CmdletName}
						Try {
							[Int32[]]$PowerPointProcessIDs = $PowerPointProcess | Select-Object -ExpandProperty 'Id' -ErrorAction 'Stop'
							Write-Log -Message "PowerPoint process [POWERPNT] has process id(s) [$($PowerPointProcessIDs -join ', ')]." -Source ${CmdletName}
						} Catch {
							Write-Log -Message "Unable to retrieve process id(s) for [POWERPNT] process. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
						}
					}

					## If previous detection method did not detect PowerPoint in fullscreen mode, then check if PowerPoint is in Presentation Mode (check only works on Windows Vista or higher)
					If ((-not $IsPowerPointFullScreen) -and (([Version]$envOSVersion).Major -gt 5)) {
						#  Note: below method does not detect PowerPoint presentation mode if the presentation is on a monitor that does not have current mouse input control
						[String]$UserNotificationState = [PSADT.UiAutomation]::GetUserNotificationState()
						Write-Log -Message "Detected user notification state [$UserNotificationState]." -Source ${CmdletName}
						Switch ($UserNotificationState) {
							'PresentationMode' {
								Write-Log -Message 'Detected that system is in [Presentation Mode].' -Source ${CmdletName}
								[Nullable[Boolean]]$IsPowerPointFullScreen = $true
							}
							'FullScreenOrPresentationModeOrLoginScreen' {
								If (([String]$PowerPointProcessIDs) -and ($PowerPointProcessIDs -contains [PSADT.UIAutomation]::GetWindowThreadProcessID([PSADT.UIAutomation]::GetForeGroundWindow()))) {
									Write-Log -Message 'Detected that fullscreen foreground window matches PowerPoint process id.' -Source ${CmdletName}
									[Nullable[Boolean]]$IsPowerPointFullScreen = $true
								}
							}
						}
					}
				} Else {
					[Nullable[Boolean]]$IsPowerPointFullScreen = $null
					Write-Log -Message 'Unable to run check to see if PowerPoint is in fullscreen mode or Presentation Mode because current process is not interactive. Configure script to run in interactive mode in your deployment tool. If using SCCM Application Model, then make sure "Allow users to view and interact with the program installation" is selected. If using SCCM Package Model, then make sure "Allow users to interact with this program" is selected.' -Severity 2 -Source ${CmdletName}
				}
			}
		} Catch {
			[Nullable[Boolean]]$IsPowerPointFullScreen = $null
			Write-Log -Message "Failed check to see if PowerPoint is running in fullscreen slideshow mode. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
		}
	}
	End {
		Write-Log -Message "PowerPoint is running in fullscreen mode [$IsPowerPointFullScreen]." -Source ${CmdletName}
		Write-Output -InputObject ($IsPowerPointFullScreen)
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

#region Function Show-InstallationRestartPrompt
Function Show-InstallationRestartPrompt {
<#
.SYNOPSIS
	Displays a restart prompt with a countdown to a forced restart.
.DESCRIPTION
	Displays a restart prompt with a countdown to a forced restart.
.PARAMETER CountdownSeconds
	Specifies the number of seconds to countdown before the system restart. Default: 60
.PARAMETER CountdownNoHideSeconds
	Specifies the number of seconds to display the restart prompt without allowing the window to be hidden. Default: 30
.PARAMETER NoSilentRestart
	Specifies whether the restart should be triggered when Deploy mode is silent or very silent. Default: $true
.PARAMETER NoCountdown
	Specifies not to show a countdown.
	The UI will restore/reposition itself persistently based on the interval value specified in the config file.
.PARAMETER SilentCountdownSeconds
	Specifies number of seconds to countdown for the restart when the toolkit is running in silent mode and NoSilentRestart is $false. Default: 5
.PARAMETER TopMost
	Specifies whether the windows is the topmost window. Default: $true.
.EXAMPLE
	Show-InstallationRestartPrompt -Countdownseconds 600 -CountdownNoHideSeconds 60
.EXAMPLE
	Show-InstallationRestartPrompt -NoCountdown
.EXAMPLE
	Show-InstallationRestartPrompt -Countdownseconds 300 -NoSilentRestart $false -SilentCountdownSeconds 10
.NOTES
	Be mindful of the countdown you specify for the reboot as code directly after this function might NOT be able to execute - that includes logging.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$CountdownSeconds = 60,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$CountdownNoHideSeconds = 30,
		[Parameter(Mandatory=$false)]
		[bool]$NoSilentRestart = $true,
		[Parameter(Mandatory=$false)]
		[switch]$NoCountdown = $false,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[int32]$SilentCountdownSeconds = 5,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$TopMost = $true
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## If in non-interactive mode
		If ($deployModeSilent) {
            If ($NoSilentRestart -eq $false) {
				Write-Log -Message "Triggering restart silently, because the deploy mode is set to [$deployMode] and [NoSilentRestart] is disabled. Timeout is set to [$SilentCountdownSeconds] seconds." -Source ${CmdletName}
				Start-Process -FilePath "$PSHOME\powershell.exe" -ArgumentList "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command `'&{ Start-Sleep -Seconds $SilentCountdownSeconds; Restart-Computer -Force; }`'" -WindowStyle 'Hidden' -ErrorAction 'SilentlyContinue'   
            }
            Else {
                Write-Log -Message "Skipping restart, because the deploy mode is set to [$deployMode] and [NoSilentRestart] is enabled." -Source ${CmdletName}
            }
			Return
		}
		## Get the parameters passed to the function for invoking the function asynchronously
		[hashtable]$installRestartPromptParameters = $psBoundParameters

		## Check if we are already displaying a restart prompt
		If (Get-Process | Where-Object { $_.MainWindowTitle -match $configRestartPromptTitle }) {
			Write-Log -Message "${CmdletName} was invoked, but an existing restart prompt was detected. Cancelling restart prompt." -Severity 2 -Source ${CmdletName}
			Return
		}

		## If the script has been dot-source invoked by the deploy app script, display the restart prompt asynchronously
		If ($deployAppScriptFriendlyName) {
			If ($NoCountdown) {
				Write-Log -Message "Invoking ${CmdletName} asynchronously with no countdown..." -Source ${CmdletName}
			}
			Else {
				Write-Log -Message "Invoking ${CmdletName} asynchronously with a [$countDownSeconds] second countdown..." -Source ${CmdletName}
			}
			## Remove Silent reboot parameters from the list that is being forwarded to the main script for asynchronous function execution. This is only for Interactive mode so we dont need silent mode reboot parameters. 
			$installRestartPromptParameters.Remove("NoSilentRestart")
			$installRestartPromptParameters.Remove("SilentCountdownSeconds")
			## Prepare a list of parameters of this function as a string
			[string]$installRestartPromptParameters = ($installRestartPromptParameters.GetEnumerator() | ForEach-Object $ResolveParameters) -join ' '
			## Start another powershell instance silently with function parameters from this function
			Start-Process -FilePath "$PSHOME\powershell.exe" -ArgumentList "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command &{& `'$scriptPath`' -ReferredInstallTitle `'$installTitle`' -ReferredInstallName `'$installName`' -ReferredLogName `'$logName`' -ShowInstallationRestartPrompt $installRestartPromptParameters -AsyncToolkitLaunch}" -WindowStyle 'Hidden' -ErrorAction 'SilentlyContinue'
			return
		}

		[datetime]$startTime = Get-Date
		[datetime]$countdownTime = $startTime

		[Windows.Forms.Application]::EnableVisualStyles()
		$formRestart = New-Object -TypeName 'System.Windows.Forms.Form'
		$labelCountdown = New-Object -TypeName 'System.Windows.Forms.Label'
		$labelTimeRemaining = New-Object -TypeName 'System.Windows.Forms.Label'
		$labelMessage = New-Object -TypeName 'System.Windows.Forms.Label'
		$buttonRestartLater = New-Object -TypeName 'System.Windows.Forms.Button'
		$pictureBanner = New-Object -TypeName 'System.Windows.Forms.PictureBox'
		$buttonRestartNow = New-Object -TypeName 'System.Windows.Forms.Button'
		$timerCountdown = New-Object -TypeName 'System.Windows.Forms.Timer'
		$flowLayoutPanel = New-Object -TypeName 'System.Windows.Forms.FlowLayoutPanel'
		$panelButtons = New-Object -TypeName 'System.Windows.Forms.Panel'

		[scriptblock]$RestartComputer = {
			Write-Log -Message 'Forcefully restarting the computer...' -Source ${CmdletName}
			Restart-Computer -Force
		}

		[scriptblock]$Restart_Form_StateCorrection_Load = {
			# Disable the X button
			try {
				$windowHandle = $formRestart.Handle
				If ($windowHandle -and ($windowHandle -ne [IntPtr]::Zero)) {
					$menuHandle = [PSADT.UiAutomation]::GetSystemMenu($windowHandle, $false)
					If ($menuHandle -and ($menuHandle -ne [IntPtr]::Zero)) {
						[PSADT.UiAutomation]::EnableMenuItem($menuHandle, 0xF060, 0x00000001)
						[PSADT.UiAutomation]::DestroyMenu($menuHandle)
					}
				}
			}
			catch {
				# Not a terminating error if we can't disable the button. Just disable the Control Box instead
				Write-Log "Failed to disable the Close button. Disabling the Control Box instead." -Severity 2 -Source ${CmdletName}
				$formRestart.ControlBox = $false
			}
			## Initialize the countdown timer
			[datetime]$currentTime = Get-Date
			[datetime]$countdownTime = $startTime.AddSeconds($countdownSeconds)
			$timerCountdown.Start()
			## Set up the form
			[timespan]$remainingTime = $countdownTime.Subtract($currentTime)
			$labelCountdown.Text = [string]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
			If ($remainingTime.TotalSeconds -le $countdownNoHideSeconds) { $buttonRestartLater.Enabled = $false }
			$formRestart.WindowState = 'Normal'
			$formRestart.AutoSize = $true
			$formRestart.TopMost = $TopMost
			$formRestart.BringToFront()
			## Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
			Set-Variable -Name 'formInstallationRestartPromptStartPosition' -Value $formRestart.Location -Scope 'Script'
		}

		## Persistence Timer
		If ($NoCountdown) {
			$restartTimerPersist = New-Object -TypeName 'System.Windows.Forms.Timer'
			$restartTimerPersist.Interval = ($configInstallationRestartPersistInterval * 1000)
			[scriptblock]$restartTimerPersist_Tick = {
				#  Show the Restart Popup
				$formRestart.WindowState = 'Normal'
				$formRestart.TopMost = $TopMost
				$formRestart.BringToFront()
				$formRestart.Location = "$($formInstallationRestartPromptStartPosition.X),$($formInstallationRestartPromptStartPosition.Y)"
			}
			$restartTimerPersist.add_Tick($restartTimerPersist_Tick)
			$restartTimerPersist.Start()
		}

		[scriptblock]$buttonRestartLater_Click = {
			## Minimize the form
			$formRestart.WindowState = 'Minimized'
			If ($NoCountdown) {
				## Reset the persistence timer
				$restartTimerPersist.Stop()
				$restartTimerPersist.Start()
			}
		}

		## Restart the computer
		[scriptblock]$buttonRestartNow_Click = { & $RestartComputer }

		## Hide the form if minimized
		[scriptblock]$formRestart_Resize = { If ($formRestart.WindowState -eq 'Minimized') { $formRestart.WindowState = 'Minimized' } }

		[scriptblock]$timerCountdown_Tick = {
			## Get the time information
			[datetime]$currentTime = Get-Date
			[datetime]$countdownTime = $startTime.AddSeconds($countdownSeconds)
			[timespan]$remainingTime = $countdownTime.Subtract($currentTime)
			## If the countdown is complete, restart the machine
			If ($countdownTime -le $currentTime) {
				$buttonRestartNow.PerformClick()
			}
			Else {
				## Update the form
				$labelCountdown.Text = [string]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
				If ($remainingTime.TotalSeconds -le $countdownNoHideSeconds) {
					$buttonRestartLater.Enabled = $false
					#  If the form is hidden when we hit the "No Hide", bring it back up
					If ($formRestart.WindowState -eq 'Minimized') {
						#  Show Popup
						$formRestart.WindowState = 'Normal'
						$formRestart.TopMost = $TopMost
						$formRestart.BringToFront()
						$formRestart.Location = "$($formInstallationRestartPromptStartPosition.X),$($formInstallationRestartPromptStartPosition.Y)"
					}
				}
			}
		}

		## Remove all event handlers from the controls
		[scriptblock]$Restart_Form_Cleanup_FormClosed = {
			Try {
				$buttonRestartLater.remove_Click($buttonRestartLater_Click)
				$buttonRestartNow.remove_Click($buttonRestartNow_Click)
				$formRestart.remove_Load($Restart_Form_StateCorrection_Load)
				$formRestart.remove_Resize($formRestart_Resize)
				$timerCountdown.remove_Tick($timerCountdown_Tick)
				$restartTimerPersist.remove_Tick($restartTimerPersist_Tick)
				$formRestart.remove_FormClosed($Restart_Form_Cleanup_FormClosed)
			}
			Catch { }
		}

		## Form
		##----------------------------------------------
		## Create zero px padding object
		$paddingNone = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList 0,0,0,0
		## Create basic control size
		$defaultControlSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList 450,0

		## Generic Button properties
		$buttonSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList 195,24

		## Picture Banner
		$pictureBanner.DataBindings.DefaultDataSourceUpdateMode = 0
		$pictureBanner.ImageLocation = $appDeployLogoBanner
		$System_Drawing_Point = New-Object -TypeName 'System.Drawing.Point' -ArgumentList 0,0
		$pictureBanner.Location = $System_Drawing_Point
		$pictureBanner.Name = 'pictureBanner'
		$System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList 450,$appDeployLogoBannerHeight
		$pictureBanner.Size = $System_Drawing_Size
		$pictureBanner.SizeMode = 'CenterImage'
		$pictureBanner.Margin = $paddingNone
		$pictureBanner.TabStop = $false

		## Label Message
		$labelMessage.DataBindings.DefaultDataSourceUpdateMode = 0
		$labelMessage.Name = 'labelMessage'
		$labelMessage.Size = $defaultControlSize
		$labelMessage.MinimumSize = $defaultControlSize
		$labelMessage.MaximumSize = $defaultControlSize
		$labelMessage.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList 0,10,0,5
		$labelMessage.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList 10,0,10,0
		$labelMessage.Text = "$configRestartPromptMessage $configRestartPromptMessageTime `r`n`r`n$configRestartPromptMessageRestart"
		If ($NoCountdown) { $labelMessage.Text = $configRestartPromptMessage }
		$labelMessage.TextAlign = 'MiddleCenter'
		$labelMessage.Anchor = 'Top'
		$labelMessage.TabStop = $false
		$labelMessage.AutoSize = $true

		## Label Time remaining message
		$labelTimeRemaining.DataBindings.DefaultDataSourceUpdateMode = 0
		$labelTimeRemaining.Name = 'labelTimeRemaining'
		$labelTimeRemaining.Size = $defaultControlSize
		$labelTimeRemaining.MinimumSize = $defaultControlSize
		$labelTimeRemaining.MaximumSize = $defaultControlSize
		$labelTimeRemaining.Margin = $paddingNone
		$labelTimeRemaining.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList 10,0,10,0
		$labelTimeRemaining.TabStop = $false
		$labelTimeRemaining.Font = 'Microsoft Sans Serif, 9pt, style=Bold'
		$labelTimeRemaining.Text = $configRestartPromptTimeRemaining
		$labelTimeRemaining.TextAlign = 'MiddleCenter'
		$labelTimeRemaining.Anchor = 'Top'
		$labelTimeRemaining.AutoSize = $true

		## Label Countdown
		$labelCountdown.DataBindings.DefaultDataSourceUpdateMode = 0
		$labelCountdown.Font = 'Microsoft Sans Serif, 18pt, style=Bold'
		$labelCountdown.Name = 'labelCountdown'
		$labelCountdown.Size = $defaultControlSize
		$labelCountdown.MinimumSize = $defaultControlSize
		$labelCountdown.MaximumSize = $defaultControlSize
		$labelCountdown.Margin = $paddingNone
		$labelCountdown.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList 10,0,10,0
		$labelCountdown.TabStop = $false
		$labelCountdown.Text = '00:00:00'
		$labelCountdown.TextAlign = 'MiddleCenter'
		$labelCountdown.AutoSize = $true

		## Panel Flow Layout
		$System_Drawing_Point = New-Object -TypeName 'System.Drawing.Point' -ArgumentList 0,$appDeployLogoBannerHeight
		$flowLayoutPanel.Location = $System_Drawing_Point
		$flowLayoutPanel.MinimumSize = $DefaultControlSize
		$flowLayoutPanel.MaximumSize = $DefaultControlSize
		$flowLayoutPanel.Size = $DefaultControlSize
		$flowLayoutPanel.Margin = $paddingNone
		$flowLayoutPanel.Padding = $paddingNone
		$flowLayoutPanel.AutoSizeMode = "GrowAndShrink"
		$flowLayoutPanel.AutoSize = $true
		$flowLayoutPanel.Anchor = 'Top'
		$flowLayoutPanel.FlowDirection = 'TopDown'
		$flowLayoutPanel.WrapContents = $true
		$flowLayoutPanel.Controls.Add($labelMessage)
		If (-not $NoCountdown) {
			$flowLayoutPanel.Controls.Add($labelTimeRemaining)
			$flowLayoutPanel.Controls.Add($labelCountdown)
		}

		## Button Minimize
		$buttonRestartLater.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonRestartLater.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList 240,4
		$buttonRestartLater.Name = 'buttonRestartLater'
		$buttonRestartLater.Size = $buttonSize
		$buttonRestartLater.MinimumSize = $buttonSize
		$buttonRestartLater.MaximumSize = $buttonSize
		$buttonRestartLater.TabIndex = 0
		$buttonRestartLater.Text = $configRestartPromptButtonRestartLater
		$buttonRestartLater.AutoSize = $true
		$buttonRestartLater.Margin = $paddingNone
		$buttonRestartLater.Padding = $paddingNone
		$buttonRestartLater.UseVisualStyleBackColor = $true
		$buttonRestartLater.add_Click($buttonRestartLater_Click)

		## Button Restart Now
		$buttonRestartNow.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonRestartNow.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList 14,4
		$buttonRestartNow.Name = 'buttonRestartNow'
		$buttonRestartNow.Size = $buttonSize
		$buttonRestartNow.MinimumSize = $buttonSize
		$buttonRestartNow.MaximumSize = $buttonSize
		$buttonRestartNow.TabIndex = 1
		$buttonRestartNow.Text = $configRestartPromptButtonRestartNow
		$buttonRestartNow.Margin = $paddingNone
		$buttonRestartNow.Padding = $paddingNone
		$buttonRestartNow.UseVisualStyleBackColor = $true
		$buttonRestartNow.add_Click($buttonRestartNow_Click)

		## Form Restart
		$formRestart.Size = $defaultControlSize
		$formRestart.MinimumSize = $defaultControlSize
		$formRestart.Padding = $paddingNone
		$formRestart.Margin = $paddingNone
		$formRestart.DataBindings.DefaultDataSourceUpdateMode = 0
		$formRestart.Name = 'formRestart'
		$formRestart.Text = $installTitle
		$formRestart.StartPosition = 'CenterScreen'
		$formRestart.FormBorderStyle = 'FixedDialog'
		$formRestart.MaximizeBox = $false
		$formRestart.MinimizeBox = $false
		$formRestart.TopMost = $TopMost
		$formRestart.TopLevel = $true
		$formRestart.Icon = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList $AppDeployLogoIcon
		$formRestart.AutoSize = $true
		$formRestart.ControlBox = $true
		$formRestart.Controls.Add($pictureBanner)

		## Button Panel
		$panelButtons.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList 450,39
		$panelButtons.Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList 450,39
		$panelButtons.MaximumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList 450,39
		$panelButtons.AutoSize = $true
		$panelButtons.Padding = $paddingNone
		$panelButtons.Margin = $paddingNone
		$panelButtons.Controls.Add($buttonRestartNow)
		$panelButtons.Controls.Add($buttonRestartLater)
		## Add the Buttons Panel to the flowPanel
		$flowLayoutPanel.Controls.Add($panelButtons)
		## Add FlowPanel to the form
		$formRestart.Controls.Add($flowLayoutPanel)
		$formRestart.add_Resize($formRestart_Resize)
		## Timer Countdown
		If (-not $NoCountdown) { $timerCountdown.add_Tick($timerCountdown_Tick) }
		##----------------------------------------------
		# Init the OnLoad event to correct the initial state of the form
		$formRestart.add_Load($Restart_Form_StateCorrection_Load)
		# Clean up the control events
		$formRestart.add_FormClosed($Restart_Form_Cleanup_FormClosed)
		$formRestartClosing = [Windows.Forms.FormClosingEventHandler]{ If ($_.CloseReason -eq 'UserClosing') { $_.Cancel = $true } }
		$formRestart.add_FormClosing($formRestartClosing)

		If ($NoCountdown) {
			Write-Log -Message 'Displaying restart prompt with no countdown.' -Source ${CmdletName}
		}
		Else {
			Write-Log -Message "Displaying restart prompt with a [$countDownSeconds] second countdown." -Source ${CmdletName}
		}

		#  Show the Form
		Write-Output -InputObject $formRestart.ShowDialog()
		$formRestart.Dispose()
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion

Function Show-InstallationPrompt {
	<#
.SYNOPSIS

Displays a custom installation prompt with the toolkit branding and optional buttons.

.DESCRIPTION

Any combination of Left, Middle or Right buttons can be displayed. The return value of the button clicked by the user is the button text specified.

.PARAMETER Title

Title of the prompt. Default: the application installation name.

.PARAMETER Message

Message text to be included in the prompt

.PARAMETER MessageAlignment

Alignment of the message text. Options: Left, Center, Right. Default: Center.

.PARAMETER ButtonLeftText

Show a button on the left of the prompt with the specified text

.PARAMETER ButtonRightText

Show a button on the right of the prompt with the specified text

.PARAMETER ButtonMiddleText

Show a button in the middle of the prompt with the specified text

.PARAMETER Icon

Show a system icon in the prompt. Options: Application, Asterisk, Error, Exclamation, Hand, Information, None, Question, Shield, Warning, WinLogo. Default: None

.PARAMETER NoWait

Specifies whether to show the prompt asynchronously (i.e. allow the script to continue without waiting for a response). Default: $false.

.PARAMETER PersistPrompt

Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml. The user will have no option but to respond to the prompt - resistance is futile!

.PARAMETER MinimizeWindows

Specifies whether to minimize other windows when displaying prompt. Default: $false.

.PARAMETER Timeout

Specifies the time period in seconds after which the prompt should timeout. Default: UI timeout value set in the config XML file.

.PARAMETER ExitOnTimeout

Specifies whether to exit the script if the UI times out. Default: $true.

.PARAMETER TopMost

Specifies whether the progress window should be topmost. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Show-InstallationPrompt -Message 'Do you want to proceed with the installation?' -ButtonRightText 'Yes' -ButtonLeftText 'No'

.EXAMPLE

Show-InstallationPrompt -Title 'Funny Prompt' -Message 'How are you feeling today?' -ButtonRightText 'Good' -ButtonLeftText 'Bad' -ButtonMiddleText 'Indifferent'

.EXAMPLE

Show-InstallationPrompt -Message 'You can customize text to appear at the end of an install, or remove it completely for unattended installations.' -Icon Information -NoWait

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[String]$Title = $installTitle,
		[Parameter(Mandatory = $false)]
		[String]$Message = '',
		[Parameter(Mandatory = $false)]
		[ValidateSet('Left', 'Center', 'Right')]
		[String]$MessageAlignment = 'Center',
		[Parameter(Mandatory = $false)]
		[String]$ButtonRightText = '',
		[Parameter(Mandatory = $false)]
		[String]$ButtonLeftText = '',
		[Parameter(Mandatory = $false)]
		[String]$ButtonMiddleText = '',
		[Parameter(Mandatory = $false)]
		[ValidateSet('Application', 'Asterisk', 'Error', 'Exclamation', 'Hand', 'Information', 'None', 'Question', 'Shield', 'Warning', 'WinLogo')]
		[String]$Icon = 'None',
		[Parameter(Mandatory = $false)]
		[Switch]$NoWait = $false,
		[Parameter(Mandatory = $false)]
		[Switch]$PersistPrompt = $false,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Boolean]$MinimizeWindows = $false,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Int32]$Timeout = $configInstallationUITimeout,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Boolean]$ExitOnTimeout = $true,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Boolean]$TopMost = $true
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Bypass if in non-interactive mode
		If ($deployModeSilent) {
			Write-Log -Message "Bypassing Show-InstallationPrompt [Mode: $deployMode]. Message:$Message" -Source ${CmdletName}
			Return
		}

		## Get parameters for calling function asynchronously
		[Hashtable]$installPromptParameters = $PSBoundParameters

		## Check if the countdown was specified
		If ($timeout -gt $configInstallationUITimeout) {
			[String]$CountdownTimeoutErr = 'The installation UI dialog timeout cannot be longer than the timeout specified in the XML configuration file.'
			Write-Log -Message $CountdownTimeoutErr -Severity 3 -Source ${CmdletName}
			Throw $CountdownTimeoutErr
		}

		## If the NoWait parameter is specified, launch a new PowerShell session to show the prompt asynchronously
		If ($NoWait) {
			# Remove the NoWait parameter so that the script is run synchronously in the new PowerShell session. This also prevents the function to loop indefinitely.
			$installPromptParameters.Remove('NoWait')
			# Format the parameters as a string
			[String]$installPromptParameters = ($installPromptParameters.GetEnumerator() | Resolve-Parameters) -join ' '


			Start-Process -FilePath "$PSHOME\powershell.exe" -ArgumentList "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command & {& `'$scriptPath`' -ReferredInstallTitle `'$Title`' -ReferredInstallName `'$installName`' -ReferredLogName `'$logName`' -ShowInstallationPrompt $installPromptParameters -AsyncToolkitLaunch}" -WindowStyle 'Hidden' -ErrorAction 'SilentlyContinue'
			Return
		}

		[Windows.Forms.Application]::EnableVisualStyles()
		$formInstallationPrompt = New-Object -TypeName 'System.Windows.Forms.Form'
		$formInstallationPrompt.SuspendLayout()
		$pictureBanner = New-Object -TypeName 'System.Windows.Forms.PictureBox'
		If ($Icon -ne 'None') {
			$pictureIcon = New-Object -TypeName 'System.Windows.Forms.PictureBox'
		}
		$labelText = New-Object -TypeName 'System.Windows.Forms.Label'
		$buttonRight = New-Object -TypeName 'System.Windows.Forms.Button'
		$buttonMiddle = New-Object -TypeName 'System.Windows.Forms.Button'
		$buttonLeft = New-Object -TypeName 'System.Windows.Forms.Button'
		$buttonAbort = New-Object -TypeName 'System.Windows.Forms.Button'
		$flowLayoutPanel = New-Object -TypeName 'System.Windows.Forms.FlowLayoutPanel'
		$panelButtons = New-Object -TypeName 'System.Windows.Forms.Panel'

		[ScriptBlock]$Install_Prompt_Form_Cleanup_FormClosed = {
			## Remove all event handlers from the controls
			Try {
				$installPromptTimer.Dispose()
				$installPromptTimer = $null
				$installPromptTimerPersist.remove_Tick($installPromptTimerPersist_Tick)
				$installPromptTimerPersist.Dispose()
				$installPromptTimerPersist = $null
				$formInstallationPrompt.remove_Load($Install_Prompt_Form_StateCorrection_Load)
				$formInstallationPrompt.remove_FormClosed($Install_Prompt_Form_Cleanup_FormClosed)
			} Catch {
			}
		}

		[ScriptBlock]$Install_Prompt_Form_StateCorrection_Load = {
			# Disable the X button
			Try {
				$windowHandle = $formInstallationPrompt.Handle
				If ($windowHandle -and ($windowHandle -ne [IntPtr]::Zero)) {
					$menuHandle = [PSADT.UiAutomation]::GetSystemMenu($windowHandle, $false)
					If ($menuHandle -and ($menuHandle -ne [IntPtr]::Zero)) {
						[PSADT.UiAutomation]::EnableMenuItem($menuHandle, 0xF060, 0x00000001)
						[PSADT.UiAutomation]::DestroyMenu($menuHandle)
					}
				}
			} Catch {
				# Not a terminating error if we can't disable the button. Just disable the Control Box instead
				Write-Log 'Failed to disable the Close button. Disabling the Control Box instead.' -Severity 2 -Source ${CmdletName}
				$formInstallationPrompt.ControlBox = $false
			}
			# Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
			Set-Variable -Name 'formInstallationPromptStartPosition' -Value $formInstallationPrompt.Location -Scope 'Script'
		}

		## Form

		##----------------------------------------------
		## Create padding object
		$paddingNone = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 0)

		## Default control size
		$DefaultControlSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 0)

		## Generic Button properties
		$buttonSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (130, 24)

		## Picture Banner
		$pictureBanner.DataBindings.DefaultDataSourceUpdateMode = 0
		$pictureBanner.ImageLocation = $appDeployLogoBanner
		$pictureBanner.ClientSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, $appDeployLogoBannerHeight)
		$pictureBanner.MinimumSize = $DefaultControlSize
		$pictureBanner.SizeMode = [System.Windows.Forms.PictureBoxSizeMode]::Zoom
		$pictureBanner.Margin = $paddingNone
		$pictureBanner.TabStop = $false
		$pictureBanner.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, 0)

		## Picture Icon
		If ($Icon -ne 'None') {
			$pictureIcon.DataBindings.DefaultDataSourceUpdateMode = 0
			$pictureIcon.Image = ([Drawing.SystemIcons]::$Icon).ToBitmap()
			$pictureIcon.Name = 'pictureIcon'
			$pictureIcon.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (64, 32)
			$pictureIcon.ClientSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (64, 32)
			$pictureIcon.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (24, 0, 8, 0)
			$pictureIcon.SizeMode = 'CenterImage'
			$pictureIcon.TabStop = $false
			$pictureIcon.Anchor = 'None'
			$pictureIcon.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 10, 0, 5)
		}

		## Label Text
		$labelText.DataBindings.DefaultDataSourceUpdateMode = 0
		$labelText.Font = $defaultFont
		$labelText.Name = 'labelText'
		$System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (386, 0)
		$labelText.ClientSize = $System_Drawing_Size
		If ($Icon -ne 'None') {
			$labelText.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (386, $pictureIcon.Height)
		} Else {
			$labelText.MinimumSize = $System_Drawing_Size
		}
		$labelText.MaximumSize = $System_Drawing_Size
		$labelText.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 10, 0, 5)
		$labelText.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (20, 0, 20, 0)
		$labelText.TabStop = $false
		$labelText.Text = $message
		$labelText.TextAlign = "Middle$($MessageAlignment)"
		$labelText.Anchor = 'None'
		$labelText.AutoSize = $true

		If ($Icon -ne 'None') {
			# Add margin for the icon based on labelText Height so its centered
			$pictureIcon.Height = $labelText.Height
		}
		## Button Left
		$buttonLeft.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonLeft.Name = 'buttonLeft'
		$buttonLeft.Font = $defaultFont
		$buttonLeft.ClientSize = $buttonSize
		$buttonLeft.MinimumSize = $buttonSize
		$buttonLeft.MaximumSize = $buttonSize
		$buttonLeft.TabIndex = 0
		$buttonLeft.Text = $buttonLeftText
		$buttonLeft.DialogResult = 'No'
		$buttonLeft.AutoSize = $false
		$buttonLeft.Margin = $paddingNone
		$buttonLeft.Padding = $paddingNone
		$buttonLeft.UseVisualStyleBackColor = $true
		$buttonLeft.Location = '14,4'

		## Button Middle
		$buttonMiddle.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonMiddle.Name = 'buttonMiddle'
		$buttonMiddle.Font = $defaultFont
		$buttonMiddle.ClientSize = $buttonSize
		$buttonMiddle.MinimumSize = $buttonSize
		$buttonMiddle.MaximumSize = $buttonSize
		$buttonMiddle.TabIndex = 1
		$buttonMiddle.Text = $buttonMiddleText
		$buttonMiddle.DialogResult = 'Ignore'
		$buttonMiddle.AutoSize = $true
		$buttonMiddle.Margin = $paddingNone
		$buttonMiddle.Padding = $paddingNone
		$buttonMiddle.UseVisualStyleBackColor = $true
		$buttonMiddle.Location = '160,4'

		## Button Right
		$buttonRight.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonRight.Name = 'buttonRight'
		$buttonRight.Font = $defaultFont
		$buttonRight.ClientSize = $buttonSize
		$buttonRight.MinimumSize = $buttonSize
		$buttonRight.MaximumSize = $buttonSize
		$buttonRight.TabIndex = 2
		$buttonRight.Text = $ButtonRightText
		$buttonRight.DialogResult = 'Yes'
		$buttonRight.AutoSize = $true
		$buttonRight.Margin = $paddingNone
		$buttonRight.Padding = $paddingNone
		$buttonRight.UseVisualStyleBackColor = $true
		$buttonRight.Location = '306,4'

		## Button Abort (Hidden)
		$buttonAbort.DataBindings.DefaultDataSourceUpdateMode = 0
		$buttonAbort.Name = 'buttonAbort'
		$buttonAbort.Font = $defaultFont
		$buttonAbort.ClientSize = '0,0'
		$buttonAbort.MinimumSize = '0,0'
		$buttonAbort.MaximumSize = '0,0'
		$buttonAbort.BackColor = [System.Drawing.Color]::Transparent
		$buttonAbort.ForeColor = [System.Drawing.Color]::Transparent
		$buttonAbort.FlatAppearance.BorderSize = 0
		$buttonAbort.FlatAppearance.MouseDownBackColor = [System.Drawing.Color]::Transparent
		$buttonAbort.FlatAppearance.MouseOverBackColor = [System.Drawing.Color]::Transparent
		$buttonAbort.FlatStyle = [System.Windows.Forms.FlatStyle]::System
		$buttonAbort.DialogResult = 'Abort'
		$buttonAbort.TabStop = $false
		$buttonAbort.Visible = $true # Has to be set visible so we can call Click on it
		$buttonAbort.Margin = $paddingNone
		$buttonAbort.Padding = $paddingNone
		$buttonAbort.UseVisualStyleBackColor = $true

		## FlowLayoutPanel
		$flowLayoutPanel.MinimumSize = $DefaultControlSize
		$flowLayoutPanel.MaximumSize = $DefaultControlSize
		$flowLayoutPanel.ClientSize = $DefaultControlSize
		$flowLayoutPanel.AutoSize = $true
		$flowLayoutPanel.AutoSizeMode = 'GrowAndShrink'
		$flowLayoutPanel.Anchor = 'Top,Left'
		$flowLayoutPanel.FlowDirection = 'LeftToRight'
		$flowLayoutPanel.WrapContents = $true
		$flowLayoutPanel.Margin = $paddingNone
		$flowLayoutPanel.Padding = $paddingNone
		## Make sure label text is positioned correctly
		If ($Icon -ne 'None') {
			$labelText.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 10, 0)
			$pictureIcon.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, 0)
			$labelText.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (64, 0)
		} Else {
			$labelText.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
			$labelText.MinimumSize = $DefaultControlSize
			$labelText.MaximumSize = $DefaultControlSize
			$labelText.ClientSize = $DefaultControlSize
			$labelText.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, 0)
		}
		If ($Icon -ne 'None') {
			$flowLayoutPanel.Controls.Add($pictureIcon)
		}
		$flowLayoutPanel.Controls.Add($labelText)
		$flowLayoutPanel.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, $appDeployLogoBannerHeight)

		## ButtonsPanel
		$panelButtons.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
		$panelButtons.ClientSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
		If ($Icon -ne 'None') {
			$panelButtons.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (64, 0)
		} Else {
			$panelButtons.Padding = $paddingNone
		}
		$panelButtons.Margin = $paddingNone
		$panelButtons.MaximumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
		$panelButtons.AutoSize = $true
		If ($buttonLeftText) {
			$panelButtons.Controls.Add($buttonLeft)
		}
		If ($buttonMiddleText) {
			$panelButtons.Controls.Add($buttonMiddle)
		}
		If ($buttonRightText) {
			$panelButtons.Controls.Add($buttonRight)
		}
		## Add the ButtonsPanel to the flowLayoutPanel if any buttons are present
		If ($buttonLeftText -or $buttonMiddleText -or $buttonRightText) {
			$flowLayoutPanel.Controls.Add($panelButtons)
		}

		## Form Installation Prompt
		$formInstallationPrompt.ClientSize = $DefaultControlSize
		$formInstallationPrompt.Padding = $paddingNone
		$formInstallationPrompt.Margin = $paddingNone
		$formInstallationPrompt.DataBindings.DefaultDataSourceUpdateMode = 0
		$formInstallationPrompt.Name = 'InstallPromptForm'
		$formInstallationPrompt.Text = $title
		$formInstallationPrompt.StartPosition = 'CenterScreen'
		$formInstallationPrompt.FormBorderStyle = 'Fixed3D'
		$formInstallationPrompt.MaximizeBox = $false
		$formInstallationPrompt.MinimizeBox = $false
		$formInstallationPrompt.TopMost = $TopMost
		$formInstallationPrompt.TopLevel = $true
		$formInstallationPrompt.AutoSize = $true
		$formInstallationPrompt.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Dpi
		$formInstallationPrompt.AutoScaleDimensions = New-Object System.Drawing.SizeF(96, 96)
		$formInstallationPrompt.Icon = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList ($AppDeployLogoIcon)
		$formInstallationPrompt.Controls.Add($pictureBanner)
		$formInstallationPrompt.Controls.Add($buttonAbort)
		$formInstallationPrompt.Controls.Add($flowLayoutPanel)
		## Timer
		$installPromptTimer = New-Object -TypeName 'System.Windows.Forms.Timer'
		$installPromptTimer.Interval = ($timeout * 1000)
		$installPromptTimer.Add_Tick({
				Write-Log -Message 'Installation action not taken within a reasonable amount of time.' -Source ${CmdletName}
				$buttonAbort.PerformClick()
			})
		## Init the OnLoad event to correct the initial state of the form
		$formInstallationPrompt.add_Load($Install_Prompt_Form_StateCorrection_Load)
		## Clean up the control events
		$formInstallationPrompt.add_FormClosed($Install_Prompt_Form_Cleanup_FormClosed)

		## Start the timer
		$installPromptTimer.Start()

		## Persistence Timer
		If ($persistPrompt) {
			$installPromptTimerPersist = New-Object -TypeName 'System.Windows.Forms.Timer'
			$installPromptTimerPersist.Interval = ($configInstallationPersistInterval * 1000)
			[ScriptBlock]$installPromptTimerPersist_Tick = {
				$formInstallationPrompt.WindowState = 'Normal'
				$formInstallationPrompt.TopMost = $TopMost
				$formInstallationPrompt.BringToFront()
				$formInstallationPrompt.Location = "$($formInstallationPromptStartPosition.X),$($formInstallationPromptStartPosition.Y)"
			}
			$installPromptTimerPersist.add_Tick($installPromptTimerPersist_Tick)
			$installPromptTimerPersist.Start()
		}

		If (-not $AsyncToolkitLaunch) {
			## Close the Installation Progress Dialog if running
			Close-InstallationProgress
		}

		[String]$installPromptLoggedParameters = ($installPromptParameters.GetEnumerator() | Resolve-Parameters) -join ' '
		Write-Log -Message "Displaying custom installation prompt with the parameters: [$installPromptLoggedParameters]." -Source ${CmdletName}


		## Show the prompt synchronously. If user cancels, then keep showing it until user responds using one of the buttons.
		$showDialog = $true
		While ($showDialog) {
			# Minimize all other windows
			If ($minimizeWindows) {
				$null = $shellApp.MinimizeAll()
			}
			# Show the Form
			$formInstallationPrompt.ResumeLayout()
			$result = $formInstallationPrompt.ShowDialog()
			If (($result -eq 'Yes') -or ($result -eq 'No') -or ($result -eq 'Ignore') -or ($result -eq 'Abort')) {
				$showDialog = $false
			}
		}
		$formInstallationPrompt.Dispose()

		Switch ($result) {
			'Yes' {
				Write-Output -InputObject ($buttonRightText)
			}
			'No' {
				Write-Output -InputObject ($buttonLeftText)
			}
			'Ignore' {
				Write-Output -InputObject ($buttonMiddleText)
			}
			'Abort' {
				# Restore minimized windows
				$null = $shellApp.UndoMinimizeAll()
				If ($ExitOnTimeout) {
					Exit-Script -ExitCode $configInstallationUIExitCode
				} Else {
					Write-Log -Message 'UI timed out but `$ExitOnTimeout set to `$false. Continue...' -Source ${CmdletName}
				}
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

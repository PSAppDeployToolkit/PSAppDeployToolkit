function Show-ADTClassicInstallationPrompt
{
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

    .PARAMETER NoExitOnTimeout
    Specifies whether to not exit the script if the UI times out. Default: $false.

    .PARAMETER NotTopMost
    Specifies whether the progress window shouldn't be topmost. Default: $false.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Show-ADTClassicInstallationPrompt -Message 'Do you want to proceed with the installation?' -ButtonRightText 'Yes' -ButtonLeftText 'No'

    .EXAMPLE
    Show-ADTClassicInstallationPrompt -Title 'Funny Prompt' -Message 'How are you feeling today?' -ButtonRightText 'Good' -ButtonLeftText 'Bad' -ButtonMiddleText 'Indifferent'

    .EXAMPLE
    Show-ADTClassicInstallationPrompt -Message 'You can customize text to appear at the end of an install, or remove it completely for unattended installations.' -Icon Information -NoWait

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title = (Get-ADTSession).GetPropertyValue('InstallTitle'),

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateSet('MiddleLeft', 'MiddleCenter', 'MiddleRight')]
        [System.Drawing.ContentAlignment]$MessageAlignment = 'MiddleCenter',

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonRightText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonLeftText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonMiddleText,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Application', 'Asterisk', 'Error', 'Exclamation', 'Hand', 'Information', 'Question', 'Shield', 'Warning', 'WinLogo')]
        [System.String]$Icon,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PersistPrompt,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$MinimizeWindows,

        [Parameter(Mandatory = $false)]
        [ValidateScript({if ($_ -gt (Get-ADTConfig).UI.DefaultTimeout) {throw [System.ArgumentException]::new("The installation UI dialog timeout cannot be longer than the timeout specified in the configuration file.")}; !!$_})]
        [System.UInt32]$Timeout = (Get-ADTConfig).UI.DefaultTimeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoExitOnTimeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost
    )

    begin {
        $adtEnv = Get-ADTEnvironment
        $adtConfig = Get-ADTConfig
        $adtSession = Get-ADTSession
        Write-ADTDebugHeader
    }

    process {
        # Bypass if in non-interactive mode
        if ($adtSession.DeployModeSilent)
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.GetPropertyValue('deployMode'))]. Message:$Message"
            return
        }

        # If the NoWait parameter is specified, launch a new PowerShell session to show the prompt asynchronously.
        if ($NoWait)
        {
            # Remove the NoWait parameter so that the script is run synchronously in the new PowerShell session. This also prevents the function to loop indefinitely.
            Export-ADTModuleState
            Start-Process -FilePath $adtEnv.envPSProcessPath -ArgumentList "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command Import-Module -Name '$((Get-ADTModuleInfo).ModuleBase)'; Import-ADTModuleState; [System.Void]($($MyInvocation.MyCommand.Name.Replace('Classic', $null)) $(($PSBoundParameters | Resolve-ADTBoundParameters -Exclude NoWait).Replace('"', '\"')))" -WindowStyle Hidden -ErrorAction Ignore
            return
        }

        # Read all form assets into memory.
        Read-ADTAssetsIntoMemory

        # Set up some default values.
        $controlSize = [System.Drawing.Size]::new($Script:FormData.Width, 0)
        $paddingNone = [System.Windows.Forms.Padding]::new(0, 0, 0, 0)
        $buttonSize = [System.Drawing.Size]::new(130, 24)

        # Define events for form windows.
        $installPromptTimer_Tick = {
            Write-ADTLogEntry -Message 'Installation action not taken within a reasonable amount of time.'
            $buttonAbort.PerformClick()
        }
        $installPromptTimerPersist_Tick = {
            $formInstallationPrompt.WindowState = [System.Windows.Forms.FormWindowState]::Normal
            $formInstallationPrompt.TopMost = !$NotTopMost
            $formInstallationPrompt.Location = $formInstallationPromptStartLocation
            $formInstallationPrompt.BringToFront()
        }
        $formInstallationPrompt_FormClosed = {
            # Remove all event handlers from the controls.
            $installPromptTimer.remove_Tick($installPromptTimer_Tick)
            $installPromptTimer.Dispose()
            $installPromptTimer = $null
            $installPromptTimerPersist.remove_Tick($installPromptTimerPersist_Tick)
            $installPromptTimerPersist.Dispose()
            $installPromptTimerPersist = $null
            $formInstallationPrompt.remove_Load($formInstallationPrompt_Load)
            $formInstallationPrompt.remove_FormClosed($formInstallationPrompt_FormClosed)
            $formInstallationPrompt.Dispose()
            $formInstallationPrompt = $null
        }
        $formInstallationPrompt_Load = {
            # Disable the X button.
            try
            {
                Disable-ADTWindowCloseButton -WindowHandle $formInstallationPrompt.Handle
            }
            catch
            {
                # Not a terminating error if we can't disable the button. Just disable the Control Box instead.
                Write-ADTLogEntry 'Failed to disable the Close button. Disabling the Control Box instead.' -Severity 2
                $formInstallationPrompt.ControlBox = $false
            }

            # Get the start position of the form so we can return the form to this position if PersistPrompt is enabled.
            $formInstallationPromptStartLocation = $formInstallationPrompt.Location
        }

        # Built out timer
        $installPromptTimer = [System.Windows.Forms.Timer]::new()
        $installPromptTimer.Interval = $Timeout * 1000
        $installPromptTimer.add_Tick($installPromptTimer_Tick)

        # Built out timer for Persist Prompt mode.
        $installPromptTimerPersist = [System.Windows.Forms.Timer]::new()
        $installPromptTimerPersist.Interval = $adtConfig.UI.DefaultPromptPersistInterval * 1000
        $installPromptTimerPersist.add_Tick($installPromptTimerPersist_Tick)

        # Picture Banner.
        $pictureBanner = [System.Windows.Forms.PictureBox]::new()
        $pictureBanner.SizeMode = [System.Windows.Forms.PictureBoxSizeMode]::Zoom
        $pictureBanner.MinimumSize = $pictureBanner.ClientSize = $pictureBanner.MaximumSize = [System.Drawing.Size]::new($Script:FormData.Width, $Script:FormData.BannerHeight)
        $pictureBanner.Location = [System.Drawing.Point]::new(0, 0)
        $pictureBanner.Name = 'PictureBanner'
        $pictureBanner.Image = $Script:FormData.Assets.Banner
        $pictureBanner.Margin = $paddingNone
        $pictureBanner.TabStop = $false

        # Label Text.
        $labelMessage = [System.Windows.Forms.Label]::new()
        $labelMessage.MinimumSize = $labelMessage.ClientSize = $labelMessage.MaximumSize = [System.Drawing.Size]::new(381, 0)
        $labelMessage.Margin = [System.Windows.Forms.Padding]::new(0, 10, 0, 5)
        $labelMessage.Padding = [System.Windows.Forms.Padding]::new(20, 0, 20, 0)
        $labelMessage.Anchor = [System.Windows.Forms.AnchorStyles]::None
        $labelMessage.Font = $Script:FormData.Font
        $labelMessage.Name = 'LabelMessage'
        $labelMessage.Text = $Message
        $labelMessage.TextAlign = $MessageAlignment
        $labelMessage.TabStop = $false
        $labelMessage.AutoSize = $true

        # Picture Icon.
        if ($Icon)
        {
            $pictureIcon = [System.Windows.Forms.PictureBox]::new()
            $pictureIcon.SizeMode = [System.Windows.Forms.PictureBoxSizeMode]::CenterImage
            $pictureIcon.MinimumSize = $pictureIcon.ClientSize = $pictureIcon.MaximumSize = [System.Drawing.Size]::new(64, 32)
            $pictureIcon.Margin = [System.Windows.Forms.Padding]::new(0, 10, 0, 5)
            $pictureIcon.Padding = [System.Windows.Forms.Padding]::new(24, 0, 8, 0)
            $pictureIcon.Anchor = [System.Windows.Forms.AnchorStyles]::None
            $pictureIcon.Name = 'PictureIcon'
            $pictureIcon.Image = ([System.Drawing.SystemIcons]::$Icon).ToBitmap()
            $pictureIcon.TabStop = $false
            $pictureIcon.Height = $labelMessage.Height
        }

        # Button Abort (Hidden).
        $buttonAbort = [System.Windows.Forms.Button]::new()
        $buttonAbort.MinimumSize = $buttonAbort.ClientSize = $buttonAbort.MaximumSize = [System.Drawing.Size]::new(0, 0)
        $buttonAbort.Margin = $buttonAbort.Padding = $paddingNone
        $buttonAbort.DialogResult = [System.Windows.Forms.DialogResult]::Abort
        $buttonAbort.Name = 'ButtonAbort'
        $buttonAbort.Font = $Script:FormData.Font
        $buttonAbort.BackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.ForeColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatAppearance.BorderSize = 0
        $buttonAbort.FlatAppearance.MouseDownBackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatAppearance.MouseOverBackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatStyle = [System.Windows.Forms.FlatStyle]::System
        $buttonAbort.TabStop = $false
        $buttonAbort.Visible = $true  # Has to be set visible so we can call Click on it
        $buttonAbort.UseVisualStyleBackColor = $true

        # FlowLayoutPanel.
        $flowLayoutPanel = [System.Windows.Forms.FlowLayoutPanel]::new()
        $flowLayoutPanel.SuspendLayout()
        $flowLayoutPanel.MinimumSize = $flowLayoutPanel.ClientSize = $flowLayoutPanel.MaximumSize = $controlSize
        $flowLayoutPanel.Location = [System.Drawing.Point]::new(0, $Script:FormData.BannerHeight)
        $flowLayoutPanel.AutoSize = $true
        $flowLayoutPanel.AutoSizeMode = [System.Windows.Forms.AutoSizeMode]::GrowAndShrink
        $flowLayoutPanel.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Left
        $flowLayoutPanel.WrapContents = $true
        $flowLayoutPanel.Margin = $flowLayoutPanel.Padding = $paddingNone

        # Make sure label text is positioned correctly before adding it.
        if ($Icon)
        {
            $labelMessage.Padding = [System.Windows.Forms.Padding]::new(0, 0, 10, 0)
            $labelMessage.Location = [System.Drawing.Point]::new(64, 0)
            $pictureIcon.Location = [System.Drawing.Point]::new(0, 0)
            $flowLayoutPanel.Controls.Add($pictureIcon)
        }
        else
        {
            $labelMessage.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
            $labelMessage.Location = [System.Drawing.Point]::new(0, 0)
            $labelMessage.MinimumSize = $labelMessage.ClientSize = $labelMessage.MaximumSize = $controlSize
        }
        $flowLayoutPanel.Controls.Add($labelMessage)

        # Add in remaining controls and resume object.
        if ($ButtonLeftText -or $ButtonMiddleText -or $ButtonRightText)
        {
            # ButtonsPanel.
            $panelButtons = [System.Windows.Forms.Panel]::new()
            $panelButtons.SuspendLayout()
            $panelButtons.MinimumSize = $panelButtons.ClientSize = $panelButtons.MaximumSize = [System.Drawing.Size]::new($Script:FormData.Width, 39)
            $panelButtons.Margin = [System.Windows.Forms.Padding]::new(0, 10, 0, 0)
            $panelButtons.AutoSize = $true
            if ($Icon)
            {
                $panelButtons.Location = [System.Drawing.Point]::new(64, 0)
            }
            else
            {
                $panelButtons.Padding = $paddingNone
            }

            # Build out and add the buttons if we have any.
            if ($ButtonLeftText)
            {
                # Button Left.
                $buttonLeft = [System.Windows.Forms.Button]::new()
                $buttonLeft.MinimumSize = $buttonLeft.ClientSize = $buttonLeft.MaximumSize = $buttonSize
                $buttonLeft.Margin = $buttonLeft.Padding = $paddingNone
                $buttonLeft.Location = [System.Drawing.Point]::new(14, 4)
                $buttonLeft.DialogResult = [System.Windows.Forms.DialogResult]::No
                $buttonLeft.Font = $Script:FormData.Font
                $buttonLeft.Name = 'ButtonLeft'
                $buttonLeft.Text = $ButtonLeftText
                $buttonLeft.TabIndex = 0
                $buttonLeft.AutoSize = $false
                $buttonLeft.UseVisualStyleBackColor = $true
                $panelButtons.Controls.Add($buttonLeft)
            }
            if ($ButtonMiddleText)
            {
                # Button Middle.
                $buttonMiddle = [System.Windows.Forms.Button]::new()
                $buttonMiddle.MinimumSize = $buttonMiddle.ClientSize = $buttonMiddle.MaximumSize = $buttonSize
                $buttonMiddle.Margin = $buttonMiddle.Padding = $paddingNone
                $buttonMiddle.Location = [System.Drawing.Point]::new(160, 4)
                $buttonMiddle.DialogResult = [System.Windows.Forms.DialogResult]::Ignore
                $buttonMiddle.Font = $Script:FormData.Font
                $buttonMiddle.Name = 'ButtonMiddle'
                $buttonMiddle.Text = $ButtonMiddleText
                $buttonMiddle.TabIndex = 1
                $buttonMiddle.AutoSize = $false
                $buttonMiddle.UseVisualStyleBackColor = $true
                $panelButtons.Controls.Add($buttonMiddle)
            }
            if ($ButtonRightText)
            {
                # Button Right.
                $buttonRight = [System.Windows.Forms.Button]::new()
                $buttonRight.MinimumSize = $buttonRight.ClientSize = $buttonRight.MaximumSize = $buttonSize
                $buttonRight.Margin = $buttonRight.Padding = $paddingNone
                $buttonRight.Location = [System.Drawing.Point]::new(306, 4)
                $buttonRight.DialogResult = [System.Windows.Forms.DialogResult]::Yes
                $buttonRight.Font = $Script:FormData.Font
                $buttonRight.Name = 'ButtonRight'
                $buttonRight.Text = $ButtonRightText
                $buttonRight.TabIndex = 2
                $buttonRight.AutoSize = $false
                $buttonRight.UseVisualStyleBackColor = $true
                $panelButtons.Controls.Add($buttonRight)
            }

            # Add the button panel in if we have buttons.
            if ($panelButtons.Controls.Count)
            {
                $panelButtons.ResumeLayout()
                $flowLayoutPanel.Controls.Add($panelButtons)
            }
        }
        $flowLayoutPanel.ResumeLayout()

        ## Form Installation Prompt
        $formInstallationPromptStartLocation = $null
        $formInstallationPrompt = [System.Windows.Forms.Form]::new()
        $formInstallationPrompt.SuspendLayout()
        $formInstallationPrompt.ClientSize = $controlSize
        $formInstallationPrompt.Margin = $formInstallationPrompt.Padding = $paddingNone
        $formInstallationPrompt.Font = $Script:FormData.Font
        $formInstallationPrompt.Name = 'InstallPromptForm'
        $formInstallationPrompt.Text = $Title
        $formInstallationPrompt.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Font
        $formInstallationPrompt.AutoScaleDimensions = [System.Drawing.SizeF]::new(7, 15)
        $formInstallationPrompt.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
        $formInstallationPrompt.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::Fixed3D
        $formInstallationPrompt.MaximizeBox = $false
        $formInstallationPrompt.MinimizeBox = $false
        $formInstallationPrompt.TopMost = !$NotTopMost
        $formInstallationPrompt.TopLevel = $true
        $formInstallationPrompt.AutoSize = $true
        $formInstallationPrompt.Icon = $Script:FormData.Assets.Icon
        $formInstallationPrompt.Controls.Add($pictureBanner)
        $formInstallationPrompt.Controls.Add($buttonAbort)
        $formInstallationPrompt.Controls.Add($flowLayoutPanel)
        $formInstallationPrompt.add_Load($formInstallationPrompt_Load)
        $formInstallationPrompt.add_FormClosed($formInstallationPrompt_FormClosed)
        $formInstallationPrompt.ResumeLayout()

        # Close the Installation Progress Dialog if running
        if (!$adtSession.GetPropertyValue('InstallPhase').Equals('Asynchronous'))
        {
            Close-ADTClassicInstallationProgress
        }
        Write-ADTLogEntry -Message "Displaying custom installation prompt with the parameters: [$($PSBoundParameters | Resolve-ADTBoundParameters)]."

        # Start the timer.
        $installPromptTimer.Start()
        if ($PersistPrompt) {$installPromptTimerPersist.Start()}

        # Show the prompt synchronously. If user cancels, then keep showing it until user responds using one of the buttons.
        do
        {
            # Minimize all other windows
            if ($MinimizeWindows)
            {
                [System.Void]$adtEnv.ShellApp.MinimizeAll()
            }

            # Show the Form
            $formResult = $formInstallationPrompt.ShowDialog()
        }
        until ($formResult -match '^(Yes|No|Ignore|Abort)$')

        # Return the button text to the caller.
        switch ($formResult) {
            'Yes' {
                return $ButtonRightText
            }
            'No' {
                return $ButtonLeftText
            }
            'Ignore' {
                return $ButtonMiddleText
            }
            'Abort' {
                # Restore minimized windows.
                [System.Void]$adtEnv.ShellApp.UndoMinimizeAll()
                if (!$NoExitOnTimeout)
                {
                    Close-ADTSession -ExitCode $adtConfig.UI.DefaultExitCode
                }
                else
                {
                    Write-ADTLogEntry -Message 'UI timed out but $NoExitOnTimeout specified. Continue...'
                }
            }
        }
    }

    end {
        Write-ADTDebugFooter
    }
}

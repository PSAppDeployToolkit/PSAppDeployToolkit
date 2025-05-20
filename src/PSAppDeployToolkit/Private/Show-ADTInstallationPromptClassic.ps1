#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationPromptClassic
#
#-----------------------------------------------------------------------------

function Private:Show-ADTInstallationPromptClassic
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'formInstallationPromptStartLocation', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UnboundArguments', Justification = "This parameter is just to trap any superfluous input at the end of the function's call.")]
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Left', 'Center', 'Right')]
        [System.String]$MessageAlignment = 'Center',

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
        [System.Management.Automation.SwitchParameter]$PersistPrompt,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$MinimizeWindows,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$Timeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoExitOnTimeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Set up some default values.
    $controlSize = [System.Drawing.Size]::new($Script:Dialogs.Classic.Width, 0)
    $paddingNone = [System.Windows.Forms.Padding]::new(0, 0, 0, 0)
    $buttonSize = [System.Drawing.Size]::new(130, 24)
    $adtEnv = Get-ADTEnvironmentTable
    $adtConfig = Get-ADTConfig

    # Initalise the classic assets.
    Initialize-ADTClassicAssets

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

        # Correct the initial state of the form to prevent the .NET maximized form issue.
        $formInstallationPrompt.WindowState = [System.Windows.Forms.FormWindowState]::Normal
        $formInstallationPrompt.BringToFront()

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
    $pictureBanner.MinimumSize = $pictureBanner.ClientSize = $pictureBanner.MaximumSize = [System.Drawing.Size]::new($Script:Dialogs.Classic.Width, $Script:Dialogs.Classic.BannerHeight)
    $pictureBanner.Location = [System.Drawing.Point]::new(0, 0)
    $pictureBanner.Name = 'PictureBanner'
    $pictureBanner.Image = $Script:Dialogs.Classic.Assets.Banner
    $pictureBanner.Margin = $paddingNone
    $pictureBanner.TabStop = $false

    # Label Text.
    $labelMessage = [System.Windows.Forms.Label]::new()
    $labelMessage.MinimumSize = $labelMessage.ClientSize = $labelMessage.MaximumSize = [System.Drawing.Size]::new(381, 0)
    $labelMessage.Margin = [System.Windows.Forms.Padding]::new(0, 10, 0, 5)
    $labelMessage.Padding = [System.Windows.Forms.Padding]::new(20, 0, 20, 0)
    $labelMessage.Anchor = [System.Windows.Forms.AnchorStyles]::None
    $labelMessage.Font = $Script:Dialogs.Classic.Font
    $labelMessage.Name = 'LabelMessage'
    $labelMessage.Text = $Message
    $labelMessage.TextAlign = [System.Drawing.ContentAlignment]::"Middle$MessageAlignment"
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
    $buttonAbort.Font = $Script:Dialogs.Classic.Font
    $buttonAbort.BackColor = [System.Drawing.Color]::Transparent
    $buttonAbort.ForeColor = [System.Drawing.Color]::Transparent
    $buttonAbort.FlatAppearance.BorderSize = 0
    $buttonAbort.FlatAppearance.MouseDownBackColor = [System.Drawing.Color]::Transparent
    $buttonAbort.FlatAppearance.MouseOverBackColor = [System.Drawing.Color]::Transparent
    $buttonAbort.FlatStyle = [System.Windows.Forms.FlatStyle]::System
    $buttonAbort.TabStop = $false
    $buttonAbort.Visible = $true  # Has to be set visible so we can call Click on it.
    $buttonAbort.UseVisualStyleBackColor = $true

    # Button Default (Hidden).
    $buttonDefault = [System.Windows.Forms.Button]::new()
    $buttonDefault.MinimumSize = $buttonDefault.ClientSize = $buttonDefault.MaximumSize = [System.Drawing.Size]::new(0, 0)
    $buttonDefault.Margin = $buttonDefault.Padding = $paddingNone
    $buttonDefault.Name = 'buttonDefault'
    $buttonDefault.Font = $Script:Dialogs.Classic.Font
    $buttonDefault.BackColor = [System.Drawing.Color]::Transparent
    $buttonDefault.ForeColor = [System.Drawing.Color]::Transparent
    $buttonDefault.FlatAppearance.BorderSize = 0
    $buttonDefault.FlatAppearance.MouseDownBackColor = [System.Drawing.Color]::Transparent
    $buttonDefault.FlatAppearance.MouseOverBackColor = [System.Drawing.Color]::Transparent
    $buttonDefault.FlatStyle = [System.Windows.Forms.FlatStyle]::System
    $buttonDefault.TabStop = $false
    $buttonDefault.Enabled = $false
    $buttonDefault.Visible = $true  # Has to be set visible so we can call Click on it.
    $buttonDefault.UseVisualStyleBackColor = $true

    # FlowLayoutPanel.
    $flowLayoutPanel = [System.Windows.Forms.FlowLayoutPanel]::new()
    $flowLayoutPanel.SuspendLayout()
    $flowLayoutPanel.MinimumSize = $flowLayoutPanel.ClientSize = $flowLayoutPanel.MaximumSize = $controlSize
    $flowLayoutPanel.Location = [System.Drawing.Point]::new(0, $Script:Dialogs.Classic.BannerHeight)
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
        $panelButtons.MinimumSize = $panelButtons.ClientSize = $panelButtons.MaximumSize = [System.Drawing.Size]::new($Script:Dialogs.Classic.Width, 39)
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
            $buttonLeft.DialogResult = [System.Windows.Forms.DialogResult]::Yes
            $buttonLeft.Font = $Script:Dialogs.Classic.Font
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
            $buttonMiddle.Font = $Script:Dialogs.Classic.Font
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
            $buttonRight.DialogResult = [System.Windows.Forms.DialogResult]::No
            $buttonRight.Font = $Script:Dialogs.Classic.Font
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

    # Form Installation Prompt.
    $formInstallationPromptStartLocation = $null
    $formInstallationPrompt = [System.Windows.Forms.Form]::new()
    $formInstallationPrompt.SuspendLayout()
    $formInstallationPrompt.ClientSize = $controlSize
    $formInstallationPrompt.Margin = $formInstallationPrompt.Padding = $paddingNone
    $formInstallationPrompt.Font = $Script:Dialogs.Classic.Font
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
    $formInstallationPrompt.Icon = $Script:Dialogs.Classic.Assets.Icon
    $formInstallationPrompt.Controls.Add($pictureBanner)
    $formInstallationPrompt.Controls.Add($buttonAbort)
    $formInstallationPrompt.Controls.Add($buttonDefault)
    $formInstallationPrompt.Controls.Add($flowLayoutPanel)
    $formInstallationPrompt.add_Load($formInstallationPrompt_Load)
    $formInstallationPrompt.add_FormClosed($formInstallationPrompt_FormClosed)
    $formInstallationPrompt.AcceptButton = $buttonDefault
    $formInstallationPrompt.ActiveControl = $buttonDefault
    $formInstallationPrompt.ResumeLayout()

    # Start the timer.
    $installPromptTimer.Start()
    if ($PersistPrompt) { $installPromptTimerPersist.Start() }

    # Show the prompt synchronously. If user cancels, then keep showing it until user responds using one of the buttons.
    do
    {
        # Minimize all other windows
        if ($MinimizeWindows)
        {
            $null = $adtEnv.ShellApp.MinimizeAll()
        }

        # Show the Form
        $formResult = $formInstallationPrompt.ShowDialog()
    }
    until ($formResult -match '^(Yes|No|Ignore|Abort)$')

    # Return the button text to the caller.
    switch ($formResult)
    {
        Yes
        {
            return $ButtonLeftText
        }
        No
        {
            return $ButtonRightText
        }
        Ignore
        {
            return $ButtonMiddleText
        }
        Abort
        {
            # Restore minimized windows.
            if ($MinimizeWindows)
            {
                $null = $adtEnv.ShellApp.UndoMinimizeAll()
            }
            if (!$NoExitOnTimeout)
            {
                if (Test-ADTSessionActive)
                {
                    Close-ADTSession -ExitCode $adtConfig.UI.DefaultExitCode
                }
            }
            else
            {
                Write-ADTLogEntry -Message 'UI timed out but -NoExitOnTimeout specified. Continue...'
            }
            break
        }
    }
}

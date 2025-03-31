#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationRestartPromptClassic
#
#-----------------------------------------------------------------------------

function Private:Show-ADTInstallationRestartPromptClassic
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'formRestartPromptStartLocation', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'CountdownNoHideSeconds', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UnboundArguments', Justification = "This parameter is just to trap any superfluous input at the end of the function's call.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeploymentType,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$CountdownSeconds,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$CountdownNoHideSeconds,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoCountdown,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Initialize variables.
    $adtConfig = Get-ADTConfig
    $adtStrings = Get-ADTStringTable

    # Initalise the classic assets.
    Initialize-ADTClassicAssets

    # Define starting counters.
    $startTime = [System.DateTime]::Now
    $countdownTime = $startTime

    # Set up some default values.
    $controlSize = [System.Drawing.Size]::new($Script:Dialogs.Classic.Width, 0)
    $paddingNone = [System.Windows.Forms.Padding]::new(0, 0, 0, 0)
    $buttonSize = [System.Drawing.Size]::new(195, 24)

    # Define events for form windows.
    $formRestart_Load = {
        # Disable the X button.
        try
        {
            Disable-ADTWindowCloseButton -WindowHandle $formRestart.Handle
        }
        catch
        {
            # Not a terminating error if we can't disable the button. Just disable the Control Box instead
            Write-ADTLogEntry 'Failed to disable the Close button. Disabling the Control Box instead.' -Severity 2
            $formRestart.ControlBox = $false
        }

        # Initialize the countdown timer.
        $currentTime = [System.DateTime]::Now
        $countdownTime = $startTime.AddSeconds($CountdownSeconds)
        $timerCountdown.Start()

        # Set up the form.
        $remainingTime = $countdownTime.Subtract($currentTime)
        $labelCountdown.Text = [System.String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
        if ($remainingTime.TotalSeconds -le $CountdownNoHideSeconds)
        {
            $buttonRestartLater.Enabled = $false
        }

        # Correct the initial state of the form to prevent the .NET maximized form issue.
        $formRestart.WindowState = [System.Windows.Forms.FormWindowState]::Normal
        $formRestart.BringToFront()

        # Get the start position of the form so we can return the form to this position if PersistPrompt is enabled.
        $formRestartPromptStartLocation = $formRestart.Location
    }
    $restartTimerPersist_Tick = {
        # Show the Restart Popup.
        $formRestart.WindowState = [System.Windows.Forms.FormWindowState]::Normal
        $formRestart.TopMost = !$NotTopMost
        $formRestart.Location = $formRestartPromptStartLocation
        $formRestart.BringToFront()
    }
    $buttonRestartLater_Click = {
        # Minimize the form.
        $formRestart.WindowState = [System.Windows.Forms.FormWindowState]::Minimized
        if ($NoCountdown)
        {
            # Reset the persistence timer.
            $restartTimerPersist.Stop()
            $restartTimerPersist.Start()
        }
    }
    $buttonRestartNow_Click = {
        Write-ADTLogEntry -Message 'Forcefully restarting the computer...'
        Restart-Computer -Force
    }
    $timerCountdown_Tick = {
        # Get the time information.
        $currentTime = Get-Date
        $countdownTime = $startTime.AddSeconds($CountdownSeconds)
        $remainingTime = $countdownTime.Subtract($currentTime)

        # If the countdown is complete, restart the machine.
        if ($countdownTime -le $currentTime)
        {
            $buttonRestartNow.PerformClick()
        }
        else
        {
            # Update the form.
            $labelCountdown.Text = [String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
            if ($remainingTime.TotalSeconds -le $CountdownNoHideSeconds)
            {
                $buttonRestartLater.Enabled = $false

                # If the form is hidden when we hit the "No Hide", bring it back up.
                if ($formRestart.WindowState.Equals([System.Windows.Forms.FormWindowState]::Minimized))
                {
                    $formRestart.WindowState = [System.Windows.Forms.FormWindowState]::Normal
                    $formRestart.TopMost = !$NotTopMost
                    $formRestart.Location = $formRestartPromptStartLocation
                    $formRestart.BringToFront()
                }
            }
        }
    }
    $formRestart_FormClosed = {
        $timerCountdown.remove_Tick($timerCountdown_Tick)
        $restartTimerPersist.remove_Tick($restartTimerPersist_Tick)
        $buttonRestartNow.remove_Click($buttonRestartNow_Click)
        $buttonRestartLater.remove_Click($buttonRestartLater_Click)
        $formRestart.remove_Load($formRestart_Load)
        $formRestart.remove_FormClosed($formRestart_FormClosed)
    }
    $formRestart_FormClosing = {
        if ($_.CloseReason -eq 'UserClosing')
        {
            $_.Cancel = $true
        }
    }

    # Persistence Timer.
    $timerCountdown = [System.Windows.Forms.Timer]::new()
    $restartTimerPersist = [System.Windows.Forms.Timer]::new()
    $restartTimerPersist.Interval = $adtConfig.UI.RestartPromptPersistInterval * 1000
    $restartTimerPersist.add_Tick($restartTimerPersist_Tick)
    if ($NoCountdown)
    {
        $restartTimerPersist.Start()
    }

    # Picture Banner.
    $pictureBanner = [System.Windows.Forms.PictureBox]::new()
    $pictureBanner.SizeMode = [System.Windows.Forms.PictureBoxSizeMode]::Zoom
    $pictureBanner.MinimumSize = $pictureBanner.ClientSize = $pictureBanner.MaximumSize = [System.Drawing.Size]::new($Script:Dialogs.Classic.Width, $Script:Dialogs.Classic.BannerHeight)
    $pictureBanner.Location = [System.Drawing.Point]::new(0, 0)
    $pictureBanner.Name = 'PictureBanner'
    $pictureBanner.Image = $Script:Dialogs.Classic.Assets.Banner
    $pictureBanner.Margin = $paddingNone
    $pictureBanner.TabStop = $false

    # Label Message.
    $labelMessage = [System.Windows.Forms.Label]::new()
    $labelMessage.MinimumSize = $labelMessage.ClientSize = $labelMessage.MaximumSize = $controlSize
    $labelMessage.Margin = [System.Windows.Forms.Padding]::new(0, 10, 0, 5)
    $labelMessage.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
    $labelMessage.Anchor = [System.Windows.Forms.AnchorStyles]::Top
    $labelMessage.Font = $Script:Dialogs.Classic.Font
    $labelMessage.Name = 'LabelMessage'
    $labelMessage.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
    $labelMessage.TabStop = $false
    $labelMessage.AutoSize = $true
    $labelMessage.Text = if (!$NoCountdown)
    {
        "$($adtStrings.RestartPrompt.Message.$DeploymentType) $($adtStrings.RestartPrompt.MessageTime)`n`n$($adtStrings.RestartPrompt.MessageRestart)"
    }
    else
    {
        $adtStrings.RestartPrompt.Message.$DeploymentType
    }

    # Label Countdown.
    $labelCountdown = [System.Windows.Forms.Label]::new()
    $labelCountdown.MinimumSize = $labelCountdown.ClientSize = $labelCountdown.MaximumSize = $controlSize
    $labelCountdown.Margin = $paddingNone
    $labelCountdown.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
    $labelCountdown.Font = [System.Drawing.Font]::new($Script:Dialogs.Classic.Font.Name, ($Script:Dialogs.Classic.Font.Size + 9), [System.Drawing.FontStyle]::Bold)
    $labelCountdown.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
    $labelCountdown.Text = '00:00:00'
    $labelCountdown.Name = 'LabelCountdown'
    $labelCountdown.TabStop = $false
    $labelCountdown.AutoSize = $true

    # Panel Flow Layout.
    $flowLayoutPanel = [System.Windows.Forms.FlowLayoutPanel]::new()
    $flowLayoutPanel.SuspendLayout()
    $flowLayoutPanel.MinimumSize = $flowLayoutPanel.ClientSize = $flowLayoutPanel.MaximumSize = $controlSize
    $flowLayoutPanel.Location = [System.Drawing.Point]::new(0, $Script:Dialogs.Classic.BannerHeight)
    $flowLayoutPanel.Margin = $flowLayoutPanel.Padding = $paddingNone
    $flowLayoutPanel.FlowDirection = [System.Windows.Forms.FlowDirection]::TopDown
    $flowLayoutPanel.AutoSize = $true
    $flowLayoutPanel.AutoSizeMode = [System.Windows.Forms.AutoSizeMode]::GrowAndShrink
    $flowLayoutPanel.Anchor = [System.Windows.Forms.AnchorStyles]::Top
    $flowLayoutPanel.WrapContents = $true
    $flowLayoutPanel.Controls.Add($labelMessage)
    if (!$NoCountdown)
    {
        # Label Time remaining message.
        $labelTimeRemaining = [System.Windows.Forms.Label]::new()
        $labelTimeRemaining.MinimumSize = $labelTimeRemaining.ClientSize = $labelTimeRemaining.MaximumSize = $controlSize
        $labelTimeRemaining.Margin = $paddingNone
        $labelTimeRemaining.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
        $labelTimeRemaining.Anchor = [System.Windows.Forms.AnchorStyles]::Top
        $labelTimeRemaining.Font = [System.Drawing.Font]::new($Script:Dialogs.Classic.Font.Name, ($Script:Dialogs.Classic.Font.Size + 3), [System.Drawing.FontStyle]::Bold)
        $labelTimeRemaining.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
        $labelTimeRemaining.Text = $adtStrings.RestartPrompt.TimeRemaining
        $labelTimeRemaining.Name = 'LabelTimeRemaining'
        $labelTimeRemaining.TabStop = $false
        $labelTimeRemaining.AutoSize = $true
        $flowLayoutPanel.Controls.Add($labelTimeRemaining)
        $flowLayoutPanel.Controls.Add($labelCountdown)
    }

    # Button Panel.
    $panelButtons = [System.Windows.Forms.Panel]::new()
    $panelButtons.SuspendLayout()
    $panelButtons.MinimumSize = $panelButtons.ClientSize = $panelButtons.MaximumSize = [System.Drawing.Size]::new($Script:Dialogs.Classic.Width, 39)
    $panelButtons.Margin = [System.Windows.Forms.Padding]::new(0, 10, 0, 0)
    $panelButtons.Padding = $paddingNone
    $panelButtons.AutoSize = $true

    # Button Restart Now.
    $buttonRestartNow = [System.Windows.Forms.Button]::new()
    $buttonRestartNow.MinimumSize = $buttonRestartNow.ClientSize = $buttonRestartNow.MaximumSize = $buttonSize
    $buttonRestartNow.Location = [System.Drawing.Point]::new(14, 4)
    $buttonRestartNow.Margin = $buttonRestartNow.Padding = $paddingNone
    $buttonRestartNow.Name = 'ButtonRestartNow'
    $buttonRestartNow.Font = $Script:Dialogs.Classic.Font
    $buttonRestartNow.Text = $adtStrings.RestartPrompt.ButtonRestartNow
    $buttonRestartNow.TabIndex = 1
    $buttonRestartNow.AutoSize = $true
    $buttonRestartNow.UseVisualStyleBackColor = $true
    $buttonRestartNow.add_Click($buttonRestartNow_Click)
    $panelButtons.Controls.Add($buttonRestartNow)

    # Button Minimize.
    $buttonRestartLater = [System.Windows.Forms.Button]::new()
    $buttonRestartLater.MinimumSize = $buttonRestartLater.ClientSize = $buttonRestartLater.MaximumSize = $buttonSize
    $buttonRestartLater.Location = [System.Drawing.Point]::new(240, 4)
    $buttonRestartLater.Margin = $buttonRestartLater.Padding = $paddingNone
    $buttonRestartLater.Name = 'ButtonRestartLater'
    $buttonRestartLater.Font = $Script:Dialogs.Classic.Font
    $buttonRestartLater.Text = $adtStrings.RestartPrompt.ButtonRestartLater
    $buttonRestartLater.TabIndex = 0
    $buttonRestartLater.AutoSize = $true
    $buttonRestartLater.UseVisualStyleBackColor = $true
    $buttonRestartLater.add_Click($buttonRestartLater_Click)
    $panelButtons.Controls.Add($buttonRestartLater)
    $panelButtons.ResumeLayout()

    # Add the Buttons Panel to the flowPanel.
    $flowLayoutPanel.Controls.Add($panelButtons)
    $flowLayoutPanel.ResumeLayout()

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

    # Form Restart.
    $formRestartPromptStartLocation = $null
    $formRestart = [System.Windows.Forms.Form]::new()
    $formRestart.SuspendLayout()
    $formRestart.ClientSize = $controlSize
    $formRestart.Margin = $formRestart.Padding = $paddingNone
    $formRestart.Font = $Script:Dialogs.Classic.Font
    $formRestart.Name = 'FormRestart'
    $formRestart.Text = $Title
    $formRestart.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Font
    $formRestart.AutoScaleDimensions = [System.Drawing.SizeF]::new(7, 15)
    $formRestart.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
    $formRestart.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::Fixed3D
    $formRestart.MaximizeBox = $false
    $formRestart.MinimizeBox = $false
    $formRestart.TopMost = !$NotTopMost
    $formRestart.TopLevel = $true
    $formRestart.AutoSize = $true
    $formRestart.Icon = $Script:Dialogs.Classic.Assets.Icon
    $formRestart.Controls.Add($pictureBanner)
    $formRestart.Controls.Add($flowLayoutPanel)
    $formRestart.Controls.Add($buttonDefault)
    $formRestart.add_Load($formRestart_Load)
    $formRestart.add_FormClosed($formRestart_FormClosed)
    $formRestart.add_FormClosing($formRestart_FormClosing)
    $formRestart.AcceptButton = $buttonDefault
    $formRestart.ActiveControl = $buttonDefault
    $formRestart.ResumeLayout()

    # Timer Countdown.
    if (!$NoCountdown)
    {
        $timerCountdown.add_Tick($timerCountdown_Tick)
    }

    # Show the Form.
    Write-ADTLogEntry -Message "Displaying restart prompt with $(if ($NoCountdown) { 'no' } else { "a [$CountdownSeconds] second" }) countdown."
    return $formRestart.ShowDialog()
}

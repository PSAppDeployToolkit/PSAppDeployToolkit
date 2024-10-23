#-----------------------------------------------------------------------------
#
# MARK: Show-ADTWelcomePromptClassic
#
#-----------------------------------------------------------------------------

function Show-ADTWelcomePromptClassic
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ProcessObjects', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    param
    (
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.ProcessObject[]]$ProcessObjects,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ($_ -gt (Get-ADTConfig).UI.DefaultTimeout)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName CloseAppsCountdown -ProvidedValue $_ -ExceptionMessage 'The close applications countdown time cannot be longer than the timeout specified in the config file.'))
                }
                return ($_ -ge 0)
            })]
        [System.Double]$CloseAppsCountdown,

        [ValidateNotNullOrEmpty()]
        [System.Int32]$DeferTimes,

        [ValidateNotNullOrEmpty()]
        [System.String]$DeferDeadline,

        [ValidateNotNullOrEmpty()]
        [System.UInt32]$ForceCountdown,

        [System.Management.Automation.SwitchParameter]$ForceCloseAppsCountdown,
        [System.Management.Automation.SwitchParameter]$PersistPrompt,
        [System.Management.Automation.SwitchParameter]$AllowDefer,
        [System.Management.Automation.SwitchParameter]$NoMinimizeWindows,
        [System.Management.Automation.SwitchParameter]$NotTopMost,
        [System.Management.Automation.SwitchParameter]$CustomText
    )

    # Perform initial setup.
    $adtConfig = Get-ADTConfig
    $adtStrings = Get-ADTStringTable
    $adtSession = Get-ADTSession

    # Initialize variables.
    $countdownTime = $startTime = [System.DateTime]::Now
    $showCountdown = $false
    $showCloseApps = $false
    $showDeference = $false
    $persistWindow = $false

    # Initial form layout: Close Applications
    if ($adtSession.RunningProcessDescriptions)
    {
        Write-ADTLogEntry -Message "Prompting the user to close application(s) [$($adtSession.RunningProcessDescriptions -join ',')]..."
        $showCloseApps = $true
    }

    # Initial form layout: Allow Deferral
    if ($AllowDefer -and (($DeferTimes -ge 0) -or $DeferDeadline))
    {
        Write-ADTLogEntry -Message 'The user has the option to defer.'
        $showDeference = $true

        # Remove the Z from universal sortable date time format, otherwise it could be converted to a different time zone.
        if ($DeferDeadline)
        {
            $DeferDeadline = (Get-Date -Date ($DeferDeadline -replace 'Z')).ToString()
        }
    }

    # If deferral is being shown and 'close apps countdown' or 'persist prompt' was specified, enable those features.
    if (!$showDeference)
    {
        if ($CloseAppsCountdown -gt 0)
        {
            Write-ADTLogEntry -Message "Close applications countdown has [$CloseAppsCountdown] seconds remaining."
            $showCountdown = $true
        }
    }
    elseif ($PersistPrompt)
    {
        $persistWindow = $true
    }

    # If 'force close apps countdown' was specified, enable that feature.
    if ($ForceCloseAppsCountdown)
    {
        Write-ADTLogEntry -Message "Close applications countdown has [$CloseAppsCountdown] seconds remaining."
        $showCountdown = $true
    }

    # If 'force countdown' was specified, enable that feature.
    if ($ForceCountdown)
    {
        Write-ADTLogEntry -Message "Countdown has [$CloseAppsCountdown] seconds remaining."
        $showCountdown = $true
    }

    # Set up some default values.
    $controlSize = [System.Drawing.Size]::new($Script:Dialogs.Classic.Width, 0)
    $paddingNone = [System.Windows.Forms.Padding]::new(0, 0, 0, 0)
    $buttonSize = [System.Drawing.Size]::new(130, 24)

    # Add the timer if it doesn't already exist - this avoids the timer being reset if the continue button is clicked.
    if (!$adtSession.WelcomeTimer)
    {
        $adtSession.WelcomeTimer = [System.Windows.Forms.Timer]::new()
    }

    # Define all form events.
    $formWelcome_FormClosed = {
        $adtSession.WelcomeTimer.remove_Tick($welcomeTimer_Tick)
        $welcomeTimerPersist.remove_Tick($welcomeTimerPersist_Tick)
        $timerRunningProcesses.remove_Tick($timerRunningProcesses_Tick)
        $formWelcome.remove_Load($formWelcome_Load)
        $formWelcome.remove_FormClosed($formWelcome_FormClosed)
    }
    $formWelcome_Load = {
        # Disable the X button.
        try
        {
            Disable-ADTWindowCloseButton -WindowHandle $formWelcome.Handle
        }
        catch
        {
            # Not a terminating error if we can't disable the button. Just disable the Control Box instead
            Write-ADTLogEntry 'Failed to disable the Close button. Disabling the Control Box instead.' -Severity 2
            $formWelcome.ControlBox = $false
        }

        # Initialize the countdown timer.
        $currentTime = [System.DateTime]::Now
        $countdownTime = $startTime.AddSeconds($CloseAppsCountdown)
        $adtSession.WelcomeTimer.Start()

        # Set up the form.
        $remainingTime = $countdownTime.Subtract($currentTime)
        $labelCountdown.Text = [System.String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)

        # Correct the initial state of the form to prevent the .NET maximized form issue.
        $formWelcome.WindowState = [System.Windows.Forms.FormWindowState]::Normal
        $formWelcome.BringToFront()

        # Get the start position of the form so we can return the form to this position if PersistPrompt is enabled.
        $adtSession.FormWelcomeStartPosition = $formWelcome.Location
    }
    $welcomeTimer_Tick = if ($showCountdown)
    {
        {
            # Get the time information.
            [DateTime]$currentTime = [System.DateTime]::Now
            [DateTime]$countdownTime = $startTime.AddSeconds($CloseAppsCountdown)
            [Timespan]$remainingTime = $countdownTime.Subtract($currentTime)
            $adtSession.CloseAppsCountdownGlobal = $remainingTime.TotalSeconds

            # If the countdown is complete, close the application(s) or continue.
            if ($countdownTime -le $currentTime)
            {
                if ($forceCountdown -eq $true)
                {
                    Write-ADTLogEntry -Message 'Countdown timer has elapsed. Force continue.'
                    $buttonContinue.PerformClick()
                }
                else
                {
                    Write-ADTLogEntry -Message 'Close application(s) countdown timer has elapsed. Force closing application(s).'
                    if ($buttonCloseApps.CanFocus)
                    {
                        $buttonCloseApps.PerformClick()
                    }
                    else
                    {
                        $buttonContinue.PerformClick()
                    }
                }
            }
            else
            {
                # Update the form.
                $labelCountdown.Text = [System.String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
            }
        }
    }
    else
    {
        $adtSession.WelcomeTimer.Interval = $adtConfig.UI.DefaultTimeout * 1000
        {
            $buttonAbort.PerformClick()
        }
    }
    $welcomeTimerPersist_Tick = {
        $formWelcome.WindowState = [System.Windows.Forms.FormWindowState]::Normal
        $formWelcome.TopMost = !$NotTopMost
        $formWelcome.Location = $adtSession.FormWelcomeStartPosition
        $formWelcome.BringToFront()
    }
    $timerRunningProcesses_Tick = {
        # Grab current list of running processes.
        $dynamicRunningProcesses = Get-ADTRunningProcesses -ProcessObjects $ProcessObjects -InformationAction SilentlyContinue
        $dynamicRunningProcessDescriptions = $dynamicRunningProcesses | Select-Object -ExpandProperty ProcessDescription | Sort-Object -Unique
        $previousRunningProcessDescriptions = $adtSession.RunningProcessDescriptions

        # Check the previous list against what's currently running.
        if (Compare-Object -ReferenceObject @($adtSession.RunningProcessDescriptions | Select-Object) -DifferenceObject @($dynamicRunningProcessDescriptions | Select-Object))
        {
            # Update the runningProcessDescriptions variable for the next time this function runs.
            $listboxCloseApps.Items.Clear()
            if (($adtSession.RunningProcessDescriptions = $dynamicRunningProcessDescriptions))
            {
                Write-ADTLogEntry -Message "The running processes have changed. Updating the apps to close: [$($adtSession.RunningProcessDescriptions -join ',')]..."
                $listboxCloseApps.Items.AddRange($adtSession.RunningProcessDescriptions)
            }
        }

        # If CloseApps processes were running when the prompt was shown, and they are subsequently detected to be closed while the form is showing, then close the form. The deferral and CloseApps conditions will be re-evaluated.
        if ($previousRunningProcessDescriptions)
        {
            if (!$dynamicRunningProcesses)
            {
                Write-ADTLogEntry -Message 'Previously detected running processes are no longer running.'
                $formWelcome.Dispose()
            }
        }
        elseif ($dynamicRunningProcesses)
        {
            # If CloseApps processes were not running when the prompt was shown, and they are subsequently detected to be running while the form is showing, then close the form for relaunch. The deferral and CloseApps conditions will be re-evaluated.
            Write-ADTLogEntry -Message 'New running processes detected. Updating the form to prompt to close the running applications.'
            $formWelcome.Dispose()
        }
    }

    # Welcome Timer.
    $adtSession.WelcomeTimer.add_Tick($welcomeTimer_Tick)

    # Persistence Timer.
    $welcomeTimerPersist = [System.Windows.Forms.Timer]::new()
    $welcomeTimerPersist.Interval = $adtConfig.UI.DefaultPromptPersistInterval * 1000
    $welcomeTimerPersist.add_Tick($welcomeTimerPersist_Tick)
    if ($persistWindow)
    {
        $welcomeTimerPersist.Start()
    }

    # Process Re-Enumeration Timer.
    $timerRunningProcesses = [System.Windows.Forms.Timer]::new()
    $timerRunningProcesses.Interval = $adtConfig.UI.DynamicProcessEvaluationInterval * 1000
    $timerRunningProcesses.add_Tick($timerRunningProcesses_Tick)
    if ($adtConfig.UI.DynamicProcessEvaluation)
    {
        $timerRunningProcesses.Start()
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

    # Label Welcome Message.
    $labelWelcomeMessage = [System.Windows.Forms.Label]::new()
    $labelWelcomeMessage.MinimumSize = $labelWelcomeMessage.ClientSize = $labelWelcomeMessage.MaximumSize = $controlSize
    $labelWelcomeMessage.Margin = [System.Windows.Forms.Padding]::new(0, 10, 0, 0)
    $labelWelcomeMessage.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
    $labelWelcomeMessage.Anchor = [System.Windows.Forms.AnchorStyles]::Top
    $labelWelcomeMessage.Font = $Script:Dialogs.Classic.Font
    $labelWelcomeMessage.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
    $labelWelcomeMessage.Text = $adtStrings.DeferPrompt.WelcomeMessage
    $labelWelcomeMessage.Name = 'LabelWelcomeMessage'
    $labelWelcomeMessage.TabStop = $false
    $labelWelcomeMessage.AutoSize = $true

    # Label App Name.
    $labelAppName = [System.Windows.Forms.Label]::new()
    $labelAppName.MinimumSize = $labelAppName.ClientSize = $labelAppName.MaximumSize = $controlSize
    $labelAppName.Margin = [System.Windows.Forms.Padding]::new(0, 5, 0, 5)
    $labelAppName.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
    $labelAppName.Anchor = [System.Windows.Forms.AnchorStyles]::Top
    $labelAppName.Font = [System.Drawing.Font]::new($Script:Dialogs.Classic.Font.Name, ($Script:Dialogs.Classic.Font.Size + 3), [System.Drawing.FontStyle]::Bold)
    $labelAppName.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
    $labelAppName.Text = $adtSession.GetPropertyValue('InstallTitle').Replace('&', '&&')
    $labelAppName.Name = 'LabelAppName'
    $labelAppName.TabStop = $false
    $labelAppName.AutoSize = $true

    # Listbox Close Applications.
    $listBoxCloseApps = [System.Windows.Forms.ListBox]::new()
    $listBoxCloseApps.MinimumSize = $listBoxCloseApps.ClientSize = $listBoxCloseApps.MaximumSize = [System.Drawing.Size]::new(420, 100)
    $listBoxCloseApps.Margin = [System.Windows.Forms.Padding]::new(15, 0, 15, 0)
    $listBoxCloseApps.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
    $listboxCloseApps.Font = $Script:Dialogs.Classic.Font
    $listBoxCloseApps.FormattingEnabled = $true
    $listBoxCloseApps.HorizontalScrollbar = $true
    $listBoxCloseApps.Name = 'ListBoxCloseApps'
    $listBoxCloseApps.TabIndex = 3
    if ($adtSession.RunningProcessDescriptions)
    {
        $null = $listboxCloseApps.Items.AddRange($adtSession.RunningProcessDescriptions)
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
    $flowLayoutPanel.Controls.Add($labelWelcomeMessage)
    $flowLayoutPanel.Controls.Add($labelAppName)
    if ($CustomText -and $adtStrings.WelcomePrompt.CustomMessage)
    {
        # Label CustomMessage.
        $labelCustomMessage = [System.Windows.Forms.Label]::new()
        $labelCustomMessage.MinimumSize = $labelCustomMessage.ClientSize = $labelCustomMessage.MaximumSize = $controlSize
        $labelCustomMessage.Margin = [System.Windows.Forms.Padding]::new(0, 0, 0, 5)
        $labelCustomMessage.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
        $labelCustomMessage.Anchor = [System.Windows.Forms.AnchorStyles]::Top
        $labelCustomMessage.Font = $Script:Dialogs.Classic.Font
        $labelCustomMessage.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
        $labelCustomMessage.Text = $adtStrings.WelcomePrompt.CustomMessage
        $labelCustomMessage.Name = 'LabelCustomMessage'
        $labelCustomMessage.TabStop = $false
        $labelCustomMessage.AutoSize = $true
        $flowLayoutPanel.Controls.Add($labelCustomMessage)
    }
    if ($showCloseApps)
    {
        # Label CloseAppsMessage.
        $labelCloseAppsMessage = [System.Windows.Forms.Label]::new()
        $labelCloseAppsMessage.MinimumSize = $labelCloseAppsMessage.ClientSize = $labelCloseAppsMessage.MaximumSize = $controlSize
        $labelCloseAppsMessage.Margin = [System.Windows.Forms.Padding]::new(0, 0, 0, 5)
        $labelCloseAppsMessage.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
        $labelCloseAppsMessage.Anchor = [System.Windows.Forms.AnchorStyles]::Top
        $labelCloseAppsMessage.Font = $Script:Dialogs.Classic.Font
        $labelCloseAppsMessage.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
        $labelCloseAppsMessage.Text = $adtStrings.ClosePrompt.Message
        $labelCloseAppsMessage.Name = 'LabelCloseAppsMessage'
        $labelCloseAppsMessage.TabStop = $false
        $labelCloseAppsMessage.AutoSize = $true
        $flowLayoutPanel.Controls.Add($labelCloseAppsMessage)

        # Listbox Close Applications.
        $flowLayoutPanel.Controls.Add($listBoxCloseApps)
    }
    if ($showDeference)
    {
        # Label Defer Expiry Message.
        $labelDeferExpiryMessage = [System.Windows.Forms.Label]::new()
        $labelDeferExpiryMessage.MinimumSize = $labelDeferExpiryMessage.ClientSize = $labelDeferExpiryMessage.MaximumSize = $controlSize
        $labelDeferExpiryMessage.Margin = [System.Windows.Forms.Padding]::new(0, 0, 0, 5)
        $labelDeferExpiryMessage.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
        $labelDeferExpiryMessage.Font = $Script:Dialogs.Classic.Font
        $labelDeferExpiryMessage.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
        $labelDeferExpiryMessage.Text = $adtStrings.DeferPrompt.ExpiryMessage
        $labelDeferExpiryMessage.Name = 'LabelDeferExpiryMessage'
        $labelDeferExpiryMessage.TabStop = $false
        $labelDeferExpiryMessage.AutoSize = $true
        $flowLayoutPanel.Controls.Add($labelDeferExpiryMessage)

        # Label Defer Deadline.
        $labelDeferDeadline = [System.Windows.Forms.Label]::new()
        $labelDeferDeadline.MinimumSize = $labelDeferDeadline.ClientSize = $labelDeferDeadline.MaximumSize = $controlSize
        $labelDeferDeadline.Margin = [System.Windows.Forms.Padding]::new(0, 0, 0, 5)
        $labelDeferDeadline.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
        $labelDeferDeadline.Font = [System.Drawing.Font]::new($Script:Dialogs.Classic.Font.Name, $Script:Dialogs.Classic.Font.Size, [System.Drawing.FontStyle]::Bold)
        $labelDeferDeadline.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
        $labelDeferDeadline.Name = 'LabelDeferDeadline'
        $labelDeferDeadline.TabStop = $false
        $labelDeferDeadline.AutoSize = $true
        if ($DeferTimes -ge 0)
        {
            $labelDeferDeadline.Text = "$($adtStrings.DeferPrompt.RemainingDeferrals) $($DeferTimes + 1)"
        }
        if ($deferDeadline)
        {
            $labelDeferDeadline.Text = "$($adtStrings.DeferPrompt.Deadline) $deferDeadline"
        }
        $flowLayoutPanel.Controls.Add($labelDeferDeadline)

        # Label Defer Expiry Message.
        $labelDeferWarningMessage = [System.Windows.Forms.Label]::new()
        $labelDeferWarningMessage.MinimumSize = $labelDeferWarningMessage.ClientSize = $labelDeferWarningMessage.MaximumSize = $controlSize
        $labelDeferWarningMessage.Margin = [System.Windows.Forms.Padding]::new(0, 0, 0, 5)
        $labelDeferWarningMessage.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
        $labelDeferWarningMessage.Font = $Script:Dialogs.Classic.Font
        $labelDeferWarningMessage.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
        $labelDeferWarningMessage.Text = $adtStrings.DeferPrompt.WarningMessage
        $labelDeferWarningMessage.Name = 'LabelDeferWarningMessage'
        $labelDeferWarningMessage.TabStop = $false
        $labelDeferWarningMessage.AutoSize = $true
        $flowLayoutPanel.Controls.Add($labelDeferWarningMessage)
    }
    if ($showCountdown)
    {
        # Label CountdownMessage.
        $labelCountdownMessage = [System.Windows.Forms.Label]::new()
        $labelCountdownMessage.MinimumSize = $labelCountdownMessage.ClientSize = $labelCountdownMessage.MaximumSize = $controlSize
        $labelCountdownMessage.Margin = $paddingNone
        $labelCountdownMessage.Padding = [System.Windows.Forms.Padding]::new(10, 0, 10, 0)
        $labelCountdownMessage.Anchor = [System.Windows.Forms.AnchorStyles]::Top
        $labelCountdownMessage.Font = [System.Drawing.Font]::new($Script:Dialogs.Classic.Font.Name, ($Script:Dialogs.Classic.Font.Size + 3), [System.Drawing.FontStyle]::Bold)
        $labelCountdownMessage.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
        $labelCountdownMessage.Name = 'LabelCountdownMessage'
        $labelCountdownMessage.TabStop = $false
        $labelCountdownMessage.AutoSize = $true
        $labelCountdownMessage.Text = if ($forceCountdown -or !$adtSession.RunningProcessDescriptions)
        {
            [System.String]::Format($adtStrings.WelcomePrompt.CountdownMessage, $adtSession.GetDeploymentTypeName())
        }
        else
        {
            $adtStrings.ClosePrompt.CountdownMessage
        }
        $flowLayoutPanel.Controls.Add($labelCountdownMessage)

        ## Label Countdown.
        $flowLayoutPanel.Controls.Add($labelCountdown)
    }

    # Panel Buttons.
    $panelButtons = [System.Windows.Forms.Panel]::new()
    $panelButtons.SuspendLayout()
    $panelButtons.MinimumSize = $panelButtons.ClientSize = $panelButtons.MaximumSize = [System.Drawing.Size]::new($Script:Dialogs.Classic.Width, 39)
    $panelButtons.Margin = [System.Windows.Forms.Padding]::new(0, 10, 0, 0)
    $panelButtons.Padding = $paddingNone
    $panelButtons.AutoSize = $true
    if ($showCloseApps)
    {
        # Button Close For Me.
        $buttonCloseApps = [System.Windows.Forms.Button]::new()
        $buttonCloseApps.MinimumSize = $buttonCloseApps.ClientSize = $buttonCloseApps.MaximumSize = $buttonSize
        $buttonCloseApps.Margin = $buttonCloseApps.Padding = $paddingNone
        $buttonCloseApps.Location = [System.Drawing.Point]::new(14, 4)
        $buttonCloseApps.DialogResult = [System.Windows.Forms.DialogResult]::Yes
        $buttonCloseApps.Font = $Script:Dialogs.Classic.Font
        $buttonCloseApps.Name = 'ButtonCloseApps'
        $buttonCloseApps.Text = $adtStrings.ClosePrompt.ButtonClose
        $buttonCloseApps.TabIndex = 1
        $buttonCloseApps.AutoSize = $true
        $buttonCloseApps.UseVisualStyleBackColor = $true
        $panelButtons.Controls.Add($buttonCloseApps)
    }
    if ($showDeference)
    {
        # Button Defer.
        $buttonDefer = [System.Windows.Forms.Button]::new()
        $buttonDefer.MinimumSize = $buttonDefer.ClientSize = $buttonDefer.MaximumSize = $buttonSize
        $buttonDefer.Margin = $buttonDefer.Padding = $paddingNone
        $buttonDefer.Location = [System.Drawing.Point]::new((14, 160)[$showCloseApps], 4)
        $buttonDefer.DialogResult = [System.Windows.Forms.DialogResult]::No
        $buttonDefer.Font = $Script:Dialogs.Classic.Font
        $buttonDefer.Name = 'ButtonDefer'
        $buttonDefer.Text = $adtStrings.ClosePrompt.ButtonDefer
        $buttonDefer.TabIndex = 0
        $buttonDefer.AutoSize = $true
        $buttonDefer.UseVisualStyleBackColor = $true
        $panelButtons.Controls.Add($buttonDefer)
    }

    # Button Continue.
    $buttonContinue = [System.Windows.Forms.Button]::new()
    $buttonContinue.MinimumSize = $buttonContinue.ClientSize = $buttonContinue.MaximumSize = $buttonSize
    $buttonContinue.Margin = $buttonContinue.Padding = $paddingNone
    $buttonContinue.Location = [System.Drawing.Point]::new(306, 4)
    $buttonContinue.DialogResult = [System.Windows.Forms.DialogResult]::OK
    $buttonContinue.Font = $Script:Dialogs.Classic.Font
    $buttonContinue.Name = 'ButtonContinue'
    $buttonContinue.Text = $adtStrings.ClosePrompt.ButtonContinue
    $buttonContinue.TabIndex = 2
    $buttonContinue.AutoSize = $true
    $buttonContinue.UseVisualStyleBackColor = $true
    if ($showCloseApps)
    {
        # Add tooltip to Continue button.
        $toolTip = [System.Windows.Forms.ToolTip]::new()
        $toolTip.BackColor = [Drawing.Color]::LightGoldenrodYellow
        $toolTip.IsBalloon = $false
        $toolTip.InitialDelay = 100
        $toolTip.ReshowDelay = 100
        $toolTip.SetToolTip($buttonContinue, $adtStrings.ClosePrompt.ButtonContinueTooltip)
    }
    $panelButtons.Controls.Add($buttonContinue)
    $panelButtons.ResumeLayout()

    # Add the Buttons Panel to the flowPanel.
    $flowLayoutPanel.Controls.Add($panelButtons)
    $flowLayoutPanel.ResumeLayout()

    # Button Abort (Hidden).
    $buttonAbort = [System.Windows.Forms.Button]::new()
    $buttonAbort.MinimumSize = $buttonAbort.ClientSize = $buttonAbort.MaximumSize = [System.Drawing.Size]::new(0, 0)
    $buttonAbort.Margin = $buttonAbort.Padding = $paddingNone
    $buttonAbort.DialogResult = [System.Windows.Forms.DialogResult]::Abort
    $buttonAbort.Name = 'buttonAbort'
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

    ## Form Welcome
    $formWelcome = [System.Windows.Forms.Form]::new()
    $formWelcome.SuspendLayout()
    $formWelcome.ClientSize = $controlSize
    $formWelcome.Margin = $formWelcome.Padding = $paddingNone
    $formWelcome.Font = $Script:Dialogs.Classic.Font
    $formWelcome.Name = 'WelcomeForm'
    $formWelcome.Text = $adtSession.GetPropertyValue('InstallTitle')
    $formWelcome.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Font
    $formWelcome.AutoScaleDimensions = [System.Drawing.SizeF]::new(7, 15)
    $formWelcome.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
    $formWelcome.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::Fixed3D
    $formWelcome.MaximizeBox = $false
    $formWelcome.MinimizeBox = $false
    $formWelcome.TopMost = !$NotTopMost
    $formWelcome.TopLevel = $true
    $formWelcome.AutoSize = $true
    $formWelcome.Icon = $Script:Dialogs.Classic.Assets.Icon
    $formWelcome.Controls.Add($pictureBanner)
    $formWelcome.Controls.Add($buttonAbort)
    $formWelcome.Controls.Add($buttonDefault)
    $formWelcome.Controls.Add($flowLayoutPanel)
    $formWelcome.add_Load($formWelcome_Load)
    $formWelcome.add_FormClosed($formWelcome_FormClosed)
    $formWelcome.AcceptButton = $buttonDefault
    $formWelcome.ActiveControl = $buttonDefault
    $formWelcome.ResumeLayout()

    # Minimize all other windows.
    if (!$NoMinimizeWindows)
    {
        $null = (Get-ADTEnvironment).ShellApp.MinimizeAll()
    }

    # Run the form and store the result.
    $result = switch ($formWelcome.ShowDialog())
    {
        OK { 'Continue'; break }
        No { 'Defer'; break }
        Yes { 'Close'; break }
        Abort { 'Timeout'; break }
    }
    $formWelcome.Dispose()

    # Shut down the timer if its running.
    if ($adtConfig.UI.DynamicProcessEvaluation)
    {
        $timerRunningProcesses.Stop()
    }

    # Return the result to the caller.
    return $result
}

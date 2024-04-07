﻿Function Show-WelcomePrompt {
    <#
.SYNOPSIS

Called by Show-InstallationWelcome to prompt the user to optionally do the following:
    1) Close the specified running applications.
    2) Provide an option to defer the installation.
    3) Show a countdown before applications are automatically closed.

.DESCRIPTION

The user is presented with a Windows Forms dialog box to close the applications themselves and continue or to have the script close the applications for them.
If the -AllowDefer option is set to true, an optional "Defer" button will be shown to the user. If they select this option, the script will exit and return a 1618 code (SCCM fast retry code).
The dialog box will timeout after the timeout specified in the XML configuration file (default 1 hour and 55 minutes) to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).

.PARAMETER ProcessDescriptions

The descriptive names of the applications that are running and need to be closed.

.PARAMETER CloseAppsCountdown

Specify the countdown time in seconds before running applications are automatically closed when deferral is not allowed or expired.

.PARAMETER ForceCloseAppsCountdown

Specify whether to show the countdown regardless of whether deferral is allowed.

.PARAMETER PersistPrompt

Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml.

.PARAMETER AllowDefer

Specify whether to provide an option to defer the installation.

.PARAMETER DeferTimes

Specify the number of times the user is allowed to defer.

.PARAMETER DeferDeadline

Specify the deadline date before the user is allowed to defer.

.PARAMETER MinimizeWindows

Specifies whether to minimize other windows when displaying prompt. Default: $true.

.PARAMETER TopMost

Specifies whether the windows is the topmost window. Default: $true.

.PARAMETER ForceCountdown

Specify a countdown to display before automatically proceeding with the installation when a deferral is enabled.

.PARAMETER CustomText

Specify whether to display a custom message specified in the XML file. Custom message must be populated for each language section in the XML.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the user's selection.

.EXAMPLE

Show-WelcomePrompt -ProcessDescriptions 'Lotus Notes, Microsoft Word' -CloseAppsCountdown 600 -AllowDefer -DeferTimes 10

.NOTES

This is an internal script function and should typically not be called directly. It is used by the Show-InstallationWelcome prompt to display a custom prompt.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [String]$ProcessDescriptions,
        [Parameter(Mandatory = $false)]
        [Int32]$CloseAppsCountdown,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ForceCloseAppsCountdown,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$PersistPrompt = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$AllowDefer = $false,
        [Parameter(Mandatory = $false)]
        [String]$DeferTimes,
        [Parameter(Mandatory = $false)]
        [String]$DeferDeadline,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$MinimizeWindows = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$TopMost = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$ForceCountdown = 0,
        [Parameter(Mandatory = $false)]
        [Switch]$CustomText = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
        $showCountdown = $false
    }
    Process {
        ## Reset switches
        [Boolean]$showCloseApps = $false
        [Boolean]$showDefer = $false
        [Boolean]$persistWindow = $false

        ## Reset times
        [DateTime]$startTime = Get-Date
        [DateTime]$countdownTime = $startTime

        ## Check if the countdown was specified
        If ($CloseAppsCountdown -and ($CloseAppsCountdown -gt $configInstallationUITimeout)) {
            Throw 'The close applications countdown time cannot be longer than the timeout specified in the XML configuration for installation UI dialogs to timeout.'
        }

        ## Initial form layout: Close Applications / Allow Deferral
        If ($processDescriptions) {
            Write-Log -Message "Prompting the user to close application(s) [$processDescriptions]..." -Source ${CmdletName}
            $showCloseApps = $true
        }
        If (($allowDefer) -and (($deferTimes -ge 0) -or ($deferDeadline))) {
            Write-Log -Message 'The user has the option to defer.' -Source ${CmdletName}
            $showDefer = $true
            If ($deferDeadline) {
                #  Remove the Z from universal sortable date time format, otherwise it could be converted to a different time zone
                $deferDeadline = $deferDeadline -replace 'Z', ''
                #  Convert the deadline date to a string
                [String]$deferDeadline = (Get-Date -Date $deferDeadline).ToString()
            }
        }

        ## If deferral is being shown and 'close apps countdown' or 'persist prompt' was specified, enable those features.
        If (-not $showDefer) {
            If ($closeAppsCountdown -gt 0) {
                Write-Log -Message "Close applications countdown has [$closeAppsCountdown] seconds remaining." -Source ${CmdletName}
                $showCountdown = $true
            }
        }
        Else {
            If ($persistPrompt) {
                $persistWindow = $true
            }
        }
        ## If 'force close apps countdown' was specified, enable that feature.
        If ($forceCloseAppsCountdown -eq $true) {
            Write-Log -Message "Close applications countdown has [$closeAppsCountdown] seconds remaining." -Source ${CmdletName}
            $showCountdown = $true
        }
        ## If 'force countdown' was specified, enable that feature.
        If ($forceCountdown -eq $true) {
            Write-Log -Message "Countdown has [$closeAppsCountdown] seconds remaining." -Source ${CmdletName}
            $showCountdown = $true
        }

        [String[]]$processDescriptions = $processDescriptions -split ','
        [Windows.Forms.Application]::EnableVisualStyles()

        $formWelcome = New-Object -TypeName 'System.Windows.Forms.Form'
        $formWelcome.SuspendLayout()
        $pictureBanner = New-Object -TypeName 'System.Windows.Forms.PictureBox'
        $labelWelcomeMessage = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelAppName = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelCustomMessage = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelCloseAppsMessage = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelCountdownMessage = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelCountdown = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelDefer = New-Object -TypeName 'System.Windows.Forms.Label'
        $listBoxCloseApps = New-Object -TypeName 'System.Windows.Forms.ListBox'
        $buttonContinue = New-Object -TypeName 'System.Windows.Forms.Button'
        $buttonDefer = New-Object -TypeName 'System.Windows.Forms.Button'
        $buttonCloseApps = New-Object -TypeName 'System.Windows.Forms.Button'
        $buttonAbort = New-Object -TypeName 'System.Windows.Forms.Button'
        $flowLayoutPanel = New-Object -TypeName 'System.Windows.Forms.FlowLayoutPanel'
        $panelButtons = New-Object -TypeName 'System.Windows.Forms.Panel'
        $toolTip = New-Object -TypeName 'System.Windows.Forms.ToolTip'

        ## Remove all event handlers from the controls
        [ScriptBlock]$Welcome_Form_Cleanup_FormClosed = {
            Try {
                $script:welcomeTimer.remove_Tick($welcomeTimer_Tick)
                $welcomeTimerPersist.remove_Tick($welcomeTimerPersist_Tick)
                $timerRunningProcesses.remove_Tick($timerRunningProcesses_Tick)
                $formWelcome.remove_Load($Welcome_Form_StateCorrection_Load)
                $formWelcome.remove_FormClosed($Welcome_Form_Cleanup_FormClosed)
            }
            Catch {
            }
        }

        [ScriptBlock]$Welcome_Form_StateCorrection_Load = {
            # Disable the X button
            Try {
                $windowHandle = $formWelcome.Handle
                If ($windowHandle -and ($windowHandle -ne [IntPtr]::Zero)) {
                    $menuHandle = [PSADT.UiAutomation]::GetSystemMenu($windowHandle, $false)
                    If ($menuHandle -and ($menuHandle -ne [IntPtr]::Zero)) {
                        [PSADT.UiAutomation]::EnableMenuItem($menuHandle, 0xF060, 0x00000001)
                        [PSADT.UiAutomation]::DestroyMenu($menuHandle)
                    }
                }
            }
            Catch {
                # Not a terminating error if we can't disable the button. Just disable the Control Box instead
                Write-Log 'Failed to disable the Close button. Disabling the Control Box instead.' -Severity 2 -Source ${CmdletName}
                $formWelcome.ControlBox = $false
            }
            #  Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
            Set-Variable -Name 'formWelcomeStartPosition' -Value $formWelcome.Location -Scope 'Script'

            ## Initialize the countdown timer
            [DateTime]$currentTime = Get-Date
            [DateTime]$countdownTime = $startTime.AddSeconds($CloseAppsCountdown)
            $script:welcomeTimer.Start()

            ## Set up the form
            [Timespan]$remainingTime = $countdownTime.Subtract($currentTime)
            $labelCountdown.Text = [String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
        }

        ## Add the timer if it doesn't already exist - this avoids the timer being reset if the continue button is clicked
        If (!(Test-Path -LiteralPath 'variable:welcomeTimer')) {
            $script:welcomeTimer = New-Object -TypeName 'System.Windows.Forms.Timer'
        }

        If ($showCountdown) {
            [ScriptBlock]$welcomeTimer_Tick = {
                ## Get the time information
                [DateTime]$currentTime = Get-Date
                [DateTime]$countdownTime = $startTime.AddSeconds($CloseAppsCountdown)
                [Timespan]$remainingTime = $countdownTime.Subtract($currentTime)
                Set-Variable -Name 'closeAppsCountdownGlobal' -Value $remainingTime.TotalSeconds -Scope 'Script'

                ## If the countdown is complete, close the application(s) or continue
                If ($countdownTime -le $currentTime) {
                    If ($forceCountdown -eq $true) {
                        Write-Log -Message 'Countdown timer has elapsed. Force continue.' -Source ${CmdletName}
                        $buttonContinue.PerformClick()
                    }
                    Else {
                        Write-Log -Message 'Close application(s) countdown timer has elapsed. Force closing application(s).' -Source ${CmdletName}
                        If ($buttonCloseApps.CanFocus) {
                            $buttonCloseApps.PerformClick()
                        }
                        Else {
                            $buttonContinue.PerformClick()
                        }
                    }
                }
                Else {
                    #  Update the form
                    $labelCountdown.Text = [String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
                }
            }
        }
        Else {
            $script:welcomeTimer.Interval = ($configInstallationUITimeout * 1000)
            [ScriptBlock]$welcomeTimer_Tick = { $buttonAbort.PerformClick() }
        }

        $script:welcomeTimer.add_Tick($welcomeTimer_Tick)

        ## Persistence Timer
        If ($persistWindow) {
            $welcomeTimerPersist = New-Object -TypeName 'System.Windows.Forms.Timer'
            $welcomeTimerPersist.Interval = ($configInstallationPersistInterval * 1000)
            [ScriptBlock]$welcomeTimerPersist_Tick = {
                $formWelcome.WindowState = 'Normal'
                $formWelcome.TopMost = $TopMost
                $formWelcome.BringToFront()
                $formWelcome.Location = "$($formWelcomeStartPosition.X),$($formWelcomeStartPosition.Y)"
            }
            $welcomeTimerPersist.add_Tick($welcomeTimerPersist_Tick)
            $welcomeTimerPersist.Start()
        }

        ## Process Re-Enumeration Timer
        If ($configInstallationWelcomePromptDynamicRunningProcessEvaluation) {
            $timerRunningProcesses = New-Object -TypeName 'System.Windows.Forms.Timer'
            $timerRunningProcesses.Interval = ($configInstallationWelcomePromptDynamicRunningProcessEvaluationInterval * 1000)
            [ScriptBlock]$timerRunningProcesses_Tick = {
                Try {
                    $dynamicRunningProcesses = $null
                    $dynamicRunningProcesses = Get-RunningProcesses -ProcessObjects $processObjects -DisableLogging
                    [String]$dynamicRunningProcessDescriptions = ($dynamicRunningProcesses | Where-Object { $_.ProcessDescription } | Select-Object -ExpandProperty 'ProcessDescription' | Sort-Object -Unique) -join ','
                    If ($dynamicRunningProcessDescriptions -ne $script:runningProcessDescriptions) {
                        # Update the runningProcessDescriptions variable for the next time this function runs
                        Set-Variable -Name 'runningProcessDescriptions' -Value $dynamicRunningProcessDescriptions -Force -Scope 'Script'
                        If ($dynamicRunningProcesses) {
                            Write-Log -Message "The running processes have changed. Updating the apps to close: [$script:runningProcessDescriptions]..." -Source ${CmdletName}
                        }
                        # Update the list box with the processes to close
                        $listboxCloseApps.Items.Clear()
                        $script:runningProcessDescriptions -split ',' | ForEach-Object { $null = $listboxCloseApps.Items.Add($_) }
                    }
                    # If CloseApps processes were running when the prompt was shown, and they are subsequently detected to be closed while the form is showing, then close the form. The deferral and CloseApps conditions will be re-evaluated.
                    If ($ProcessDescriptions) {
                        If (-not $dynamicRunningProcesses) {
                            Write-Log -Message 'Previously detected running processes are no longer running.' -Source ${CmdletName}
                            $formWelcome.Dispose()
                        }
                    }
                    # If CloseApps processes were not running when the prompt was shown, and they are subsequently detected to be running while the form is showing, then close the form for relaunch. The deferral and CloseApps conditions will be re-evaluated.
                    Else {
                        If ($dynamicRunningProcesses) {
                            Write-Log -Message 'New running processes detected. Updating the form to prompt to close the running applications.' -Source ${CmdletName}
                            $formWelcome.Dispose()
                        }
                    }
                }
                Catch {
                }
            }
            $timerRunningProcesses.add_Tick($timerRunningProcesses_Tick)
            $timerRunningProcesses.Start()
        }

        ## Form

        ##----------------------------------------------
        ## Create zero px padding object
        $paddingNone = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 0)
        ## Create basic control size
        $defaultControlSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 0)

        ## Generic Button properties
        $buttonSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (130, 24)

        ## Picture Banner
        $pictureBanner.DataBindings.DefaultDataSourceUpdateMode = 0
        $pictureBanner.ImageLocation = $appDeployLogoBanner
        $System_Drawing_Point = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, 0)
        $pictureBanner.Location = $System_Drawing_Point
        $pictureBanner.Name = 'pictureBanner'
        $System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, $appDeployLogoBannerHeight)
        $pictureBanner.ClientSize = $System_Drawing_Size
        $pictureBanner.SizeMode = [System.Windows.Forms.PictureBoxSizeMode]::Zoom
        $pictureBanner.Margin = $paddingNone
        $pictureBanner.TabStop = $false

        ## Label Welcome Message
        $labelWelcomeMessage.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelWelcomeMessage.Font = $defaultFont
        $labelWelcomeMessage.Name = 'labelWelcomeMessage'
        $labelWelcomeMessage.ClientSize = $defaultControlSize
        $labelWelcomeMessage.MinimumSize = $defaultControlSize
        $labelWelcomeMessage.MaximumSize = $defaultControlSize
        $labelWelcomeMessage.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 10, 0, 0)
        $labelWelcomeMessage.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelWelcomeMessage.TabStop = $false
        $labelWelcomeMessage.Text = $configDeferPromptWelcomeMessage
        $labelWelcomeMessage.TextAlign = 'MiddleCenter'
        $labelWelcomeMessage.Anchor = 'Top'
        $labelWelcomeMessage.AutoSize = $true

        ## Label App Name
        $labelAppName.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelAppName.Font = "$($defaultFont.Name), $($defaultFont.Size + 2), style=Bold"
        $labelAppName.Name = 'labelAppName'
        $labelAppName.ClientSize = $defaultControlSize
        $labelAppName.MinimumSize = $defaultControlSize
        $labelAppName.MaximumSize = $defaultControlSize
        $labelAppName.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 5, 0, 5)
        $labelAppName.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelAppName.TabStop = $false
        $labelAppName.Text = $installTitle
        $labelAppName.TextAlign = 'MiddleCenter'
        $labelAppName.Anchor = 'Top'
        $labelAppName.AutoSize = $true

        ## Label CustomMessage
        $labelCustomMessage.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelCustomMessage.Font = $defaultFont
        $labelCustomMessage.Name = 'labelCustomMessage'
        $labelCustomMessage.ClientSize = $defaultControlSize
        $labelCustomMessage.MinimumSize = $defaultControlSize
        $labelCustomMessage.MaximumSize = $defaultControlSize
        $labelCustomMessage.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 5)
        $labelCustomMessage.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelCustomMessage.TabStop = $false
        $labelCustomMessage.Text = $configClosePromptMessage
        $labelCustomMessage.TextAlign = 'MiddleCenter'
        $labelCustomMessage.Anchor = 'Top'
        $labelCustomMessage.AutoSize = $true

        ## Label CloseAppsMessage
        $labelCloseAppsMessage.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelCloseAppsMessage.Font = $defaultFont
        $labelCloseAppsMessage.Name = 'labelCloseAppsMessage'
        $labelCloseAppsMessage.ClientSize = $defaultControlSize
        $labelCloseAppsMessage.MinimumSize = $defaultControlSize
        $labelCloseAppsMessage.MaximumSize = $defaultControlSize
        $labelCloseAppsMessage.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 5)
        $labelCloseAppsMessage.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelCloseAppsMessage.TabStop = $false
        $labelCloseAppsMessage.Text = $configClosePromptMessage
        $labelCloseAppsMessage.TextAlign = 'MiddleCenter'
        $labelCloseAppsMessage.Anchor = 'Top'
        $labelCloseAppsMessage.AutoSize = $true

        ## Listbox Close Applications
        $listBoxCloseApps.DataBindings.DefaultDataSourceUpdateMode = 0
        $listboxCloseApps.Font = $defaultFont
        $listBoxCloseApps.FormattingEnabled = $true
        $listBoxCloseApps.HorizontalScrollbar = $true
        $listBoxCloseApps.Name = 'listBoxCloseApps'
        $System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (420, 100)
        $listBoxCloseApps.ClientSize = $System_Drawing_Size
        $listBoxCloseApps.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (15, 0, 15, 0)
        $listBoxCloseApps.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $listBoxCloseApps.TabIndex = 3
        $ProcessDescriptions | ForEach-Object { $null = $listboxCloseApps.Items.Add($_) }

        ## Label Defer
        $labelDefer.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelDefer.Font = $defaultFont
        $labelDefer.Name = 'labelDefer'
        $labelDefer.ClientSize = $defaultControlSize
        $labelDefer.MinimumSize = $defaultControlSize
        $labelDefer.MaximumSize = $defaultControlSize
        $labelDefer.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 5)
        $labelDefer.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelDefer.TabStop = $false
        $deferralText = "$configDeferPromptExpiryMessage`r`n"

        If ($deferTimes -ge 0) {
            $deferralText = "$deferralText `r`n$configDeferPromptRemainingDeferrals $([Int32]$deferTimes + 1)"
        }
        If ($deferDeadline) {
            $deferralText = "$deferralText `r`n$configDeferPromptDeadline $deferDeadline"
        }
        If (($deferTimes -lt 0) -and (-not $DeferDeadline)) {
            $deferralText = "$deferralText `r`n$configDeferPromptNoDeadline"
        }
        $deferralText = "$deferralText `r`n`r`n$configDeferPromptWarningMessage"
        $labelDefer.Text = $deferralText
        $labelDefer.TextAlign = 'MiddleCenter'
        $labelDefer.AutoSize = $true

        ## Label CountdownMessage
        $labelCountdownMessage.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelCountdownMessage.Name = 'labelCountdownMessage'
        $labelCountdownMessage.Font = "$($defaultFont.Name), $($defaultFont.Size + 2), style=Regular"
        $labelCountdownMessage.ClientSize = $defaultControlSize
        $labelCountdownMessage.MinimumSize = $defaultControlSize
        $labelCountdownMessage.MaximumSize = $defaultControlSize
        $labelCountdownMessage.Margin = $paddingNone
        $labelCountdownMessage.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelCountdownMessage.TabStop = $false
        If (($forceCountdown -eq $true) -or (-not $script:runningProcessDescriptions)) {
            Switch ($deploymentType) {
                'Uninstall' {
                    $labelCountdownMessage.Text = ($configWelcomePromptCountdownMessage -f $configDeploymentTypeUninstall); Break
                }
                'Repair' {
                    $labelCountdownMessage.Text = ($configWelcomePromptCountdownMessage -f $configDeploymentTypeRepair); Break
                }
                Default {
                    $labelCountdownMessage.Text = ($configWelcomePromptCountdownMessage -f $configDeploymentTypeInstall); Break
                }
            }
        }
        Else {
            $labelCountdownMessage.Text = $configClosePromptCountdownMessage
        }
        $labelCountdownMessage.TextAlign = 'MiddleCenter'
        $labelCountdownMessage.Anchor = 'Top'
        $labelCountdownMessage.AutoSize = $true

        ## Label Countdown
        $labelCountdown.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelCountdown.Name = 'labelCountdown'
        $labelCountdown.Font = "$($defaultFont.Name), $($defaultFont.Size + 9), style=Bold"
        $labelCountdown.ClientSize = $defaultControlSize
        $labelCountdown.MinimumSize = $defaultControlSize
        $labelCountdown.MaximumSize = $defaultControlSize
        $labelCountdown.Margin = $paddingNone
        $labelCountdown.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelCountdown.TabStop = $false
        $labelCountdown.Text = '00:00:00'
        $labelCountdown.TextAlign = 'MiddleCenter'
        $labelCountdown.AutoSize = $true

        ## Panel Flow Layout
        $System_Drawing_Point = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, $appDeployLogoBannerHeight)
        $flowLayoutPanel.Location = $System_Drawing_Point
        $flowLayoutPanel.MinimumSize = $DefaultControlSize
        $flowLayoutPanel.MaximumSize = $DefaultControlSize
        $flowLayoutPanel.ClientSize = $DefaultControlSize
        $flowLayoutPanel.Margin = $paddingNone
        $flowLayoutPanel.Padding = $paddingNone
        $flowLayoutPanel.AutoSizeMode = 'GrowAndShrink'
        $flowLayoutPanel.AutoSize = $true
        $flowLayoutPanel.Anchor = 'Top'
        $flowLayoutPanel.FlowDirection = 'TopDown'
        $flowLayoutPanel.WrapContents = $true
        $flowLayoutPanel.Controls.Add($labelWelcomeMessage)
        $flowLayoutPanel.Controls.Add($labelAppName)

        If ($CustomText -and $configWelcomePromptCustomMessage) {
            $labelCustomMessage.Text = $configWelcomePromptCustomMessage
            $flowLayoutPanel.Controls.Add($labelCustomMessage)
        }
        If ($showCloseApps) {
            $flowLayoutPanel.Controls.Add($labelCloseAppsMessage)
            $flowLayoutPanel.Controls.Add($listBoxCloseApps)
        }
        If ($showDefer) {
            $flowLayoutPanel.Controls.Add($labelDefer)
        }
        If ($showCountdown) {
            $flowLayoutPanel.Controls.Add($labelCountdownMessage)
            $flowLayoutPanel.Controls.Add($labelCountdown)
        }

        ## Button Close For Me
        $buttonCloseApps.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonCloseApps.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (14, 4)
        $buttonCloseApps.Font = $defaultFont
        $buttonCloseApps.Name = 'buttonCloseApps'
        $buttonCloseApps.ClientSize = $buttonSize
        $buttonCloseApps.MinimumSize = $buttonSize
        $buttonCloseApps.MaximumSize = $buttonSize
        $buttonCloseApps.TabIndex = 1
        $buttonCloseApps.Text = $configClosePromptButtonClose
        $buttonCloseApps.DialogResult = 'Yes'
        $buttonCloseApps.AutoSize = $true
        $buttonCloseApps.Margin = $paddingNone
        $buttonCloseApps.Padding = $paddingNone
        $buttonCloseApps.UseVisualStyleBackColor = $true

        ## Button Defer
        $buttonDefer.DataBindings.DefaultDataSourceUpdateMode = 0
        If (-not $showCloseApps) {
            $buttonDefer.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (14, 4)
        }
        Else {
            $buttonDefer.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (160, 4)
        }
        $buttonDefer.Name = 'buttonDefer'
        $buttonDefer.Font = $defaultFont
        $buttonDefer.ClientSize = $buttonSize
        $buttonDefer.MinimumSize = $buttonSize
        $buttonDefer.MaximumSize = $buttonSize
        $buttonDefer.TabIndex = 0
        $buttonDefer.Text = $configClosePromptButtonDefer
        $buttonDefer.DialogResult = 'No'
        $buttonDefer.AutoSize = $true
        $buttonDefer.Margin = $paddingNone
        $buttonDefer.Padding = $paddingNone
        $buttonDefer.UseVisualStyleBackColor = $true

        ## Button Continue
        $buttonContinue.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonContinue.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (306, 4)
        $buttonContinue.Name = 'buttonContinue'
        $buttonContinue.Font = $defaultFont
        $buttonContinue.ClientSize = $buttonSize
        $buttonContinue.MinimumSize = $buttonSize
        $buttonContinue.MaximumSize = $buttonSize
        $buttonContinue.TabIndex = 2
        $buttonContinue.Text = $configClosePromptButtonContinue
        $buttonContinue.DialogResult = 'OK'
        $buttonContinue.AutoSize = $true
        $buttonContinue.Margin = $paddingNone
        $buttonContinue.Padding = $paddingNone
        $buttonContinue.UseVisualStyleBackColor = $true
        If ($showCloseApps) {
            #  Add tooltip to Continue button
            $toolTip.BackColor = [Drawing.Color]::LightGoldenrodYellow
            $toolTip.IsBalloon = $false
            $toolTip.InitialDelay = 100
            $toolTip.ReshowDelay = 100
            $toolTip.SetToolTip($buttonContinue, $configClosePromptButtonContinueTooltip)
        }

        ## Button Abort (Hidden)
        $buttonAbort.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonAbort.Name = 'buttonAbort'
        $buttonAbort.Font = $defaultFont
        $buttonAbort.ClientSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (0, 0)
        $buttonAbort.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (0, 0)
        $buttonAbort.MaximumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (0, 0)
        $buttonAbort.BackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.ForeColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatAppearance.BorderSize = 0
        $buttonAbort.FlatAppearance.MouseDownBackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatAppearance.MouseOverBackColor = [System.Drawing.Color]::Transparent
        $buttonAbort.FlatStyle = [System.Windows.Forms.FlatStyle]::System
        $buttonAbort.TabStop = $false
        $buttonAbort.DialogResult = 'Abort'
        $buttonAbort.Visible = $true # Has to be set visible so we can call Click on it
        $buttonAbort.Margin = $paddingNone
        $buttonAbort.Padding = $paddingNone
        $buttonAbort.UseVisualStyleBackColor = $true

        ## Form Welcome
        $formWelcome.ClientSize = $defaultControlSize
        $formWelcome.Padding = $paddingNone
        $formWelcome.Margin = $paddingNone
        $formWelcome.DataBindings.DefaultDataSourceUpdateMode = 0
        $formWelcome.Name = 'WelcomeForm'
        $formWelcome.Text = $installTitle
        $formWelcome.StartPosition = 'CenterScreen'
        $formWelcome.FormBorderStyle = 'Fixed3D'
        $formWelcome.MaximizeBox = $false
        $formWelcome.MinimizeBox = $false
        $formWelcome.TopMost = $TopMost
        $formWelcome.TopLevel = $true
        $formWelcome.Icon = New-Object -TypeName 'System.Drawing.Icon' -ArgumentList ($AppDeployLogoIcon)
        $formWelcome.AutoSize = $true
        $formWelcome.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Dpi
        $formWelcome.AutoScaleDimensions = New-Object System.Drawing.SizeF(96,96)
        $formWelcome.Controls.Add($pictureBanner)
        $formWelcome.Controls.Add($buttonAbort)
        ## Panel Button
        $panelButtons.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        $panelButtons.ClientSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        $panelButtons.MaximumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (450, 39)
        $panelButtons.AutoSize = $true
        $panelButtons.Padding = $paddingNone
        $panelButtons.Margin = $paddingNone
        If ($showCloseApps) {
            $panelButtons.Controls.Add($buttonCloseApps)
        }
        If ($showDefer) {
            $panelButtons.Controls.Add($buttonDefer)
        }
        $panelButtons.Controls.Add($buttonContinue)

        ## Add the Buttons Panel to the flowPanel
        $flowLayoutPanel.Controls.Add($panelButtons)
        ## Add FlowPanel to the form
        $formWelcome.Controls.Add($flowLayoutPanel)
        #  Init the OnLoad event to correct the initial state of the form
        $formWelcome.add_Load($Welcome_Form_StateCorrection_Load)
        #  Clean up the control events
        $formWelcome.add_FormClosed($Welcome_Form_Cleanup_FormClosed)

        ## Minimize all other windows
        If ($minimizeWindows) {
            $null = $shellApp.MinimizeAll()
        }

        ## Show the form
        $formWelcome.ResumeLayout()
        $result = $formWelcome.ShowDialog()
        $formWelcome.Dispose()

        Switch ($result) {
            OK {
                $result = 'Continue'
            }
            No {
                $result = 'Defer'
            }
            Yes {
                $result = 'Close'
            }
            Abort {
                $result = 'Timeout'
            }
        }

        If ($configInstallationWelcomePromptDynamicRunningProcessEvaluation) {
            $timerRunningProcesses.Stop()
        }

        Write-Output -InputObject ($result)
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}

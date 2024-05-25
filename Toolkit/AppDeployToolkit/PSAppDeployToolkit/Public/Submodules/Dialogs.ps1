#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Show-ADTInstallationPrompt
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
    Show-ADTInstallationPrompt -Message 'Do you want to proceed with the installation?' -ButtonRightText 'Yes' -ButtonLeftText 'No'

    .EXAMPLE
    Show-ADTInstallationPrompt -Title 'Funny Prompt' -Message 'How are you feeling today?' -ButtonRightText 'Good' -ButtonLeftText 'Bad' -ButtonMiddleText 'Indifferent'

    .EXAMPLE
    Show-ADTInstallationPrompt -Message 'You can customize text to appear at the end of an install, or remove it completely for unattended installations.' -Icon Information -NoWait

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title = $Script:ADT.CurrentSession.GetPropertyValue('InstallTitle'),

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
        [ValidateScript({if ($_ -gt $Script:ADT.Config.UI.DefaultTimeout) {throw [System.ArgumentException]::new("The installation UI dialog timeout cannot be longer than the timeout specified in the configuration file.")}; !!$_})]
        [System.UInt32]$Timeout = $Script:ADT.Config.UI.DefaultTimeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoExitOnTimeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost
    )

    begin {
        Write-DebugHeader
    }

    process {
        # Bypass if in non-interactive mode
        if ($Script:ADT.CurrentSession.DeployModeSilent)
        {
            Write-ADTLogEntry -Message "Bypassing Show-ADTInstallationPrompt [Mode: $($Script:ADT.CurrentSession.GetPropertyValue('deployMode'))]. Message:$Message"
            return
        }

        # If the NoWait parameter is specified, launch a new PowerShell session to show the prompt asynchronously.
        if ($NoWait)
        {
            # Remove the NoWait parameter so that the script is run synchronously in the new PowerShell session. This also prevents the function to loop indefinitely.
            Export-ADTModuleState
            Start-Process -FilePath $Script:ADT.Environment.envPSProcessPath -ArgumentList "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command Import-Module -Name '$([System.IO.Path]::GetDirectoryName($Script:MyInvocation.MyCommand.Path))'; Import-ADTModuleState; [System.Void]($($MyInvocation.MyCommand) $(($PSBoundParameters | Resolve-ADTBoundParameters -Exclude NoWait).Replace('"', '\"')))" -WindowStyle Hidden -ErrorAction Ignore
            return
        }

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
        $installPromptTimerPersist.Interval = $Script:ADT.Config.UI.DefaultPromptPersistInterval * 1000
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
        if (!$Script:ADT.CurrentSession.GetPropertyValue('InstallPhase').Equals('Asynchronous'))
        {
            Close-ADTInstallationProgress
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
                [System.Void]$Script:ADT.Environment.ShellApp.MinimizeAll()
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
                [System.Void]$Script:ADT.Environment.ShellApp.UndoMinimizeAll()
                if (!$NoExitOnTimeout)
                {
                    Close-ADTSession -ExitCode $Script:ADT.Config.UI.DefaultExitCode
                }
                else
                {
                    Write-ADTLogEntry -Message 'UI timed out but $NoExitOnTimeout specified. Continue...'
                }
            }
        }
    }

    end {
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Show-ADTDialogBox
{
    <#

    .SYNOPSIS
    Display a custom dialog box with optional title, buttons, icon and timeout.

    Show-ADTInstallationPrompt is recommended over this function as it provides more customization and uses consistent branding with the other UI components.

    .DESCRIPTION
    Display a custom dialog box with optional title, buttons, icon and timeout. The default button is "OK", the default Icon is "None", and the default Timeout is None

    .PARAMETER Text
    Text in the message dialog box

    .PARAMETER Title
    Title of the message dialog box

    .PARAMETER Buttons
    Buttons to be included on the dialog box. Options: OK, OKCancel, AbortRetryIgnore, YesNoCancel, YesNo, RetryCancel, CancelTryAgainContinue. Default: OK.

    .PARAMETER DefaultButton
    The Default button that is selected. Options: First, Second, Third. Default: First.

    .PARAMETER Icon
    Icon to display on the dialog box. Options: None, Stop, Question, Exclamation, Information. Default: None

    .PARAMETER Timeout
    Timeout period in seconds before automatically closing the dialog box with the return message "Timeout". Default: UI timeout value set in the config XML file.

    .PARAMETER TopMost
    Specifies whether the message box is a system modal message box and appears in a topmost window. Default: $true.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the text of the button that was clicked.

    .EXAMPLE
    Show-ADTDialogBox -Title 'Installed Complete' -Text 'Installation has completed. Please click OK and restart your computer.' -Icon 'Information'

    .EXAMPLE
    Show-ADTDialogBox -Title 'Installation Notice' -Text 'Installation will take approximately 30 minutes. Do you wish to proceed?' -Buttons 'OKCancel' -DefaultButton 'Second' -Icon 'Exclamation' -Timeout 600 -Topmost $false

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $true, Position = 0, HelpMessage = 'Enter a message for the dialog box.')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Text,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Title = $Script:ADT.CurrentSession.GetPropertyValue('InstallTitle'),

        [Parameter(Mandatory = $false)]
        [ValidateSet('OK', 'OKCancel', 'AbortRetryIgnore', 'YesNoCancel', 'YesNo', 'RetryCancel', 'CancelTryAgainContinue')]
        [System.String]$Buttons = 'OK',

        [Parameter(Mandatory = $false)]
        [ValidateSet('First', 'Second', 'Third')]
        [System.String]$DefaultButton = 'First',

        [Parameter(Mandatory = $false)]
        [ValidateSet('Exclamation', 'Information', 'None', 'Stop', 'Question')]
        [System.String]$Icon = 'None',

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$Timeout = $Script:ADT.Config.UI.DefaultTimeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost
    )

    begin {
        $dialogButtons = @{
            OK = 0
            OKCancel = 1
            AbortRetryIgnore = 2
            YesNoCancel = 3
            YesNo = 4
            RetryCancel = 5
            CancelTryAgainContinue = 6
        }
        $dialogIcons = @{
            None = 0
            Stop = 16
            Question = 32
            Exclamation = 48
            Information = 64
        }
        $dialogDefaultButton = @{
            First = 0
            Second = 256
            Third = 512
        }

        Write-DebugHeader
    }

    process {
        # Bypass if in silent mode.
        if ($Script:ADT.CurrentSession.DeployModeSilent)
        {
            Write-ADTLogEntry -Message "Bypassing Show-ADTDialogBox [Mode: $($Script:ADT.CurrentSession.GetPropertyValue('deployMode'))]. Text:$Text"
            return
        }

        Write-ADTLogEntry -Message "Displaying Dialog Box with message: $Text..."
        $result = switch ($Script:ADT.Environment.Shell.Popup($Text, $Timeout, $Title, ($dialogButtons[$Buttons] + $dialogIcons[$Icon] + $dialogDefaultButton[$DefaultButton] + (4096 * !$NotTopMost))))
        {
            1 {'OK'; break}
            2 {'Cancel'; break}
            3 {'Abort'; break}
            4 {'Retry'; break}
            5 {'Ignore'; break}
            6 {'Yes'; break}
            7 {'No'; break}
            10 {'Try Again'; break}
            11 {'Continue'; break}
            -1 {'Timeout'; break}
            default {'Unknown'; break}
        }

        Write-ADTLogEntry -Message "Dialog Box Response: $result"
        return $result
    }

    end {
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Show-ADTInstallationWelcome
{
    <#

    .SYNOPSIS
    Show a welcome dialog prompting the user with information about the installation and actions to be performed before the installation can begin.

    .DESCRIPTION
    The following prompts can be included in the welcome dialog:
        a) Close the specified running applications, or optionally close the applications without showing a prompt (using the -Silent switch).
        b) Defer the installation a certain number of times, for a certain number of days or until a deadline is reached.
        c) Countdown until applications are automatically closed.
        d) Prevent users from launching the specified applications while the installation is in progress.

    .PARAMETER ProcessObjects
    Name of the process to stop (do not include the .exe). Specify multiple processes separated by a comma. Specify custom descriptions like this: @{ProcessName = 'winword'; ProcessDescription = 'Microsoft Office Word'},@{ProcessName = 'excel'; ProcessDescription = 'Microsoft Office Excel'}

    .PARAMETER Silent
    Stop processes without prompting the user.

    .PARAMETER CloseAppsCountdown
    Option to provide a countdown in seconds until the specified applications are automatically closed. This only takes effect if deferral is not allowed or has expired.

    .PARAMETER ForceCloseAppsCountdown
    Option to provide a countdown in seconds until the specified applications are automatically closed regardless of whether deferral is allowed.

    .PARAMETER PromptToSave
    Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button. Option does not work in SYSTEM context unless toolkit launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

    .PARAMETER PersistPrompt
    Specify whether to make the Show-ADTInstallationWelcome prompt persist in the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml. The user will have no option but to respond to the prompt. This only takes effect if deferral is not allowed or has expired.

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

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return objects.

    .EXAMPLE
    # Prompt the user to close Internet Explorer, Word and Excel.
    Show-ADTInstallationWelcome -CloseApps 'iexplore,winword,excel'

    .EXAMPLE
    # Close Word and Excel without prompting the user.
    Show-ADTInstallationWelcome -CloseApps 'winword,excel' -Silent

    .EXAMPLE
    # Close Word and Excel and prevent the user from launching the applications while the installation is in progress.
    Show-ADTInstallationWelcome -CloseApps 'winword,excel' -BlockExecution

    .EXAMPLE
    # Prompt the user to close Word and Excel, with customized descriptions for the applications and automatically close the applications after 10 minutes.
    Show-ADTInstallationWelcome -CloseApps 'winword=Microsoft Office Word,excel=Microsoft Office Excel' -CloseAppsCountdown 600

    .EXAMPLE
    # Prompt the user to close Word, MSAccess and Excel.
    # By using the PersistPrompt switch, the dialog will return to the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml, so the user cannot ignore it by dragging it aside.
    Show-ADTInstallationWelcome -CloseApps 'winword,msaccess,excel' -PersistPrompt

    .EXAMPLE
    # Allow the user to defer the installation until the deadline is reached.
    Show-ADTInstallationWelcome -AllowDefer -DeferDeadline '25/08/2013'

    .EXAMPLE
    # Close Word and Excel and prevent the user from launching the applications while the installation is in progress.
    # Allow the user to defer the installation a maximum of 10 times or until the deadline is reached, whichever happens first.
    # When deferral expires, prompt the user to close the applications and automatically close them after 10 minutes.
    Show-ADTInstallationWelcome -CloseApps 'winword,excel' -BlockExecution -AllowDefer -DeferTimes 10 -DeferDeadline '25/08/2013' -CloseAppsCountdown 600

    .NOTES
    The process descriptions are retrieved from WMI, with a fall back on the process name if no description is available. Alternatively, you can specify the description yourself with a '=' symbol - see examples.

    The dialog box will timeout after the timeout specified in the XML configuration file (default 1 hour and 55 minutes) to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding(DefaultParameterSetName = 'None')]
    param (
        [Parameter(Mandatory = $false, HelpMessage = 'Specify process names and an optional process description, e.g. @{ProcessName = "winword"; ProcessDescription = "Microsoft Word"}')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSObject[]]$ProcessObjects,

        [Parameter(Mandatory = $false, HelpMessage = 'Specify whether to prompt user or force close the applications.')]
        [System.Management.Automation.SwitchParameter]$Silent,

        [Parameter(Mandatory = $false, HelpMessage = 'Specify a countdown to display before automatically closing applications where deferral is not allowed or has expired.')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$CloseAppsCountdown,

        [Parameter(Mandatory = $false, HelpMessage = 'Specify a countdown to display before automatically closing applications whether or not deferral is allowed.')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$ForceCloseAppsCountdown,

        [Parameter(Mandatory = $false, HelpMessage = 'Specify whether to prompt to save working documents when the user chooses to close applications by selecting the "Close Programs" button.')]
        [System.Management.Automation.SwitchParameter]$PromptToSave,

        [Parameter(Mandatory = $false, HelpMessage = ' Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the AppDeployToolkitConfig.xml.')]
        [System.Management.Automation.SwitchParameter]$PersistPrompt,

        [Parameter(Mandatory = $false, HelpMessage = ' Specify whether to block execution of the processes during installation.')]
        [System.Management.Automation.SwitchParameter]$BlockExecution,

        [Parameter(Mandatory = $false, HelpMessage = ' Specify whether to enable the optional defer button on the dialog box.')]
        [System.Management.Automation.SwitchParameter]$AllowDefer,

        [Parameter(Mandatory = $false, HelpMessage = ' Specify whether to enable the optional defer button on the dialog box only if an app needs to be closed.')]
        [System.Management.Automation.SwitchParameter]$AllowDeferCloseApps,

        [Parameter(Mandatory = $false, HelpMessage = 'Specify the number of times the deferral is allowed.')]
        [ValidateNotNullorEmpty()]
        [System.UInt32]$DeferTimes,

        [Parameter(Mandatory = $false, HelpMessage = 'Specify the number of days since first run that the deferral is allowed.')]
        [ValidateNotNullorEmpty()]
        [System.UInt32]$DeferDays,

        [Parameter(Mandatory = $false, HelpMessage = 'Specify the deadline (in format dd/mm/yyyy) for which deferral will expire as an option.')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DeferDeadline,

        [Parameter(Mandatory = $true, HelpMessage = 'Specify whether to check if there is enough disk space for the installation to proceed. If this parameter is specified without the RequiredDiskSpace parameter, the required disk space is calculated automatically based on the size of the script source and associated files.', ParameterSetName = 'CheckDiskSpace')]
        [System.Management.Automation.SwitchParameter]$CheckDiskSpace,

        [Parameter(Mandatory = $false, HelpMessage = 'Specify required disk space in MB, used in combination with $CheckDiskSpace.', ParameterSetName = 'CheckDiskSpace')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$RequiredDiskSpace,

        [Parameter(Mandatory = $false, HelpMessage = 'Specify whether to minimize other windows when displaying prompt.')]
        [System.Management.Automation.SwitchParameter]$NoMinimizeWindows,

        [Parameter(Mandatory = $false, HelpMessage = 'Specifies whether the window is the topmost window.')]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false, HelpMessage = 'Specify a countdown to display before automatically proceeding with the installation when a deferral is enabled.')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$ForceCountdown,

        [Parameter(Mandatory = $false, HelpMessage = 'Specify whether to display a custom message specified in the XML file. Custom message must be populated for each language section in the XML.')]
        [System.Management.Automation.SwitchParameter]$CustomText
    )

    begin {
        Write-DebugHeader
    }

    process {
        # If running in NonInteractive mode, force the processes to close silently.
        if ($Script:ADT.CurrentSession.DeployModeNonInteractive)
        {
            $Silent = $true
        }

        # If using Zero-Config MSI Deployment, append any executables found in the MSI to the CloseApps list
        if ($Script:ADT.CurrentSession.GetPropertyValue('UseDefaultMsi'))
        {
            $ProcessObjects = $($ProcessObjects; $Script:ADT.CurrentSession.DefaultMsiExecutablesList)
        }

        # Check disk space requirements if specified
        if ($CheckDiskSpace)
        {
            Write-ADTLogEntry -Message 'Evaluating disk space requirements.'
            if (!$RequiredDiskSpace)
            {
                try
                {
                    # Determine the size of the Files folder
                    $fso = New-Object -ComObject Scripting.FileSystemObject
                    $RequiredDiskSpace = [System.Math]::Round($fso.GetFolder($Script:ADT.CurrentSession.GetPropertyValue('ScriptParentPath')).Size / 1MB)
                }
                catch
                {
                    Write-ADTLogEntry -Message "Failed to calculate disk space requirement from source files.`n$(Resolve-Error)" -Severity 3
                }
                finally
                {
                    try
                    {
                        [System.Void][System.Runtime.Interopservices.Marshal]::ReleaseComObject($fso)
                    }
                    catch
                    {
                        [System.Void]$null
                    }
                }
            }
            if (($freeDiskSpace = Get-ADTFreeDiskSpace) -lt $RequiredDiskSpace)
            {
                Write-ADTLogEntry -Message "Failed to meet minimum disk space requirement. Space Required [$RequiredDiskSpace MB], Space Available [$freeDiskSpace MB]." -Severity 3
                if (!$Silent)
                {
                    Show-ADTInstallationPrompt -Message ($Script:ADT.Strings.DiskSpace.Message -f $Script:ADT.CurrentSession.GetPropertyValue('installTitle'), $RequiredDiskSpace, $freeDiskSpace) -ButtonRightText OK -Icon Error
                }
                Close-ADTSession -ExitCode $Script:ADT.Config.UI.DefaultExitCode
            }
            Write-ADTLogEntry -Message 'Successfully passed minimum disk space requirement check.'
        }

        # Check Deferral history and calculate remaining deferrals.
        [String]$deferDeadlineUniversal = $null
        if ($AllowDefer -or $AllowDeferCloseApps)
        {
            # Set $AllowDefer to true if $AllowDeferCloseApps is true.
            $AllowDefer = $true

            # Get the deferral history from the registry.
            $deferHistory = Get-DeferHistory
            $deferHistoryTimes = $deferHistory | Select-Object -ExpandProperty DeferTimesRemaining -ErrorAction Ignore
            $deferHistoryDeadline = $deferHistory | Select-Object -ExpandProperty DeferDeadline -ErrorAction Ignore

            # Reset switches.
            $checkDeferDays = $DeferDays -ne 0
            $checkDeferDeadline = !!$DeferDeadline

            if ($DeferTimes -ne 0)
            {
                $DeferTimes = if ($deferHistoryTimes -ge 0)
                {
                    Write-ADTLogEntry -Message "Defer history shows [$($deferHistory.DeferTimesRemaining)] deferrals remaining."
                    $deferHistory.DeferTimesRemaining - 1
                }
                else
                {
                    $DeferTimes - 1
                }
                Write-ADTLogEntry -Message "The user has [$DeferTimes] deferrals remaining."

                if ($DeferTimes -lt 0)
                {
                    Write-ADTLogEntry -Message 'Deferral has expired.'
                    $DeferTimes = 0
                    $AllowDefer = $false
                }
            }

            if ($checkDeferDays -and $AllowDefer)
            {
                [String]$deferDeadlineUniversal = if ($deferHistoryDeadline)
                {
                    Write-ADTLogEntry -Message "Defer history shows a deadline date of [$deferHistoryDeadline]."
                    Get-UniversalDate -DateTime $deferHistoryDeadline
                }
                else
                {
                    Get-UniversalDate -DateTime (Get-Date -Date ([System.DateTime]::Now.AddDays($DeferDays)) -Format $Script:ADT.Environment.culture.DateTimeFormat.UniversalDateTimePattern).ToString()
                }
                Write-ADTLogEntry -Message "The user has until [$deferDeadlineUniversal] before deferral expires."

                if ((Get-UniversalDate) -gt $deferDeadlineUniversal)
                {
                    Write-ADTLogEntry -Message 'Deferral has expired.'
                    $AllowDefer = $false
                }
            }

            if ($checkDeferDeadline -and $AllowDefer)
            {
                # Validate date.
                try
                {
                    [String]$deferDeadlineUniversal = Get-UniversalDate -DateTime $DeferDeadline
                }
                catch
                {
                    Write-ADTLogEntry -Message "Date is not in the correct format for the current culture. Type the date in the current locale format, such as 20/08/2014 (Europe) or 08/20/2014 (United States). If the script is intended for multiple cultures, specify the date in the universal sortable date/time format, e.g. '2013-08-22 11:51:52Z'.`n$(Resolve-Error)" -Severity 3
                    throw
                }
                Write-ADTLogEntry -Message "The user has until [$deferDeadlineUniversal] remaining."

                if ((Get-UniversalDate) -gt $deferDeadlineUniversal)
                {
                    Write-ADTLogEntry -Message 'Deferral has expired.'
                    $AllowDefer = $false
                }
            }
        }

        if (($deferTimes -lt 0) -and !$deferDeadlineUniversal)
        {
            $AllowDefer = $false
        }

        # Prompt the user to close running applications and optionally defer if enabled.
        if (!$Script:ADT.CurrentSession.DeployModeSilent -and !$Silent)
        {
            # Keep the same variable for countdown to simplify the code.
            if ($ForceCloseAppsCountdown -gt 0)
            {
                $CloseAppsCountdown = $ForceCloseAppsCountdown
            }
            elseif ($ForceCountdown -gt 0)
            {
                $CloseAppsCountdown = $ForceCountdown
            }
            $Script:ADT.CurrentSession.State.CloseAppsCountdownGlobal = $CloseAppsCountdown
            $promptResult = $null

            while (($runningProcesses = $processObjects | Get-ADTRunningProcesses) -or (($promptResult -ne 'Defer') -and ($promptResult -ne 'Close')))
            {
                # Get all unique running process descriptions.
                $runningProcessDescriptions = $runningProcesses | Select-Object -ExpandProperty ProcessDescription | Sort-Object -Unique

                # Define parameters for welcome prompt.
                $promptParams = @{
                    ForceCloseAppsCountdown = !!$ForceCloseAppsCountdown
                    ForceCountdown = $ForceCountdown
                    PersistPrompt = $PersistPrompt
                    NoMinimizeWindows =$NoMinimizeWindows
                    CustomText = $CustomText
                    NotTopMost = $NotTopMost
                }

                # Check if we need to prompt the user to defer, to defer and close apps, or not to prompt them at all
                if ($AllowDefer)
                {
                    # If there is deferral and closing apps is allowed but there are no apps to be closed, break the while loop.
                    if ($AllowDeferCloseApps -and !$runningProcessDescriptions)
                    {
                        break
                    }
                    elseif (($promptResult -ne 'Close') -or ($runningProcessDescriptions -and ($promptResult -ne 'Continue')))
                    {
                        # Otherwise, as long as the user has not selected to close the apps or the processes are still running and the user has not selected to continue, prompt user to close running processes with deferral.
                        $deferParams = @{AllowDefer = $true; DeferTimes = $deferTimes}
                        if ($deferDeadlineUniversal) {$deferParams.Add('DeferDeadline', $deferDeadlineUniversal)}
                        [String]$promptResult = Show-ADTWelcomePrompt @promptParams @deferParams
                    }
                }
                elseif (($runningProcessDescriptions) -or !!$forceCountdown)
                {
                    # If there is no deferral and processes are running, prompt the user to close running processes with no deferral option.
                    [String]$promptResult = Show-ADTWelcomePrompt @promptParams
                }
                else
                {
                    # If there is no deferral and no processes running, break the while loop.
                    break
                }

                # Process the form results.
                if ($promptResult -eq 'Continue')
                {
                    # If the user has clicked OK, wait a few seconds for the process to terminate before evaluating the running processes again.
                    Write-ADTLogEntry -Message 'The user selected to continue...'
                    if (!$runningProcesses)
                    {
                        # Break the while loop if there are no processes to close and the user has clicked OK to continue.
                        break
                    }
                    [System.Threading.Thread]::Sleep(2000)
                }
                elseif ($promptResult -eq 'Close')
                {
                    # Force the applications to close.
                    Write-ADTLogEntry -Message 'The user selected to force the application(s) to close...'
                    if ($PromptToSave -and $Script:ADT.Environment.SessionZero -and !$Script:ADT.Environment.IsProcessUserInteractive)
                    {
                        Write-ADTLogEntry -Message 'Specified [-PromptToSave] option will not be available, because current process is running in session zero and is not interactive.' -Severity 2
                    }

                    # Update the process list right before closing, in case it changed.
                    $AllOpenWindows = Get-ADTWindowTitle -GetAllWindowTitles -DisableFunctionLogging
                    $PromptToSaveTimeout = New-TimeSpan -Seconds $Script:ADT.Config.UI.PromptToSaveTimeout
                    $PromptToSaveStopWatch = [System.Diagnostics.StopWatch]::new()
                    foreach ($runningProcess in ($runningProcesses = $processObjects | Get-ADTRunningProcesses))
                    {
                        # If the PromptToSave parameter was specified and the process has a window open, then prompt the user to save work if there is work to be saved when closing window.
                        $AllOpenWindowsForRunningProcess = $AllOpenWindows | Where-Object {$_.ParentProcess -eq $runningProcess.ProcessName}
                        if ($PromptToSave -and !($Script:ADT.Environment.SessionZero -and !$Script:ADT.Environment.IsProcessUserInteractive) -and $AllOpenWindowsForRunningProcess -and ($runningProcess.MainWindowHandle -ne [IntPtr]::Zero))
                        {
                            foreach ($OpenWindow in $AllOpenWindowsForRunningProcess)
                            {
                                try
                                {
                                    Write-ADTLogEntry -Message "Stopping process [$($runningProcess.ProcessName)] with window title [$($OpenWindow.WindowTitle)] and prompt to save if there is work to be saved (timeout in [$($Script:ADT.Config.UI.PromptToSaveTimeout)] seconds)..."
                                    [System.Void][PSADT.UiAutomation]::BringWindowToFront($OpenWindow.WindowHandle)
                                    if (!$runningProcess.CloseMainWindow())
                                    {
                                        Write-ADTLogEntry -Message "Failed to call the CloseMainWindow() method on process [$($runningProcess.ProcessName)] with window title [$($OpenWindow.WindowTitle)] because the main window may be disabled due to a modal dialog being shown." -Severity 3
                                    }
                                    else
                                    {
                                        $PromptToSaveStopWatch.Reset()
                                        $PromptToSaveStopWatch.Start()
                                        do
                                        {
                                            if (!($IsWindowOpen = $AllOpenWindows | Where-Object {$_.WindowHandle -eq $OpenWindow.WindowHandle}))
                                            {
                                                Break
                                            }
                                            [System.Threading.Thread]::Sleep(3000)
                                        }
                                        while (($IsWindowOpen) -and ($PromptToSaveStopWatch.Elapsed -lt $PromptToSaveTimeout))

                                        if ($IsWindowOpen)
                                        {
                                            Write-ADTLogEntry -Message "Exceeded the [$($Script:ADT.Config.UI.PromptToSaveTimeout)] seconds timeout value for the user to save work associated with process [$($runningProcess.ProcessName)] with window title [$($OpenWindow.WindowTitle)]." -Severity 2
                                        }
                                        else
                                        {
                                            Write-ADTLogEntry -Message "Window [$($OpenWindow.WindowTitle)] for process [$($runningProcess.ProcessName)] was successfully closed."
                                        }
                                    }
                                }
                                catch
                                {
                                    Write-ADTLogEntry -Message "Failed to close window [$($OpenWindow.WindowTitle)] for process [$($runningProcess.ProcessName)]. `r`n$(Resolve-Error)" -Severity 3
                                }
                                finally
                                {
                                    $runningProcess.Refresh()
                                }
                            }
                        }
                        else
                        {
                            Write-ADTLogEntry -Message "Stopping process $($runningProcess.ProcessName)..."
                            Stop-Process -Name $runningProcess.ProcessName -Force -ErrorAction Ignore
                        }
                    }

                    if ($runningProcesses = $processObjects | Get-ADTRunningProcesses -DisableLogging)
                    {
                        # Apps are still running, give them 2s to close. If they are still running, the Welcome Window will be displayed again.
                        Write-ADTLogEntry -Message 'Sleeping for 2 seconds because the processes are still not closed...'
                        [System.Threading.Thread]::Sleep(2000)
                    }
                }
                elseif ($promptResult -eq 'Timeout')
                {
                    # Stop the script (if not actioned before the timeout value).
                    Write-ADTLogEntry -Message 'Installation not actioned before the timeout value.'
                    $BlockExecution = $false
                    if (($deferTimes -ge 0) -or $deferDeadlineUniversal)
                    {
                        Set-DeferHistory -DeferTimesRemaining $DeferTimes -DeferDeadline $deferDeadlineUniversal
                    }

                    # Dispose the welcome prompt timer here because if we dispose it within the Show-ADTWelcomePrompt function we risk resetting the timer and missing the specified timeout period.
                    if ($Script:ADT.CurrentSession.State.WelcomeTimer)
                    {
                        try
                        {
                            $Script:ADT.CurrentSession.State.WelcomeTimer.Dispose()
                            $Script:ADT.CurrentSession.State.WelcomeTimer = $null
                        }
                        catch
                        {
                            [System.Void]$null
                        }
                    }

                    # Restore minimized windows.
                    [System.Void]$Script:ADT.Environment.ShellApp.UndoMinimizeAll()
                    Close-ADTSession -ExitCode $Script:ADT.Config.UI.DefaultExitCode
                }
                elseif ($promptResult -eq 'Defer')
                {
                    #  Stop the script (user chose to defer)
                    Write-ADTLogEntry -Message 'Installation deferred by the user.'
                    $BlockExecution = $false
                    Set-DeferHistory -DeferTimesRemaining $DeferTimes -DeferDeadline $deferDeadlineUniversal

                    # Restore minimized windows.
                    [System.Void]$Script:ADT.Environment.ShellApp.UndoMinimizeAll()
                    Close-ADTSession -ExitCode $Script:ADT.Config.UI.DefaultExitCode
                }
            }
        }

        # Force the processes to close silently, without prompting the user.
        if (($Silent -or $Script:ADT.CurrentSession.DeployModeSilent) -and ($runningProcesses = $ProcessObjects | Get-ADTRunningProcesses))
        {
            Write-ADTLogEntry -Message "Force closing application(s) [$(($runningProcesses.ProcessDescription | Sort-Object -Unique) -join ',')] without prompting user."
            $runningProcesses.ProcessName | Stop-Process -Force -ErrorAction Ignore
            [System.Threading.Thread]::Sleep(2000)
        }

        # If block execution switch is true, call the function to block execution of these processes.
        if ($BlockExecution)
        {
            # Make this variable globally available so we can check whether we need to call Unblock-AppExecution
            $Script:ADT.CurrentSession.State.BlockExecution = $BlockExecution
            Write-ADTLogEntry -Message '[-BlockExecution] parameter specified.'
            Block-AppExecution -ProcessName ($ProcessObjects | Select-Object -ExpandProperty ProcessName)
        }
    }

    end {
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

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

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the version of the specified file.

.EXAMPLE

Show-InstallationRestartPrompt -Countdownseconds 600 -CountdownNoHideSeconds 60

.EXAMPLE

Show-InstallationRestartPrompt -NoCountdown

.EXAMPLE

Show-InstallationRestartPrompt -Countdownseconds 300 -NoSilentRestart $false -SilentCountdownSeconds 10

.NOTES

Be mindful of the countdown you specify for the reboot as code directly after this function might NOT be able to execute - that includes logging.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$CountdownSeconds = 60,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$CountdownNoHideSeconds = 30,
        [Parameter(Mandatory = $false)]
        [Boolean]$NoSilentRestart = $true,
        [Parameter(Mandatory = $false)]
        [Switch]$NoCountdown = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Int32]$SilentCountdownSeconds = 5,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$TopMost = $true
    )

    Begin {
        Write-DebugHeader
    }
    Process {
        ## If in non-interactive mode
        If ($Script:ADT.CurrentSession.DeployModeSilent) {
            If ($NoSilentRestart -eq $false) {
                Write-ADTLogEntry -Message "Triggering restart silently, because the deploy mode is set to [$($Script:ADT.CurrentSession.GetPropertyValue('deployMode'))] and [NoSilentRestart] is disabled. Timeout is set to [$SilentCountdownSeconds] seconds."
                Start-Process -FilePath $Script:ADT.Environment.envPSProcessPath -ArgumentList "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command `"& { Start-Sleep -Seconds $SilentCountdownSeconds; Restart-Computer -Force; }`"" -WindowStyle 'Hidden' -ErrorAction 'Ignore'
            }
            Else {
                Write-ADTLogEntry -Message "Skipping restart, because the deploy mode is set to [$($Script:ADT.CurrentSession.GetPropertyValue('deployMode'))] and [NoSilentRestart] is enabled."
            }
            Return
        }
        ## Get the parameters passed to the function for invoking the function asynchronously
        [Hashtable]$installRestartPromptParameters = $PSBoundParameters

        ## Check if we are already displaying a restart prompt
        If (Get-Process | Where-Object { $_.MainWindowTitle -match $Script:ADT.Strings.RestartPrompt.Title }) {
            Write-ADTLogEntry -Message "$($MyInvocation.MyCommand.Name) was invoked, but an existing restart prompt was detected. Cancelling restart prompt." -Severity 2
            Return
        }

        ## If the script has been dot-source invoked by the deploy app script, display the restart prompt asynchronously
        If (!$Script:ADT.CurrentSession.GetPropertyValue('InstallPhase').Equals('Asynchronous')) {
            If ($NoCountdown) {
                Write-ADTLogEntry -Message "Invoking $($MyInvocation.MyCommand.Name) asynchronously with no countdown..."
            }
            Else {
                Write-ADTLogEntry -Message "Invoking $($MyInvocation.MyCommand.Name) asynchronously with a [$countDownSeconds] second countdown..."
            }
            ## Remove Silent reboot parameters from the list that is being forwarded to the main script for asynchronous function execution. This is only for Interactive mode so we dont need silent mode reboot parameters.
            $installRestartPromptParameters.Remove('NoSilentRestart')
            $installRestartPromptParameters.Remove('SilentCountdownSeconds')
            ## Prepare a list of parameters of this function as a string
            [String]$installRestartPromptParameters = $installRestartPromptParameters | Resolve-ADTBoundParameters
            ## Start another powershell instance silently with function parameters from this function
            Export-ADTModuleState
            Start-Process -FilePath $Script:ADT.Environment.envPSProcessPath -ArgumentList "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command Import-Module -Name '$([System.IO.Path]::GetDirectoryName($Script:MyInvocation.MyCommand.Path))'; Import-ADTModuleState; [System.Void]($($MyInvocation.MyCommand) $installRestartPromptParameters)" -WindowStyle 'Hidden' -ErrorAction 'Ignore'
            Return
        }

        [DateTime]$startTime = Get-Date
        [DateTime]$countdownTime = $startTime

        $formRestart = New-Object -TypeName 'System.Windows.Forms.Form'
        $formRestart.SuspendLayout()
        $labelCountdown = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelTimeRemaining = New-Object -TypeName 'System.Windows.Forms.Label'
        $labelMessage = New-Object -TypeName 'System.Windows.Forms.Label'
        $buttonRestartLater = New-Object -TypeName 'System.Windows.Forms.Button'
        $pictureBanner = New-Object -TypeName 'System.Windows.Forms.PictureBox'
        $buttonRestartNow = New-Object -TypeName 'System.Windows.Forms.Button'
        $timerCountdown = New-Object -TypeName 'System.Windows.Forms.Timer'
        $flowLayoutPanel = New-Object -TypeName 'System.Windows.Forms.FlowLayoutPanel'
        $panelButtons = New-Object -TypeName 'System.Windows.Forms.Panel'

        [ScriptBlock]$RestartComputer = {
            Write-ADTLogEntry -Message 'Forcefully restarting the computer...'
            Restart-Computer -Force
        }

        [ScriptBlock]$Restart_Form_StateCorrection_Load = {
            # Disable the X button
            Try {
                Disable-ADTWindowCloseButton -WindowHandle $formRestart.Handle
            }
            Catch {
                # Not a terminating error if we can't disable the button. Just disable the Control Box instead
                Write-ADTLogEntry 'Failed to disable the Close button. Disabling the Control Box instead.' -Severity 2
                $formRestart.ControlBox = $false
            }
            ## Initialize the countdown timer
            [DateTime]$currentTime = Get-Date
            [DateTime]$countdownTime = $startTime.AddSeconds($countdownSeconds)
            $timerCountdown.Start()
            ## Set up the form
            [Timespan]$remainingTime = $countdownTime.Subtract($currentTime)
            $labelCountdown.Text = [String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
            If ($remainingTime.TotalSeconds -le $countdownNoHideSeconds) {
                $buttonRestartLater.Enabled = $false
            }
            ## Get the start position of the form so we can return the form to this position if PersistPrompt is enabled
            $Script:ADT.CurrentSession.State.FormInstallationRestartPromptStartPosition = $formRestart.Location
        }

        ## Persistence Timer
        If ($NoCountdown) {
            $restartTimerPersist = New-Object -TypeName 'System.Windows.Forms.Timer'
            $restartTimerPersist.Interval = ($Script:ADT.Config.UI.RestartPromptPersistInterval * 1000)
            [ScriptBlock]$restartTimerPersist_Tick = {
                #  Show the Restart Popup
                $formRestart.WindowState = 'Normal'
                $formRestart.TopMost = $TopMost
                $formRestart.BringToFront()
                $formRestart.Location = $Script:ADT.CurrentSession.State.FormInstallationRestartPromptStartPosition
            }
            $restartTimerPersist.add_Tick($restartTimerPersist_Tick)
            $restartTimerPersist.Start()
        }

        [ScriptBlock]$buttonRestartLater_Click = {
            ## Minimize the form
            $formRestart.WindowState = 'Minimized'
            If ($NoCountdown) {
                ## Reset the persistence timer
                $restartTimerPersist.Stop()
                $restartTimerPersist.Start()
            }
        }

        ## Restart the computer
        [ScriptBlock]$buttonRestartNow_Click = { & $RestartComputer }

        ## Hide the form if minimized
        [ScriptBlock]$formRestart_Resize = { If ($formRestart.WindowState -eq 'Minimized') {
                $formRestart.WindowState = 'Minimized'
            } }

        [ScriptBlock]$timerCountdown_Tick = {
            ## Get the time information
            [DateTime]$currentTime = Get-Date
            [DateTime]$countdownTime = $startTime.AddSeconds($countdownSeconds)
            [Timespan]$remainingTime = $countdownTime.Subtract($currentTime)
            ## If the countdown is complete, restart the machine
            If ($countdownTime -le $currentTime) {
                $buttonRestartNow.PerformClick()
            }
            Else {
                ## Update the form
                $labelCountdown.Text = [String]::Format('{0}:{1:d2}:{2:d2}', $remainingTime.Days * 24 + $remainingTime.Hours, $remainingTime.Minutes, $remainingTime.Seconds)
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
        [ScriptBlock]$Restart_Form_Cleanup_FormClosed = {
            Try {
                $buttonRestartLater.remove_Click($buttonRestartLater_Click)
                $buttonRestartNow.remove_Click($buttonRestartNow_Click)
                $formRestart.remove_Load($Restart_Form_StateCorrection_Load)
                $formRestart.remove_Resize($formRestart_Resize)
                $timerCountdown.remove_Tick($timerCountdown_Tick)
                $restartTimerPersist.remove_Tick($restartTimerPersist_Tick)
                $formRestart.remove_FormClosed($Restart_Form_Cleanup_FormClosed)
            }
            Catch {
            }
        }

        ## Form
        ##----------------------------------------------
        ## Create zero px padding object
        $paddingNone = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 0, 0, 0)
        ## Create basic control size
        $defaultControlSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList ($Script:FormData.Width, 0)

        ## Generic Button properties
        $buttonSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList (195, 24)

        ## Picture Banner
        $pictureBanner.DataBindings.DefaultDataSourceUpdateMode = 0
        $pictureBanner.Image = $Script:FormData.Assets.Banner
        $System_Drawing_Point = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, 0)
        $pictureBanner.Location = $System_Drawing_Point
        $pictureBanner.Name = 'pictureBanner'
        $System_Drawing_Size = New-Object -TypeName 'System.Drawing.Size' -ArgumentList ($Script:FormData.Width, $Script:FormData.BannerHeight)
        $pictureBanner.ClientSize = $System_Drawing_Size
        $pictureBanner.SizeMode = [System.Windows.Forms.PictureBoxSizeMode]::Zoom
        $pictureBanner.Margin = $paddingNone
        $pictureBanner.TabStop = $false

        ## Label Message
        $labelMessage.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelMessage.Font = $Script:FormData.Font
        $labelMessage.Name = 'labelMessage'
        $labelMessage.ClientSize = $defaultControlSize
        $labelMessage.MinimumSize = $defaultControlSize
        $labelMessage.MaximumSize = $defaultControlSize
        $labelMessage.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 10, 0, 5)
        $labelMessage.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelMessage.Text = "$($Script:ADT.Strings.RestartPrompt.Message) $($Script:ADT.Strings.RestartPrompt.MessageTime)`n`n$($Script:ADT.Strings.RestartPrompt.MessageRestart)"
        If ($NoCountdown) {
            $labelMessage.Text = $Script:ADT.Strings.RestartPrompt.Message
        }
        $labelMessage.TextAlign = 'MiddleCenter'
        $labelMessage.Anchor = 'Top'
        $labelMessage.TabStop = $false
        $labelMessage.AutoSize = $true

        ## Label Time remaining message
        $labelTimeRemaining.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelTimeRemaining.Font = New-Object -TypeName 'System.Drawing.Font' -ArgumentList ($Script:FormData.Font.Name, ($Script:FormData.Font.Size + 3), [System.Drawing.FontStyle]::Bold)
        $labelTimeRemaining.Name = 'labelTimeRemaining'
        $labelTimeRemaining.ClientSize = $defaultControlSize
        $labelTimeRemaining.MinimumSize = $defaultControlSize
        $labelTimeRemaining.MaximumSize = $defaultControlSize
        $labelTimeRemaining.Margin = $paddingNone
        $labelTimeRemaining.Padding = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (10, 0, 10, 0)
        $labelTimeRemaining.TabStop = $false
        $labelTimeRemaining.Text = $Script:ADT.Strings.RestartPrompt.TimeRemaining
        $labelTimeRemaining.TextAlign = 'MiddleCenter'
        $labelTimeRemaining.Anchor = 'Top'
        $labelTimeRemaining.AutoSize = $true

        ## Label Countdown
        $labelCountdown.DataBindings.DefaultDataSourceUpdateMode = 0
        $labelCountdown.Font = New-Object -TypeName 'System.Drawing.Font' -ArgumentList ($Script:FormData.Font.Name, ($Script:FormData.Font.Size + 9), [System.Drawing.FontStyle]::Bold)
        $labelCountdown.Name = 'labelCountdown'
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
        $System_Drawing_Point = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (0, $Script:FormData.BannerHeight)
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
        $flowLayoutPanel.Controls.Add($labelMessage)
        If (-not $NoCountdown) {
            $flowLayoutPanel.Controls.Add($labelTimeRemaining)
            $flowLayoutPanel.Controls.Add($labelCountdown)
        }

        ## Button Minimize
        $buttonRestartLater.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonRestartLater.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (240, 4)
        $buttonRestartLater.Name = 'buttonRestartLater'
        $buttonRestartLater.Font = New-Object -TypeName 'System.Drawing.Font' -ArgumentList ($Script:FormData.Font.Name, ($Script:FormData.Font.Size - 0.5), [System.Drawing.FontStyle]::Regular)
        $buttonRestartLater.ClientSize = $buttonSize
        $buttonRestartLater.MinimumSize = $buttonSize
        $buttonRestartLater.MaximumSize = $buttonSize
        $buttonRestartLater.TabIndex = 0
        $buttonRestartLater.Text = $Script:ADT.Strings.RestartPrompt.ButtonRestartLater
        $buttonRestartLater.AutoSize = $true
        $buttonRestartLater.Margin = $paddingNone
        $buttonRestartLater.Padding = $paddingNone
        $buttonRestartLater.UseVisualStyleBackColor = $true
        $buttonRestartLater.add_Click($buttonRestartLater_Click)

        ## Button Restart Now
        $buttonRestartNow.DataBindings.DefaultDataSourceUpdateMode = 0
        $buttonRestartNow.Location = New-Object -TypeName 'System.Drawing.Point' -ArgumentList (14, 4)
        $buttonRestartNow.Name = 'buttonRestartNow'
        $buttonRestartNow.Font = New-Object -TypeName 'System.Drawing.Font' -ArgumentList ($Script:FormData.Font.Name, ($Script:FormData.Font.Size - 0.5), [System.Drawing.FontStyle]::Regular)
        $buttonRestartNow.ClientSize = $buttonSize
        $buttonRestartNow.MinimumSize = $buttonSize
        $buttonRestartNow.MaximumSize = $buttonSize
        $buttonRestartNow.TabIndex = 1
        $buttonRestartNow.Text = $Script:ADT.Strings.RestartPrompt.ButtonRestartNow
        $buttonRestartNow.Margin = $paddingNone
        $buttonRestartNow.Padding = $paddingNone
        $buttonRestartNow.UseVisualStyleBackColor = $true
        $buttonRestartNow.add_Click($buttonRestartNow_Click)

        ## Form Restart
        $formRestart.ClientSize = $defaultControlSize
        $formRestart.Padding = $paddingNone
        $formRestart.Margin = $paddingNone
        $formRestart.DataBindings.DefaultDataSourceUpdateMode = 0
        $formRestart.Name = 'formRestart'
        $formRestart.Text = $Script:ADT.CurrentSession.GetPropertyValue('installTitle')
        $formRestart.StartPosition = 'CenterScreen'
        $formRestart.FormBorderStyle = 'Fixed3D'
        $formRestart.MaximizeBox = $false
        $formRestart.MinimizeBox = $false
        $formRestart.TopMost = $TopMost
        $formRestart.TopLevel = $true
        $formRestart.Icon = $Script:FormData.Assets.Icon
        $formRestart.AutoSize = $true
        $formRestart.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Dpi
        $formRestart.AutoScaleDimensions = New-Object System.Drawing.SizeF(96,96)
        $formRestart.ControlBox = $true
        $formRestart.Controls.Add($pictureBanner)

        ## Button Panel
        $panelButtons.MinimumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList ($Script:FormData.Width, 39)
        $panelButtons.ClientSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList ($Script:FormData.Width, 39)
        $panelButtons.MaximumSize = New-Object -TypeName 'System.Drawing.Size' -ArgumentList ($Script:FormData.Width, 39)
        $panelButtons.AutoSize = $true
        $panelButtons.Padding = $paddingNone
        $panelButtons.Margin = New-Object -TypeName 'System.Windows.Forms.Padding' -ArgumentList (0, 10, 0, 0)
        $panelButtons.Controls.Add($buttonRestartNow)
        $panelButtons.Controls.Add($buttonRestartLater)
        ## Add the Buttons Panel to the flowPanel
        $flowLayoutPanel.Controls.Add($panelButtons)
        ## Add FlowPanel to the form
        $formRestart.Controls.Add($flowLayoutPanel)
        $formRestart.add_Resize($formRestart_Resize)
        ## Timer Countdown
        If (-not $NoCountdown) {
            $timerCountdown.add_Tick($timerCountdown_Tick)
        }
        ##----------------------------------------------
        # Init the OnLoad event to correct the initial state of the form
        $formRestart.add_Load($Restart_Form_StateCorrection_Load)
        # Clean up the control events
        $formRestart.add_FormClosed($Restart_Form_Cleanup_FormClosed)
        $formRestartClosing = [Windows.Forms.FormClosingEventHandler] { If ($_.CloseReason -eq 'UserClosing') {
                $_.Cancel = $true
            } }
        $formRestart.add_FormClosing($formRestartClosing)

        If ($NoCountdown) {
            Write-ADTLogEntry -Message 'Displaying restart prompt with no countdown.'
        }
        Else {
            Write-ADTLogEntry -Message "Displaying restart prompt with a [$countDownSeconds] second countdown."
        }

        #  Show the Form
        $formRestart.ResumeLayout()
        Write-Output -InputObject ($formRestart.ShowDialog())
        $formRestart.Dispose()
    }
    End {
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Show-BalloonTip {
    <#
.SYNOPSIS

Displays a balloon tip notification in the system tray.

.DESCRIPTION

Displays a balloon tip notification in the system tray.

.PARAMETER BalloonTipText

Text of the balloon tip.

.PARAMETER BalloonTipTitle

Title of the balloon tip.

.PARAMETER BalloonTipIcon

Icon to be used. Options: 'Error', 'Info', 'None', 'Warning'. Default is: Info.

.PARAMETER BalloonTipTime

Time in milliseconds to display the balloon tip. Default: 10000.

.PARAMETER NoWait

Create the balloontip asynchronously. Default: $false

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the version of the specified file.

.EXAMPLE

Show-BalloonTip -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'

.EXAMPLE

Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name' -BalloonTipTime 1000

.NOTES

For Windows 10 OS and above a Toast notification is displayed in place of a balloon tip if toast notifications are enabled in the XML config file.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [String]$BalloonTipText,
        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String]$BalloonTipTitle = $Script:ADT.CurrentSession.GetPropertyValue('InstallTitle'),
        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateSet('Error', 'Info', 'None', 'Warning')]
        [Windows.Forms.ToolTipIcon]$BalloonTipIcon = 'Info',
        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullorEmpty()]
        [Int32]$BalloonTipTime = 10000,
        [Parameter(Mandatory = $false, Position = 4)]
        [Switch]$NoWait = $false
    )

    Begin {
        Write-DebugHeader
    }
    Process {
        ## Skip balloon if in silent mode, disabled in the config or presentation is detected
        If ($Script:ADT.CurrentSession.DeployModeSilent -or !$Script:ADT.Config.UI.BalloonNotifications) {
            Write-ADTLogEntry -Message "Bypassing Show-BalloonTip [Mode:$($Script:ADT.CurrentSession.GetPropertyValue('deployMode')), Config Show Balloon Notifications:$($Script:ADT.Config.UI.BalloonNotifications)]. BalloonTipText:$BalloonTipText"
            Return
        }
        If (Test-PowerPoint) {
            Write-ADTLogEntry -Message "Bypassing Show-BalloonTip [Mode:$($Script:ADT.CurrentSession.GetPropertyValue('deployMode')), Presentation Detected:$true]. BalloonTipText:$BalloonTipText"
            Return
        }
        ## Dispose of previous balloon
        If ($Script:ADT.CurrentSession.State.NotifyIcon) {
            Try {
                $Script:ADT.CurrentSession.State.NotifyIcon.Dispose()
                $Script:ADT.CurrentSession.State.NotifyIcon = $null
            }
            Catch {
            }
        }

        If (($Script:ADT.Environment.envOSVersionMajor -lt 10) -or ($Script:ADT.Config.Toast.Disable -eq $true)) {
            ## NoWait - Create the balloontip icon asynchronously
            If ($NoWait) {
                Write-ADTLogEntry -Message "Displaying balloon tip notification asynchronously with message [$BalloonTipText]."
                ## Create a script block to display the balloon notification in a new PowerShell process so that we can wait to cleanly dispose of the balloon tip without having to make the deployment script wait
                ## Scriptblock text has to be as short as possible because it is passed as a parameter to powershell
                ## Don't strongly type parameter BalloonTipIcon as System.Drawing assembly not loaded yet in asynchronous scriptblock so will throw error
                [ScriptBlock]$notifyIconScriptBlock = {
                    Param(
                        [Parameter(Mandatory = $true, Position = 0)]
                        [ValidateNotNullOrEmpty()]
                        [String]$BalloonTipText,
                        [Parameter(Mandatory = $false, Position = 1)]
                        [ValidateNotNullorEmpty()]
                        [String]$BalloonTipTitle,
                        [Parameter(Mandatory = $false, Position = 2)]
                        [ValidateSet('Error', 'Info', 'None', 'Warning')]
                        $BalloonTipIcon = 'Info',
                        [Parameter(Mandatory = $false, Position = 3)]
                        [ValidateNotNullorEmpty()]
                        [Int32]$BalloonTipTime,
                        [Parameter(Mandatory = $false, Position = 4)]
                        [ValidateNotNullorEmpty()]
                        [String]$AppDeployLogoIcon
                    )
                    Add-Type -AssemblyName 'System.Windows.Forms', 'System.Drawing' -ErrorAction 'Stop'
                    $BalloonTipIconText = [String]::Concat($BalloonTipTitle, ' - ', $BalloonTipText)
                    If ($BalloonTipIconText.Length -gt 63) {
                        $BalloonTipIconText = [String]::Concat($BalloonTipIconText.Substring(0, 60), '...')
                    }
                    [Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
                    $Script:ADT.CurrentSession.State.NotifyIcon = New-Object -TypeName 'System.Windows.Forms.NotifyIcon' -Property @{
                        BalloonTipIcon  = $BalloonTipIcon
                        BalloonTipText  = $BalloonTipText
                        BalloonTipTitle = $BalloonTipTitle
                        Icon            = $Script:FormData.Assets.Icon
                        Text            = $BalloonTipIconText
                        Visible         = $true
                    }

                    $Script:ADT.CurrentSession.State.NotifyIcon.ShowBalloonTip($BalloonTipTime)
                    Start-Sleep -Milliseconds ($BalloonTipTime)
                    $Script:ADT.CurrentSession.State.NotifyIcon.Dispose() }

                ## Invoke a separate PowerShell process passing the script block as a command and associated parameters to display the balloon tip notification asynchronously
                Try {
                    Execute-Process -Path $Script:ADT.Environment.envPSProcessPath -Parameters "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command & {$notifyIconScriptBlock} `'$BalloonTipText`' `'$BalloonTipTitle`' `'$BalloonTipIcon`' `'$BalloonTipTime`' `'$AppDeployLogoIcon`'" -NoWait -WindowStyle 'Hidden' -CreateNoWindow
                }
                Catch {
                }
            }
            ## Otherwise create the balloontip icon synchronously
            Else {
                Write-ADTLogEntry -Message "Displaying balloon tip notification with message [$BalloonTipText]."
                ## Prepare Text - Cut it if longer than 63 chars
                $BalloonTipIconText = [String]::Concat($BalloonTipTitle, ' - ', $BalloonTipText)
                If ($BalloonTipIconText.Length -gt 63) {
                    $BalloonTipIconText = [String]::Concat($BalloonTipIconText.Substring(0, 60), '...')
                }
                ## Create the BalloonTip
                [Windows.Forms.ToolTipIcon]$BalloonTipIcon = $BalloonTipIcon
                $Script:ADT.CurrentSession.State.NotifyIcon = New-Object -TypeName 'System.Windows.Forms.NotifyIcon' -Property @{
                    BalloonTipIcon  = $BalloonTipIcon
                    BalloonTipText  = $BalloonTipText
                    BalloonTipTitle = $BalloonTipTitle
                    Icon            = $Script:FormData.Assets.Icon
                    Text            = $BalloonTipIconText
                    Visible         = $true
                }
                ## Display the balloon tip notification
                $Script:ADT.CurrentSession.State.NotifyIcon.ShowBalloonTip($BalloonTipTime)
            }
        }
        # Otherwise use toast notification
        Else {
            $toastAppID = $Script:ADT.Environment.appDeployToolkitName
            $toastAppDisplayName = $Script:ADT.Config.Toast.AppName

            [scriptblock]$toastScriptBlock  = {
                Param(
                    [Parameter(Mandatory = $true, Position = 0)]
                    [ValidateNotNullOrEmpty()]
                    [String]$BalloonTipText,
                    [Parameter(Mandatory = $false, Position = 1)]
                    [ValidateNotNullorEmpty()]
                    [String]$BalloonTipTitle,
                    [Parameter(Mandatory = $false, Position = 2)]
                    [ValidateNotNullorEmpty()]
                    [String]$AppDeployLogoImage,
                    [Parameter(Mandatory = $false, Position = 3)]
                    [ValidateNotNullorEmpty()]
                    [String]$toastAppID,
                    [Parameter(Mandatory = $false, Position = 4)]
                    [ValidateNotNullorEmpty()]
                    [String]$toastAppDisplayName
                )

                # Check for required entries in registry for when using Powershell as application for the toast
                # Register the AppID in the registry for use with the Action Center, if required
                $regPathToastNotificationSettings = 'Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Notifications\Settings'
                $regPathToastApp = 'Registry::HKEY_CURRENT_USER\Software\Classes\AppUserModelId'

                # Create the registry entries
                $null = New-Item -Path "$regPathToastNotificationSettings\$toastAppId" -Force
                # Make sure the app used with the action center is enabled
                $null = New-ItemProperty -Path "$regPathToastNotificationSettings\$toastAppId" -Name 'ShowInActionCenter' -Value 1 -PropertyType 'DWORD' -Force
                $null = New-ItemProperty -Path "$regPathToastNotificationSettings\$toastAppId" -Name 'Enabled' -Value 1 -PropertyType 'DWORD' -Force
                $null = New-ItemProperty -Path "$regPathToastNotificationSettings\$toastAppId" -Name 'SoundFile' -PropertyType 'STRING' -Force

                # Create the registry entries
                $null = New-Item -Path "$regPathToastApp\$toastAppId" -Force
                $null = New-ItemProperty -Path "$regPathToastApp\$toastAppId" -Name 'DisplayName' -Value "$($toastAppDisplayName)" -PropertyType 'STRING' -Force
                $null = New-ItemProperty -Path "$regPathToastApp\$toastAppId" -Name 'ShowInSettings' -Value 0 -PropertyType 'DWORD' -Force
                $null = New-ItemProperty -Path "$regPathToastApp\$toastAppId" -Name 'IconUri' -Value $appDeployLogoImage -PropertyType 'ExpandString' -Force
                $null = New-ItemProperty -Path "$regPathToastApp\$toastAppId" -Name 'IconBackgroundColor' -Value 0 -PropertyType 'ExpandString' -Force

                # Handle PowerShell 7-specific setup. ## FIXME
                If ($PSVersionTable.PSEdition.Equals('Core')) {
                    Add-Type -AssemblyName (Get-ChildItem -Path "$Script:PSScriptRoot\lib\*\*.dll").FullName
                }
                else {
                    [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                    [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
                }

                ## Gets the Template XML so we can manipulate the values
                $Template = [Windows.UI.Notifications.ToastTemplateType]::ToastImageAndText01
                [xml] $ToastTemplate = ([Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent($Template).GetXml())
                [xml] $ToastTemplate = @"
<toast launch="app-defined-string">
    <visual>
        <binding template="ToastImageAndText02">
            <text id="1">$BalloonTipTitle</text>
            <text id="2">$BalloonTipText</text>
            <image id="1" src="file://$appDeployLogoImage" />
        </binding>
    </visual>
</toast>
"@

                $ToastXml = New-Object -TypeName Windows.Data.Xml.Dom.XmlDocument
                $ToastXml.LoadXml($ToastTemplate.OuterXml)

                $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($toastAppId)
                $notifier.Show($toastXml)

            }

            If ($Script:ADT.Environment.ProcessNTAccount -eq $Script:ADT.Environment.runAsActiveUser.NTAccount) {
                Write-ADTLogEntry -Message "Displaying toast notification with message [$BalloonTipText]."
                Invoke-Command -ScriptBlock $toastScriptBlock -ArgumentList $BalloonTipText, $BalloonTipTitle, $AppDeployLogoImage, $toastAppID, $toastAppDisplayName
            }
            Else {
                ## Invoke a separate PowerShell process as the current user passing the script block as a command and associated parameters to display the toast notification in the user context
                Try {
                    Write-ADTLogEntry -Message "Displaying toast notification with message [$BalloonTipText] using Execute-ProcessAsUser."
                    $executeToastAsUserScript = "$($Script:ADT.CurrentSession.LoggedOnUserTempPath)" + "$($Script:ADT.Environment.appDeployToolkitName)-ToastNotification.ps1"
                    Set-Content -Path $executeToastAsUserScript -Value $toastScriptBlock -Force
                    Execute-ProcessAsUser -Path $Script:ADT.Environment.envPSProcessPath -Parameters "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -File `"$executeToastAsUserScript`" `"$BalloonTipText`" `"$BalloonTipTitle`" `"$AppDeployLogoImage`" `"$toastAppID`" `"$toastAppDisplayName`"" -TempPath $($Script:ADT.Environment.loggedOnUserTempPath) -Wait -RunLevel 'LeastPrivilege'
                }
                Catch {
                }
            }
        }
    }
    End {
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Show-ADTInstallationProgress
{
    <#

    .SYNOPSIS
    Displays a progress dialog in a separate thread with an updateable custom message.

    .DESCRIPTION
    Create a WPF window in a separate thread to display a marquee style progress ellipse with a custom message that can be updated. The status message supports line breaks.

    The first time this function is called in a script, it will display a balloon tip notification to indicate that the installation has started (provided balloon tips are enabled in the configuration).

    .PARAMETER WindowTitle
    The titke of the window to be displayed. The default is the derived value from $InstallTitle.

    .PARAMETER StatusMessage
    The status message to be displayed. The default status message is taken from the XML configuration file.

    .PARAMETER WindowLocation
    The location of the progress window. Default: center of the screen.

    .PARAMETER NotTopMost
    Specifies whether the progress window shouldn't be topmost. Default: $false.

    .PARAMETER Quiet
    Specifies whether to not log the success of updating the progress message. Default: $false.

    .PARAMETER NoRelocation
    Specifies whether to not reposition the window upon updating the message. Default: $false.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    # Use the default status message from the XML configuration file.
    Show-ADTInstallationProgress

    .EXAMPLE
    Show-ADTInstallationProgress -StatusMessage 'Installation in Progress...'

    .EXAMPLE
    Show-ADTInstallationProgress -StatusMessage "Installation in Progress...`nThe installation may take 20 minutes to complete."

    .EXAMPLE
    Show-ADTInstallationProgress -StatusMessage 'Installation in Progress...' -WindowLocation 'BottomRight' -TopMost $false

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [ValidateNotNullOrEmpty()]
        [System.String]$WindowTitle = $Script:ADT.CurrentSession.GetPropertyValue('InstallTitle'),

        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessage = $Script:ADT.Strings.Progress."Message$($Script:ADT.CurrentSession.GetPropertyValue('DeploymentType'))",

        [ValidateSet('Default', 'TopLeft', 'Top', 'TopRight', 'TopCenter', 'BottomLeft', 'Bottom', 'BottomRight')]
        [System.String]$WindowLocation = 'Default',

        [System.Management.Automation.SwitchParameter]$NotTopMost,
        [System.Management.Automation.SwitchParameter]$Quiet,
        [System.Management.Automation.SwitchParameter]$NoRelocation
    )

    begin {
        function Update-WindowLocation
        {
            param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullOrEmpty()]
                [System.Windows.Window]$Window
            )

            # Calculate the position on the screen where the progress dialog should be placed.
            [System.Double]$screenCenterWidth = [System.Windows.SystemParameters]::WorkArea.Width - $Window.ActualWidth
            [System.Double]$screenCenterHeight = [System.Windows.SystemParameters]::WorkArea.Height - $Window.ActualHeight

            # Set the start position of the Window based on the screen size.
            switch ($WindowLocation)
            {
                'TopLeft' {
                    $Window.Left = 0.
                    $Window.Top = 0.
                    break
                }
                'Top' {
                    $Window.Left = $screenCenterWidth * 0.5
                    $Window.Top = 0.
                    break
                }
                'TopRight' {
                    $Window.Left = $screenCenterWidth
                    $Window.Top = 0.
                    break
                }
                'TopCenter' {
                    $Window.Left = $screenCenterWidth * 0.5
                    $Window.Top = $screenCenterHeight * (1. / 6.)
                    break
                }
                'BottomLeft' {
                    $Window.Left = 0.
                    $Window.Top = $screenCenterHeight
                    break
                }
                'Bottom' {
                    $Window.Left = $screenCenterWidth * 0.5
                    $Window.Top = $screenCenterHeight
                    break
                }
                'BottomRight' {
                    # The -100 offset is needed to not overlap system tray toast notifications.
                    $Window.Left = $screenCenterWidth
                    $Window.Top = $screenCenterHeight - 100
                    break
                }
                default {
                    # Center the progress window by calculating the center of the workable screen based on the width of the screen minus half the width of the progress bar
                    $Window.Left = $screenCenterWidth * 0.5
                    $Window.Top = $screenCenterHeight * 0.5
                    break
                }
            }
        }

        Write-DebugHeader
    }

    process {
        # Return early in silent mode.
        if ($Script:ADT.CurrentSession.DeployModeSilent)
        {
            Write-ADTLogEntry -Message "Bypassing Show-ADTInstallationProgress [Mode: $($Script:ADT.CurrentSession.GetPropertyValue('deployMode'))]. Status message:$StatusMessage" -DebugMessage:$Quiet
            return
        }

        # Check if the progress thread is running before invoking methods on it.
        if (!$Script:ProgressWindow.Count -or !$Script:ProgressWindow.Running)
        {
            # Notify user that the software installation has started.
            Show-BalloonTip -BalloonTipIcon Info -BalloonTipText "$($Script:ADT.CurrentSession.DeploymentTypeName) $($Script:ADT.Strings.BalloonText.Start)"

            # Set up the PowerShell instance and add the initial scriptblock.
            $Script:ProgressWindow.PowerShell = [System.Management.Automation.PowerShell]::Create().AddScript({
                # Set required variables to ensure script functionality.
                $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
                $ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
                Set-PSDebug -Strict
                Set-StrictMode -Version Latest

                # Create XAML window.
                $SyncHash.Add('Window', [System.Windows.Markup.XamlReader]::Parse($XamlConfig))
                $SyncHash.Add('Message', $SyncHash.Window.FindName('ProgressText'))
                $SyncHash.Message.Text = $StatusMessage
                $SyncHash.Window.add_MouseLeftButtonDown({$this.DragMove()})
                $SyncHash.Window.add_Closing({$this.Cancel = $true})
                $SyncHash.Window.add_Loaded({
                    # Relocate the window and disable the X button.
                    & $UpdateWindowLocation.GetNewClosure() -Window $this
                    & $DisableWindowCloseButton.GetNewClosure() -WindowHandle ([System.Windows.Interop.WindowInteropHelper]::new($this).Handle)
                })

                # Bring up the window and capture any errors thereafter.
                [System.Void]$SyncHash.Window.ShowDialog()
                if ($Error.Count) {$SyncHash.Add('Error', $Error)}
            })

            # Commence invocation.
            Write-ADTLogEntry -Message "Creating the progress dialog in a separate thread with message: [$StatusMessage]."
            $Script:ProgressWindow.PowerShell.Runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $Script:ProgressWindow.PowerShell.Runspace.ApartmentState = [System.Threading.ApartmentState]::STA
            $Script:ProgressWindow.PowerShell.Runspace.ThreadOptions = [System.Management.Automation.Runspaces.PSThreadOptions]::ReuseThread
            $Script:ProgressWindow.PowerShell.Runspace.Open()
            $Script:ProgressWindow.PowerShell.Runspace.SessionStateProxy.SetVariable('SyncHash', $Script:ProgressWindow.SyncHash)
            $Script:ProgressWindow.PowerShell.Runspace.SessionStateProxy.SetVariable('XamlConfig', [System.String]::Format($Script:ProgressWindow.Xaml, $WindowTitle, (!$NotTopMost).ToString(), $Script:ADT.Config.Assets.Banner, $Script:ADT.Config.Assets.Icon))
            $Script:ProgressWindow.PowerShell.Runspace.SessionStateProxy.SetVariable('StatusMessage', $StatusMessage)
            $Script:ProgressWindow.PowerShell.Runspace.SessionStateProxy.SetVariable('UpdateWindowLocation', ${Function:Update-WindowLocation})
            $Script:ProgressWindow.PowerShell.Runspace.SessionStateProxy.SetVariable('DisableWindowCloseButton', ${Function:Disable-ADTWindowCloseButton})
            $Script:ProgressWindow.Invocation = $Script:ProgressWindow.PowerShell.BeginInvoke()

            # Allow the thread to be spun up safely before invoking actions against it.
            while (!($Script:ProgressWindow.Running = $Script:ProgressWindow.SyncHash.ContainsKey('Window') -and $Script:ProgressWindow.SyncHash.Window.IsInitialized -and ($Script:ProgressWindow.SyncHash.Window.Dispatcher.Thread.ThreadState -eq 'Running')))
            {
                if ($Script:ProgressWindow.SyncHash.ContainsKey('Error') -and $Script:ProgressWindow.SyncHash.Error.Count)
                {
                    Write-ADTLogEntry -Message "Failure while displaying progress dialog.`n$(Resolve-Error -ErrorRecord $Script:ProgressWindow.SyncHash.Error)" -Severity 3
                    Close-ADTInstallationProgress
                    break
                }
            }
        }
        else
        {
            # Invoke update events against an established window.
            $Script:ProgressWindow.SyncHash.Window.Dispatcher.Invoke(
                {
                    $Script:ProgressWindow.SyncHash.Window.Title = $WindowTitle
                    $Script:ProgressWindow.SyncHash.Message.Text = $StatusMessage
                    if (!$NoRelocation)
                    {
                        Update-WindowLocation -Window $Script:ProgressWindow.SyncHash.Window
                    }
                },
                [System.Windows.Threading.DispatcherPriority]::Send
            )
            Write-ADTLogEntry -Message "Updated the progress message: [$StatusMessage]." -DebugMessage:$Quiet
        }
    }

    end {
        Write-DebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Show-BlockedAppDialog
{
    # Return early if someone happens to call this in a non-async mode.
    if (!$Script:ADT.CurrentSession.GetPropertyValue('InstallPhase').Equals('Asynchronous'))
    {
        return
    }

    # If we're here, we're not to log anything.
    $Script:ADT.CurrentSession.SetPropertyValue('DisableLogging', $true)

    try {
        # Create a mutex and specify a name without acquiring a lock on the mutex.
        $showBlockedAppDialogMutexName = "Global\$($Script:ADT.Environment.appDeployToolkitName)_ShowBlockedAppDialog_Message"
        $showBlockedAppDialogMutex = [System.Threading.Mutex]::new($false, $showBlockedAppDialogMutexName)

        # Attempt to acquire an exclusive lock on the mutex, attempt will fail after 1 millisecond if unable to acquire exclusive lock.
        if ($showBlockedAppDialogMutexLocked = (Test-IsMutexAvailable -MutexName $showBlockedAppDialogMutexName -MutexWaitTimeInMilliseconds 1) -and $showBlockedAppDialogMutex.WaitOne(1))
        {
            Show-ADTInstallationPrompt -Title $Script:ADT.CurrentSession.GetPropertyValue('InstallTitle') -Message $Script:ADT.Strings.BlockExecution.Message -Icon Warning -ButtonRightText OK
        }
        else
        {
            # If attempt to acquire an exclusive lock on the mutex failed, then exit script as another blocked app dialog window is already open.
            Write-ADTLogEntry -Message "Unable to acquire an exclusive lock on mutex [$showBlockedAppDialogMutexName] because another blocked application dialog window is already open. Exiting script..." -Severity 2
        }
        exit 0
    }
    catch
    {
        Write-ADTLogEntry -Message "There was an error in displaying the Installation Prompt.`n$(Resolve-Error)" -Severity 3
        exit 60005
    }
    finally
    {
        if ($showBlockedAppDialogMutexLocked)
        {
            [System.Void]$showBlockedAppDialogMutex.ReleaseMutex()
        }
        if ($showBlockedAppDialogMutex)
        {
            $showBlockedAppDialogMutex.Close()
        }
    }
}

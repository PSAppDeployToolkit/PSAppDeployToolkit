#-----------------------------------------------------------------------------
#
# MARK: Show-ADTWelcomePromptClassic
#
#-----------------------------------------------------------------------------

function Show-ADTWelcomePromptFluent {
    <#

    .SYNOPSIS
    Called by Show-ADTInstallationWelcome to prompt the user to optionally do the following:
        1) Close the specified running applications.
        2) Provide an option to defer the installation.
        3) Show a countdown before applications are automatically closed.

    .DESCRIPTION
    The user is presented with a Windows Forms dialog box to close the applications themselves and continue or to have the script close the applications for them.

    If the -AllowDefer option is set to true, an optional "Defer" button will be shown to the user. If they select this option, the script will exit and return a 1618 code (SCCM fast retry code).

    The dialog box will timeout after the timeout specified in the XML configuration file (default 1 hour and 55 minutes) to prevent SCCM installations from timing out and returning a failure code to SCCM. When the dialog times out, the script will exit and return a 1618 code (SCCM fast retry code).

    .PARAMETER CloseAppsCountdown
    Specify the countdown time in seconds before running applications are automatically closed when deferral is not allowed or expired.

    .PARAMETER ForceCloseAppsCountdown
    Specify whether to show the countdown regardless of whether deferral is allowed.

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
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the user's selection.

    .EXAMPLE
    Show-ADTWelcomePromptClassic -CloseAppsCountdown 600 -AllowDefer -DeferTimes 10

    .NOTES
    This is an internal script function and should typically not be called directly. It is used by the Show-ADTInstallationWelcome prompt to display a custom prompt.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ProcessObjects', Justification = 'This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.')]
    param
    (
        [ValidateNotNullOrEmpty()]
        [PSADT.Types.ProcessObject[]]$ProcessObjects,

        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if ($_ -gt (& $Script:CommandTable.'Get-ADTConfig').UI.DefaultTimeout) {
                    $PSCmdlet.ThrowTerminatingError((& $Script:CommandTable.'New-ADTValidateScriptErrorRecord' -ParameterName CloseAppsCountdown -ProvidedValue $_ -ExceptionMessage 'The close applications countdown time cannot be longer than the timeout specified in the config file.'))
                }
                return ($_ -ge 0)
            })]
        [System.Double]$CloseAppsCountdown,

        [ValidateNotNullOrEmpty()]
        [System.UInt32]$DeferTimes,

        [ValidateNotNullOrEmpty()]
        [System.String]$DeferDeadline,

        [System.Management.Automation.SwitchParameter]$AllowDefer,
        [System.Management.Automation.SwitchParameter]$NoMinimizeWindows,
        [System.Management.Automation.SwitchParameter]$NotTopMost,
        [System.Management.Automation.SwitchParameter]$CustomText,
        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Perform initial setup.
    $adtConfig = & $Script:CommandTable.'Get-ADTConfig'
    $adtStrings = & $Script:CommandTable.'Get-ADTStringTable'
    $adtSession = & $Script:CommandTable.'Get-ADTSession'

    # Initialize variables.
    $countdownTime = $startTime = [System.DateTime]::Now
    $showCountdown = $false
    $showCloseApps = $false
    $showDeference = $false

    # Initial form layout: Close Applications
    if ($adtSession.RunningProcessDescriptions) {
        & $Script:CommandTable.'Write-ADTLogEntry' -Message "Prompting the user to close application(s) [$($adtSession.RunningProcessDescriptions -join ',')]..."
        $showCloseApps = $true
    }

    # Initial form layout: Allow Deferral
    if ($AllowDefer -and (($DeferTimes -ge 0) -or $DeferDeadline)) {
        & $Script:CommandTable.'Write-ADTLogEntry' -Message 'The user has the option to defer.'
        $showDeference = $true

        # Remove the Z from universal sortable date time format, otherwise it could be converted to a different time zone.
        if ($DeferDeadline) {
            $DeferDeadline = (& $Script:CommandTable.'Get-Date' -Date ($DeferDeadline -replace 'Z')).ToString()
        }
    }

    if (!$Script:Dialogs.Fluent.WelcomePrompt.Running) {
        # Make sure we only create the application session once.
        If ($null -eq $Script:Dialogs.Fluent.ApplicationSession) {
            $Script:Dialogs.Fluent.ApplicationSession = [PSADT.UserInterface.ADTApplication]::new()
        }

        # Instantiate a new progress window object and start it up.
        & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Creating the welcome dialog in a separate thread.'
        if (!$Script:Dialogs.Fluent.WelcomePrompt.Window) {
            $closeAppsList = [System.Collections.Generic.List[PSADT.UserInterface.Classes.AppProcessInfo]]$(
                & $Script:CommandTable.'Get-ADTRunningProcesses' -ProcessObject $ProcessObjects {
                    process {
                        @{ ProcessName = $_.ProcessName; ProcessDescription = $_.ProcessDescription }
                    }
                }
            )

            # $closeAppsList = New-Object System.Collections.Generic.List[PSADT.UserInterface.Classes.AppProcessInfo]
            # $closeAppsList.Add(@{ProcessName = 'winscp'; ProcessDescription = 'WinSCP' })
            # $closeAppsList.Add(@{ProcessName = 'vlc'; ProcessDescription = 'VideoLAN VLC Player' })

            ## $Script:Dialogs.Fluent.WelcomePrompt.Window = $Script:Dialogs.Fluent.ApplicationSession.ShowWelcomeDialog(($adtSession.GetPropertyValue('InstallTitle').Replace('&', '&&')), (& $Script:CommandTable.'Get-ADTConfig').Assets.Logo, $closeAppsList)

            $Script:Dialogs.Fluent.WelcomePrompt.Window = $Script:Dialogs.Fluent.ApplicationSession.ShowWelcomeDialogSync(`
                ($adtSession.GetPropertyValue('InstallTitle').Replace('&', '&&')), `
                $null, `
                $($DeferTimes), `
                $($ProcessObjects), `
                $($adtConfig.Assets.Icon), `
                $($adtConfig.Assets.Fluent.Banner.Light), `
                $($adtConfig.Assets.Fluent.Banner.Dark), `
                $($adtStrings.WelcomePrompt.Fluent.Message), `
                $($adtStrings.WelcomePrompt.Fluent.ButtonDefer), `
                $($adtStrings.WelcomePrompt.Fluent.Message.Subtitle), `
                $($adtStrings.WelcomePrompt.Fluent.Message.DialogMessage), `
                $($adtStrings.WelcomePrompt.Fluent.Message.Remaining), `
                $($adtStrings.WelcomePrompt.Fluent.Message.ButtonLeftText), `
                $($adtStrings.WelcomePrompt.Fluent.Message.ButtonRightText));

            do {
                $Script:Dialogs.Fluent.WelcomePrompt.Running = $Script:Dialogs.Fluent.ApplicationSession.CurrentWindow.IsVisible
            }
            until ($Script:Dialogs.Fluent.WelcomePrompt.Running)
        }
        else {
            # Update an existing object and present the dialog.
            Update-WelcomePromptValues
            $Script:Dialogs.Fluent.WelcomePrompt.Running = $true
        }
    }
    else {
        # Update all values.
        Update-WelcomePromptValues
        & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Updated the progress message.'
    }

    do {
        $Script:Dialogs.Fluent.WelcomePrompt.Running = $Script:Dialogs.Fluent.ApplicationSession.CurrentWindow.IsVisible
    }
    until (!$Script:Dialogs.Fluent.WelcomePrompt.Running)

    # Run the form and store the result.
    $result = switch ($Script:Dialogs.Fluent.ApplicationSession.CurrentWindow.Result) {
        'Install' { 'Close'; break }
        'Defer' { 'Defer'; break }
    }

    # Return the result to the caller.
    return $result
}

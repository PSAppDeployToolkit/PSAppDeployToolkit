#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationProgressFluent
#
#-----------------------------------------------------------------------------

function Private:Show-ADTInstallationProgressFluent
{
    <#
    .SYNOPSIS
        Internal function to display or update the Installation Progress dialog using the Fluent UI.

    .DESCRIPTION
        Called by Show-ADTInstallationProgress. Uses the UnifiedAdtApplication C# class
        to display a Progress dialog or update an existing one. Handles parameter mapping.

    .PARAMETER WindowTitle
        Dialog title.

    .PARAMETER WindowSubtitle
        Dialog subtitle.

    .PARAMETER StatusMessage
        The main status message to display.

    .PARAMETER StatusMessageDetail
        The detailed status message to display.

    .PARAMETER NotTopMost
        Switch to prevent the dialog from being topmost.

    .OUTPUTS
        None
    #>
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UnboundArguments', Justification = "This parameter is just to trap any superfluous input at the end of the function's call.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WindowTitle,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WindowSubtitle,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessage,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessageDetail,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false)]
        [ValidateRange(0, 100)]
        [System.Double]$ProgressPercent, # New parameter for progress percentage

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Perform initial setup.
    $adtConfig = Get-ADTConfig

    # Advise that repositioning the progress window is unsupported for fluent.
    if ($UnboundArguments -eq '-WindowLocation:')
    {
        Write-ADTLogEntry -Message "The parameter [-WindowLocation] is not supported with fluent dialogs and has no effect." -Severity 2
    }

    # Check if the progress dialog is already visible.
    # We use a script-scoped variable set by Close-ADTInstallationProgressFluent
    # and also check the C# state just in case.
    if (!$Script:Dialogs.Fluent.ProgressWindow.Running -or ![PSADT.UserInterface.UnifiedADTApplication]::CurrentDialogVisible())
    {
        # Map parameters for ShowProgressDialog
        $dialogParams = @{
            dialogExpiryDuration = [System.TimeSpan]::FromMinutes((Get-ADTConfig).UI.DialogStyleFluentOptions.ExpiryDuration)
            dialogAccentColor    = (Get-ADTConfig).UI.DialogStyleFluentOptions.AccentColor
            dialogPosition       = (Get-ADTConfig).UI.DialogStyleFluentOptions.Position
            dialogTopMost        = !$NotTopMost
            dialogAllowMove      = (Get-ADTConfig).UI.DialogStyleFluentOptions.AllowMove
            appTitle             = $WindowTitle
            subtitle             = $WindowSubtitle
            appIconImage         = $adtConfig.Assets.Logo
            progressMessage      = $StatusMessage
            progressDetailMessage= $StatusMessageDetail
        }

        # Instantiate a new progress window object and start it up.
        [PSADT.UserInterface.UnifiedADTApplication]::ShowProgressDialog(
            $dialogParams.dialogExpiryDuration,
            $dialogParams.dialogAccentColor,
            $dialogParams.dialogPosition,
            $dialogParams.dialogTopMost,
            $dialogParams.dialogAllowMove,
            $dialogParams.appTitle,
            $dialogParams.subtitle,
            $dialogParams.appIconImage,
            $dialogParams.progressMessage,
            $dialogParams.progressDetailMessage
        )
        # Set the flag indicating the progress window is running
        $Script:Dialogs.Fluent.ProgressWindow.Running = $true
    }
    else
    {
        # Update the existing progress dialog.
        # Note: The C# UpdateProgress takes (message, detail, percent).
        # This function only provides message and detail.
        [PSADT.UserInterface.UnifiedADTApplication]::UpdateProgress(
            $StatusMessage, # progressMessage
            $StatusMessageDetail, # progressMessageDetail
            $ProgressPercent        # progressPercent (pass the new parameter)
        )
    }
}

#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationProgressFluent
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationProgressFluent
{
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

    # Check if the progress thread is running before invoking methods on it.
    if (!$Script:Dialogs.Fluent.ProgressWindow.Running)
    {
        # Instantiate a new progress window object and start it up.
        [PSADT.UserInterface.UnifiedADTApplication]::ShowProgressDialog(
            $WindowTitle,
            $WindowSubtitle,
            !$NotTopMost,
            $adtConfig.Assets.Logo,
            $StatusMessage,
            $StatusMessageDetail
        )

        # Allow the thread to be spun up safely before invoking actions against it.
        do
        {
            $Script:Dialogs.Fluent.ProgressWindow.Running = [PSADT.UserInterface.UnifiedADTApplication]::CurrentDialogVisible()
        }
        until ($Script:Dialogs.Fluent.ProgressWindow.Running)
    }
    else
    {
        # Update all values.
        [PSADT.UserInterface.UnifiedADTApplication]::UpdateProgress($null, $StatusMessage, $StatusMessageDetail)
    }
}

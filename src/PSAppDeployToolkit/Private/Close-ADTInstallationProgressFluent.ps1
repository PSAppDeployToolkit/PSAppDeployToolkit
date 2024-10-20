#-----------------------------------------------------------------------------
#
# MARK: Close-ADTInstallationProgressFluent
#
#-----------------------------------------------------------------------------

function Close-ADTInstallationProgressFluent
{
    # Hide the dialog and reset the state bool.
    Write-ADTLogEntry -Message 'Closing the installation progress dialog.'
    [PSADT.UserInterface.UnifiedADTApplication]::CloseProgressDialog()
    $Script:Dialogs.Fluent.ProgressWindow.Running = $false
}

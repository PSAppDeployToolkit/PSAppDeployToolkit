#-----------------------------------------------------------------------------
#
# MARK: Close-ADTInstallationProgressFluent
#
#-----------------------------------------------------------------------------

function Close-ADTInstallationProgressFluent
{
    # Hide the dialog and reset the state bool.
    & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Closing the installation progress dialog.'
    $Script:Dialogs.Fluent.ProgressWindow.Window.HideDialog()
    $Script:Dialogs.Fluent.ProgressWindow.Running = $false
}

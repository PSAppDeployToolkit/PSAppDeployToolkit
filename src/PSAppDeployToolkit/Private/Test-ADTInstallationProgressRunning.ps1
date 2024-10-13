#-----------------------------------------------------------------------------
#
# MARK: Test-ADTInstallationProgressRunning
#
#-----------------------------------------------------------------------------

function Test-ADTInstallationProgressRunning
{
    # Return the value of the global state's bool.
    return $Script:Dialogs.((Get-ADTConfig).UI.DialogStyle).ProgressWindow.Running
}

#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Test-ADTInstallationProgressRunning
{
    # Call the underlying function to get the progress window state.
    & (Get-ADTDialogFunction)
}

#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Test-ADTInstallationProgressRunningFluent
{
    # Return the value of the global state's bool.
    return $Script:ProgressWindow.Running
}

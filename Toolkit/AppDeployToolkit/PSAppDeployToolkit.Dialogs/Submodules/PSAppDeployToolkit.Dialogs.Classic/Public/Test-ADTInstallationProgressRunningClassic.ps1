#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Test-ADTInstallationProgressRunningClassic
{
    # Return the value of the global state's bool.
    return $Script:ProgressWindow.Running
}

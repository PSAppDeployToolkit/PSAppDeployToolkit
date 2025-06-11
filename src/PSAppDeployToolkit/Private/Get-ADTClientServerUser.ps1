#-----------------------------------------------------------------------------
#
# MARK: Get-ADTClientServerUser
#
#-----------------------------------------------------------------------------

function Private:Get-ADTClientServerUser
{
    # Get the active user from the environment if available.
    $runAsActiveUser = if ((Test-ADTSessionActive) -or (Test-ADTModuleInitialized))
    {
        (Get-ADTEnvironmentTable).RunAsActiveUser
    }
    else
    {
        Get-ADTRunAsActiveUser 4>$null
    }
    if ($runAsActiveUser)
    {
        return $runAsActiveUser
    }
}

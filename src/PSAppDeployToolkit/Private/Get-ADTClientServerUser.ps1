#-----------------------------------------------------------------------------
#
# MARK: Get-ADTClientServerUser
#
#-----------------------------------------------------------------------------

function Private:Get-ADTClientServerUser
{
    # Get the active user from the environment if available.
    $runAsActiveUser = if ([System.Environment]::UserInteractive -and ![PSADT.AccountManagement.AccountUtilities]::CallerSid.IsWellKnown([System.Security.Principal.WellKnownSidType]::LocalSystemSid))
    {
        [PSADT.Module.RunAsActiveUser]::new([PSADT.AccountManagement.AccountUtilities]::CallerUsername, [PSADT.AccountManagement.AccountUtilities]::CallerSid)
    }
    elseif ((Test-ADTSessionActive) -or (Test-ADTModuleInitialized))
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

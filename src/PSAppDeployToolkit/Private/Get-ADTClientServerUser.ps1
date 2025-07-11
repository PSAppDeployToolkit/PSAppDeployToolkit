#-----------------------------------------------------------------------------
#
# MARK: Get-ADTClientServerUser
#
#-----------------------------------------------------------------------------

function Private:Get-ADTClientServerUser
{
    # Determine whether the caller is missing any privileges to create a process as another user.
    $missingPermissions = [PSADT.Security.SE_PRIVILEGE]::SeDebugPrivilege, [PSADT.Security.SE_PRIVILEGE]::SeIncreaseQuotaPrivilege, [PSADT.Security.SE_PRIVILEGE]::SeAssignPrimaryTokenPrivilege | & {
        process
        {
            if (![PSADT.AccountManagement.AccountUtilities]::CallerPrivileges.Contains($_))
            {
                return $_
            }
        }
    }

    # Get the active user from the environment if available.
    $runAsActiveUser = if ([System.Environment]::UserInteractive -and ![PSADT.AccountManagement.AccountUtilities]::CallerSid.IsWellKnown([System.Security.Principal.WellKnownSidType]::LocalSystemSid) -and ($null -ne $missingPermissions))
    {
        Write-ADTLogEntry -Message "The calling account [$([PSADT.AccountManagement.AccountUtilities]::CallerUsername)] is running interactively, but not as the logged on user and missing the permission(s) ['$($missingPermissions -join "', '")'] necessary to run a process as another user. The client/server process will run as the calling account, however PSAppDeployToolkit's client/server operations are designed to run directly as a logged on user, and as such, it is recommended to log on directly with the account you're testing via, assign this account the missing permissions, or test via the SYSTEM account just as ConfigMgr or Intune uses for its operations." -Severity Warning
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

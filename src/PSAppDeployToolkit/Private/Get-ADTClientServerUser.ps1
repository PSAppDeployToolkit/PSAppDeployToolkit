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

    # If there's no RunAsActiveUser but the current process is interactive, just run it as the current user.
    if (!$runAsActiveUser -and [System.Environment]::UserInteractive)
    {
        return [PSADT.Module.RunAsActiveUser]::new([PSADT.AccountManagement.AccountUtilities]::CallerUsername, [PSADT.AccountManagement.AccountUtilities]::CallerSid)
    }

    # Return the calculated RunAsActiveUser if we have one.
    if ($runAsActiveUser)
    {
        # If we're running as an interactive user that isn't the RunAsActiveUser, that's not SYSTEM, and doesn't have the permissions needed to create a process as another user, advise the caller and create an explicit RunAsActiveUser object for the caller instead.
        if (!$runAsActiveUser.SID.Equals([PSADT.AccountManagement.AccountUtilities]::CallerSid) -and [System.Environment]::UserInteractive -and ![PSADT.AccountManagement.AccountUtilities]::CallerIsLocalSystem -and ($null -ne ($missingPermissions = [PSADT.Security.SE_PRIVILEGE]::SeDebugPrivilege, [PSADT.Security.SE_PRIVILEGE]::SeIncreaseQuotaPrivilege, [PSADT.Security.SE_PRIVILEGE]::SeAssignPrimaryTokenPrivilege | & { process { if (![PSADT.AccountManagement.AccountUtilities]::CallerPrivileges.Contains($_)) { return $_ } } })))
        {
            Write-ADTLogEntry -Message "The calling account [$([PSADT.AccountManagement.AccountUtilities]::CallerUsername)] is running interactively, but not as the logged on user and is missing the permission(s) ['$([System.String]::Join("', '", $missingPermissions))'] necessary to create a process as another user. The client/server process will be created as the calling account, however PSAppDeployToolkit's client/server process is designed to operate directly as a logged on user. As such, it is recommended to either log on directly to Windows using this account you're testing with, assign this account the missing permissions, or test via the SYSTEM account just as ConfigMgr or Intune uses for its operations." -Severity Warning
            return [PSADT.Module.RunAsActiveUser]::new([PSADT.AccountManagement.AccountUtilities]::CallerUsername, [PSADT.AccountManagement.AccountUtilities]::CallerSid)
        }
        return $runAsActiveUser
    }
}

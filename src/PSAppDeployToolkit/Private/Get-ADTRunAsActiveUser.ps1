#-----------------------------------------------------------------------------
#
# MARK: Get-ADTRunAsActiveUser
#
#-----------------------------------------------------------------------------

function Private:Get-ADTRunAsActiveUser
{
    # Get all active sessions for subsequent filtration. Attempting to get it from $args is to try and speed up module init.
    $userSessions = if (!$args.Count -or ($args[-1] -isnot [System.Collections.ObjectModel.ReadOnlyCollection[PSADT.TerminalServices.SessionInfo]]))
    {
        Get-ADTLoggedOnUser -InformationAction SilentlyContinue
    }
    else
    {
        $($args[-1])
    }

    # Determine the account that will be used to execute client/server commands in the user's context.
    # Favour the caller's session if it's found and is currently an active user session on the device.
    Write-ADTLogEntry -Message 'Finding the active user session on this device.'
    foreach ($session in $userSessions)
    {
        if ($session.SID.Equals([PSADT.AccountManagement.AccountUtilities]::CallerSid) -and $session.IsActiveUserSession)
        {
            Write-ADTLogEntry -Message "The active user session on this device is [$($session.NTAccount)]."
            return $session.ToRunAsActiveUser()
        }
    }

    # The caller SID isn't the active user session, try to find the best available match.
    if ($session = $userSessions | & { process { if ($_.IsActiveUserSession) { return $_ } } } | Sort-Object -Property LogonTime -Descending | Select-Object -First 1)
    {
        Write-ADTLogEntry -Message "The active user session on this device is [$($session.NTAccount)]."
        return $session.ToRunAsActiveUser()
    }
    Write-ADTLogEntry -Message 'There was no active user session found on this device.'
}

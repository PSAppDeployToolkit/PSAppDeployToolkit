#-----------------------------------------------------------------------------
#
# MARK: Get-ADTRunAsActiveUser
#
#-----------------------------------------------------------------------------

function Private:Get-ADTRunAsActiveUser
{
    <#
    .SYNOPSIS
        Retrieves the active user session information.

    .DESCRIPTION
        The Get-ADTRunAsActiveUser function determines the account that will be used to execute commands in the user session when the toolkit is running under the SYSTEM account.

        The active console user will be chosen first. If no active console user is found, for multi-session operating systems, the first logged-on user will be used instead.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.TerminalServices.SessionInfo

        Returns a custom object containing the user session information.

    .EXAMPLE
        Get-ADTRunAsActiveUser

        This example retrieves the active user session information.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTRunAsActiveUser
    #>

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

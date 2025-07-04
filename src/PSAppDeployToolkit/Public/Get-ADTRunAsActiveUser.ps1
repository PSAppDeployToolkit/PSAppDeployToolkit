#-----------------------------------------------------------------------------
#
# MARK: Get-ADTRunAsActiveUser
#
#-----------------------------------------------------------------------------

function Get-ADTRunAsActiveUser
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

    # Determine the account that will be used to execute client/server commands in the user's context.
    # Favour the caller's session if it's found and is currently an active user session on the device.
    Write-ADTLogEntry -Message 'Finding the active user session on this device.'
    $userSessions = if (!$args.Count -or $args[-1] -isnot [System.Collections.ObjectModel.ReadOnlyCollection[PSADT.TerminalServices.SessionInfo]])
    {
        Get-ADTLoggedOnUser -InformationAction SilentlyContinue
    }
    else
    {
        $args[-1]
    }
    $callerSid = [PSADT.AccountManagement.AccountUtilities]::CallerSid
    foreach ($session in $userSessions)
    {
        if ($callerSid.Equals($session.SID) -and $session.IsActiveUserSession)
        {
            Write-ADTLogEntry -Message "The active user session on this device is [$($session.NTAccount)]."
            return $session
        }
    }

    # The caller SID isn't the active user session, try to find the best available match.
    $sessionInfoMember = if ([PSADT.OperatingSystem.OSVersionInfo]::Current.IsWorkstationEnterpriseMultiSessionOS) { 'IsCurrentSession' } else { 'IsActiveUserSession' }
    foreach ($session in $userSessions)
    {
        if ($session.NTAccount -and $session.$sessionInfoMember)
        {
            Write-ADTLogEntry -Message "The active user session on this device is [$($session.NTAccount)]."
            return $session
        }
    }
    Write-ADTLogEntry -Message 'There was no active user session found on this device.'
}

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

    .PARAMETER UserSessionInfo
        An array of UserSessionInfo objects to enumerate through. If not supplied, a fresh query will be performed.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.UserSessionInfo

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

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.TerminalServices.SessionInfo[]]$UserSessionInfo = (Get-ADTLoggedOnUser -InformationAction SilentlyContinue)
    )

    # Determine the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account.
    # The active console user will be chosen first. Failing that, for multi-session operating systems, the first logged on user will be used instead.
    try
    {
        Write-ADTLogEntry -Message 'Finding the active user session on this device.'
        $sessionInfoMember = if ([PSADT.OperatingSystem.OSVersionInfo]::Current.IsWorkstationEnterpriseMultiSessionOS) { 'IsCurrentSession' } else { 'IsActiveUserSession' }
        foreach ($userSessionInfo in $UserSessionInfo)
        {
            if ($userSessionInfo.NTAccount -and $userSessionInfo.$sessionInfoMember)
            {
                Write-ADTLogEntry -Message "The active user session on this device is [$($userSessionInfo.NTAccount)]."
                return $userSessionInfo
            }
        }
        Write-ADTLogEntry -Message 'There was no active user session found on this device.'
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

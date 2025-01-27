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
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.WTSSession.CompatibilitySessionInfo[]]$UserSessionInfo = (Get-ADTLoggedOnUser)
    )

    # Determine the account that will be used to execute commands in the user session when toolkit is running under the SYSTEM account.
    # The active console user will be chosen first. Failing that, for multi-session operating systems, the first logged on user will be used instead.
    try
    {
        $sessionInfoMember = if (Test-ADTIsMultiSessionOS) { 'IsCurrentSession' } else { 'IsActiveUserSession' }
        foreach ($userSessionInfo in $UserSessionInfo)
        {
            if ($userSessionInfo.NTAccount -and $userSessionInfo.$sessionInfoMember)
            {
                return $userSessionInfo
            }
        }
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

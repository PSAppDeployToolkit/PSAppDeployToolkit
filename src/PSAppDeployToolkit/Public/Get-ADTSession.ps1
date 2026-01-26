#-----------------------------------------------------------------------------
#
# MARK: Get-ADTSession
#
#-----------------------------------------------------------------------------

function Get-ADTSession
{
    <#
    .SYNOPSIS
        Retrieves the most recent ADT session.

    .DESCRIPTION
        The Get-ADTSession function returns the most recent session from the ADT module data. If no sessions are found, it throws an error indicating that an ADT session should be opened using Open-ADTSession before calling this function.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSAppDeployToolkit.SessionManagement.DeploymentSession

        Returns the most recent session object from the ADT module data.

    .EXAMPLE
        Get-ADTSession

        This example retrieves the most recent ADT session.

    .EXAMPLE
        PS C:\>$adtSession = Get-ADTSession
        PS C:\>...
        PS C:\>Close-ADTSession
        PS C:\>$adtSession.GetExitCode()

        This example retrieves the given deployment session's exit code after the session has closed.

    .NOTES
        An active ADT session is required to use this function.

        Requires: PSADT session should be initialized using Open-ADTSession

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTSession
    #>

    [CmdletBinding()]
    param
    (
    )

    # Return the most recent session in the database.
    if (!$Script:ADT.Sessions.Count)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("Please ensure that [Open-ADTSession] is called before using any $($MyInvocation.MyCommand.Module.Name) functions.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ADTSessionBufferEmpty'
            TargetObject = $Script:ADT.Sessions
            RecommendedAction = "Please ensure a session is opened via [Open-ADTSession] and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    return $Script:ADT.Sessions[-1]
}

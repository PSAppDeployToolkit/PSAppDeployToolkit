#-----------------------------------------------------------------------------
#
# MARK: Get-ADTDeferHistory
#
#-----------------------------------------------------------------------------

function Get-ADTDeferHistory
{
    <#
    .SYNOPSIS
        Get the history of deferrals in the registry for the current application.

    .DESCRIPTION
        Get the history of deferrals in the registry for the current application.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSAppDeployToolkit.Foundation.DeferHistory

        When a deferal history exists for the current deployment, this function returns a DeferHistory object represending the deferal history with the following properties:
        - DeferTimesRemaining
        - DeferDeadline
        - DeferRunIntervalLastTime

    .EXAMPLE
        Get-DeferHistory

    .NOTES
        An active ADT session is required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTDeferHistory

    #>

    [CmdletBinding()]
    [OutputType([PSAppDeployToolkit.Foundation.DeferHistory])]
    param
    (
    )

    try
    {
        return (Get-ADTSession).GetDeferHistory()
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

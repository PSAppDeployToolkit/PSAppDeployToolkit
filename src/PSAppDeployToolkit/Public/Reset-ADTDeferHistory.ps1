#-----------------------------------------------------------------------------
#
# MARK: Reset-ADTDeferHistory
#
#-----------------------------------------------------------------------------

function Reset-ADTDeferHistory
{
    <#
    .SYNOPSIS
        Reset the history of deferrals in the registry for the current application.

    .DESCRIPTION
        Reset the history of deferrals in the registry for the current application.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Reset-DeferHistory

    .NOTES
        An active ADT session is required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Reset-ADTDeferHistory

    #>

    [CmdletBinding()]
    param
    (
    )

    try
    {
        (Get-ADTSession).ResetDeferHistory()
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

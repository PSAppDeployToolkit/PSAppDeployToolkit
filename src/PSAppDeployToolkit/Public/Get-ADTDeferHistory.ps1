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
        None

        This function does not return any objects.

    .EXAMPLE
        Get-DeferHistory

    .NOTES
        An active ADT session is required to use this function.

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
    )

    try
    {
        (Get-ADTSession).GetDeferHistory()
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

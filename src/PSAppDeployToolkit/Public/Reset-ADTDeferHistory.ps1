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

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
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
        (Get-ADTSession).ResetDeferHistory()
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

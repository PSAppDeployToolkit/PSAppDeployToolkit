#-----------------------------------------------------------------------------
#
# MARK: Add-ADTSessionFinishingCallback
#
#-----------------------------------------------------------------------------

function Add-ADTSessionFinishingCallback
{
    <#
    .SYNOPSIS
        Adds a callback to be executed when the ADT session is finishing.

    .DESCRIPTION
        The Add-ADTSessionFinishingCallback function registers a callback command to be executed when the ADT session is finishing. This function sends the callback to the backend function for processing.

    .PARAMETER Callback
        The callback command(s) to be executed when the ADT session is finishing.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Add-ADTSessionFinishingCallback -Callback $myCallback

        This example adds the specified callback to be executed when the ADT session is finishing.

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
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.CommandInfo[]]$Callback
    )

    # Send it off to the backend function.
    try
    {
        Invoke-ADTSessionCallbackOperation -Type Finishing -Action Add @PSBoundParameters
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

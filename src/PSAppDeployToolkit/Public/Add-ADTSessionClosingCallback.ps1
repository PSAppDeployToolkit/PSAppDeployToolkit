﻿#-----------------------------------------------------------------------------
#
# MARK: Add-ADTSessionClosingCallback
#
#-----------------------------------------------------------------------------

function Add-ADTSessionClosingCallback
{
    <#
    .SYNOPSIS
        Adds a callback to be executed when the ADT session is closing.

    .DESCRIPTION
        The Add-ADTSessionClosingCallback function registers a callback command to be executed when the ADT session is closing. This function sends the callback to the backend function for processing.

    .PARAMETER Callback
        The callback command(s) to be executed when the ADT session is closing.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Add-ADTSessionClosingCallback -Callback $myCallback

        This example adds the specified callback to be executed when the ADT session is closing.

    .NOTES
        An active ADT session is required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: © 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Add-ADTSessionClosingCallback
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
        Invoke-ADTSessionCallbackOperation -Type Closing -Action Add @PSBoundParameters
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

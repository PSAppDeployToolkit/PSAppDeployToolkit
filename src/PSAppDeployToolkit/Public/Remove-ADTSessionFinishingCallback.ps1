﻿#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTSessionFinishingCallback
#
#-----------------------------------------------------------------------------

function Remove-ADTSessionFinishingCallback
{
    <#
    .SYNOPSIS
        Removes a callback function from the ADT session finishing event.

    .DESCRIPTION
        This function removes a specified callback function from the ADT session finishing event. The callback function must be provided as a parameter. If the operation fails, it throws a terminating error.

    .PARAMETER Callback
        The callback function to remove from the ADT session finishing event.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Remove-ADTSessionFinishingCallback -Callback (Get-Command -Name 'MyCallbackFunction')

        Removes the specified callback function from the ADT session finishing event.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTSessionFinishingCallback
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
        Invoke-ADTSessionCallbackOperation -Type Finishing -Action Remove @PSBoundParameters
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

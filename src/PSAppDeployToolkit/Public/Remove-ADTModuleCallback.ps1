#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTModuleCallback
#
#-----------------------------------------------------------------------------

function Remove-ADTModuleCallback
{
    <#
    .SYNOPSIS
        Removes a callback function from the nominated hooking point.

    .DESCRIPTION
        This function removes a specified callback function from the nominated hooking point.

    .PARAMETER Hookpoint
        Where you wish for the callback to be removed from.

    .PARAMETER Callback
        The callback function to remove from the nominated hooking point.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Remove-ADTModuleCallback -Hookpoint PostOpen -Callback (Get-Command -Name 'MyCallbackFunction')

        Removes the specified callback function from being invoked after a DeploymentSession has opened.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTModuleCallback
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Module.CallbackType]$Hookpoint,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.CommandInfo[]]$Callback
    )

    # Send it off to the backend function.
    try
    {
        Invoke-ADTModuleCallbackOperation -Action Remove @PSBoundParameters
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

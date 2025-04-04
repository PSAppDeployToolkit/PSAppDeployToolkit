#-----------------------------------------------------------------------------
#
# MARK: Add-ADTModuleCallback
#
#-----------------------------------------------------------------------------

function Add-ADTModuleCallback
{
    <#
    .SYNOPSIS
        Adds a callback function to the nominated hooking point.

    .DESCRIPTION
        This function adds a specified callback function to the nominated hooking point.

    .PARAMETER Hookpoint
        Where you wish for the callback to be executed at.

    .PARAMETER Callback
        The callback function to add to the nominated hooking point.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Add-ADTModuleCallback -Hookpoint PostOpen -Callback (Get-Command -Name 'MyCallbackFunction')

        Adds the specified callback function to be invoked after a DeploymentSession has opened.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Add-ADTModuleCallback
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
        Invoke-ADTModuleCallbackOperation -Action Add @PSBoundParameters
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

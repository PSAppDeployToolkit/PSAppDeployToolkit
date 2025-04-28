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
        Valid hookpoints are:
        OnInit      (The callback is executed before the module is initialized.)
        OnStart     (The callback is executed before the first deployment session is opened.)
        PreOpen     (The callback is executed before a deployment session is opened.)
        PostOpen    (The callback is executed after a deployment session is opened.)
        PreClose    (The callback is executed before the deployment session is closed.)
        PostClose   (The callback is executed after the deployment session is closed.)
        OnFinish    (The callback is executed before the last deployment session is closed.)
        OnExit      (The callback is executed after the last deployment session is closed.)
        
        You can have multiple Callbacks of one type. IOW: you can have 2 'OnExit' Callbacks
        To see a list all the registered callbacks in order, use: Get-ADTCommandTable

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
        Also see Remove-ADTModuleCallback

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

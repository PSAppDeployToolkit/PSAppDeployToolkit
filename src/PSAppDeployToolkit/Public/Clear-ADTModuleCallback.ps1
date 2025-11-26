#-----------------------------------------------------------------------------
#
# MARK: Clear-ADTModuleCallback
#
#-----------------------------------------------------------------------------

function Clear-ADTModuleCallback
{
    <#
    .SYNOPSIS
        Clears the nominated hooking point of all callbacks.

    .DESCRIPTION
        This function clears the nominated hooking point of all callbacks.

    .PARAMETER Hookpoint
        The callback hook point that you wish to clear.

        Valid hookpoints are:
        * OnInit (The callback is executed before the module is initialized)
        * OnStart (The callback is executed before the first deployment session is opened)
        * PreOpen (The callback is executed before a deployment session is opened)
        * PostOpen (The callback is executed after a deployment session is opened)
        * OnDefer (The callback is executed when a user defers the active deployment)
        * PreClose (The callback is executed before the deployment session is closed)
        * PostClose (The callback is executed after the deployment session is closed)
        * OnFinish (The callback is executed before the last deployment session is closed)
        * OnExit (The callback is executed after the last deployment session is closed)

        To see a list all the registered callbacks in order, use `Get-ADTModuleCallback`.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Clear-ADTModuleCallback -Hookpoint PostOpen

        Clears all callbacks to be invoked after a DeploymentSession has opened.

    .NOTES
        An active ADT session is NOT required to use this function.

        Also see `Remove-ADTModuleCallback` about how callbacks can be removed.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Clear-ADTModuleCallback
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Module.CallbackType]$Hookpoint
    )

    # Directly clear the backend list.
    try
    {
        $Script:ADT.Callbacks.$Hookpoint.Clear()
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

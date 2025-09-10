#-----------------------------------------------------------------------------
#
# MARK: Get-ADTModuleCallback
#
#-----------------------------------------------------------------------------

function Get-ADTModuleCallback
{
    <#
    .SYNOPSIS
        Returns all callbacks from the nominated hooking point.

    .DESCRIPTION
        This function returns all callbacks from the nominated hooking point.

    .PARAMETER Hookpoint
        The hook point to return the callbacks for.

        Valid hookpoints are:
        * OnInit (The callback is executed before the module is initialized)
        * OnStart (The callback is executed before the first deployment session is opened)
        * PreOpen (The callback is executed before a deployment session is opened)
        * PostOpen (The callback is executed after a deployment session is opened)
        * PreClose (The callback is executed before the deployment session is closed)
        * PostClose (The callback is executed after the deployment session is closed)
        * OnFinish (The callback is executed before the last deployment session is closed)
        * OnExit (The callback is executed after the last deployment session is closed)

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Get-ADTModuleCallback -Hookpoint PostOpen

        Returns all callbacks to be invoked after a DeploymentSession has opened.

    .NOTES
        An active ADT session is NOT required to use this function.

        Also see `Remove-ADTModuleCallback` about how callbacks can be removed.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTModuleCallback
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
        $PSCmdlet.WriteObject([System.Collections.Generic.IReadOnlyList[System.Management.Automation.CommandInfo]]$Script:ADT.Callbacks.$Hookpoint.AsReadOnly(), $false)
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

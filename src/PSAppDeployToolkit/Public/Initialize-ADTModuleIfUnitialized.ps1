#-----------------------------------------------------------------------------
#
# MARK: Initialize-ADTModuleIfUnitialized
#
#-----------------------------------------------------------------------------

function Initialize-ADTModuleIfUnitialized
{
    <#
    .SYNOPSIS
        Convenience function to initialize the module if required, optionally returning the active session if available.

    .DESCRIPTION
        Convenience function to initialize the module if required, optionally returning the active session if available. This is available as a shorthand function for extension module developers and will likely serve no benefit for regular deployment scripts.

    .PARAMETER Cmdlet
        The cmdlet that is being initialized.

    .PARAMETER PassThruActiveSession
        Returns the active DeploymentSession if available.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSAppDeployToolkit.Foundation.DeploymentSession

        Returns the most recent session object from the ADT module data.

    .EXAMPLE
        Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet

        Initializes the ADT module with the default settings and configurations if it is uninitialized.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Initialize-ADTModuleIfUnitialized
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThruActiveSession
    )

    # Initialize the module if there's no session and it hasn't been previously initialized.
    if (!($adtSession = if (Test-ADTSessionActive) { Get-ADTSession }) -and !(Test-ADTModuleInitialized))
    {
        try
        {
            Initialize-ADTModule
        }
        catch
        {
            $Cmdlet.ThrowTerminatingError($_)
        }
    }

    # Return the current session if we happened to get one.
    if ($adtSession -and $PassThruActiveSession)
    {
        return $adtSession
    }
}

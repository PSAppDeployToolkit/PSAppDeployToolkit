#-----------------------------------------------------------------------------
#
# MARK: Get-ADTEnvironmentTable
#
#-----------------------------------------------------------------------------

function Get-ADTEnvironmentTable
{
    <#
    .SYNOPSIS
        Retrieves the environment data for the PSAppDeployToolkit module.

    .DESCRIPTION
        The `Get-ADTEnvironmentTable` function retrieves the environment data for the PSAppDeployToolkit module. This function ensures that the PSAppDeployToolkit module has been initialized before attempting to retrieve the environment data. If the module is not initialized, it throws an error.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSAppDeployToolkit.Foundation.EnvironmentTable

        Returns the environment data as an EnvironmentTable object with read-only properties.

    .EXAMPLE
        $environment = Get-ADTEnvironmentTable

        This example retrieves the environment data for the PSAppDeployToolkit module and stores it in the `$environment` variable.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTEnvironmentTable

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/modules/PSAppDeployToolkit/Public/Get-ADTEnvironmentTable.ps1
    #>

    [CmdletBinding()]
    [OutputType([PSAppDeployToolkit.Foundation.EnvironmentTable])]
    param
    (
    )

    # Return the environment database if initialized. Rethrown from here so the caller's own line
    # is what the error reports, rather than a line inside this module that means nothing to them.
    try
    {
        return (Get-ADTModuleState).Environment
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

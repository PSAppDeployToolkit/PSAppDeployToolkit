#-----------------------------------------------------------------------------
#
# MARK: Get-ADTEnvironment
#
#-----------------------------------------------------------------------------

function Get-ADTEnvironment
{
    <#
    .SYNOPSIS
        Retrieves the environment data for the ADT module. This function has been replaced by Get-ADTEnvironmentTable and will be removed from a future release.

    .DESCRIPTION
        The Get-ADTEnvironment function retrieves the environment data for the ADT module. This function ensures that the ADT module has been initialized before attempting to retrieve the environment data. If the module is not initialized, it throws an error.

        This function has been replaced by Get-ADTEnvironmentTable and will be removed from a future release.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Collections.Specialized.OrderedDictionary

        Returns the environment data as a read-only ordered dictionary.

    .EXAMPLE
        $environment = Get-ADTEnvironment

        This example retrieves the environment data for the ADT module and stores it in the $environment variable.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
    )

    # Announce deprecation and return the environment database if initialized.
    Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] has been replaced by [Get-ADTEnvironmentTable]. Please migrate your scripts as this will be removed in a future update." -Severity 2
    return (Get-ADTEnvironmentTable)
}

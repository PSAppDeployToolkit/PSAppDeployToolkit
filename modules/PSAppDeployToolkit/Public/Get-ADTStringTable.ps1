#-----------------------------------------------------------------------------
#
# MARK: Get-ADTStringTable
#
#-----------------------------------------------------------------------------

function Get-ADTStringTable
{
    <#
    .SYNOPSIS
        Gets the string database from the PSAppDeployToolkit module.

    .DESCRIPTION
        The `Get-ADTStringTable` function gets the module's string database, if it has been initialized. If the string database is not initialized, it throws an error indicating that `Initialize-ADTModule` should be called before using this function.

    .PARAMETER SessionState
        The SessionState in which to expand variables from if specified.

    .INPUTS
        None

        This function does not take any pipeline input.

    .OUTPUTS
        System.Collections.Hashtable

        Returns a hashtable containing the string database.

    .EXAMPLE
        Get-ADTStringTable

        This example retrieves the string database from the PSAppDeployToolkit module.

    .NOTES
        An active ADT session is NOT required to use this function.

        Requires: The module should be initialized using `Initialize-ADTModule`

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTStringTable

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/modules/PSAppDeployToolkit/Public/Get-ADTStringTable.ps1
    #>

    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState
    )

    # Return a copied hashtable with variables expanded if a SessionState is provided, otherwise just return a reference to what we've got.
    # Rethrown from here so the caller's own line is what the error reports, rather than a line inside this module that means nothing to them.
    $stringTable = try
    {
        (Get-ADTModuleState).StringTable
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
    if ($PSBoundParameters.ContainsKey('SessionState'))
    {
        $stringTable = [PSADT.ClientServer.DataSerialization]::DeserializeFromBytes([PSADT.ClientServer.DataSerialization]::SerializeToBytes($stringTable), [System.Collections.Hashtable])
        Expand-ADTVariablesInHashtable -Hashtable $stringTable -SessionState $SessionState
        return $stringTable
    }
    return $stringTable
}

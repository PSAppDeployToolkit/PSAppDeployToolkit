#-----------------------------------------------------------------------------
#
# MARK: Get-ADTStringTable
#
#-----------------------------------------------------------------------------

function Get-ADTStringTable
{
    <#
    .SYNOPSIS
        Retrieves the string database from the ADT module.

    .DESCRIPTION
        The Get-ADTStringTable function returns the string database if it has been initialized. If the string database is not initialized, it throws an error indicating that Initialize-ADTModule should be called before using this function.

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

        This example retrieves the string database from the ADT module.

    .NOTES
        An active ADT session is NOT required to use this function.

        Requires: The module should be initialized using Initialize-ADTModule

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTStringTable
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState
    )

    # Return the string database if initialized.
    if (!$Script:ADT.Strings -or !$Script:ADT.Strings.Count)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("Please ensure that [Initialize-ADTModule] is called before using any $($MyInvocation.MyCommand.Module.Name) functions.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ADTStringTableNotInitialized'
            TargetObject = $Script:ADT.Strings
            RecommendedAction = "Please ensure the module is initialized via [Initialize-ADTModule] and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

    # Return a copied hashtable with variables expanded if a SessionState is provided, otherwise just return a reference to what we've got.
    if ($PSBoundParameters.ContainsKey('SessionState'))
    {
        $strings = [PSADT.Utilities.SimpleSerializer]::Deserialize([PSADT.Utilities.SimpleSerializer]::Serialize($Script:ADT.Strings), $Script:ADT.Strings.GetType())
        Expand-ADTVariablesInHashtable -Hashtable $strings -SessionState $SessionState
        return $strings
    }
    return $Script:ADT.Strings
}

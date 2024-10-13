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

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
    )

    # Return the string database if initialized.
    if (!($adtData = Get-ADTModuleData).Strings -or !$adtData.Strings.Count)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("Please ensure that [Initialize-ADTModule] is called before using any $($MyInvocation.MyCommand.Module.Name) functions.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ADTStringTableNotInitialized'
            TargetObject = $adtData.Strings
            RecommendedAction = "Please ensure the module is initialized via [Initialize-ADTModule] and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    return $adtData.Strings
}

#-----------------------------------------------------------------------------
#
# MARK: Get-ADTCommandTable
#
#-----------------------------------------------------------------------------

function Get-ADTCommandTable
{
    <#
    .SYNOPSIS
        Returns PSAppDeployToolkit's safe command lookup table.

    .DESCRIPTION
        This function returns PSAppDeployToolkit's safe command lookup table, which can be used for command lookups within extending modules.

        Please note that PSAppDeployToolkit's safe command table only has commands in it that are used within this module, and not necessarily all commands offered by PowerShell and its built-in modules out of the box.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Collections.ObjectModel.ReadOnlyDictionary[System.String, System.Management.Automation.CommandInfo]

        Returns PSAppDeployTookit's safe command lookup table as a ReadOnlyDictionary.

    .EXAMPLE
        Get-ADTCommandTable

        Returns PSAppDeployToolkit's safe command lookup table.

    .NOTES
        An active ADT session is required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    # Return the module's read-only CommandTable to the caller.
    return $Script:CommandTable
}

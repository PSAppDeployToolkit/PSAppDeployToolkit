#-----------------------------------------------------------------------------
#
# MARK: Test-ADTModuleInitialized
#
#-----------------------------------------------------------------------------

function Test-ADTModuleInitialized
{
    <#
    .SYNOPSIS
        Checks if the ADT (PSAppDeployToolkit) module is initialized.

    .DESCRIPTION
        This function checks if the ADT (PSAppDeployToolkit) module is initialized by retrieving the module data and returning the initialization status.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true if the ADT module is initialized, otherwise $false.

    .EXAMPLE
        Test-ADTModuleInitialized

        Checks if the ADT module is initialized and returns true or false.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    return (Get-ADTModuleData).Initialized
}

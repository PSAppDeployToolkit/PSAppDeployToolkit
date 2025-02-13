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

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Test-ADTModuleInitialized
    #>

    return $Script:ADT.Initialized
}

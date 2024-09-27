#-----------------------------------------------------------------------------
#
# MARK: Get-ADTMsiProductCodeRegexPattern
#
#-----------------------------------------------------------------------------

function Get-ADTMsiProductCodeRegexPattern
{
    <#
    .SYNOPSIS
        Returns a regex pattern to use for MSI ProductCode matching, or matching any UUID.

    .DESCRIPTION
        This function returns a regex pattern to use for MSI ProductCode matching, or matching any UUID.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns a regex pattern to use for MSI ProductCode matching, or matching any UUID.

    .EXAMPLE
        Get-ADTMsiProductCodeRegexPattern

        Returns a regex pattern to use for MSI ProductCode matching, or matching any UUID.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    return '^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$'
}

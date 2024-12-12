#-----------------------------------------------------------------------------
#
# MARK: Get-ADTOperatingSystemInfo
#
#-----------------------------------------------------------------------------

function Get-ADTOperatingSystemInfo
{
    <#
    .SYNOPSIS
        Gets information about the current computer's operating system.

    .DESCRIPTION
        Gets information about the current computer's operating system, such as name, version, edition, and other information.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.OperatingSystem.OSVersionInfo

        Returns an PSADT.OperatingSystem.OSVersionInfo object containing the current computer's operating system information.

    .EXAMPLE
        Get-ADTOperatingSystemInfo

        Gets an PSADT.OperatingSystem.OSVersionInfo object containing the current computer's operating system information.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    return [PSADT.OperatingSystem.OSVersionInfo]::Current
}

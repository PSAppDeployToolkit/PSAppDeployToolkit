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
        PSADT.DeviceManagement.OperatingSystemInfo

        Returns an PSADT.DeviceManagement.OperatingSystemInfo object containing the current computer's operating system information.

    .EXAMPLE
        Get-ADTOperatingSystemInfo

        Gets an PSADT.DeviceManagement.OperatingSystemInfo object containing the current computer's operating system information.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTOperatingSystemInfo
    #>

    return [PSADT.DeviceManagement.OperatingSystemInfo]::Current
}

#-----------------------------------------------------------------------------
#
# MARK: Out-ADTPowerShellEncodedCommand
#
#-----------------------------------------------------------------------------

function Out-ADTPowerShellEncodedCommand
{
    <#
    .SYNOPSIS
        Encodes a PowerShell command into a Base64 string.

    .DESCRIPTION
        This function takes a PowerShell command as input and encodes it into a Base64 string. This is useful for passing commands to PowerShell through mechanisms that require encoded input.

    .PARAMETER Command
        The PowerShell command to be encoded.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        This function returns the encoded Base64 string representation of the input command.

    .EXAMPLE
        Out-ADTPowerShellEncodedCommand -Command 'Get-Process'

        Encodes the "Get-Process" command into a Base64 string.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Command
    )

    return [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($Command))
}

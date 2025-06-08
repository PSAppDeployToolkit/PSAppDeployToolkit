#-----------------------------------------------------------------------------
#
# MARK: Remove-ADTInvalidFileNameChars
#
#-----------------------------------------------------------------------------

function Remove-ADTInvalidFileNameChars
{
    <#
    .SYNOPSIS
        Remove invalid characters from the supplied string.

    .DESCRIPTION
        This function removes invalid characters from the supplied string and returns a valid filename as a string. It ensures that the resulting string does not contain any characters that are not allowed in filenames. This function should not be used for entire paths as '\' is not a valid filename character.

    .PARAMETER Name
        Text to remove invalid filename characters from.

    .INPUTS
        System.String

        A string containing invalid filename characters.

    .OUTPUTS
        System.String

        Returns the input string with the invalid characters removed.

    .EXAMPLE
        Remove-ADTInvalidFileNameChars -Name "Filename/\1"

        Removes invalid filename characters from the string "Filename/\1".

    .NOTES
        An active ADT session is NOT required to use this function.

        This function always returns a string; however, it can be empty if the name only contains invalid characters. Do not use this command for an entire path as '\' is not a valid filename character.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Remove-ADTInvalidFileNameChars
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyString()]
        [System.String]$Name
    )

    process
    {
        return ($Name.Trim() -replace "[$([System.Text.RegularExpressions.Regex]::Escape([System.String]::Join([System.String]::Empty, [System.IO.Path]::GetInvalidFileNameChars())))]")
    }
}

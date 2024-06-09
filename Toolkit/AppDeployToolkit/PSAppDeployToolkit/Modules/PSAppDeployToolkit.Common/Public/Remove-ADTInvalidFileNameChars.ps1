function Remove-ADTInvalidFileNameChars
{
    <#

    .SYNOPSIS
    Remove invalid characters from the supplied string.

    .DESCRIPTION
    Remove invalid characters from the supplied string and returns a valid filename as a string.

    .PARAMETER Name
    Text to remove invalid filename characters from.

    .INPUTS
    System.String. A string containing invalid filename characters.

    .OUTPUTS
    System.String. Returns the input string with the invalid characters removed.

    .EXAMPLE
    Remove-ADTInvalidFileNameChars -Name "Filename/\1"

    .NOTES
    This functions always returns a string however it can be empty if the name only contains invalid characters.
    Do no use this command for an entire path as '\' is not a valid filename character.

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyString()]
        [System.String]$Name
    )

    begin {
        Write-ADTDebugHeader
    }

    process {
        return $Name.Trim() -replace (Get-ADTEnvironment).InvalidFileNameCharsRegExPattern
    }

    end {
        Write-ADTDebugFooter
    }
}

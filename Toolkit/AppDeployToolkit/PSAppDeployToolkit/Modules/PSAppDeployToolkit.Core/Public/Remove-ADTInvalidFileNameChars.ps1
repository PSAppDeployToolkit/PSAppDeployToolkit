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

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyString()]
        [System.String]$Name
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                return $Name.Trim() -replace "($([System.String]::Join('|', [System.IO.Path]::GetInvalidFileNameChars().ForEach({[System.Text.RegularExpressions.Regex]::Escape($_)}))))"
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -Prefix 'Failed to remove invalid characters from the supplied filename.'
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

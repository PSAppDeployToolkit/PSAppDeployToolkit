function Test-FluenceInput
{
    <#
    .SYNOPSIS
        Validates captured dialog values against their prompt rules.
    .DESCRIPTION
        Walks the prompts in order and applies each one's rules to the value captured for it:
        -ValidateNotEmpty, -ValidatePattern and -ValidateScript. Returns on the first failure, so
        the message names the prompt that stopped the dialog closing.
    .PARAMETER Prompts
        The Fluence.Prompt objects the dialog was built from.
    .PARAMETER Values
        The result hashtable holding one captured value per prompt name.
    .OUTPUTS
        A hashtable with IsValid (bool) and Message (string).
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [object[]]$Prompts,

        [Parameter(Mandatory = $true)]
        [hashtable]$Values
    )

    foreach ($p in $Prompts)
    {
        $value = $Values[$p.Name]
        $asText = $null
        if ($value -is [System.Security.SecureString])
        {
            $isEmpty = $value.Length -eq 0
        }
        else
        {
            $asText = [string]$value
            $isEmpty = [string]::IsNullOrWhiteSpace($asText)
        }

        if ($p.ValidateNotEmpty -and $isEmpty)
        {
            return @{ IsValid = $false; Message = "'$($p.Name)' is required." }
        }

        if (-not [string]::IsNullOrWhiteSpace($p.ValidatePattern) -and -not [string]::IsNullOrWhiteSpace($asText))
        {
            if ($asText -notmatch $p.ValidatePattern)
            {
                return @{ IsValid = $false; Message = "'$($p.Name)' does not match the required format." }
            }
        }

        if ($null -ne $p.ValidateScript)
        {
            $ok = $false
            try
            {
                # A multi-statement validator that does not suppress intermediate output returns an
                # array; [bool] of a 2+-element array is always $true, which would bypass validation.
                # Use the LAST object the scriptblock emits as the result.
                $output = & $p.ValidateScript $value
                $ok = [bool](@($output)[-1])
            }
            catch
            {
                # A validator that throws counts as a failed validation, but the exception is the only
                # clue that the scriptblock itself is broken (a typo, a missing cmdlet) rather than
                # the value being rejected, so record it instead of discarding it.
                if ($p.InputType -eq 'Password')
                {
                    Write-Verbose "ValidateScript for '$($p.Name)' threw. Exception text is omitted for password prompts."
                }
                else
                {
                    Write-Verbose "ValidateScript for '$($p.Name)' threw: $($_.Exception.Message)"
                }
                $ok = $false
            }
            if (-not $ok)
            {
                return @{ IsValid = $false; Message = "'$($p.Name)' failed validation." }
            }
        }
    }

    return @{ IsValid = $true; Message = '' }
}

<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


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

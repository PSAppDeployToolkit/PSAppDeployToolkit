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


function ConvertTo-FluencePromptList
{
    <#
    .SYNOPSIS
        Normalizes a mixed array of strings and Fluence.Prompt objects into a Fluence.Prompt list.
    .DESCRIPTION
        Strings are converted via New-FluencePrompt -Message. Fluence.Prompt objects pass through
        unchanged. Any other type raises a terminating error naming the bad item.
    .PARAMETER InputObject
        An array of strings or Fluence.Prompt objects.
    .OUTPUTS
        Fluence.Prompt
    .NOTES
        Private helper. Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.Prompt')]
    param
    (
        [Parameter(Mandatory = $true)]
        [object[]]$InputObject
    )

    $result = @()
    foreach ($item in $InputObject)
    {
        if ($item -is [string])
        {
            $result += New-FluencePrompt -Message $item
        }
        elseif ($item.PSObject.TypeNames -contains 'Fluence.Prompt')
        {
            $result += $item
        }
        elseif ($null -eq $item)
        {
            # Without this branch the null falls through to GetType() in the throw below and the
            # caller sees 'You cannot call a method on a null-valued expression' instead of the
            # diagnostic that names the parameter.
            throw "ConvertTo-FluencePromptList: a $null item is not a valid prompt specification."
        }
        else
        {
            throw "ConvertTo-FluencePromptList: unsupported item type '$($item.GetType().FullName)'. Expected [string] or [Fluence.Prompt]."
        }
    }
    return $result
}

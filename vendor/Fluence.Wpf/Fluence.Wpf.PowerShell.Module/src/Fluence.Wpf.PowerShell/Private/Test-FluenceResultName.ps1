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


function Test-FluenceResultName
{
    <#
    .SYNOPSIS
        Rejects ambiguous result keys before a dialog is dispatched.
    .DESCRIPTION
        Prompt names, button names and the module's result flags share one case-insensitive
        namespace. Duplicate or reserved keys would overwrite captured input or outcome flags.
    .PARAMETER Prompts
        The normalized prompt specifications.
    .PARAMETER Buttons
        The normalized button specifications.
    .NOTES
        Does not require a host application. Throws on an invalid specification.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]]$Prompts,

        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]]$Buttons
    )

    $names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($reserved in @('Cancelled', 'TimedOut', 'PSTypeName'))
    {
        $null = $names.Add($reserved)
    }
    foreach ($item in @($Prompts) + @($Buttons))
    {
        if ([string]::IsNullOrWhiteSpace([string]$item.Name))
        {
            throw 'Every prompt and button must have a non-empty result name.'
        }
        if (-not $names.Add([string]$item.Name))
        {
            throw "Result name '$($item.Name)' is duplicate or reserved. Prompt and button names must be unique and cannot be Cancelled, TimedOut or PSTypeName."
        }
    }
}

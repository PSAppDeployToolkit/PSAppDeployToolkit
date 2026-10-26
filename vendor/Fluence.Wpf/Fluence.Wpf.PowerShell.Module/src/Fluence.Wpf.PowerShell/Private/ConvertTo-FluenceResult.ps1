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


function ConvertTo-FluenceResult
{
    <#
    .SYNOPSIS
        Converts a raw result hashtable into a typed Fluence.DialogResult PSCustomObject.
    .DESCRIPTION
        Copies every key from the input hashtable into an ordered hashtable, then sets
        PSTypeName = 'Fluence.DialogResult' last (so a caller key named 'PSTypeName' cannot
        override the type) and casts the result to [pscustomobject].
    .PARAMETER Result
        The hashtable of collected values from the dialog renderer.
    .OUTPUTS
        Fluence.DialogResult
    .NOTES
        Private helper. Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.DialogResult')]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Result
    )

    # Seed PSTypeName AFTER copying the caller's keys so a result key literally named 'PSTypeName'
    # cannot clobber the synthetic type marker (hashtable keys are case-insensitive); the
    # [pscustomobject] cast consumes PSTypeName as the type tag rather than as a property.
    $ordered = [ordered]@{}
    foreach ($key in $Result.Keys)
    {
        $ordered[$key] = $Result[$key]
    }
    $ordered['PSTypeName'] = 'Fluence.DialogResult'
    return [pscustomobject]$ordered
}

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


function Resolve-FluenceProgressState
{
    <#
    .SYNOPSIS
        Merges a progress update into the current progress state and clamps the percentage.
    .DESCRIPTION
        Pure logic shared by Show-FluenceProgress and Update-FluenceProgress. Only the values the
        caller bound are changed; a bound -PercentComplete is clamped to 0..100 and switches the bar
        to determinate, and -Indeterminate switches it back. Returns the new state as an ordered
        hashtable with Message, Detail, PercentComplete and Indeterminate.
    .PARAMETER Current
        The state to start from (Message, Detail, PercentComplete, Indeterminate). Null means defaults.
    .PARAMETER Bound
        The caller's $PSBoundParameters, read for Message, Detail, PercentComplete and Indeterminate.
    .OUTPUTS
        System.Collections.Hashtable
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter()]
        [System.Collections.IDictionary]$Current,

        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$Bound
    )

    $state = @{
        Message         = ''
        Detail          = ''
        PercentComplete = 0
        Indeterminate   = $true
    }
    if ($null -ne $Current)
    {
        foreach ($key in @('Message', 'Detail', 'PercentComplete', 'Indeterminate'))
        {
            if ($Current.ContainsKey($key))
            {
                $state[$key] = $Current[$key]
            }
        }
    }

    if ($Bound.ContainsKey('Message'))
    {
        $state.Message = [string]$Bound['Message']
    }
    if ($Bound.ContainsKey('Detail'))
    {
        $state.Detail = [string]$Bound['Detail']
    }
    if ($Bound.ContainsKey('PercentComplete'))
    {
        $percent = [double]$Bound['PercentComplete']
        if ([double]::IsNaN($percent))
        {
            $percent = 0
        }
        $state.PercentComplete = [System.Math]::Min(100.0, [System.Math]::Max(0.0, $percent))
        $state.Indeterminate = $false
    }
    if ($Bound.ContainsKey('Indeterminate') -and [bool]$Bound['Indeterminate'])
    {
        $state.Indeterminate = $true
    }

    return $state
}

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


function Resolve-FluenceRestartOutcome
{
    <#
    .SYNOPSIS
        Maps a restart prompt's dialog result to 'Restart', 'Later', or 'TimedOut'.
    .DESCRIPTION
        A Restart flag wins, then TimedOut, and everything else (the Restart later button, Esc, the
        title-bar X, or a result with no recognised flag) is 'Later', the safe outcome.
    .PARAMETER Result
        The Fluence.DialogResult (or raw hashtable) from the restart prompt's Show-FluenceDialog call.
    .OUTPUTS
        System.String
    .NOTES
        Does not require a host application; pure logic helper for Show-FluenceRestartPrompt.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true)]
        [object]$Result
    )

    $values = @{}
    if ($Result -is [System.Collections.IDictionary])
    {
        foreach ($key in $Result.Keys)
        {
            $values[$key] = $Result[$key]
        }
    }
    else
    {
        foreach ($property in $Result.PSObject.Properties)
        {
            $values[$property.Name] = $property.Value
        }
    }

    if ($values['Restart'] -eq $true)
    {
        return 'Restart'
    }
    if ($values['TimedOut'] -eq $true)
    {
        return 'TimedOut'
    }
    return 'Later'
}

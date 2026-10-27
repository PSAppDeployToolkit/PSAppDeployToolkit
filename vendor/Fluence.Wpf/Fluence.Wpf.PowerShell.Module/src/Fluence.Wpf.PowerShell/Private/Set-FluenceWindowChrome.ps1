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


function Set-FluenceWindowChrome
{
    <#
    .SYNOPSIS
        Applies a hashtable of FluenceWindow property names to a window.
    .DESCRIPTION
        Assigns each entry of the chrome hashtable onto the matching property of the window. The
        hashtable holds only the chrome parameters the caller explicitly bound, so unset properties
        keep the window's own defaults. A null hashtable is a no-op.
    .PARAMETER Window
        The window to apply the chrome to.
    .PARAMETER ChromeBound
        A hashtable mapping FluenceWindow property names to values; null is treated as no chrome.
    .NOTES
        Runs on the UI (STA) thread. Sets in-memory window properties only; changes no external state.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Sets in-memory WPF window properties the caller explicitly bound; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.Windows.Window]$Window,

        [Parameter()]
        [hashtable]$ChromeBound
    )

    if ($null -eq $ChromeBound) { return }
    foreach ($key in $ChromeBound.Keys)
    {
        $Window.$key = $ChromeBound[$key]
    }
}

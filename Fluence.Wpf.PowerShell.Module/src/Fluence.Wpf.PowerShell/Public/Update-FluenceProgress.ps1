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


function Update-FluenceProgress
{
    <#
    .SYNOPSIS
        Changes the message, detail, or percentage of an open progress window.
    .DESCRIPTION
        Only the values you pass change; the rest keep their current text. Passing -PercentComplete
        makes the bar determinate (clamped to 0..100); -Indeterminate switches it back. On the inline
        STA host this call also lets the window repaint, so call it at least every few seconds during
        long work even when nothing changed.
    .PARAMETER Handle
        The Fluence.ProgressHandle returned by Show-FluenceProgress.
    .PARAMETER Message
        The new main status line.
    .PARAMETER Detail
        The new detail line. An empty string hides it.
    .PARAMETER PercentComplete
        The new percentage; the bar becomes determinate.
    .PARAMETER Indeterminate
        Switch the bar back to indeterminate.
    .EXAMPLE
        Update-FluenceProgress -Handle $progress -Message 'Installing components' -PercentComplete 40
    .EXAMPLE
        Update-FluenceProgress -Handle $progress -Detail 'Waiting for the service to start' -Indeterminate
    .NOTES
        Runs on the UI thread that owns the window; blocks only for the duration of the update.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Updates a transient in-process window the caller opened; makes no persistent or destructive system change.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [PSTypeName('Fluence.ProgressHandle')]
        [object]$Handle,

        [Parameter()]
        [string]$Message,

        [Parameter()]
        [string]$Detail,

        [Parameter()]
        [double]$PercentComplete,

        [Parameter()]
        [switch]$Indeterminate
    )

    if (-not $Handle.IsOpen)
    {
        throw 'The progress window has already been closed.'
    }

    $next = Resolve-FluenceProgressState -Current $Handle.State -Bound $PSBoundParameters
    foreach ($key in @('Message', 'Detail', 'PercentComplete', 'Indeterminate'))
    {
        $Handle.State[$key] = $next[$key]
    }

    $null = Invoke-OnFluenceUi -Script {
        param($h)
        Set-FluenceProgressState -Parts $h.Parts -State $h.State
        if ($h.Mode -eq 'Inline')
        {
            Invoke-FluenceDispatcherPump
        }
    } -ArgumentList @($Handle)
}

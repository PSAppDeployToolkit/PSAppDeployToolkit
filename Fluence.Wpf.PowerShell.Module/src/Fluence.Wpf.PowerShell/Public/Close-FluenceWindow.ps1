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


function Close-FluenceWindow
{
    <#
    .SYNOPSIS
        Closes a Fluence-hosted window on its UI thread, optionally stashing a result for the host.
    .DESCRIPTION
        Runs on the UI (STA) thread that owns the window. When -Result is supplied it is stored on
        the window's Tag so the window host can return it, then the window is closed.
    .PARAMETER Window
        The System.Windows.Window to close.
    .PARAMETER Result
        An optional value to stash on the window's Tag before closing.
    .EXAMPLE
        Close-FluenceWindow -Window $window
    .EXAMPLE
        Close-FluenceWindow -Window $window -Result 'Saved'
    .NOTES
        Stashes -Result on the window's Tag for the window host to return, then closes on the UI
        thread. A window implies a running Application, so no application is created here.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Closes a window the caller explicitly named; makes no persistent or destructive system change, so ShouldProcess prompting is not appropriate.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [System.Windows.Window]$Window,

        [Parameter()]
        [object]$Result
    )

    $null = Invoke-OnFluenceUi -Script {
        param($window, $result, $hasResult)

        if ($hasResult)
        {
            $window.Tag = $result
        }

        $window.Close()
    } -ArgumentList @($Window, $Result, $PSBoundParameters.ContainsKey('Result'))
}

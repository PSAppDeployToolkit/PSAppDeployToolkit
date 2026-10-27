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


function Show-FluenceWindowCore
{
    <#
    .SYNOPSIS
        Ensures the WPF Application, builds and populates the window, shows it modally, and returns the
        stashed result.
    .DESCRIPTION
        Seeds the three theme slots and accent through Initialize-FluenceApplication, builds the window
        and its content via Set-FluenceWindowContent (rethrowing any user-block error), optionally
        watches OS theme changes for the window's lifetime, and shows the window with ShowDialog. The
        value stashed on the window's Tag (for example by Close-FluenceWindow) is returned wrapped in a
        hashtable so the caller's collection-unwrap is unambiguous.
    .PARAMETER Spec
        The normalized window specification hashtable from Show-FluenceWindow.
    .OUTPUTS
        System.Collections.Hashtable
    .NOTES
        Must run on a UI (STA) thread; call it through Invoke-OnFluenceUi. Blocks until the window
        closes.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Spec
    )

    Initialize-FluenceApplication -Theme $Spec.Theme -Backdrop $Spec.Backdrop -Accent $Spec.Accent

    $state = @{ Error = $null }
    $window = Set-FluenceWindowContent -Spec $Spec -State $state

    try
    {
        if ($null -ne $state.Error)
        {
            throw $state.Error
        }

        if ($Spec.WatchSystemTheme)
        {
            [Fluence.Wpf.SystemThemeWatcher]::Watch($window)
            $unwatch = {
                [Fluence.Wpf.SystemThemeWatcher]::UnWatch($window)
            }.GetNewClosure()
            $window.add_Closed($unwatch)
        }

        $null = $window.ShowDialog()

        if ($null -ne $state.Error)
        {
            throw $state.Error
        }

        # Wrap in a hashtable so the caller's IList-unwrap is unambiguous even when the user's stashed
        # result is itself an array.
        return @{ Result = $window.Tag }
    }
    finally
    {
        # A callback can fail before ShowDialog; WPF still registers the constructed window.
        $window.Close()
    }
}

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


function Set-FluenceBackdrop
{
    <#
    .SYNOPSIS
        Sets the Fluent system backdrop for the process (and optionally a specific window).
    .DESCRIPTION
        Runs on the UI (STA) thread. When -Window is supplied its SystemBackdropType is set, then
        the theme pipeline is re-applied with the requested backdrop and the current theme so the
        computed resources match. The accent intent is preserved.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, None, or Auto. Auto lets the library pick the backdrop the running
        Windows build supports.
    .PARAMETER Window
        An optional System.Windows.Window whose SystemBackdropType is updated. When supplied it
        must be a window owned by the Fluence UI thread.
    .EXAMPLE
        Set-FluenceBackdrop -Backdrop Acrylic
    .EXAMPLE
        Set-FluenceBackdrop -Backdrop Mica -Window $window
    .NOTES
        Uses the calling STA thread or a module-owned STA runspace. An existing host application
        is supported only when the command already runs on its dispatcher thread. When -Window is supplied it
        must be a window owned by the Fluence UI thread.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Mutates transient in-process WPF backdrop resources only; makes no persistent or destructive system change, so ShouldProcess prompting is not appropriate.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop,

        [Parameter()]
        [System.Windows.Window]$Window
    )

    $null = Invoke-OnFluenceUi -Script {
        param($backdropName, $window)

        if ($null -eq [System.Windows.Application]::Current)
        {
            $app = [System.Windows.Application]::new()
            $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
        }

        $bd = ConvertTo-FluenceBackdropType -Backdrop $backdropName
        if ($null -ne $window)
        {
            # SystemBackdropType is a DependencyProperty; it can only be set on the window's own
            # dispatcher thread. CheckAccess is true when the Fluence UI thread running this block
            # owns the window (the supported case). A window from another thread (the documented
            # contract violation) would otherwise throw an opaque cross-thread InvalidOperationException,
            # so fail with a clear, actionable message instead.
            if (-not $window.Dispatcher.CheckAccess())
            {
                throw "The -Window passed to Set-FluenceBackdrop is owned by a different thread than the Fluence UI thread. Pass a window created on the Fluence UI thread (for example one shown via Show-FluenceWindow)."
            }
            $window.SystemBackdropType = $bd
        }

        [Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationThemeManager]::CurrentTheme, $bd)

        # Record the apply so a later dialog without -Backdrop leaves this backdrop alone instead of
        # re-seeding Mica; see Initialize-FluenceApplication.
        [System.AppDomain]::CurrentDomain.SetData($script:ThemeSeededSlot, $true)
    } -ArgumentList @($Backdrop, $Window)
}

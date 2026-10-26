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


function Set-FluenceTheme
{
    <#
    .SYNOPSIS
        Applies a Fluent theme (and optionally a backdrop) to the process-wide WPF resources.
    .DESCRIPTION
        Runs the Fluence theme engine on the UI (STA) thread, rebuilding the computed color and
        brush dictionary for the requested theme. When -Backdrop is omitted the current backdrop
        is preserved. The accent intent is always preserved by the engine.
    .PARAMETER Theme
        Auto, Light, Dark, or HighContrast.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, None, or Auto. When omitted, the current backdrop is kept.
    .PARAMETER UpdateAccent
        Accepted and ignored. Earlier library versions took an updateAccent flag on Apply; the 0.9
        engine always preserves the accent intent, so the switch stays only for contract stability.
    .EXAMPLE
        Set-FluenceTheme -Theme Dark
    .EXAMPLE
        Set-FluenceTheme -Theme Light -Backdrop Acrylic
    .NOTES
        Uses the calling STA thread or a module-owned STA runspace. An existing host application
        is supported only when the command already runs on its dispatcher thread. Omitting -Backdrop keeps the
        current backdrop, and the accent intent is preserved.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Mutates transient in-process WPF theme resources only; makes no persistent or destructive system change, so ShouldProcess prompting is not appropriate.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme,

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop,

        [Parameter()]
        [switch]$UpdateAccent
    )

    $backdropName = $null
    if ($PSBoundParameters.ContainsKey('Backdrop'))
    {
        $backdropName = $Backdrop
    }

    $null = Invoke-OnFluenceUi -Script {
        param($themeName, $backdropName)

        if ($null -eq [System.Windows.Application]::Current)
        {
            $app = [System.Windows.Application]::new()
            $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
        }

        $theme = [Fluence.Wpf.ApplicationTheme]$themeName
        if ([string]::IsNullOrWhiteSpace($backdropName))
        {
            $backdrop = [Fluence.Wpf.ApplicationThemeManager]::CurrentBackdrop
        }
        else
        {
            $backdrop = ConvertTo-FluenceBackdropType -Backdrop $backdropName
        }

        [Fluence.Wpf.ApplicationThemeManager]::Apply($theme, $backdrop)

        # Record the apply so a later dialog without -Theme or -Backdrop leaves this state alone
        # instead of re-seeding the module defaults; see Initialize-FluenceApplication.
        [System.AppDomain]::CurrentDomain.SetData($script:ThemeSeededSlot, $true)
    } -ArgumentList @($Theme, $backdropName)
}

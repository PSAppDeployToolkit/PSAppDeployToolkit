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


function Set-FluenceAccent
{
    <#
    .SYNOPSIS
        Sets the Fluent accent color, either to a custom color or back to the system accent.
    .DESCRIPTION
        Runs the Fluence accent resolver on the UI (STA) thread and re-runs the theme pipeline so
        every accent-derived brush is recomputed. Use -Color to pin the accent ramp to a custom
        color, or -System to reset the accent intent to the OS palette.
    .PARAMETER Color
        The custom accent color (System.Windows.Media.Color or a parseable string).
    .PARAMETER System
        Reset the accent intent to the system (OS) accent.
    .EXAMPLE
        Set-FluenceAccent -Color '#0078D4'
    .EXAMPLE
        Set-FluenceAccent -System
    .NOTES
        Uses the calling STA thread or a module-owned STA runspace. An existing host application
        is supported only when the command already runs on its dispatcher thread.
    #>
    [CmdletBinding(DefaultParameterSetName = 'Custom')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Mutates transient in-process WPF accent resources only; makes no persistent or destructive system change, so ShouldProcess prompting is not appropriate.')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'System',
        Justification = 'Selects the System parameter set; its presence is read via $PSCmdlet.ParameterSetName, which PSScriptAnalyzer cannot statically trace.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'Custom')]
        [System.Windows.Media.Color]$Color,

        [Parameter(Mandatory = $true, ParameterSetName = 'System')]
        [switch]$System
    )

    $null = Invoke-OnFluenceUi -Script {
        param($parameterSetName, $color)

        if ($null -eq [System.Windows.Application]::Current)
        {
            $app = [System.Windows.Application]::new()
            $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
        }

        if ($parameterSetName -eq 'System')
        {
            [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
        }
        else
        {
            [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent([System.Windows.Media.Color]$color)
        }

        # Record the apply so a later dialog without -Accent leaves this accent alone instead of
        # resetting it to the system accent; see Initialize-FluenceApplication.
        [System.AppDomain]::CurrentDomain.SetData($script:ThemeSeededSlot, $true)
    } -ArgumentList @($PSCmdlet.ParameterSetName, $Color)
}

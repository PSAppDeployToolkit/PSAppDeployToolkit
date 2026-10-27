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


#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Opt-in UI tests for the Task 1 theming helpers. Every It is tagged UI and skips unless
# FLUENCE_PS_UI=1, mirroring the library's opt-in Screenshots pattern and the render-test gate.
# These exercise the process-wide theme statics through the public helpers, so they touch
# AppDomain-wide state and must run on a host that owns (or can establish) a WPF Application.

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force
}

# Not UI-tagged: the stickiness contract is decided by the parameter surface, which is readable
# without a WPF Application. A default on -Theme or -Backdrop would make every dialog re-seed the
# module defaults and silently discard a Set-FluenceTheme or Set-FluenceBackdrop pin, which is the
# behaviour both how-to guides promise does not happen.
Describe 'Theme and backdrop stickiness contract' {
    It '<Cmdlet> leaves -Theme and -Backdrop without a default' -ForEach @(
        @{ Cmdlet = 'Show-FluenceDialog' }
        @{ Cmdlet = 'Show-FluenceMessage' }
        @{ Cmdlet = 'Get-FluenceInput' }
        @{ Cmdlet = 'Show-FluenceListSelection' }
        @{ Cmdlet = 'Show-FluenceRestartPrompt' }
        @{ Cmdlet = 'Show-FluenceProgress' }
        @{ Cmdlet = 'Show-FluenceWindow' }
    ) {
        $ast = (Get-Command $Cmdlet).ScriptBlock.Ast
        foreach ($name in @('Theme', 'Backdrop'))
        {
            $parameter = $ast.Body.ParamBlock.Parameters |
                Where-Object { $_.Name.VariablePath.UserPath -eq $name }
            $parameter | Should -Not -BeNullOrEmpty -Because "$Cmdlet should expose -$name"
            $parameter.DefaultValue | Should -BeNullOrEmpty -Because "a default on -$name re-seeds the theme on every call"
        }
    }
    It 'Set-FluenceBackdrop accepts the same backdrop names as every other backdrop parameter' {
        $expected = (Get-Command Show-FluenceDialog).Parameters['Backdrop'].Attributes.ValidValues
        (Get-Command Set-FluenceBackdrop).Parameters['Backdrop'].Attributes.ValidValues |
            Should -Be $expected
    }
}

Describe 'Fluence theming helpers' -Tag UI {

    It 'a dialog without -Theme does not disturb an applied theme' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        Set-FluenceTheme -Theme Dark -Backdrop None

        # Initialize-FluenceApplication is the whole theming step a dialog performs before its window
        # is built, so calling it with no theme is exactly what a -Theme-less dialog does, without
        # opening a window.
        & (Get-Module Fluence.Wpf.PowerShell) { Initialize-FluenceApplication }

        $after = Get-FluenceTheme
        $after.CurrentTheme | Should -Be ([Fluence.Wpf.ApplicationTheme]::Dark)
        $after.CurrentBackdrop.ToString() | Should -Be 'None'
    }

    It 'a dialog with -Theme applies it' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        Set-FluenceTheme -Theme Dark -Backdrop None

        & (Get-Module Fluence.Wpf.PowerShell) { Initialize-FluenceApplication -Theme 'Light' }

        $after = Get-FluenceTheme
        $after.CurrentTheme | Should -Be ([Fluence.Wpf.ApplicationTheme]::Light)
        $after.CurrentBackdrop.ToString() | Should -Be 'None'
    }


    It 'Set-FluenceTheme changes CurrentTheme and preserves an omitted backdrop' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        Set-FluenceTheme -Theme Dark -Backdrop None

        $afterDark = Get-FluenceTheme
        $afterDark.CurrentTheme | Should -Be ([Fluence.Wpf.ApplicationTheme]::Dark)
        $afterDark.CurrentBackdrop.ToString() | Should -Be 'None'

        Set-FluenceTheme -Theme Light

        $afterLight = Get-FluenceTheme
        $afterLight.CurrentTheme | Should -Be ([Fluence.Wpf.ApplicationTheme]::Light)
        $afterLight.CurrentBackdrop.ToString() | Should -Be 'None'
    }

    It 'Set-FluenceAccent round-trips a custom color and -System does not throw' -Skip:($env:FLUENCE_PS_UI -ne '1') {
        $red = [System.Windows.Media.Color]::FromRgb(255, 0, 0)
        Set-FluenceAccent -Color $red

        $accent = [Fluence.Wpf.ApplicationAccentColorManager]::SystemAccentColor
        $accent.R | Should -Be 255
        $accent.G | Should -Be 0
        $accent.B | Should -Be 0

        { Set-FluenceAccent -System } | Should -Not -Throw
    }
}

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

#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# The four real severities take their glyph and brush key from the library helper on InfoBar, so the
# assertions read the expected values from the same helper rather than hard-coding glyph literals.
# The library has moved the InfoBar glyphs once already (to the WinUI canonical set); a literal here
# would only pin the module to one library build.

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
    $script:Resolve = { param($icon) & (Get-Module Fluence.Wpf.PowerShell) { param($i) Get-FluenceSeverityIcon -Icon $i } $icon }
    $script:Expected = {
        param($severityName)
        $severity = [Fluence.Wpf.InfoBarSeverity]$severityName
        @{
            Glyph    = [Fluence.Wpf.Controls.InfoBar]::GetSeverityGlyph($severity)
            BrushKey = [Fluence.Wpf.Controls.InfoBar]::GetSeverityBrushKey($severity)
        }
    }
}

Describe 'Get-FluenceSeverityIcon' {
    It 'returns $null for None' {
        & $script:Resolve 'None' | Should -BeNullOrEmpty
    }
    It 'returns $null for an empty icon name' {
        & $script:Resolve '' | Should -BeNullOrEmpty
    }
    It 'maps Info to the Informational glyph and brush' {
        $r = & $script:Resolve 'Info'
        $e = & $script:Expected 'Informational'
        $r.Glyph | Should -Be $e.Glyph
        $r.BrushKey | Should -Be $e.BrushKey
    }
    It 'maps <Icon> to the InfoBar <Severity> glyph and brush' -ForEach @(
        @{ Icon = 'Success'; Severity = 'Success' }
        @{ Icon = 'Warning'; Severity = 'Warning' }
        @{ Icon = 'Error'; Severity = 'Error' }
    ) {
        $r = & $script:Resolve $Icon
        $e = & $script:Expected $Severity
        $r.Glyph | Should -Not -BeNullOrEmpty
        $r.Glyph | Should -Be $e.Glyph
        $r.BrushKey | Should -Be $e.BrushKey
    }
    It 'maps Question to the Help glyph with the Informational brush' {
        $r = & $script:Resolve 'Question'
        $e = & $script:Expected 'Informational'
        $r.Glyph | Should -Be ([string][char]0xE897)
        $r.BrushKey | Should -Be $e.BrushKey
    }
}

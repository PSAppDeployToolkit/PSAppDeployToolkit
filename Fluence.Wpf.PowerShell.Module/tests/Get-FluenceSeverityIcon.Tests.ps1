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

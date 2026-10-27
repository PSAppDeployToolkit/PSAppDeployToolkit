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

# Caller-thread contract of Show-FluenceMessage: preset resolution and -DefaultButton validation run
# before Show-FluenceDialog is called, so these cases open no window. The timeout-to-default-button
# mapping itself is pure logic in Resolve-FluenceClickedButton and is pinned in its own test file.

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Show-FluenceMessage parameter validation' {
    It 'rejects a -DefaultButton that is not part of the <Preset> preset' -ForEach @(
        @{ Preset = 'YesNo'; Default = 'Cancel' }
        @{ Preset = 'OK'; Default = 'No' }
        @{ Preset = 'OKCancel'; Default = 'Yes' }
    ) {
        { Show-FluenceMessage -Message 'x' -Buttons $Preset -DefaultButton $Default } |
            Should -Throw "*-DefaultButton '$Default' is not a button of the '$Preset' preset*"
    }
    It 'rejects a -DefaultButton name outside the known button names' {
        { Show-FluenceMessage -Message 'x' -Buttons YesNo -DefaultButton Maybe } |
            Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
    }
    It 'rejects -Countdown without -Timeout' {
        { Show-FluenceMessage -Message 'x' -Countdown } | Should -Throw '*-Countdown requires -Timeout*'
    }
    It 'rejects an -Image with an unsupported scheme before any UI work' {
        { Show-FluenceMessage -Message 'x' -Image 'https://example.com/logo.png' } | Should -Throw "*unsupported scheme 'https'*"
    }
    It 'rejects an unknown -Position' {
        { Show-FluenceMessage -Message 'x' -Position Middle } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
    }
    # -Icon None is the image-led shape used by the dialogs how-to: an
    # image and text with no severity glyph. The wrapper forwards Icon verbatim to Show-FluenceDialog,
    # which has always accepted None, so this set must match or those call sites fail at binding.
    It 'accepts -Icon None, which draws no severity glyph' {
        (Get-Command Show-FluenceMessage).Parameters['Icon'].Attributes.ValidValues | Should -Contain 'None'
        & (Get-Module Fluence.Wpf.PowerShell) { Get-FluenceSeverityIcon -Icon 'None' } | Should -BeNullOrEmpty
    }
}

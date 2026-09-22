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
    # -Icon None is the image-led shape that examples/ImageDialog.ps1 and the dialogs how-to use: an
    # image and text with no severity glyph. The wrapper forwards Icon verbatim to Show-FluenceDialog,
    # which has always accepted None, so this set must match or those call sites fail at binding.
    It 'accepts -Icon None, which draws no severity glyph' {
        (Get-Command Show-FluenceMessage).Parameters['Icon'].Attributes.ValidValues | Should -Contain 'None'
        & (Get-Module Fluence.Wpf.PowerShell) { Get-FluenceSeverityIcon -Icon 'None' } | Should -BeNullOrEmpty
    }
}

#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Caller-thread contract of Show-FluenceDialog: everything that is validated before any UI work is
# dispatched. These cases never reach Invoke-OnFluenceUi, so no window opens and they belong to the
# logic lane. The render path is covered by Show-FluenceDialog.Render.Tests.ps1.

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Show-FluenceDialog parameter validation' {
    Context 'Timeout and Countdown' {
        It 'rejects -Countdown without -Timeout before any UI work' {
            { Show-FluenceDialog -Message 'x' -Countdown } | Should -Throw '*-Countdown requires -Timeout*'
        }
        It 'rejects a -Timeout of <Value>' -ForEach @(
            @{ Value = 0 }
            @{ Value = -5 }
            @{ Value = 86401 }
        ) {
            { Show-FluenceDialog -Message 'x' -Timeout $Value } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
    Context 'Buttons' {
        It 'rejects an unsupported button item type before any UI work' {
            { Show-FluenceDialog -Message 'x' -Buttons @(42) } | Should -Throw '*unsupported item type*'
        }
    }
    Context 'Image and layout' {
        It 'rejects an -Image with the <Scheme> scheme before any UI work' -ForEach @(
            @{ Scheme = 'https'; Value = 'https://example.com/logo.png' }
            @{ Scheme = 'data'; Value = 'data:image/png;base64,iVBORw0KGgo=' }
        ) {
            { Show-FluenceDialog -Message 'x' -Image $Value } | Should -Throw "*unsupported scheme '$Scheme'*"
        }
        It 'rejects a missing -Image file before any UI work' {
            { Show-FluenceDialog -Message 'x' -Image (Join-Path $TestDrive 'nope.png') } | Should -Throw '*not found*'
        }
        It 'rejects an unknown -Position' {
            { Show-FluenceDialog -Message 'x' -Position Middle } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
        It 'rejects an unknown -MessageAlignment' {
            { Show-FluenceDialog -Message 'x' -MessageAlignment Justify } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

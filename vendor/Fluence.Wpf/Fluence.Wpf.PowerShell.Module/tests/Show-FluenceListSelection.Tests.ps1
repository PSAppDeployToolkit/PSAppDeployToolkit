#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Logic-lane contract of Show-FluenceListSelection: validation that fails before any window opens.
# The live dialog is rendered in Show-FluenceListSelection.Render.Tests.ps1.

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Show-FluenceListSelection parameter validation' {
    It 'requires at least one item' {
        { Show-FluenceListSelection -Items @() } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
    }
    It 'rejects -Countdown without -Timeout' {
        { Show-FluenceListSelection -Items 'A', 'B' -Countdown } | Should -Throw '*-Countdown requires -Timeout*'
    }
}

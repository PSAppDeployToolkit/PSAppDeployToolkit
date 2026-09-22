#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Caller-thread contract of Show-FluenceProgress: parameter validation that fails before any window
# opens. The live window path (open, update, close on each host shape) is in
# Show-FluenceProgress.Render.Tests.ps1.

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Show-FluenceProgress parameter validation' {
    It 'requires a message' {
        { Show-FluenceProgress -Message '' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
    }
    It 'rejects a width of <Value>' -ForEach @(
        @{ Value = 10 }
        @{ Value = 5000 }
    ) {
        { Show-FluenceProgress -Message 'x' -Width $Value } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
    }
    It 'rejects an unknown position' {
        { Show-FluenceProgress -Message 'x' -Position Middle } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
    }
}

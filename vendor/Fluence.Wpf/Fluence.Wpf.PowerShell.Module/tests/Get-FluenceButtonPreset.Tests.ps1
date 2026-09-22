# Imported at discovery time, not in BeforeAll: InModuleScope below needs the module loaded
# while Pester is still discovering the cases.
Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
# The subject is a module-private helper. AGENTS.md forbids dot-sourcing a copy of one, so the
# cases run inside the imported module instead, where the private functions already resolve.
InModuleScope 'Fluence.Wpf.PowerShell' {
    Describe 'Get-FluenceButtonPreset' {
        It 'maps OKCancel to two buttons with OK default and Cancel cancel' {
            $b = Get-FluenceButtonPreset -Preset OKCancel
            $b.Count | Should -Be 2
            ($b | Where-Object Text -eq 'OK').IsDefault | Should -BeTrue
            ($b | Where-Object Text -eq 'Cancel').IsCancel | Should -BeTrue
        }
        It 'maps YesNo to Yes/No' {
            (Get-FluenceButtonPreset -Preset YesNo | ForEach-Object Text) | Should -Be @('Yes', 'No')
        }
    }

}

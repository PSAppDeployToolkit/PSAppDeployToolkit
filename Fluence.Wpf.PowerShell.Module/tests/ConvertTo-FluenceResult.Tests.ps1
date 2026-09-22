# Imported at discovery time, not in BeforeAll: InModuleScope below needs the module loaded
# while Pester is still discovering the cases.
Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

# The subject is a module-private helper. AGENTS.md forbids dot-sourcing a copy of one, so the
# cases run inside the imported module instead, where the private functions already resolve.
InModuleScope 'Fluence.Wpf.PowerShell' {
    Describe 'ConvertTo-FluenceResult' {
        It 'projects the hashtable to a PSCustomObject with the same keys' {
            $h = @{ User = 'bob'; OK = $true; Cancel = $false; Cancelled = $false }
            $o = ConvertTo-FluenceResult -Result $h
            $o.User | Should -Be 'bob'
            $o.OK | Should -BeTrue
            $o.Cancelled | Should -BeFalse
        }
        It 'tags the object as Fluence.DialogResult' {
            (ConvertTo-FluenceResult -Result @{ Cancelled = $true }).PSObject.TypeNames[0] |
                Should -Be 'Fluence.DialogResult'
        }
        It 'keeps the Fluence.DialogResult type even when a result key is named PSTypeName' {
            $o = ConvertTo-FluenceResult -Result @{ PSTypeName = 'Hacked'; Cancelled = $true }
            $o.PSObject.TypeNames[0] | Should -Be 'Fluence.DialogResult'
        }
    }

}

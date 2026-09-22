# Imported at discovery time, not in BeforeAll: InModuleScope below needs the module loaded
# while Pester is still discovering the cases.
Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

# The subject is a module-private helper. AGENTS.md forbids dot-sourcing a copy of one, so the
# cases run inside the imported module instead, where the private functions already resolve.
InModuleScope 'Fluence.Wpf.PowerShell' {
    Describe 'ConvertTo-FluencePromptList' {
        It 'turns a bare string into a Text prompt' {
            $list = ConvertTo-FluencePromptList -InputObject @('Your name?')
            $list[0].PSObject.TypeNames[0] | Should -Be 'Fluence.Prompt'
            $list[0].InputType | Should -Be 'Text'
            $list[0].Message | Should -Be 'Your name?'
        }
        It 'passes a Fluence.Prompt through' {
            $p = New-FluencePrompt -Name A -Message 'a'
            (ConvertTo-FluencePromptList -InputObject @($p))[0].Name | Should -Be 'A'
        }
    }

    Describe 'ConvertTo-FluenceButtonList' {
        It 'turns a bare string into a button' {
            $list = ConvertTo-FluenceButtonList -InputObject @('OK')
            $list[0].PSObject.TypeNames[0] | Should -Be 'Fluence.Button'
            $list[0].Text | Should -Be 'OK'
        }
        It "bare 'Cancel' becomes IsCancel=true" {
            $list = ConvertTo-FluenceButtonList -InputObject @('Cancel')
            $list[0].Text | Should -Be 'Cancel'
            $list[0].IsCancel | Should -Be $true
        }
        It "bare 'cancel' (lowercase) becomes IsCancel=true (case-insensitive)" {
            $list = ConvertTo-FluenceButtonList -InputObject @('cancel')
            $list[0].IsCancel | Should -Be $true
        }
        It "bare 'OK' stays IsCancel=false" {
            $list = ConvertTo-FluenceButtonList -InputObject @('OK')
            $list[0].IsCancel | Should -Be $false
        }
        It 'explicit Fluence.Button with IsCancel=false passes through unchanged' {
            $btn = New-FluenceButton -Text 'Cancel'
            $list = ConvertTo-FluenceButtonList -InputObject @($btn)
            $list[0].IsCancel | Should -Be $false
        }
    }

}

# Imported at discovery time, not in BeforeAll: InModuleScope below needs the module loaded
# while Pester is still discovering the cases.
Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

# The subject is a module-private helper. AGENTS.md forbids dot-sourcing a copy of one, so the
# cases run inside the imported module instead, where the private functions already resolve.
InModuleScope 'Fluence.Wpf.PowerShell' {
    Describe 'Test-FluenceInput' {
        It 'fails an empty required value' {
            $p = New-FluencePrompt -Name U -Message 'u' -ValidateNotEmpty
            $r = Test-FluenceInput -Prompts @($p) -Values @{ U = '' }
            $r.IsValid | Should -BeFalse
            $r.Message | Should -Match 'U'
        }
        It 'passes a present required value' {
            $p = New-FluencePrompt -Name U -Message 'u' -ValidateNotEmpty
            (Test-FluenceInput -Prompts @($p) -Values @{ U = 'abc' }).IsValid | Should -BeTrue
        }
        It 'enforces a pattern' {
            $p = New-FluencePrompt -Name Code -Message 'c' -ValidatePattern '^\d{3}$'
            (Test-FluenceInput -Prompts @($p) -Values @{ Code = '12' }).IsValid | Should -BeFalse
            (Test-FluenceInput -Prompts @($p) -Values @{ Code = '123' }).IsValid | Should -BeTrue
        }
        It 'runs a ValidateScript' {
            $p = New-FluencePrompt -Name N -Message 'n' -ValidateScript { param($v) [int]$v -gt 5 }
            (Test-FluenceInput -Prompts @($p) -Values @{ N = '3' }).IsValid | Should -BeFalse
            (Test-FluenceInput -Prompts @($p) -Values @{ N = '9' }).IsValid | Should -BeTrue
        }
        It 'fails validation when the script emits extra output before a false final result' {
            # A multi-statement validator that does not suppress intermediate output emits an array;
            # the meaningful result is the LAST element, not the truthiness of the whole stream.
            $p = New-FluencePrompt -Name N -Message 'n' -ValidateScript { param($v) $v; [int]$v -gt 5 }
            (Test-FluenceInput -Prompts @($p) -Values @{ N = '3' }).IsValid | Should -BeFalse
        }
        It 'passes validation when extra output precedes a true final result' {
            $p = New-FluencePrompt -Name N -Message 'n' -ValidateScript { param($v) $v; [int]$v -gt 5 }
            (Test-FluenceInput -Prompts @($p) -Values @{ N = '9' }).IsValid | Should -BeTrue
        }
    }

}

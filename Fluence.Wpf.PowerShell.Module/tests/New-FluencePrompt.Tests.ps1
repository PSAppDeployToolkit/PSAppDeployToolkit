BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'New-FluencePrompt' {
    Context 'Output contract' {
        It 'tags the object as Fluence.Prompt' {
            $p = New-FluencePrompt -Name City -Message 'City?'
            $p.PSObject.TypeNames[0] | Should -Be 'Fluence.Prompt'
        }
        It 'defaults InputType to Text' {
            (New-FluencePrompt -Name City -Message 'City?').InputType | Should -Be 'Text'
        }
        It 'carries Name, Message and DefaultValue' {
            $p = New-FluencePrompt -Name City -Message 'City?' -DefaultValue 'Leeds'
            $p.Name | Should -Be 'City'
            $p.Message | Should -Be 'City?'
            $p.DefaultValue | Should -Be 'Leeds'
        }
    }
    Context 'Input validation' {
        It 'rejects an unknown InputType' {
            { New-FluencePrompt -Name X -Message 'x' -InputType Nope } | Should -Throw
        }
        It 'requires Choice prompts to supply a ValidateSet' {
            { New-FluencePrompt -Name X -Message 'x' -InputType Choice } | Should -Throw '*ValidateSet*'
        }
        It 'throws a clear error when a Number DefaultValue is not coercible' {
            { New-FluencePrompt -Name X -Message 'x' -InputType Number -DefaultValue 'n/a' } |
                Should -Throw '*Number*'
        }
        It 'throws a clear error when a Date DefaultValue is not coercible' {
            { New-FluencePrompt -Name X -Message 'x' -InputType Date -DefaultValue 'soon' } |
                Should -Throw '*Date*'
        }
        It 'coerces a Number DefaultValue to [double]' {
            (New-FluencePrompt -Name X -Message 'x' -InputType Number -DefaultValue '42').DefaultValue |
                Should -BeOfType ([double])
        }
        It 'coerces a Checkbox string DefaultValue to [bool] without the non-empty-string footgun' {
            (New-FluencePrompt -Name X -Message 'x' -InputType Checkbox -DefaultValue 'false').DefaultValue |
                Should -BeFalse
        }
    }
    Context 'List prompts' {
        It 'requires List prompts to supply a ValidateSet' {
            { New-FluencePrompt -Name X -Message 'x' -InputType List } | Should -Throw '*ValidateSet*'
        }
        It 'rejects -MultiSelect on a non-List prompt' {
            { New-FluencePrompt -Name X -Message 'x' -InputType Choice -ValidateSet 'a', 'b' -MultiSelect } | Should -Throw '*-MultiSelect applies only to List prompts*'
        }
        It 'carries MultiSelect as a boolean on the spec' {
            (New-FluencePrompt -Name X -Message 'x' -InputType List -ValidateSet 'a', 'b' -MultiSelect).MultiSelect | Should -BeTrue
            (New-FluencePrompt -Name X -Message 'x' -InputType List -ValidateSet 'a', 'b').MultiSelect | Should -BeFalse
        }
    }
}

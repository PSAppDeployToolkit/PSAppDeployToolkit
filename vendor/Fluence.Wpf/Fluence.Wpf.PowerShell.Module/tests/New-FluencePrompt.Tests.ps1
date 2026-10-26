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

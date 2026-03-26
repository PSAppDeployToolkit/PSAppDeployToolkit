BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Convert-ADTValuesFromRemainingArguments' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
    }

    Context 'Return Type' {
        It 'Returns a Generic Dictionary[String, Object]' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Name', 'foo')
            $result | Should -BeOfType ([System.Collections.Generic.Dictionary[System.String, System.Object]])
        }

        It 'Returns a non-null result for valid input' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Name', 'foo')
            $result | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Named Parameters' {
        It 'Converts a single -Name value pair to a dictionary entry' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Name', 'foo')
            $result['Name'] | Should -Be 'foo'
        }

        It 'Strips the leading dash from the parameter name' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Foo', 'bar')
            $result.ContainsKey('Foo') | Should -BeTrue
            $result.ContainsKey('-Foo') | Should -BeFalse
        }

        It 'Converts multiple named parameters into separate dictionary entries' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Name', 'alice', '-Count', '5')
            $result['Name'] | Should -Be 'alice'
            $result['Count'] | Should -Be '5'
        }

        It 'Does not throw for a well-formed named-parameter list' {
            { Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Key', 'value') } | Should -Not -Throw
        }
    }

    Context 'Null and Empty Input' {
        It 'Returns an empty dictionary for null input' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments $null
            $result | Should -BeOfType ([System.Collections.Generic.Dictionary[System.String, System.Object]])
            $result.Count | Should -Be 0
        }

        It 'Returns an empty dictionary for an empty list' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments @()
            $result.Count | Should -Be 0
        }

        It 'Does not throw for null input' {
            { Convert-ADTValuesFromRemainingArguments -RemainingArguments $null } | Should -Not -Throw
        }

        It 'Does not throw for an empty list' {
            { Convert-ADTValuesFromRemainingArguments -RemainingArguments @() } | Should -Not -Throw
        }
    }
}

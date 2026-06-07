BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Convert-ADTValuesFromRemainingArguments' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Returns a Dictionary[String, Object]' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Name', 'Alice')
            $result | Should -BeOfType ([System.Collections.Generic.Dictionary[System.String, System.Object]])
        }
        It 'Parses a named parameter with a value' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-Name', 'Alice')
            $result.ContainsKey('Name') | Should -BeTrue
            $result['Name'] | Should -Be 'Alice'
        }
        It 'Parses multiple named parameters' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments @('-First', 'Alice', '-Last', 'Smith')
            $result['First'] | Should -Be 'Alice'
            $result['Last'] | Should -Be 'Smith'
        }
        It 'Returns an empty dictionary when RemainingArguments is null' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments $null
            $result | Should -BeOfType ([System.Collections.Generic.Dictionary[System.String, System.Object]])
            $result.Count | Should -Be 0
        }
        It 'Returns an empty dictionary when RemainingArguments is an empty collection' {
            $result = Convert-ADTValuesFromRemainingArguments -RemainingArguments @()
            $result.Count | Should -Be 0
        }
    }

    Context 'Input Validation' {
        It 'Throws when a non-list object is passed that cannot be converted to IReadOnlyList' {
            # An unconvertible type triggers a ParameterBindingException.
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Convert-ADTValuesFromRemainingArguments -RemainingArguments ([System.IO.FileInfo]::new('C:\x.txt')) } | Should @shouldParams
        }
    }
}

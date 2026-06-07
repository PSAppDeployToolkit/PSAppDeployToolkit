BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Select-ADTUniqueObject' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'De-duplicates an integer array' {
            $result = 1, 2, 2, 3 | Select-ADTUniqueObject
            $result | Should -HaveCount 3
            $result | Should -Contain 1
            $result | Should -Contain 2
            $result | Should -Contain 3
        }
        It 'De-duplicates strings case-insensitively by default' {
            $result = 'alpha', 'Beta', 'beta', 'GAMMA' | Select-ADTUniqueObject
            $result | Should -HaveCount 3
        }
        It 'Preserves distinct-cased strings when -CaseSensitivity Ordinal is specified' {
            $result = 'alpha', 'Alpha', 'ALPHA' | Select-ADTUniqueObject -CaseSensitivity Ordinal
            $result | Should -HaveCount 3
            $result | Should -Contain 'alpha'
            $result | Should -Contain 'Alpha'
            $result | Should -Contain 'ALPHA'
        }
        It 'Returns a single item when all inputs are the same value' {
            $result = 42, 42, 42 | Select-ADTUniqueObject
            $result | Should -HaveCount 1
            $result | Should -Be 42
        }
        It 'Accepts array input via -InputObject directly' {
            $result = Select-ADTUniqueObject -InputObject @(1, 1, 2)
            $result | Should -HaveCount 2
        }
        It 'Returns nothing when only null or whitespace items are supplied' {
            $result = $null, '', '   ' | Select-ADTUniqueObject
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentTransformationError when CaseSensitivity is an invalid value' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Select-ADTUniqueObject'
            }
            { 'a', 'b' | Select-ADTUniqueObject -CaseSensitivity 'NotAValue' } | Should @shouldParams
        }
    }
}

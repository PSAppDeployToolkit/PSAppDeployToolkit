BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTEnvironmentVariable' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        BeforeEach {
            [System.Environment]::SetEnvironmentVariable('PSADT_TEST_GETVAR', 'TestValue123', 'Process')
        }
        AfterEach {
            [System.Environment]::SetEnvironmentVariable('PSADT_TEST_GETVAR', $null, 'Process')
        }

        It 'Should return the value of an existing process-scope variable without -Target' {
            $result = Get-ADTEnvironmentVariable -Variable 'PSADT_TEST_GETVAR'
            $result | Should -Be 'TestValue123'
        }

        It 'Should return a [System.String] for an existing variable' {
            $result = Get-ADTEnvironmentVariable -Variable 'PSADT_TEST_GETVAR'
            $result | Should -BeOfType ([System.String])
        }

        It 'Should return the value of an existing process-scope variable with -Target Process' {
            $result = Get-ADTEnvironmentVariable -Variable 'PSADT_TEST_GETVAR' -Target Process
            $result | Should -Be 'TestValue123'
        }

        It 'Should return null or empty for a variable that does not exist' {
            [System.Environment]::SetEnvironmentVariable('PSADT_TEST_GETVAR', $null, 'Process')
            $result = Get-ADTEnvironmentVariable -Variable 'PSADT_TEST_NONEXISTENT_ZZZZZZ'
            $result | Should -BeNullOrEmpty
        }

        It 'Should not throw when called with a valid variable name' {
            { Get-ADTEnvironmentVariable -Variable 'PSADT_TEST_GETVAR' } | Should -Not -Throw
        }

        It 'Should skip Machine-scope retrieval (requires elevation or persists state)' {
            Set-ItResult -Skipped -Because 'Machine-scope reads require elevation or persist system state'
        }

        It 'Should skip User-scope retrieval (requires active user session)' {
            Set-ItResult -Skipped -Because 'User-scope reads require an active logged-on user session'
        }
    }

    Context 'Input Validation' {
        It 'Should throw ParameterArgumentValidationError when -Variable is null, empty, or whitespace' -ForEach @(
            @{ BadValue = $null }
            @{ BadValue = '' }
            @{ BadValue = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Get-ADTEnvironmentVariable'
            }
            { Get-ADTEnvironmentVariable -Variable $BadValue } | Should @shouldParams
        }

        It 'Should require -Variable (Mandatory = true)' {
            $isMandatory = (Get-Command Get-ADTEnvironmentVariable).Parameters['Variable'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory
            $isMandatory | Should -Contain $true
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of System.String' {
            $outputTypes = (Get-Command Get-ADTEnvironmentVariable).OutputType.Type
            $outputTypes | Should -Contain ([System.String])
        }
    }
}

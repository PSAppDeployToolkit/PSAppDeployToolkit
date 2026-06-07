BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTEnvironmentVariable' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        BeforeEach {
            [System.Environment]::SetEnvironmentVariable('PSADT_TEST_RMVAR', 'RemoveMe', 'Process')
        }
        AfterEach {
            [System.Environment]::SetEnvironmentVariable('PSADT_TEST_RMVAR', $null, 'Process')
        }

        It 'Should remove an existing process-scope variable without -Target' {
            Remove-ADTEnvironmentVariable -Variable 'PSADT_TEST_RMVAR'
            [System.Environment]::GetEnvironmentVariable('PSADT_TEST_RMVAR', 'Process') | Should -BeNullOrEmpty
        }

        It 'Should remove an existing process-scope variable with -Target Process' {
            Remove-ADTEnvironmentVariable -Variable 'PSADT_TEST_RMVAR' -Target Process
            [System.Environment]::GetEnvironmentVariable('PSADT_TEST_RMVAR', 'Process') | Should -BeNullOrEmpty
        }

        It 'Should not throw when removing a variable that exists' {
            { Remove-ADTEnvironmentVariable -Variable 'PSADT_TEST_RMVAR' } | Should -Not -Throw
        }

        It 'Should not throw when removing a variable that does not exist' {
            { Remove-ADTEnvironmentVariable -Variable 'PSADT_TEST_NONEXISTENT_ZZZZZZ' } | Should -Not -Throw
        }

        It 'Should produce no output (void) when removing a variable' {
            $result = Remove-ADTEnvironmentVariable -Variable 'PSADT_TEST_RMVAR'
            $result | Should -BeNullOrEmpty
        }

        It 'Should skip Machine-scope remove (requires elevation or persists state)' {
            Set-ItResult -Skipped -Because 'Machine-scope removes require elevation and persist system state'
        }

        It 'Should skip User-scope remove (requires active user session)' {
            Set-ItResult -Skipped -Because 'User-scope removes require an active logged-on user session'
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
                ErrorId       = 'ParameterArgumentValidationError,Remove-ADTEnvironmentVariable'
            }
            { Remove-ADTEnvironmentVariable -Variable $BadValue } | Should @shouldParams
        }

        It 'Should require -Variable (Mandatory = true)' {
            $isMandatory = (Get-Command Remove-ADTEnvironmentVariable).Parameters['Variable'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory
            $isMandatory | Should -Contain $true
        }
    }

    Context 'Metadata' {
        It 'Should support -WhatIf (ShouldProcess)' {
            $cmd = Get-Command Remove-ADTEnvironmentVariable
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }
}

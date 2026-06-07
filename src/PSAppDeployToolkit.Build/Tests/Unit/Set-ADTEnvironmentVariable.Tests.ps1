BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Set-ADTEnvironmentVariable' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        AfterEach {
            [System.Environment]::SetEnvironmentVariable('PSADT_TEST_SETVAR', $null, 'Process')
        }

        It 'Should set a new process-scope variable without -Target' {
            Set-ADTEnvironmentVariable -Variable 'PSADT_TEST_SETVAR' -Value 'Hello'
            [System.Environment]::GetEnvironmentVariable('PSADT_TEST_SETVAR', 'Process') | Should -Be 'Hello'
        }

        It 'Should set a new process-scope variable with -Target Process' {
            Set-ADTEnvironmentVariable -Variable 'PSADT_TEST_SETVAR' -Value 'WorldProcess' -Target Process
            [System.Environment]::GetEnvironmentVariable('PSADT_TEST_SETVAR', 'Process') | Should -Be 'WorldProcess'
        }

        It 'Should overwrite an existing process-scope variable' {
            [System.Environment]::SetEnvironmentVariable('PSADT_TEST_SETVAR', 'Original', 'Process')
            Set-ADTEnvironmentVariable -Variable 'PSADT_TEST_SETVAR' -Value 'Updated'
            [System.Environment]::GetEnvironmentVariable('PSADT_TEST_SETVAR', 'Process') | Should -Be 'Updated'
        }

        It 'Should overwrite (not concatenate) at process scope with -Append, since append/remove semantics apply only to User/Machine scope' {
            # EnvironmentUtilities.SetEnvironmentVariable short-circuits the Process target to a plain
            # set (ignoring -Append/-Remove/-Expandable), so -Append overwrites at process scope.
            [System.Environment]::SetEnvironmentVariable('PSADT_TEST_SETVAR', 'First', 'Process')
            Set-ADTEnvironmentVariable -Variable 'PSADT_TEST_SETVAR' -Value ';Second' -Append
            [System.Environment]::GetEnvironmentVariable('PSADT_TEST_SETVAR', 'Process') | Should -Be ';Second'
        }

        It 'Should not throw when setting a process-scope variable' {
            { Set-ADTEnvironmentVariable -Variable 'PSADT_TEST_SETVAR' -Value 'NoThrow' } | Should -Not -Throw
        }

        It 'Should produce no output (void) when setting a variable' {
            $result = Set-ADTEnvironmentVariable -Variable 'PSADT_TEST_SETVAR' -Value 'VoidCheck'
            $result | Should -BeNullOrEmpty
        }

        It 'Should skip Machine-scope set (requires elevation or persists state)' {
            Set-ItResult -Skipped -Because 'Machine-scope writes require elevation and persist system state'
        }

        It 'Should skip User-scope set (requires active user session)' {
            Set-ItResult -Skipped -Because 'User-scope writes require an active logged-on user session'
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
                ErrorId       = 'ParameterArgumentValidationError,Set-ADTEnvironmentVariable'
            }
            { Set-ADTEnvironmentVariable -Variable $BadValue -Value 'x' } | Should @shouldParams
        }

        It 'Should throw ParameterArgumentValidationError when -Value is null, empty, or whitespace' -ForEach @(
            @{ BadValue = $null }
            @{ BadValue = '' }
            @{ BadValue = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Set-ADTEnvironmentVariable'
            }
            { Set-ADTEnvironmentVariable -Variable 'PSADT_TEST_SETVAR' -Value $BadValue } | Should @shouldParams
        }

        It 'Should require -Variable (Mandatory = true)' {
            $isMandatory = (Get-Command Set-ADTEnvironmentVariable).Parameters['Variable'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory
            $isMandatory | Should -Contain $true
        }

        It 'Should require -Value (Mandatory = true)' {
            $isMandatory = (Get-Command Set-ADTEnvironmentVariable).Parameters['Value'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory
            $isMandatory | Should -Contain $true
        }
    }

    Context 'Metadata' {
        It 'Should support -WhatIf (ShouldProcess)' {
            $cmd = Get-Command Set-ADTEnvironmentVariable
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }
}

BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
    # Unique test variable name — avoids collisions with real environment variables.
    $script:TestVar = "PSADT_TEST_$(([System.Guid]::NewGuid().ToString('N').Substring(0, 8)).ToUpper())"
}

AfterAll {
    # Final safety cleanup in case any test left the variable behind.
    [System.Environment]::SetEnvironmentVariable($script:TestVar, $null)
}

Describe 'Remove-ADTEnvironmentVariable' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Process-Scope Remove' {
        BeforeEach {
            # Seed the test variable so every test starts with it present.
            [System.Environment]::SetEnvironmentVariable($script:TestVar, 'initial-value')
        }

        AfterEach {
            # Unconditional cleanup after each test.
            [System.Environment]::SetEnvironmentVariable($script:TestVar, $null)
        }

        It 'Removes the variable from the process environment' {
            Remove-ADTEnvironmentVariable -Variable $script:TestVar
            [System.Environment]::GetEnvironmentVariable($script:TestVar) | Should -BeNullOrEmpty
        }

        It 'Produces no pipeline output' {
            $result = Remove-ADTEnvironmentVariable -Variable $script:TestVar
            $result | Should -BeNullOrEmpty
        }

        It 'Does not throw for an existing variable' {
            { Remove-ADTEnvironmentVariable -Variable $script:TestVar } | Should -Not -Throw
        }
    }

    Context 'Non-Existent Variable' {
        It 'Does not throw when the variable does not exist' {
            { Remove-ADTEnvironmentVariable -Variable "PSADT_TEST_$(New-Guid)" } | Should -Not -Throw
        }
    }

    Context '-WhatIf Support' {
        BeforeEach {
            [System.Environment]::SetEnvironmentVariable($script:TestVar, 'preserve-me')
        }

        AfterEach {
            [System.Environment]::SetEnvironmentVariable($script:TestVar, $null)
        }

        It 'Does not remove the variable when -WhatIf is specified' {
            Remove-ADTEnvironmentVariable -Variable $script:TestVar -WhatIf
            [System.Environment]::GetEnvironmentVariable($script:TestVar) | Should -Be 'preserve-me'
        }
    }

    Context 'Input Validation' {
        It 'Throws when Variable is null' {
            { Remove-ADTEnvironmentVariable -Variable $null } | Should -Throw
        }

        It 'Throws when Variable is an empty string' {
            { Remove-ADTEnvironmentVariable -Variable '' } | Should -Throw
        }
    }
}

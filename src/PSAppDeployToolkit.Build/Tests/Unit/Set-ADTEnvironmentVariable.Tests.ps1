BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
    # Unique test variable name — avoids collisions with real environment variables.
    $script:TestVar = "PSADT_TEST_$(([System.Guid]::NewGuid().ToString('N').Substring(0, 8)).ToUpper())"
}

AfterAll {
    # Ensure the test variable is fully cleaned up regardless of test outcome.
    [System.Environment]::SetEnvironmentVariable($script:TestVar, $null)
}

Describe 'Set-ADTEnvironmentVariable' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    AfterEach {
        # Remove the test variable after each test so state does not bleed between tests.
        [System.Environment]::SetEnvironmentVariable($script:TestVar, $null)
    }

    Context 'Process-Scope Set' {
        # The no-Target overload of EnvironmentUtilities::SetEnvironmentVariable(string,string,bool,bool,bool)
        # is not present in this build; use -Target Process to reach the 6-param overload instead.
        It 'Sets the variable to the specified value in process scope' {
            Set-ADTEnvironmentVariable -Variable $script:TestVar -Value 'hello' -Target Process
            [System.Environment]::GetEnvironmentVariable($script:TestVar) | Should -Be 'hello'
        }

        It 'Updates the variable when called a second time' {
            Set-ADTEnvironmentVariable -Variable $script:TestVar -Value 'first'  -Target Process
            Set-ADTEnvironmentVariable -Variable $script:TestVar -Value 'second' -Target Process
            [System.Environment]::GetEnvironmentVariable($script:TestVar) | Should -Be 'second'
        }

        It 'Produces no pipeline output' {
            $result = Set-ADTEnvironmentVariable -Variable $script:TestVar -Value 'hello' -Target Process
            $result | Should -BeNullOrEmpty
        }

        It 'Does not throw for a valid variable and value' {
            { Set-ADTEnvironmentVariable -Variable $script:TestVar -Value 'hello' -Target Process } | Should -Not -Throw
        }
    }

    Context '-WhatIf Support' {
        It 'Does not set the variable when -WhatIf is specified' {
            Set-ADTEnvironmentVariable -Variable $script:TestVar -Value 'hello' -WhatIf
            [System.Environment]::GetEnvironmentVariable($script:TestVar) | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Throws when Variable is null' {
            { Set-ADTEnvironmentVariable -Variable $null -Value 'val' } | Should -Throw
        }

        It 'Throws when Variable is an empty string' {
            { Set-ADTEnvironmentVariable -Variable '' -Value 'val' } | Should -Throw
        }

        It 'Throws when Value is null' {
            { Set-ADTEnvironmentVariable -Variable $script:TestVar -Value $null } | Should -Throw
        }

        It 'Throws when Value is an empty string' {
            { Set-ADTEnvironmentVariable -Variable $script:TestVar -Value '' } | Should -Throw
        }
    }
}

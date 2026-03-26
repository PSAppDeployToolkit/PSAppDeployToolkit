BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTEnvironmentVariable' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Default (no Target)' {
        It 'Returns a non-empty value for ComSpec' {
            Get-ADTEnvironmentVariable -Variable 'ComSpec' | Should -Not -BeNullOrEmpty
        }

        It 'Returns a System.String' {
            Get-ADTEnvironmentVariable -Variable 'ComSpec' | Should -BeOfType [System.String]
        }

        It 'Returns the same value as the native environment table' {
            $expected = [System.Environment]::GetEnvironmentVariable('ComSpec')
            Get-ADTEnvironmentVariable -Variable 'ComSpec' | Should -Be $expected
        }

        It 'Returns null or empty for a variable that does not exist' {
            $result = Get-ADTEnvironmentVariable -Variable "PSADT_NONEXISTENT_$(New-Guid)"
            $result | Should -BeNullOrEmpty
        }

        It 'Does not throw for a known variable' {
            { Get-ADTEnvironmentVariable -Variable 'ComSpec' } | Should -Not -Throw
        }
    }

    Context 'With -Target Process' {
        It 'Returns a non-empty value for SystemRoot in Process scope' {
            Get-ADTEnvironmentVariable -Variable 'SystemRoot' -Target Process | Should -Not -BeNullOrEmpty
        }

        It 'Returns the same value as the native Process-scope lookup' {
            $expected = [System.Environment]::GetEnvironmentVariable('SystemRoot', [System.EnvironmentVariableTarget]::Process)
            Get-ADTEnvironmentVariable -Variable 'SystemRoot' -Target Process | Should -Be $expected
        }

        It 'Does not throw for a valid variable with Process target' {
            { Get-ADTEnvironmentVariable -Variable 'SystemRoot' -Target Process } | Should -Not -Throw
        }
    }

    Context 'Input Validation' {
        It 'Throws when Variable is null' {
            { Get-ADTEnvironmentVariable -Variable $null } | Should -Throw
        }

        It 'Throws when Variable is an empty string' {
            { Get-ADTEnvironmentVariable -Variable '' } | Should -Throw
        }
    }
}

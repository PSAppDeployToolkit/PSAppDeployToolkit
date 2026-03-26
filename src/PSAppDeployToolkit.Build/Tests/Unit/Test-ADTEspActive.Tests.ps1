BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Test-ADTEspActive' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Return type' {
        It 'Returns a System.Boolean' {
            Test-ADTEspActive | Should -BeOfType [System.Boolean]
        }
    }

    Context 'Error handling' {
        It 'Does not throw under normal conditions' {
            { Test-ADTEspActive } | Should -Not -Throw
        }

        It 'Can be called multiple times consecutively without error' {
            { Test-ADTEspActive; Test-ADTEspActive } | Should -Not -Throw
        }
    }

    Context 'Logging' {
        It 'Calls Write-ADTLogEntry at least once per invocation' {
            Test-ADTEspActive
            Should -Invoke Write-ADTLogEntry -ModuleName PSAppDeployToolkit -Scope It
        }
    }

    Context 'wwahost.exe not running' {
        It 'Returns $false when wwahost.exe is not running' {
            # wwahost.exe only runs during the Windows Enrollment Status Page — absent on dev machines.
            if ([System.Diagnostics.Process]::GetProcessesByName('wwahost').Length -gt 0)
            {
                Set-ItResult -Skipped -Because 'wwahost.exe is running on this machine (ESP may be active)'
                return
            }
            Test-ADTEspActive | Should -Be $false
        }
    }

    Context 'wwahost.exe running' {
        It 'Returns a bool regardless of downstream ESP state' {
            if ([System.Diagnostics.Process]::GetProcessesByName('wwahost').Length -eq 0)
            {
                Set-ItResult -Skipped -Because 'wwahost.exe is not running on this machine'
                return
            }
            Test-ADTEspActive | Should -BeOfType [System.Boolean]
        }
    }
}

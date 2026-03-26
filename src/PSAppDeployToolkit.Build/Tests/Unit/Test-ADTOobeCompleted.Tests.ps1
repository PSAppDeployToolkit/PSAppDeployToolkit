BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Test-ADTOobeCompleted' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Return type' {
        It 'Returns a System.Boolean' {
            Test-ADTOobeCompleted | Should -BeOfType [System.Boolean]
        }
    }

    Context 'Error handling' {
        It 'Does not throw under normal conditions' {
            { Test-ADTOobeCompleted } | Should -Not -Throw
        }

        It 'Can be called multiple times consecutively without error' {
            { Test-ADTOobeCompleted; Test-ADTOobeCompleted } | Should -Not -Throw
        }
    }

    Context 'OOBE completed' {
        It 'Returns $true on a machine that has completed OOBE' {
            if (![PSADT.DeviceManagement.DeviceUtilities]::IsOOBEComplete())
            {
                Set-ItResult -Skipped -Because 'OOBE has not completed on this machine'
                return
            }
            Test-ADTOobeCompleted | Should -Be $true
        }
    }

    Context 'OOBE not completed' {
        It 'Returns $false on a machine where OOBE is still in progress' {
            if ([PSADT.DeviceManagement.DeviceUtilities]::IsOOBEComplete())
            {
                Set-ItResult -Skipped -Because 'OOBE has already completed on this machine'
                return
            }
            Test-ADTOobeCompleted | Should -Be $false
        }
    }
}

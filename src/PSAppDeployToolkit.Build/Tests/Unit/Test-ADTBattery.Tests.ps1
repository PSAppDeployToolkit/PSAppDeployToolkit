BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Test-ADTBattery' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Default (no -PassThru)' {
        It 'Returns a boolean' {
            Test-ADTBattery | Should -BeOfType ([System.Boolean])
        }

        It 'Does not throw' {
            { Test-ADTBattery } | Should -Not -Throw
        }
    }

    Context '-PassThru' {
        It 'Returns a BatteryInfo object' {
            Test-ADTBattery -PassThru | Should -BeOfType ([PSADT.DeviceManagement.BatteryInfo])
        }

        It 'BatteryInfo.IsUsingACPower is a boolean' {
            (Test-ADTBattery -PassThru).IsUsingACPower | Should -BeOfType ([System.Boolean])
        }

        It 'BatteryInfo.IsLaptop is a boolean' {
            (Test-ADTBattery -PassThru).IsLaptop | Should -BeOfType ([System.Boolean])
        }

        It 'Does not throw with -PassThru' {
            { Test-ADTBattery -PassThru } | Should -Not -Throw
        }

        It 'Default boolean output matches -PassThru.IsUsingACPower' {
            $bool = Test-ADTBattery
            $detail = Test-ADTBattery -PassThru
            $detail.IsUsingACPower | Should -Be $bool
        }
    }
}

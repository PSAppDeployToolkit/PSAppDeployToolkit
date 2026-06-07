BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Test-ADTBattery' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality - default (Boolean) mode' {
        It 'Should not throw' {
            { Test-ADTBattery } | Should -Not -Throw
        }

        It 'Should return a [System.Boolean]' {
            $result = Test-ADTBattery
            $result | Should -BeOfType ([System.Boolean])
        }

        It 'Should return a non-null value' {
            $result = Test-ADTBattery
            $null -ne $result | Should -BeTrue
        }
    }

    Context 'Functionality - PassThru mode' {
        It 'Should not throw when -PassThru is specified' {
            { Test-ADTBattery -PassThru } | Should -Not -Throw
        }

        It 'Should return an object of type PSADT.DeviceManagement.BatteryInfo when -PassThru is specified' {
            $result = Test-ADTBattery -PassThru
            $result | Should -BeOfType ([PSADT.DeviceManagement.BatteryInfo])
        }

        It 'Should expose an IsUsingACPower property that matches the default (non-PassThru) return value' {
            $boolResult = Test-ADTBattery
            $infoResult = Test-ADTBattery -PassThru
            $infoResult.IsUsingACPower | Should -Be $boolResult
        }

        It 'Should expose an IsLaptop Boolean property' {
            $result = Test-ADTBattery -PassThru
            $result.IsLaptop | Should -BeOfType ([System.Boolean])
        }

        It 'Should expose an ACPowerLineStatus property' {
            $result = Test-ADTBattery -PassThru
            $null -ne $result.ACPowerLineStatus | Should -BeTrue
        }

        It 'Should expose a BatteryChargeStatus property' {
            $result = Test-ADTBattery -PassThru
            $null -ne $result.BatteryChargeStatus | Should -BeTrue
        }

        It 'IsLaptop is $false on a system with no battery hardware' {
            Set-ItResult -Skipped -Because 'Battery hardware state is environment-specific; skipped to tolerate desktop/VM runners'
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType System.Boolean' {
            $outputTypes = (Get-Command Test-ADTBattery).OutputType.Type
            $outputTypes | Should -Contain ([System.Boolean])
        }

        It 'Should declare OutputType PSADT.DeviceManagement.BatteryInfo' {
            $outputTypes = (Get-Command Test-ADTBattery).OutputType.Type
            $outputTypes | Should -Contain ([PSADT.DeviceManagement.BatteryInfo])
        }
    }
}

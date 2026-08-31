BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    # The power status types live here, and PowerShell 7 does not load the assembly on its own.
    Add-Type -AssemblyName System.Windows.Forms
}

Describe 'Test-ADTBattery' {
    Context 'Functionality' {
        BeforeAll {
            $script:Battery = Test-ADTBattery -PassThru
        }

        It 'Returns a boolean by default' {
            Test-ADTBattery | Should -BeOfType ([System.Boolean])
        }

        It 'Returns the detail with -PassThru' {
            $script:Battery | Should -BeOfType ([PSADT.DeviceManagement.BatteryInfo])
        }

        It 'Reports being on AC power the same way Windows does' {
            $script:Battery.IsUsingACPower | Should -Be ([System.Windows.Forms.SystemInformation]::PowerStatus.PowerLineStatus -eq [System.Windows.Forms.PowerLineStatus]::Online)
        }

        It 'Agrees with itself about the power line status' {
            $script:Battery.ACPowerLineStatus | Should -Be ([System.Windows.Forms.SystemInformation]::PowerStatus.PowerLineStatus)
        }

        It 'Answers the question the boolean form is asked' {
            # The bare call answers "is this machine on AC power", per its synopsis, so it tracks
            # IsUsingACPower rather than inverting it.
            Test-ADTBattery | Should -Be $script:Battery.IsUsingACPower
        }

        It 'Reports a charge percentage as a whole number out of a hundred' {
            # Scaled to 0-100 rather than left as the 0-1 fraction the underlying API returns, and Windows
            # uses 255 for unknown, so anything outside that range would mean the scaling had gone wrong.
            if ($script:Battery.BatteryChargeStatus -ne [System.Windows.Forms.BatteryChargeStatus]::NoSystemBattery)
            {
                $script:Battery.BatteryLifePercent | Should -BeGreaterOrEqual 0
                $script:Battery.BatteryLifePercent | Should -BeLessOrEqual 100
            }
        }

        It 'Only calls the machine a laptop when it has a battery' {
            if ($script:Battery.IsLaptop)
            {
                $script:Battery.BatteryChargeStatus | Should -Not -Be ([System.Windows.Forms.BatteryChargeStatus]::NoSystemBattery)
            }
        }
    }
}

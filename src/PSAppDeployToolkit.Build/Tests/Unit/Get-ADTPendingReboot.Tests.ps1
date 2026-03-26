BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTPendingReboot' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Return Type and Shape' {
        It 'Does not throw when called' {
            { Get-ADTPendingReboot } | Should -Not -Throw
        }

        It 'Returns a non-null result' {
            $result = Get-ADTPendingReboot
            $result | Should -Not -BeNull
        }

        It 'Returns an object of type PSADT.DeviceManagement.RebootInfo' {
            $result = Get-ADTPendingReboot
            $result | Should -BeOfType ([PSADT.DeviceManagement.RebootInfo])
        }
    }

    Context 'ComputerName Property' {
        It 'ComputerName matches the local hostname' {
            $result = Get-ADTPendingReboot
            $result.ComputerName | Should -Be ([System.Net.Dns]::GetHostName())
        }

        It 'ComputerName is a non-empty string' {
            $result = Get-ADTPendingReboot
            $result.ComputerName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'LastBootUpTime Property' {
        It 'LastBootUpTime is a valid DateTime' {
            $result = Get-ADTPendingReboot
            $result.LastBootUpTime | Should -BeOfType ([System.DateTime])
        }

        It 'LastBootUpTime is not the default DateTime minimum value' {
            $result = Get-ADTPendingReboot
            $result.LastBootUpTime | Should -Not -Be ([System.DateTime]::MinValue)
        }
    }

    Context 'Boolean Reboot Indicator Properties' {
        It 'IsSystemRebootPending is a bool' {
            $result = Get-ADTPendingReboot
            $result.IsSystemRebootPending | Should -BeOfType ([System.Boolean])
        }

        It 'IsCBServicingRebootPending is a bool' {
            $result = Get-ADTPendingReboot
            $result.IsCBServicingRebootPending | Should -BeOfType ([System.Boolean])
        }

        It 'IsWindowsUpdateRebootPending is a bool' {
            $result = Get-ADTPendingReboot
            $result.IsWindowsUpdateRebootPending | Should -BeOfType ([System.Boolean])
        }

        It 'IsFileRenameRebootPending is a bool' {
            $result = Get-ADTPendingReboot
            $result.IsFileRenameRebootPending | Should -BeOfType ([System.Boolean])
        }
    }

    Context 'ErrorMsg Property' {
        It 'ErrorMsg is not null' {
            $result = Get-ADTPendingReboot
            ($null -ne $result.ErrorMsg) | Should -BeTrue
        }

        It 'ErrorMsg.Count is zero or greater when no errors occurred' {
            $result = Get-ADTPendingReboot
            $result.ErrorMsg.Count | Should -BeGreaterOrEqual 0
        }
    }
}

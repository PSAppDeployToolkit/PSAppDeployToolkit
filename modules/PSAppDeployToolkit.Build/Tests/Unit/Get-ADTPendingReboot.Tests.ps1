BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTPendingReboot' {
    Context 'Functionality' {
        BeforeAll {
            $script:Reboot = Get-ADTPendingReboot
        }

        It 'Returns a RebootInfo' {
            $script:Reboot | Should -BeOfType ([PSADT.DeviceManagement.RebootInfo])
        }

        It 'Names this computer' {
            $script:Reboot.ComputerName | Should -BeExactly ([System.Net.Dns]::GetHostName())
        }

        It 'Reports a plausible last boot time' {
            # Cross-checked against the tick count rather than CIM, so the two do not share a source.
            $uptime = [System.DateTime]::Now - $script:Reboot.LastBootUpTime
            $uptime.TotalMilliseconds | Should -BeGreaterThan 0
            [System.Math]::Abs($uptime.TotalMilliseconds - [System.Environment]::TickCount64) | Should -BeLessThan 300000
        }

        It 'Derives <Property> from <RegistryPath>' -ForEach @(
            @{
                Property = 'IsCBServicingRebootPending'
                RegistryPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending'
                Kind = 'Key'
            }
            @{
                Property = 'IsWindowsUpdateRebootPending'
                RegistryPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired'
                Kind = 'Key'
            }
            @{
                Property = 'IsFileRenameRebootPending'
                RegistryPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager'
                Kind = 'Value'
                ValueName = 'PendingFileRenameOperations'
            }
        ) {
            # Each flag is checked against the registry state it is read from, so the property reflects the
            # machine rather than simply being present.
            $expected = if ($Kind -eq 'Key')
            {
                Test-Path -LiteralPath $RegistryPath
            }
            else
            {
                $null -ne (Get-ItemProperty -LiteralPath $RegistryPath -Name $ValueName -ErrorAction Ignore).$ValueName
            }
            $script:Reboot.$Property | Should -Be $expected
        }

        It 'Sets the overall flag when any individual one is set' {
            $individual = @(
                $script:Reboot.IsCBServicingRebootPending
                $script:Reboot.IsWindowsUpdateRebootPending
                $script:Reboot.IsSCCMClientRebootPending
                $script:Reboot.IsIntuneClientRebootPending
                $script:Reboot.IsAppVRebootPending
                $script:Reboot.IsFileRenameRebootPending
            )
            $script:Reboot.IsSystemRebootPending | Should -Be ($individual -contains $true)
        }

        It 'Lists the pending renames only when it says renames are pending' {
            if ($script:Reboot.IsFileRenameRebootPending)
            {
                $script:Reboot.PendingFileRenameOperations | Should -Not -BeNullOrEmpty
            }
            else
            {
                $script:Reboot.PendingFileRenameOperations | Should -BeNullOrEmpty
            }
        }
    }
}

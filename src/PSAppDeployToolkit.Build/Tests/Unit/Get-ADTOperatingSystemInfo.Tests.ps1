BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTOperatingSystemInfo' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should not throw' {
            { Get-ADTOperatingSystemInfo } | Should -Not -Throw
        }

        It 'Should return an object of type PSADT.DeviceManagement.OperatingSystemInfo' {
            $result = Get-ADTOperatingSystemInfo
            $result | Should -BeOfType ([PSADT.DeviceManagement.OperatingSystemInfo])
        }

        It 'Should return a non-null Name property' {
            $result = Get-ADTOperatingSystemInfo
            $result.Name | Should -Not -BeNullOrEmpty
        }

        It 'Should return a non-null Version property of type System.Version' {
            $result = Get-ADTOperatingSystemInfo
            $result.Version | Should -Not -BeNullOrEmpty
            $result.Version | Should -BeOfType ([System.Version])
        }

        It 'Should report Is64BitOperatingSystem as a Boolean' {
            $result = Get-ADTOperatingSystemInfo
            $result.Is64BitOperatingSystem | Should -BeOfType ([System.Boolean])
        }

        It 'Should report exactly one of IsWorkstation / IsServer / IsDomainController as true' {
            $result = Get-ADTOperatingSystemInfo
            $exclusive = [int]$result.IsWorkstation + [int]$result.IsServer + [int]$result.IsDomainController
            $exclusive | Should -Be 1
        }

        It 'Should return a non-empty Edition string' {
            $result = Get-ADTOperatingSystemInfo
            $result.Edition | Should -Not -BeNullOrEmpty
        }

        It 'Should return a non-empty Architecture value' {
            $result = Get-ADTOperatingSystemInfo
            $result.Architecture | Should -Not -BeNullOrEmpty
        }

        It 'Should return the same singleton on consecutive calls' {
            $first  = Get-ADTOperatingSystemInfo
            $second = Get-ADTOperatingSystemInfo
            [System.Object]::ReferenceEquals($first, $second) | Should -BeTrue
        }
    }

    Context 'Metadata' {
        It 'Should return an object whose type matches the documented OutputType' {
            # The function body returns the singleton directly; verify the runtime type.
            $result = Get-ADTOperatingSystemInfo
            $result.GetType().FullName | Should -Be 'PSADT.DeviceManagement.OperatingSystemInfo'
        }
    }
}

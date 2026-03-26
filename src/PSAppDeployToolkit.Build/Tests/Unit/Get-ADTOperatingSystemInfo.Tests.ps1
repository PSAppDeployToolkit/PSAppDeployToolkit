BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTOperatingSystemInfo' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'Return Type' {
        It 'Returns a non-null result' {
            Get-ADTOperatingSystemInfo | Should -Not -BeNullOrEmpty
        }

        It 'Returns an OperatingSystemInfo object' {
            Get-ADTOperatingSystemInfo | Should -BeOfType ([PSADT.DeviceManagement.OperatingSystemInfo])
        }

        It 'Does not throw' {
            { Get-ADTOperatingSystemInfo } | Should -Not -Throw
        }
    }

    Context 'Properties' {
        It 'Version is a System.Version' {
            (Get-ADTOperatingSystemInfo).Version | Should -BeOfType ([System.Version])
        }

        It 'Name is a non-empty string' {
            (Get-ADTOperatingSystemInfo).Name | Should -Not -BeNullOrEmpty
        }

        It 'Is64BitOperatingSystem matches Environment.Is64BitOperatingSystem' {
            $expected = [System.Environment]::Is64BitOperatingSystem
            (Get-ADTOperatingSystemInfo).Is64BitOperatingSystem | Should -Be $expected
        }

        It 'IsWorkstation and IsServer are mutually exclusive' {
            $info = Get-ADTOperatingSystemInfo
            ($info.IsWorkstation -xor $info.IsServer) | Should -BeTrue
        }

        It 'Returns the same singleton on successive calls' {
            $a = Get-ADTOperatingSystemInfo
            $b = Get-ADTOperatingSystemInfo
            [object]::ReferenceEquals($a, $b) | Should -BeTrue
        }
    }
}

BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTFreeDiskSpace' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Default Drive (System Drive)' {
        It 'Returns a non-negative value when called without a Drive parameter' {
            $result = Get-ADTFreeDiskSpace
            $result | Should -BeGreaterOrEqual 0
        }

        It 'Returns a System.Double' {
            Get-ADTFreeDiskSpace | Should -BeOfType [System.Double]
        }

        It 'Does not throw when called with no arguments' {
            { Get-ADTFreeDiskSpace } | Should -Not -Throw
        }
    }

    Context 'Explicit Drive' {
        It 'Returns a non-negative value for the system drive specified explicitly' {
            $result = Get-ADTFreeDiskSpace -Drive ([System.IO.DriveInfo]$env:SystemDrive)
            $result | Should -BeGreaterOrEqual 0
        }

        It 'Does not throw when an explicit valid drive is supplied' {
            { Get-ADTFreeDiskSpace -Drive ([System.IO.DriveInfo]$env:SystemDrive) } | Should -Not -Throw
        }
    }

    Context 'Return Value Precision' {
        It 'Result is a whole number (Math.Round strips fractional MB)' {
            $result = Get-ADTFreeDiskSpace
            # The function returns [Math]::Round(AvailableFreeSpace / 1MB) so no fractional part.
            ($result % 1) | Should -Be 0
        }
    }
}

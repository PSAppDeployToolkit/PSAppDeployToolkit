BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTMsiExitCodeMessage' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Known Exit Codes with Messages' {
        It 'Returns a non-empty string for exit code 1618 (another install running)' {
            Get-ADTMsiExitCodeMessage -MsiExitCode 1618 | Should -Not -BeNullOrEmpty
        }

        It 'Returns a System.String for exit code 1618' {
            Get-ADTMsiExitCodeMessage -MsiExitCode 1618 | Should -BeOfType [System.String]
        }
    }

    Context 'Exit Codes with No Message in msimsg.dll' {
        It 'Returns no output for exit code 1602 (no DLL resource on this platform)' {
            # 1602 has no string resource in msimsg.dll on this machine — function returns nothing.
            $result = Get-ADTMsiExitCodeMessage -MsiExitCode 1602
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Does Not Throw' {
        It 'Does not throw for exit code 1618' {
            { Get-ADTMsiExitCodeMessage -MsiExitCode 1618 } | Should -Not -Throw
        }

        It 'Does not throw for exit code 0' {
            { Get-ADTMsiExitCodeMessage -MsiExitCode 0 } | Should -Not -Throw
        }

        It 'Does not throw for exit code 1602' {
            { Get-ADTMsiExitCodeMessage -MsiExitCode 1602 } | Should -Not -Throw
        }
    }

    Context 'Input Validation' {
        It 'Throws when MsiExitCode is null' {
            { Get-ADTMsiExitCodeMessage -MsiExitCode $null } | Should -Throw
        }
    }
}

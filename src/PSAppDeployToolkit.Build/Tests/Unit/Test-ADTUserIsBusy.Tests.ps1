BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Test-ADTUserIsBusy' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
        # Mock all three delegate functions to control their return values.
        Mock -ModuleName PSAppDeployToolkit Test-ADTMicrophoneInUse { $false }
        Mock -ModuleName PSAppDeployToolkit Get-ADTPresentationSettingsEnabledUsers { $null }
        Mock -ModuleName PSAppDeployToolkit Test-ADTPowerPoint { $false }

        # Test-ADTUserIsBusy compiles with [PSADT.AccountManagement.AccountUtilities]::<member>
        # references in its call graph. PowerShell resolves type literals at compile time,
        # triggering the static constructor which requires admin rights.
        $script:IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }

    BeforeEach {
        if (!$script:IsAdmin) { Set-ItResult -Skipped -Because 'Requires admin rights (AccountUtilities static constructor triggered at compile time)'; return }
    }

    Context 'Return type' {
        It 'Returns a System.Boolean' {
            Test-ADTUserIsBusy | Should -BeOfType [System.Boolean]
        }
    }

    Context 'All delegates return false/null (user not busy)' {
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Get-ADTClientServerUser { return [PSCustomObject]@{ UserName = 'DOMAIN\TestUser' } }
            Mock -ModuleName PSAppDeployToolkit Test-ADTUserInFocusMode { $false }
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserToastNotificationMode { return 0 }
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserNotificationState { return [PSADT.Interop.QUERY_USER_NOTIFICATION_STATE]::QUNS_ACCEPTS_NOTIFICATIONS }
        }

        It 'Returns $false when microphone is not in use, no presentation mode, PowerPoint not running' {
            Test-ADTUserIsBusy | Should -Be $false
        }

        It 'Does not throw when all delegates return false' {
            { Test-ADTUserIsBusy } | Should -Not -Throw
        }
    }

    Context 'Microphone in use' {
        It 'Returns $true when Test-ADTMicrophoneInUse returns $true' {
            Mock -ModuleName PSAppDeployToolkit Test-ADTMicrophoneInUse { $true }
            Test-ADTUserIsBusy | Should -Be $true
        }
    }

    Context 'PowerPoint running in presentation mode' {
        It 'Returns $true when Test-ADTPowerPoint returns $true' {
            Mock -ModuleName PSAppDeployToolkit Test-ADTPowerPoint { $true }
            Test-ADTUserIsBusy | Should -Be $true
        }
    }

    Context 'Error propagation' {
        It 'Re-throws as a terminating error when a delegate function throws' {
            Mock -ModuleName PSAppDeployToolkit Test-ADTMicrophoneInUse {
                throw [System.InvalidOperationException]::new('Microphone check failed.')
            }
            { Test-ADTUserIsBusy } | Should -Throw
        }
    }
}

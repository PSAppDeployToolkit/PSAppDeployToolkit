BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Test-ADTSessionActive' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'No Active Session' {
        # Default state after a plain Import-Module.
        It 'Returns $false when no sessions are active' {
            Test-ADTSessionActive | Should -BeFalse
        }

        It 'Does not throw' {
            { Test-ADTSessionActive } | Should -Not -Throw
        }
    }

    Context 'With Active Session' {
        BeforeAll {
            # Replace the Sessions collection with a non-empty array so Count > 0.
            # $Script:ADT is a PSCustomObject so its NoteProperties accept any value.
            InModuleScope PSAppDeployToolkit {
                $Script:ADTPesterSessionsBackup = $Script:ADT.Sessions
                $Script:ADT.Sessions = @('FakeSession')
            }
        }

        AfterAll {
            InModuleScope PSAppDeployToolkit {
                $Script:ADT.Sessions = $Script:ADTPesterSessionsBackup
                $Script:ADTPesterSessionsBackup = $null
            }
        }

        It 'Returns $true when a session is active' {
            Test-ADTSessionActive | Should -BeTrue
        }

        It 'Returns a boolean' {
            Test-ADTSessionActive | Should -BeOfType ([System.Boolean])
        }
    }
}

BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Reset-ADTDeferHistory' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Session mock whose ResetDeferHistory ScriptMethod records invocation via $Global:.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'MockSession', Justification = 'Used in It blocks via the Get-ADTSession mock.')]
        $MockSession = [PSCustomObject]@{}
        $MockSession | Add-Member -MemberType ScriptMethod -Name 'ResetDeferHistory' -Value {
            $script:ADTTestResetDeferCalled = $true
        }
        Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $MockSession }
    }

    BeforeEach {
        $script:ADTTestResetDeferCalled = $false
    }

    AfterAll {
        Remove-Variable -Name ADTTestResetDeferCalled -Scope Script -ErrorAction SilentlyContinue
    }

    Context 'Session delegation' {
        It 'Does not throw' {
            { Reset-ADTDeferHistory } | Should -Not -Throw
        }

        It 'Calls ResetDeferHistory on the session object' {
            Reset-ADTDeferHistory
            $script:ADTTestResetDeferCalled | Should -Be $true
        }

        It 'Returns no output' {
            $result = Reset-ADTDeferHistory
            $result | Should -BeNull
        }

        It 'Can be called multiple times without error' {
            { Reset-ADTDeferHistory; Reset-ADTDeferHistory } | Should -Not -Throw
        }
    }

    Context 'Error propagation' {
        BeforeAll {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'ThrowingSession', Justification = 'Used in It blocks.')]
            $ThrowingSession = [PSCustomObject]@{}
            $ThrowingSession | Add-Member -MemberType ScriptMethod -Name 'ResetDeferHistory' -Value {
                throw [System.InvalidOperationException]::new('Simulated ResetDeferHistory failure.')
            }
        }

        It 'Re-throws as a terminating error when ResetDeferHistory fails' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $ThrowingSession }
            { Reset-ADTDeferHistory } | Should -Throw
        }

        It 'Re-throws as a terminating error when Get-ADTSession fails' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { throw [System.InvalidOperationException]::new('No active session.') }
            { Reset-ADTDeferHistory } | Should -Throw
        }
    }
}

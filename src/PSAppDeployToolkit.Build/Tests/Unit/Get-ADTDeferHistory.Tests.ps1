BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTDeferHistory' {
    BeforeAll {
        # Get-ADTDeferHistory has no Initialize-ADTFunction, but standard mocks are included
        # to silence any module-internal logging side effects.
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Session mock whose GetDeferHistory ScriptMethod returns whatever $Global:ADTTestGetDeferReturn holds.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'MockSession', Justification = 'Used in It blocks via the Get-ADTSession mock.')]
        $MockSession = [PSCustomObject]@{}
        $MockSession | Add-Member -MemberType ScriptMethod -Name 'GetDeferHistory' -Value {
            return $script:ADTTestGetDeferReturn
        }
        Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $MockSession }
    }

    BeforeEach {
        $script:ADTTestGetDeferReturn = $null
    }

    AfterAll {
        Remove-Variable -Name ADTTestGetDeferReturn -Scope Script -ErrorAction SilentlyContinue
    }

    Context 'Returns the session GetDeferHistory result' {
        It 'Returns $null when GetDeferHistory returns $null' {
            $script:ADTTestGetDeferReturn = $null
            Get-ADTDeferHistory | Should -BeNull
        }

        It 'Returns a non-null object when GetDeferHistory returns one' {
            $script:ADTTestGetDeferReturn = [PSCustomObject]@{ DeferTimesRemaining = 3 }
            Get-ADTDeferHistory | Should -Not -BeNull
        }

        It 'Returns DeferTimesRemaining matching the mock value' {
            $script:ADTTestGetDeferReturn = [PSCustomObject]@{ DeferTimesRemaining = 5 }
            $result = Get-ADTDeferHistory
            $result.DeferTimesRemaining | Should -Be 5
        }

        It 'Returns DeferDeadline matching the mock value' {
            $deadline = [DateTime]::Now.AddDays(7)
            $script:ADTTestGetDeferReturn = [PSCustomObject]@{ DeferDeadline = $deadline }
            $result = Get-ADTDeferHistory
            $result.DeferDeadline | Should -Be $deadline
        }

        It 'Does not throw when GetDeferHistory returns $null' {
            $script:ADTTestGetDeferReturn = $null
            { Get-ADTDeferHistory } | Should -Not -Throw
        }
    }

    Context 'Error propagation' {
        BeforeAll {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'ThrowingSession', Justification = 'Used in It blocks.')]
            $ThrowingSession = [PSCustomObject]@{}
            $ThrowingSession | Add-Member -MemberType ScriptMethod -Name 'GetDeferHistory' -Value {
                throw [System.InvalidOperationException]::new('Simulated GetDeferHistory failure.')
            }
        }

        It 'Re-throws as a terminating error when GetDeferHistory fails' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $ThrowingSession }
            { Get-ADTDeferHistory } | Should -Throw
        }

        It 'Re-throws as a terminating error when Get-ADTSession fails' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { throw [System.InvalidOperationException]::new('No active session.') }
            { Get-ADTDeferHistory } | Should -Throw
        }
    }
}

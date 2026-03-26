BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Set-ADTDeferHistory' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Build a mock session whose SetDeferHistory ScriptMethod records call arguments
        # into a $Global: variable so they survive execution inside the module's scope.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'MockSession', Justification = 'Used in It blocks via the Get-ADTSession mock.')]
        $MockSession = [PSCustomObject]@{}
        $MockSession | Add-Member -MemberType ScriptMethod -Name 'SetDeferHistory' -Value {
            $script:ADTTestSetDeferHistoryInvoked = $true
            $script:ADTTestSetDeferHistoryArgs = $args
        }
        Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $MockSession }
    }

    BeforeEach {
        $script:ADTTestSetDeferHistoryInvoked = $false
        $script:ADTTestSetDeferHistoryArgs = $null
    }

    AfterAll {
        Remove-Variable -Name ADTTestSetDeferHistoryInvoked -Scope Script -ErrorAction SilentlyContinue
        Remove-Variable -Name ADTTestSetDeferHistoryArgs -Scope Script -ErrorAction SilentlyContinue
    }

    Context 'At-least-one-parameter validation' {
        It 'Throws when called with no parameters' {
            { Set-ADTDeferHistory } | Should -Throw
        }

        It 'Does not throw when -DeferTimesRemaining is the only parameter' {
            { Set-ADTDeferHistory -DeferTimesRemaining 3 } | Should -Not -Throw
        }

        It 'Does not throw when -DeferDeadline is the only parameter' {
            { Set-ADTDeferHistory -DeferDeadline ([DateTime]::Now.AddDays(7)) } | Should -Not -Throw
        }

        It 'Does not throw when -DeferRunInterval is the only parameter' {
            { Set-ADTDeferHistory -DeferRunInterval ([TimeSpan]::FromHours(1)) } | Should -Not -Throw
        }

        It 'Does not throw when -DeferRunIntervalLastTime is the only parameter' {
            { Set-ADTDeferHistory -DeferRunIntervalLastTime ([DateTime]::Now) } | Should -Not -Throw
        }
    }

    Context 'SetDeferHistory is called on the session object' {
        It 'Invokes SetDeferHistory when -DeferTimesRemaining is specified' {
            Set-ADTDeferHistory -DeferTimesRemaining 5
            $script:ADTTestSetDeferHistoryInvoked | Should -Be $true
        }

        It 'Passes the DeferTimesRemaining value as the first argument' {
            Set-ADTDeferHistory -DeferTimesRemaining 5
            # args[0] is DeferTimesRemaining (nullable uint)
            $script:ADTTestSetDeferHistoryArgs[0] | Should -Be 5
        }

        It 'Passes $null for DeferTimesRemaining when that parameter is omitted' {
            Set-ADTDeferHistory -DeferDeadline ([DateTime]::Now.AddDays(1))
            $script:ADTTestSetDeferHistoryArgs[0] | Should -BeNullOrEmpty
        }

        It 'Invokes SetDeferHistory when -DeferDeadline is specified' {
            Set-ADTDeferHistory -DeferDeadline ([DateTime]::Now.AddDays(7))
            $script:ADTTestSetDeferHistoryInvoked | Should -Be $true
        }

        It 'Invokes SetDeferHistory when -DeferRunInterval is specified' {
            Set-ADTDeferHistory -DeferRunInterval ([TimeSpan]::FromMinutes(30))
            $script:ADTTestSetDeferHistoryInvoked | Should -Be $true
        }

        It 'Invokes SetDeferHistory when -DeferRunIntervalLastTime is specified' {
            Set-ADTDeferHistory -DeferRunIntervalLastTime ([DateTime]::Now)
            $script:ADTTestSetDeferHistoryInvoked | Should -Be $true
        }

        It 'Accepts DeferTimesRemaining value of 0' {
            { Set-ADTDeferHistory -DeferTimesRemaining 0 } | Should -Not -Throw
        }

        It 'Accepts all four parameters at once' {
            $deadline = [DateTime]::Now.AddDays(14)
            $interval = [TimeSpan]::FromHours(2)
            $lastTime = [DateTime]::Now
            { Set-ADTDeferHistory -DeferTimesRemaining 2 -DeferDeadline $deadline -DeferRunInterval $interval -DeferRunIntervalLastTime $lastTime } | Should -Not -Throw
            $script:ADTTestSetDeferHistoryInvoked | Should -Be $true
        }
    }

    Context 'Error propagation' {
        BeforeAll {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'ThrowingSession', Justification = 'Used in It blocks via the Get-ADTSession mock.')]
            $ThrowingSession = [PSCustomObject]@{}
            $ThrowingSession | Add-Member -MemberType ScriptMethod -Name 'SetDeferHistory' -Value {
                throw [System.InvalidOperationException]::new('Simulated SetDeferHistory failure.')
            }
        }

        It 'Re-throws as a terminating error when SetDeferHistory fails' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { return $ThrowingSession }
            { Set-ADTDeferHistory -DeferTimesRemaining 1 } | Should -Throw
        }

        It 'Re-throws as a terminating error when Get-ADTSession fails' {
            Mock -ModuleName PSAppDeployToolkit Get-ADTSession { throw [System.InvalidOperationException]::new('No active session.') }
            { Set-ADTDeferHistory -DeferTimesRemaining 1 } | Should -Throw
        }
    }
}

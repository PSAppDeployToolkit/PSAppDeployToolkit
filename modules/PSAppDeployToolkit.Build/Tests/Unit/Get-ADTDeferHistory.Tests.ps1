BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }

    # Deferral history reads and writes the registry under the configured key, pointed at the test hive so
    # the machine's real toolkit key is left alone. The session captures it on construction.
    $script:RegistryRoot = (New-Item -Path 'TestRegistry:\GetDeferHistoryProbe' -ItemType Directory).PSPath
    Initialize-ADTTestModule -Path $TestDrive -RegistryPath $script:RegistryRoot
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTDeferHistory' {
    Context 'With no session open' {
        It 'Tells the caller to open one first' {
            { Get-ADTDeferHistory } | Should -Throw -ErrorId 'ADTSessionBufferEmpty,Get-ADTDeferHistory'
        }
    }

    Context 'With a session open' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'GetDeferProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        AfterEach {
            Reset-ADTDeferHistory -InformationAction SilentlyContinue
        }

        It 'Returns nothing for a deployment that has never deferred' {
            # A first run has no history, and callers branch on that rather than on a zeroed record.
            Get-ADTDeferHistory | Should -BeNullOrEmpty
        }

        It 'Returns a history once something has been recorded' {
            Set-ADTDeferHistory -DeferTimesRemaining 3 -InformationAction SilentlyContinue
            Get-ADTDeferHistory | Should -BeOfType ([PSAppDeployToolkit.Foundation.DeferHistory])
        }

        It 'Reads back every value that was set' {
            $deadline = [System.DateTime]::new(2030, 5, 6, 7, 8, 9)
            $lastTime = [System.DateTime]::new(2029, 4, 3, 2, 1, 0)
            Set-ADTDeferHistory -DeferTimesRemaining 2 -DeferDeadline $deadline -DeferRunInterval 3600 -DeferRunIntervalLastTime $lastTime -InformationAction SilentlyContinue

            $history = Get-ADTDeferHistory
            $history.DeferTimesRemaining | Should -Be 2
            $history.DeferDeadline | Should -Be $deadline
            $history.DeferRunIntervalLastTime | Should -Be $lastTime
            $history.DeferRunInterval | Should -Be ([System.TimeSpan]::FromHours(1))
        }

        It 'Leaves the values that were never set empty' {
            Set-ADTDeferHistory -DeferTimesRemaining 1 -InformationAction SilentlyContinue
            $history = Get-ADTDeferHistory
            $history.DeferTimesRemaining | Should -Be 1
            $history.DeferDeadline | Should -BeNullOrEmpty
            $history.DeferRunIntervalLastTime | Should -BeNullOrEmpty
            $history.DeferRunInterval | Should -BeNullOrEmpty
        }

        It 'Compares equal to a second read of the same state' {
            # DeferHistory is a record precisely so a later run can compare what it reads against what it
            # read before.
            Set-ADTDeferHistory -DeferTimesRemaining 2 -InformationAction SilentlyContinue
            Get-ADTDeferHistory | Should -Be (Get-ADTDeferHistory)
        }

        It 'Round-trips the deadline through the registry without drifting' {
            # Written as a universal round-trip string, so a machine in a non-UTC timezone has to read back
            # the same local time it was given.
            $deadline = [System.DateTime]::new(2031, 12, 31, 23, 59, 58)
            Set-ADTDeferHistory -DeferDeadline $deadline -InformationAction SilentlyContinue
            (Get-ADTDeferHistory).DeferDeadline | Should -Be $deadline
        }
    }
}

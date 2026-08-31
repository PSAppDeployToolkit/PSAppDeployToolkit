BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }

    # Deferral history persists to the registry under the toolkit's configured key, so that key is pointed
    # at the test hive before a session is opened. The session captures it on construction.
    $script:RegistryRoot = (New-Item -Path 'TestRegistry:\DeferHistoryProbe' -ItemType Directory).PSPath -replace '^Microsoft\.PowerShell\.Core\\Registry::', 'Microsoft.PowerShell.Core\Registry::'
    Initialize-ADTTestModule -Path $TestDrive -RegistryPath $script:RegistryRoot
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Set-ADTDeferHistory' {
    Context 'With no session open' {
        It 'Tells the caller to open one first' {
            { Set-ADTDeferHistory -DeferTimesRemaining 3 } | Should -Throw -ErrorId 'ADTSessionBufferEmpty,Set-ADTDeferHistory'
        }
    }

    Context 'With a session open' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'DeferProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        AfterEach {
            Reset-ADTDeferHistory -InformationAction SilentlyContinue
        }

        It 'Records the remaining deferrals' {
            Set-ADTDeferHistory -DeferTimesRemaining 3 -InformationAction SilentlyContinue
            (Get-ADTDeferHistory).DeferTimesRemaining | Should -Be 3
        }

        It 'Records the deadline' {
            $deadline = [System.DateTime]::new(2030, 1, 2, 3, 4, 5)
            Set-ADTDeferHistory -DeferDeadline $deadline -InformationAction SilentlyContinue
            (Get-ADTDeferHistory).DeferDeadline | Should -Be $deadline
        }

        It 'Persists the run interval even though the history does not surface it' {
            # DeferHistory carries the remaining count, the deadline and the last interval time only.
            # The interval itself is written to the registry but never read back: Show-ADTInstallationWelcome
            # takes it as a parameter on each run and compares it against DeferRunIntervalLastTime.
            Set-ADTDeferHistory -DeferRunInterval 3600 -InformationAction SilentlyContinue
            (Get-ADTDeferHistory).PSObject.Properties.Name | Should -Not -Contain 'DeferRunInterval'

            $key = Get-ChildItem -LiteralPath $script:RegistryRoot -Recurse | Select-Object -Last 1
            Get-ItemPropertyValue -LiteralPath $key.PSPath -Name 'DeferRunInterval' | Should -BeExactly '01:00:00'
        }

        It 'Reads a bare number on the run interval as seconds' {
            Set-ADTDeferHistory -DeferRunInterval 90 -InformationAction SilentlyContinue
            $key = Get-ChildItem -LiteralPath $script:RegistryRoot -Recurse | Select-Object -Last 1
            Get-ItemPropertyValue -LiteralPath $key.PSPath -Name 'DeferRunInterval' | Should -BeExactly '00:01:30'
        }

        It 'Records the time the interval last elapsed' {
            $last = [System.DateTime]::new(2029, 6, 7, 8, 9, 10)
            Set-ADTDeferHistory -DeferRunIntervalLastTime $last -InformationAction SilentlyContinue
            (Get-ADTDeferHistory).DeferRunIntervalLastTime | Should -Be $last
        }

        It 'Records several values at once' {
            Set-ADTDeferHistory -DeferTimesRemaining 2 -DeferDeadline ([System.DateTime]::new(2031, 1, 1)) -InformationAction SilentlyContinue
            $history = Get-ADTDeferHistory
            $history.DeferTimesRemaining | Should -Be 2
            $history.DeferDeadline | Should -Be ([System.DateTime]::new(2031, 1, 1))
        }

        It 'Writes under the configured registry key rather than the machine default' {
            # The whole point of redirecting RegPath: a deployment's deferral state must land where the
            # config says, which is what lets this run without touching the real toolkit key.
            Set-ADTDeferHistory -DeferTimesRemaining 1 -InformationAction SilentlyContinue
            @(Get-ChildItem -LiteralPath $script:RegistryRoot -Recurse).Count | Should -BeGreaterThan 0
        }

        It 'Requires at least one value to set' {
            { Set-ADTDeferHistory } | Should -Throw -ErrorId 'SetDeferHistoryNoParamSpecified,Set-ADTDeferHistory'
        }
    }
}

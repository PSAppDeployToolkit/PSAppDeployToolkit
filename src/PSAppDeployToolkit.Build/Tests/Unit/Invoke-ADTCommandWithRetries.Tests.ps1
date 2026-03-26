BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Always returns 'ok'. Used to verify basic pass-through and validation.
    function global:Invoke-ADTTestAlwaysSucceed
    {
        [CmdletBinding()]
        [OutputType([string])]
        param()
        return 'ok'
    }

    # Echoes back the -Value parameter. Used to verify argument forwarding.
    function global:Invoke-ADTTestEchoValue
    {
        [CmdletBinding()]
        [OutputType([string])]
        param([string]$Value)
        return $Value
    }

    # Throws $script:ADTTestRetryFailCount times, then returns 'done'.
    # $script:ADTTestRetryAttempt tracks cumulative call count (reset via BeforeEach).
    function global:Invoke-ADTTestRetryThenSucceed
    {
        [CmdletBinding()]
        [OutputType([string])]
        param()
        if ($script:ADTTestRetryAttempt -lt $script:ADTTestRetryFailCount)
        {
            $script:ADTTestRetryAttempt++
            throw [System.IO.IOException]::new('Simulated transient error')
        }
        $script:ADTTestRetryAttempt++
        return 'done'
    }
}

AfterAll {
    Remove-Item -Path 'Function:\Invoke-ADTTestAlwaysSucceed' -ErrorAction SilentlyContinue
    Remove-Item -Path 'Function:\Invoke-ADTTestEchoValue' -ErrorAction SilentlyContinue
    Remove-Item -Path 'Function:\Invoke-ADTTestRetryThenSucceed' -ErrorAction SilentlyContinue
}

Describe 'Invoke-ADTCommandWithRetries' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Successful Command' {
        It 'Returns the output from a command that succeeds on the first try' {
            $result = Invoke-ADTCommandWithRetries -Command 'Invoke-ADTTestAlwaysSucceed' -SleepDuration ([System.TimeSpan]::FromMilliseconds(10))
            $result | Should -Be 'ok'
        }

        It 'Returns a System.String when the command outputs a string' {
            Invoke-ADTCommandWithRetries -Command 'Invoke-ADTTestAlwaysSucceed' -SleepDuration ([System.TimeSpan]::FromMilliseconds(10)) | Should -BeOfType [System.String]
        }

        It 'Does not throw when the command succeeds' {
            { Invoke-ADTCommandWithRetries -Command 'Invoke-ADTTestAlwaysSucceed' -SleepDuration ([System.TimeSpan]::FromMilliseconds(10)) } | Should -Not -Throw
        }
    }

    Context 'Retry Behavior' {
        BeforeEach {
            $script:ADTTestRetryAttempt = 0
            $script:ADTTestRetryFailCount = 2
        }

        It 'Returns the successful result after transient failures' {
            $result = Invoke-ADTCommandWithRetries -Command 'Invoke-ADTTestRetryThenSucceed' -Retries 5 -SleepDuration ([System.TimeSpan]::FromMilliseconds(10))
            $result | Should -Be 'done'
        }

        It 'Invokes the command exactly (FailCount + 1) times before returning' {
            # 2 failures + 1 success = 3 total invocations.
            Invoke-ADTCommandWithRetries -Command 'Invoke-ADTTestRetryThenSucceed' -Retries 5 -SleepDuration ([System.TimeSpan]::FromMilliseconds(10))
            $script:ADTTestRetryAttempt | Should -Be 3
        }
    }

    Context 'Parameter Forwarding' {
        It 'Forwards remaining arguments to the target command' {
            $result = Invoke-ADTCommandWithRetries -Command 'Invoke-ADTTestEchoValue' -SleepDuration ([System.TimeSpan]::FromMilliseconds(10)) -Value 'hello'
            $result | Should -Be 'hello'
        }
    }

    Context 'SleepDuration Validation' {
        It 'Throws when SleepDuration is zero' {
            { Invoke-ADTCommandWithRetries -Command 'Invoke-ADTTestAlwaysSucceed' -SleepDuration ([System.TimeSpan]::Zero) } | Should -Throw
        }

        It 'Throws when SleepDuration is negative' {
            { Invoke-ADTCommandWithRetries -Command 'Invoke-ADTTestAlwaysSucceed' -SleepDuration ([System.TimeSpan]::FromSeconds(-1)) } | Should -Throw
        }

        It 'Does not throw when SleepDuration is a small positive value' {
            { Invoke-ADTCommandWithRetries -Command 'Invoke-ADTTestAlwaysSucceed' -SleepDuration ([System.TimeSpan]::FromMilliseconds(10)) } | Should -Not -Throw
        }
    }

    Context 'MaximumElapsedTime Validation' {
        It 'Throws when MaximumElapsedTime is zero' {
            { Invoke-ADTCommandWithRetries -Command 'Invoke-ADTTestAlwaysSucceed' -SleepDuration ([System.TimeSpan]::FromMilliseconds(10)) -MaximumElapsedTime ([System.TimeSpan]::Zero) } | Should -Throw
        }

        It 'Throws when MaximumElapsedTime is negative' {
            { Invoke-ADTCommandWithRetries -Command 'Invoke-ADTTestAlwaysSucceed' -SleepDuration ([System.TimeSpan]::FromMilliseconds(10)) -MaximumElapsedTime ([System.TimeSpan]::FromSeconds(-1)) } | Should -Throw
        }
    }

    Context 'Input Validation' {
        It 'Throws when Command is null' {
            { Invoke-ADTCommandWithRetries -Command $null } | Should -Throw
        }
    }
}

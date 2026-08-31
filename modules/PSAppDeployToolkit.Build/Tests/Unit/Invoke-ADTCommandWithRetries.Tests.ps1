BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    # A probe that fails a set number of times before succeeding, so retry behaviour can be observed. It is
    # handed over as a CommandInfo because resolving it by name would need it in the global scope, and
    # because the module resolves its own commands through a table captured at import, which a mock on the
    # target would never be consulted for.
    $script:ProbeAttempts = 0
    function Test-ADTRetryProbe
    {
        param
        (
            [Parameter(Mandatory = $false)]
            [System.Int32]$FailTimes = 0,

            [Parameter(Mandatory = $false)]
            [System.String]$Return = 'succeeded'
        )

        $script:ProbeAttempts++
        if ($script:ProbeAttempts -le $FailTimes)
        {
            throw "deliberate failure $($script:ProbeAttempts)"
        }
        return $Return
    }
    $script:Probe = Get-Command Test-ADTRetryProbe
}

Describe 'Invoke-ADTCommandWithRetries' {
    BeforeEach {
        $script:ProbeAttempts = 0
    }

    Context 'Functionality' {
        It 'Runs the command once when it succeeds' {
            # Deliberately passes nothing through to the target. That leaves $Parameters null, which used to
            # throw out of Convert-ADTValuesFromRemainingArguments and broke every argument-free call.
            Invoke-ADTCommandWithRetries -Command $script:Probe | Should -BeExactly 'succeeded'
            $script:ProbeAttempts | Should -Be 1
        }

        It 'Resolves a command given by name, with no arguments of its own' {
            Invoke-ADTCommandWithRetries -Command Get-ADTPowerShellProcessPath | Should -Not -BeNullOrEmpty
        }

        It 'Resolves a command given by name, with arguments' {
            # The name path goes through the module's own command table rather than the CommandInfo branch
            # the rest of these use.
            Invoke-ADTCommandWithRetries -Command Get-ADTFreeDiskSpace -Drive $env:SystemDrive | Should -BeOfType ([System.Double])
        }

        It 'Retries until the command succeeds' {
            Invoke-ADTCommandWithRetries -Command $script:Probe -SleepDuration 0.01 -FailTimes 2 | Should -BeExactly 'succeeded'
            $script:ProbeAttempts | Should -Be 3
        }

        It 'Passes remaining arguments through to the command' {
            Invoke-ADTCommandWithRetries -Command $script:Probe -SleepDuration 0.01 -Return 'passed-through' | Should -BeExactly 'passed-through'
        }

        It 'Makes one more attempt than the retry count before giving up' {
            # -Retries counts the retries after the first attempt, so 2 means three attempts in all.
            { Invoke-ADTCommandWithRetries -Command $script:Probe -Retries 2 -SleepDuration 0.01 -FailTimes 99 } | Should -Throw
            $script:ProbeAttempts | Should -Be 3
        }

        It 'Surfaces the failure from the final attempt' {
            { Invoke-ADTCommandWithRetries -Command $script:Probe -Retries 1 -SleepDuration 0.01 -FailTimes 99 } | Should -Throw -ExpectedMessage 'deliberate failure 2'
        }

        It 'Waits the requested duration between attempts' {
            $elapsed = Measure-Command { { Invoke-ADTCommandWithRetries -Command $script:Probe -Retries 1 -SleepDuration 0.25 -FailTimes 99 } | Should -Throw }
            $script:ProbeAttempts | Should -Be 2
            $elapsed.TotalMilliseconds | Should -BeGreaterThan 200
        }

        It 'Stops retrying once the maximum elapsed time has passed' {
            $elapsed = Measure-Command { { Invoke-ADTCommandWithRetries -Command $script:Probe -SleepDuration 0.2 -MaximumElapsedTime 0.5 -FailTimes 99 } | Should -Throw }
            # The clock is checked after sleeping, so it can overshoot by one sleep, but it must give up well
            # before the default three retries would have.
            $elapsed.TotalSeconds | Should -BeLessThan 2
            $script:ProbeAttempts | Should -BeGreaterThan 1
        }
    }

    Context 'Input Validation' {
        It 'Should verify that -<Parameter> is greater than zero' -ForEach @(
            @{ Parameter = 'Retries' }
            @{ Parameter = 'SleepDuration' }
            @{ Parameter = 'MaximumElapsedTime' }
        ) {
            $splat = @{ Command = $script:Probe; $Parameter = 0 }
            { Invoke-ADTCommandWithRetries @splat } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,Invoke-ADTCommandWithRetries'
        }

        It 'Should accept a bare number as seconds for -<Parameter>' -ForEach @(
            @{ Parameter = 'SleepDuration' }
            @{ Parameter = 'MaximumElapsedTime' }
        ) {
            # TimeSpanTransformation reads a bare number as seconds, which is what the help's examples use.
            $splat = @{ Command = $script:Probe; Return = 'ok'; $Parameter = 90 }
            Invoke-ADTCommandWithRetries @splat | Should -BeExactly 'ok'
        }
    }
}

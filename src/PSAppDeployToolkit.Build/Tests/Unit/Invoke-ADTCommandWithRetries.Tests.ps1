BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    function global:Test-ADTRetrySuccess
    {
        param([System.Int32]$FailUntil = 0)
        $script:RetryCallCount++
        if ($script:RetryCallCount -le $FailUntil)
        {
            throw "Simulated transient failure on attempt $script:RetryCallCount"
        }
        return 'Success'
    }

    function global:Test-ADTRetryPermanentFail
    {
        $script:RetryCallCount++
        throw "Simulated permanent failure on attempt $script:RetryCallCount"
    }
}

AfterAll {
    Remove-Item Function:\Test-ADTRetrySuccess -ErrorAction SilentlyContinue
    Remove-Item Function:\Test-ADTRetryPermanentFail -ErrorAction SilentlyContinue
}

Describe 'Invoke-ADTCommandWithRetries' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        BeforeEach {
            $script:RetryCallCount = 0
        }

        It 'Returns the success value when the command succeeds on the first attempt' {
            $result = Invoke-ADTCommandWithRetries -Command Test-ADTRetrySuccess -FailUntil 0 -Retries 3 -SleepDuration ([System.TimeSpan]::FromMilliseconds(1))
            $result | Should -Be 'Success'
            $script:RetryCallCount | Should -Be 1
        }

        It 'Retries and succeeds when the command fails on the first attempt then succeeds' {
            $result = Invoke-ADTCommandWithRetries -Command Test-ADTRetrySuccess -FailUntil 1 -Retries 3 -SleepDuration ([System.TimeSpan]::FromMilliseconds(1))
            $result | Should -Be 'Success'
            $script:RetryCallCount | Should -Be 2
        }

        It 'Retries the configured number of times before returning success' {
            $result = Invoke-ADTCommandWithRetries -Command Test-ADTRetrySuccess -FailUntil 2 -Retries 5 -SleepDuration ([System.TimeSpan]::FromMilliseconds(1))
            $result | Should -Be 'Success'
            $script:RetryCallCount | Should -Be 3
        }

        It 'Rethrows after exhausting all retries when command always fails' {
            { Invoke-ADTCommandWithRetries -Command Test-ADTRetryPermanentFail -Retries 2 -SleepDuration ([System.TimeSpan]::FromMilliseconds(1)) } | Should -Throw
            $script:RetryCallCount | Should -Be 3
        }
    }

    Context 'Input Validation' {
        It 'Should have a mandatory Command parameter' {
            (Get-Command Invoke-ADTCommandWithRetries).Parameters['Command'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws ParameterArgumentValidationError when Command is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Invoke-ADTCommandWithRetries'
            }
            { Invoke-ADTCommandWithRetries -Command $null } | Should @shouldParams
        }

        It 'Throws ParameterArgumentValidationError when Command is empty string' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Invoke-ADTCommandWithRetries'
            }
            { Invoke-ADTCommandWithRetries -Command '' } | Should @shouldParams
        }

        It 'Throws ParameterArgumentValidationError when Retries is zero' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Invoke-ADTCommandWithRetries'
            }
            { Invoke-ADTCommandWithRetries -Command Test-ADTRetrySuccess -Retries 0 } | Should @shouldParams
        }
    }
}

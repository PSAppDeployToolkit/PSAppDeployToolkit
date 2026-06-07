BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Invoke-ADTFunctionErrorHandler' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Advanced-function harness mirroring how an ADT cmdlet handles errors:
        # Initialize-ADTFunction archives the caller's -ErrorAction into the
        # session state, then the catch block forwards the ErrorRecord to
        # Invoke-ADTFunctionErrorHandler which honours that archived preference.
        function Test-ErrorHarness
        {
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $false)]
                [System.Management.Automation.SwitchParameter]$Silent,

                [Parameter(Mandatory = $false)]
                [System.String]$LogMessage
            )

            Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
            try
            {
                throw [System.InvalidOperationException]::new('harness boom')
            }
            catch
            {
                $iaehParams = @{
                    Cmdlet = $PSCmdlet
                    SessionState = $ExecutionContext.SessionState
                    ErrorRecord = $_
                }
                if ($Silent) { $iaehParams.Add('Silent', $true) }
                if ($PSBoundParameters.ContainsKey('LogMessage')) { $iaehParams.Add('LogMessage', $LogMessage) }
                Invoke-ADTFunctionErrorHandler @iaehParams
            }
        }
    }

    Context 'ErrorActionPreference honouring' {
        It 'Throws a terminating error (rethrowing the original exception) when caller used -ErrorAction Stop' {
            { Test-ErrorHarness -ErrorAction Stop } | Should -Throw -ExceptionType ([System.InvalidOperationException]) -ExpectedMessage 'harness boom'
        }

        It 'Does not throw when caller used -ErrorAction SilentlyContinue' {
            { Test-ErrorHarness -ErrorAction SilentlyContinue } | Should -Not -Throw
        }

        It 'Does not throw when caller used -ErrorAction Ignore' {
            { Test-ErrorHarness -ErrorAction Ignore } | Should -Not -Throw
        }

        It 'Surfaces a terminating exception that preserves the original error message' {
            { Test-ErrorHarness -ErrorAction Stop } | Should -Throw -ExpectedMessage 'harness boom'
        }
    }

    Context 'Logging' {
        # Note: Initialize-ADTFunction emits its own 'Function Start' debug entry,
        # so assertions are scoped to the error-severity log the handler writes.
        It 'Writes the error to the log with Severity Error by default' {
            Test-ErrorHarness -ErrorAction SilentlyContinue 2>$null
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 1 -Exactly -ParameterFilter { $Severity -eq 'Error' }
        }

        It 'Does not write an error-severity log entry when -Silent is specified' {
            Test-ErrorHarness -ErrorAction SilentlyContinue -Silent 2>$null
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 0 -Exactly -ParameterFilter { $Severity -eq 'Error' }
        }

        It 'Includes the caller-supplied LogMessage in the logged error' {
            Test-ErrorHarness -ErrorAction SilentlyContinue -LogMessage 'Custom failure context' 2>$null
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 1 -Exactly -ParameterFilter { ($Severity -eq 'Error') -and ($Message -match 'Custom failure context') }
        }

        It 'Falls back to the ErrorRecord exception message when no LogMessage is supplied' {
            Test-ErrorHarness -ErrorAction SilentlyContinue 2>$null
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 1 -Exactly -ParameterFilter { ($Severity -eq 'Error') -and ($Message -match 'harness boom') }
        }
    }

    Context 'Input Validation' {
        It 'Has a mandatory <Param> parameter' -ForEach @(
            @{ Param = 'Cmdlet' }
            @{ Param = 'SessionState' }
            @{ Param = 'ErrorRecord' }
        ) {
            (Get-Command Invoke-ADTFunctionErrorHandler).Parameters[$Param].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws a parameter binding error when ErrorRecord is null' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Invoke-ADTFunctionErrorHandler'
            }
            { Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $null } | Should @shouldParams
        }
    }
}

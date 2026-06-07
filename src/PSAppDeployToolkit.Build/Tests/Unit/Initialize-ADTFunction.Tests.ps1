BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Initialize-ADTFunction' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Advanced-function harness that calls Initialize-ADTFunction the way a real
        # ADT cmdlet does, then surfaces the caller-scope variables it sets so that
        # tests can assert against them. The module forces its own
        # $InformationPreference to 'Continue', so the debug logging branch always
        # runs regardless of the caller's session preference.
        function Test-InitHarness
        {
            [CmdletBinding()]
            param()

            Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
            return [pscustomobject]@{
                OriginalErrorAction = $OriginalErrorAction
                ErrorActionPreference = $ErrorActionPreference
            }
        }

        # Harness that always binds a parameter so the "invoked with bound
        # parameter(s)" logging branch is exercised.
        function Test-InitHarnessWithParam
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Name', Justification = 'The parameter exists solely so the caller binds a parameter, exercising the bound-parameter logging branch in Initialize-ADTFunction.')]
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $false)]
                [System.String]$Name
            )

            Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        }
    }

    Context 'Caller-scope variable handling' {
        It 'Forces the caller ErrorActionPreference to Stop' {
            $result = Test-InitHarness
            $result.ErrorActionPreference | Should -Be ([System.Management.Automation.ActionPreference]::Stop)
        }

        It 'Archives the module default (Stop) into OriginalErrorAction when caller binds no -ErrorAction' {
            $result = Test-InitHarness
            $result.OriginalErrorAction | Should -Be 'Stop'
        }

        It "Archives the caller's bound -ErrorAction value into OriginalErrorAction (<EA>)" -ForEach @(
            @{ EA = 'SilentlyContinue' }
            @{ EA = 'Continue' }
            @{ EA = 'Ignore' }
            @{ EA = 'Stop' }
        ) {
            $result = Test-InitHarness -ErrorAction $EA
            $result.OriginalErrorAction | Should -Be $EA
        }

        It 'Stores OriginalErrorAction as a string (so Ignore round-trips safely)' {
            $result = Test-InitHarness -ErrorAction Ignore
            $result.OriginalErrorAction | Should -BeOfType ([System.String])
        }
    }

    Context 'Logging' {
        It 'Logs Function Start as a debug message' {
            $null = Test-InitHarness
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 1 -Exactly -ParameterFilter { ($Message -eq 'Function Start') -and $DebugMessage }
        }

        It 'Logs that the function was invoked without bound parameters when none were supplied' {
            $null = Test-InitHarness
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 1 -Exactly -ParameterFilter { $Message -eq 'Function invoked without any bound parameters.' }
        }

        It 'Logs the bound parameter table when the caller supplied parameters' {
            Test-InitHarnessWithParam -Name 'Example'
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 1 -Exactly -ParameterFilter { $Message -match '^Function invoked with bound parameter' }
        }
    }

    Context 'Input Validation' {
        It 'Has a mandatory Cmdlet parameter' {
            (Get-Command Initialize-ADTFunction).Parameters['Cmdlet'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Has a non-mandatory SessionState parameter' {
            (Get-Command Initialize-ADTFunction).Parameters['SessionState'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        }

        It 'Throws a parameter binding error when Cmdlet is null' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Initialize-ADTFunction'
            }
            { Initialize-ADTFunction -Cmdlet $null } | Should @shouldParams
        }
    }
}

BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Complete-ADTFunction' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Advanced-function harness that calls Complete-ADTFunction the way a real
        # ADT cmdlet finalizes itself in its end{} block.
        function Test-CompleteHarness
        {
            [CmdletBinding()]
            param()

            Complete-ADTFunction -Cmdlet $PSCmdlet
        }
    }

    Context 'Logging' {
        It 'Logs Function End as a debug message' {
            Test-CompleteHarness
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 1 -Exactly -ParameterFilter { ($Message -eq 'Function End') -and $DebugMessage }
        }

        It 'Sources the log entry from the calling function name' {
            Test-CompleteHarness
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 1 -Exactly -ParameterFilter { ($Message -eq 'Function End') -and ($Source -eq 'Test-CompleteHarness') }
        }
    }

    Context 'Behaviour' {
        It 'Does not throw when invoked from a valid cmdlet context' {
            { Test-CompleteHarness } | Should -Not -Throw
        }

        It 'Returns no output' {
            $result = Test-CompleteHarness
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Has a mandatory Cmdlet parameter' {
            (Get-Command Complete-ADTFunction).Parameters['Cmdlet'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws a parameter binding error when Cmdlet is null' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Complete-ADTFunction'
            }
            { Complete-ADTFunction -Cmdlet $null } | Should @shouldParams
        }
    }
}

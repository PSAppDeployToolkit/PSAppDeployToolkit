BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Initialize-ADTFunction' {
    Context 'Functionality' {
        It 'Overrides the caller''s error action preference' {
            # Every module function calls this first so that it stops on a dime regardless of what the
            # caller had set, which is what makes the try/catch wrappers around each body work.
            function Test-Probe
            {
                [CmdletBinding()]
                [OutputType([System.Management.Automation.ActionPreference])]
                param
                (
                )

                $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Continue
                Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
                return $ErrorActionPreference
            }
            Test-Probe | Should -Be ([System.Management.Automation.ActionPreference]::Stop)
        }

        It 'Archives the caller''s own error action as OriginalErrorAction' {
            # Invoke-ADTFunctionErrorHandler reads this back to decide whether to rethrow, so what the
            # caller asked for has to survive the override above.
            function Test-Probe
            {
                [CmdletBinding()]
                param
                (
                )

                Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
                return $OriginalErrorAction
            }
            Test-Probe -ErrorAction Ignore | Should -BeExactly 'Ignore'
        }

        It 'Falls back to the module default when the caller specified nothing' {
            function Test-Probe
            {
                [CmdletBinding()]
                param
                (
                )

                Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
                return $OriginalErrorAction
            }
            # Compared against the module's own preference rather than a literal, since that is the value
            # the function reaches for and the two must not drift apart.
            Test-Probe | Should -BeExactly (InModuleScope PSAppDeployToolkit { $ErrorActionPreference.ToString() })
        }

        It 'Sets the variables in the caller rather than in itself' {
            # The whole point is reaching up a scope, so a probe that never called it must be untouched.
            function Test-Untouched
            {
                [CmdletBinding()]
                param
                (
                )

                return (Get-Variable -Name OriginalErrorAction -ErrorAction Ignore)
            }
            Test-Untouched | Should -BeNullOrEmpty
        }

        It 'Requires a cmdlet to work against' {
            { Initialize-ADTFunction -SessionState $ExecutionContext.SessionState } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

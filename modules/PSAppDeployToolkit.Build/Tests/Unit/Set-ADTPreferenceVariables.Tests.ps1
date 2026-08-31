BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Set-ADTPreferenceVariables' {
    Context 'Functionality' {
        BeforeAll {
            # The function walks the call stack looking for a caller that bound a common parameter, then
            # sets the matching preference variable in the session state it was handed. This probe is that
            # caller: it binds nothing itself, so whatever reaches it came from the outer call.
            function Get-ProbePreference
            {
                [CmdletBinding()]
                param
                (
                    [Parameter(Mandatory = $true)]
                    [System.String]$Variable
                )

                InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ State = $ExecutionContext.SessionState } {
                    Set-ADTPreferenceVariables -SessionState $State
                }
                return (Get-Variable -Name $Variable -ValueOnly -ErrorAction Ignore)
            }
        }

        It 'Carries -<Parameter> down as $<Variable>' -ForEach @(
            @{ Parameter = 'Verbose'; Variable = 'VerbosePreference'; Expected = [System.Management.Automation.ActionPreference]::Continue }
            @{ Parameter = 'Debug'; Variable = 'DebugPreference'; Expected = [System.Management.Automation.ActionPreference]::Continue }
        ) {
            # The reason the function exists: a switch such as -Verbose does not reach a callee on its own,
            # so it is translated into the preference variable the callee will actually read.
            $splat = @{ Variable = $Variable; $Parameter = $true }
            Get-ProbePreference @splat | Should -Be $Expected
        }

        It 'Carries -ErrorAction down as $ErrorActionPreference' {
            Get-ProbePreference -Variable 'ErrorActionPreference' -ErrorAction Ignore | Should -Be ([System.Management.Automation.ActionPreference]::Ignore)
        }

        It 'Leaves a preference alone when the caller bound nothing' {
            # No common parameter is supplied, so the ambient value should survive untouched.
            $before = $VerbosePreference
            $null = Get-ProbePreference -Variable 'VerbosePreference'
            $VerbosePreference | Should -Be $before
        }

        It 'Ignores a switch that was explicitly turned off' {
            # -Verbose:$false is bound but not present, and should not be promoted to Continue.
            Get-ProbePreference -Variable 'VerbosePreference' -Verbose:$false | Should -Not -Be ([System.Management.Automation.ActionPreference]::Continue)
        }

        It 'Rejects a scope of zero' {
            # ValidateGreaterThanZero on -Scope; scope 0 would be the function's own frame, which is useless.
            {
                InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ State = $ExecutionContext.SessionState } {
                    Set-ADTPreferenceVariables -SessionState $State -Scope 0
                }
            } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,*'
        }
    }
}

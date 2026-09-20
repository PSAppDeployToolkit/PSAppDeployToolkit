BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Invoke-ADTClientServerOperation' {
    # Contract only. Every operation this offers reaches the client running in the logged-on user's
    # session, and the ones worth asserting against are covered through the functions that call it:
    # Test-ADTNotifyIconOpen, Test-ADTInstallationProgressOpen and Start-ADTProcessAsUser among them.
    Context 'Input Validation' {
        It 'Requires an operation to perform' {
            # Each operation is its own switch in its own parameter set, so a call naming none of them
            # cannot resolve to anything.
            Test-ADTParameterSetSatisfied -Command (InModuleScope PSAppDeployToolkit { Get-Command Invoke-ADTClientServerOperation }) | Should -BeFalse
        }

        It 'Refuses two operations at once' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Invoke-ADTClientServerOperation -ProgressDialogOpen -NotifyIconOpen -User (Get-ADTClientServerUser) } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }

        It 'Requires a user to perform it as' {
            # The client belongs to a logged-on session, so there is nowhere to run an operation without
            # naming whose session it is.
            Test-ADTParameterSetSatisfied -Command (InModuleScope PSAppDeployToolkit { Get-Command Invoke-ADTClientServerOperation }) -Parameter ProgressDialogOpen | Should -BeFalse
        }

        It 'Refuses something that is not a user' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Invoke-ADTClientServerOperation -ProgressDialogOpen -User 'not a user' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }

        It 'Requires a silent restart to be asked for without waiting' {
            # SilentRestart has no counterpart on ServerInstance, so it only works via the argv route that
            # -NoWait selects. Without it the call reaches a SilentRestartAsync() that does not exist,
            # which being mandatory turns into a binding failure instead.
            Test-ADTParameterSetSatisfied -Command (InModuleScope PSAppDeployToolkit { Get-Command Invoke-ADTClientServerOperation }) -Parameter SilentRestart, User, Options | Should -BeFalse
            Test-ADTParameterSetSatisfied -Command (InModuleScope PSAppDeployToolkit { Get-Command Invoke-ADTClientServerOperation }) -Parameter SilentRestart, User, Options, NoWait | Should -BeTrue
        }

        It 'Requires a silent restart to say what it is restarting for' {
            # The countdown, the reason and the force-close switch all travel as one serialized object
            # now, so a call without it reaches the client with nothing to act on.
            Test-ADTParameterSetSatisfied -Command (InModuleScope PSAppDeployToolkit { Get-Command Invoke-ADTClientServerOperation }) -Parameter SilentRestart, User, NoWait | Should -BeFalse
        }
    }
}

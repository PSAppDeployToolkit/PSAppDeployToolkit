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
    }
}

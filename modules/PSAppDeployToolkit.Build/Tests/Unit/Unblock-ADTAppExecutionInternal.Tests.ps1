BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Unblock-ADTAppExecutionInternal' {
    # Contract only. This is the worker Unblock-ADTAppExecution calls and the scheduled task it registers
    # runs at startup, so everything it does is to the machine's execution options and task library.
    Context 'Input Validation' {
        It 'Refuses a blank task name' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Unblock-ADTAppExecutionInternal -TaskName '   ' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }

        It 'Refuses a name and a set of tasks together' {
            # They are the two ways of naming what to clean up, and one of them has to win.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Unblock-ADTAppExecutionInternal -TaskName 'anything' -Tasks @() } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }
    }
}

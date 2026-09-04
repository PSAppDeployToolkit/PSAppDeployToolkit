BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Invoke-ADTTerminalServerModeChange' {
    # Contract only. Either mode changes how the machine installs software for the rest of its uptime,
    # which is not something a test run gets to do to it.
    Context 'Input Validation' {
        It 'Refuses a mode it does not know' {
            # Install and Execute are the only two states, so anything else would have to guess.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Invoke-ADTTerminalServerModeChange -Mode 'Frobnicate' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }

        It 'Requires a mode' {
            Test-ADTMandatoryParameter -Command (InModuleScope PSAppDeployToolkit { Get-Command Invoke-ADTTerminalServerModeChange }) -Parameter Mode | Should -BeTrue
        }
    }
}

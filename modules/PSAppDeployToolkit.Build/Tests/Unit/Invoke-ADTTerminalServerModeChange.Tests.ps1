BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Invoke-ADTTerminalServerModeChange' {
    # change.exe is stood in for throughout. Either mode changes how the machine installs software for the
    # rest of its uptime, which is not something a test run gets to do to it, and standing it in for is what
    # lets the reading of its exit code be tested at all.
    Context 'Reporting' {
        It 'Takes an exit code of 1 for the success it is' {
            # change.exe reports a successful change with 1, so testing for zero would call every one of
            # them a failure and refuse to install anything on a terminal server.
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { [PSADT.ProcessManagement.ProcessResult]::new(1, $null, $null, $null) }
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Invoke-ADTTerminalServerModeChange -Mode Install } | Should -Not -Throw
            }
        }

        It 'Refuses any other exit code, zero included' {
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { [PSADT.ProcessManagement.ProcessResult]::new(0, $null, $null, $null) }
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Invoke-ADTTerminalServerModeChange -Mode Install } | Should -Throw -ErrorId 'RdsChangeUtilityFailure,Invoke-ADTTerminalServerModeChange'
            }
        }

        It 'Says what change.exe reported' {
            # The exit code alone says nothing about why a terminal server would not change mode, so the
            # utility's own words have to reach the log.
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { [PSADT.ProcessManagement.ProcessResult]::new(2, $null, [System.String[]]('User session is in use'), [System.String[]]('User session is in use')) }
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Invoke-ADTTerminalServerModeChange -Mode Install } | Should -Throw
            }
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*User session is in use*' }
        }

        It 'Carries what change.exe reported on the error it throws' {
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { [PSADT.ProcessManagement.ProcessResult]::new(2, $null, [System.String[]]('User session is in use'), [System.String[]]('User session is in use')) }
            InModuleScope -ModuleName PSAppDeployToolkit {
                $thrown = { Invoke-ADTTerminalServerModeChange -Mode Install } | Should -Throw -PassThru
                $thrown.Exception | Should -BeOfType ([PSADT.ProcessManagement.ProcessException])
                $thrown.Exception.Result.ExitCode | Should -Be 2
                $thrown.Exception.Result.Interleaved | Should -Be 'User session is in use'
            }
        }
    }

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

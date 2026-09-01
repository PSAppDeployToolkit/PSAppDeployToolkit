BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}
Describe 'Start-ADTProcessAsUser' {
    Context 'Running a process' {
        It 'Returns a result with -PassThru' {
            # The process is started in the logged-on user's session through the client, so this covers
            # that round trip as well as the function itself.
            $result = Start-ADTProcessAsUser -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PassThru
            $result | Should -BeOfType ([PSADT.ProcessManagement.ProcessResult])
            $result.ExitCode | Should -Be 0
        }

        It 'Returns nothing unless asked' {
            Start-ADTProcessAsUser -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow | Should -BeNullOrEmpty
        }

        It 'Captures standard output' {
            (Start-ADTProcessAsUser -FilePath cmd.exe -ArgumentList '/c', 'echo captured-out' -CreateNoWindow -PassThru).StdOut | Should -Contain 'captured-out'
        }

        It 'Fails on an exit code it was not told about' {
            { Start-ADTProcessAsUser -FilePath cmd.exe -ArgumentList '/c', 'exit 3' -CreateNoWindow } | Should -Throw
        }

        It 'Accepts an exit code nominated as success' {
            (Start-ADTProcessAsUser -FilePath cmd.exe -ArgumentList '/c', 'exit 3' -CreateNoWindow -PassThru -SuccessExitCodes 3).ExitCode | Should -Be 3
        }

        It 'Starts nothing with -WhatIf' {
            Start-ADTProcessAsUser -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PassThru -WhatIf | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Requires something to run' {
            { Start-ADTProcessAsUser } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a blank file path' {
            { Start-ADTProcessAsUser -FilePath '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTProcessAsUser'
        }

        It 'Refuses a user that is not an account' {
            { Start-ADTProcessAsUser -Username '' -FilePath cmd.exe } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a window style alongside no window at all' {
            { Start-ADTProcessAsUser -FilePath cmd.exe -CreateNoWindow -WindowStyle Hidden } | Should -Throw -ErrorId 'AmbiguousParameterSet,Start-ADTProcessAsUser'
        }
    }
}

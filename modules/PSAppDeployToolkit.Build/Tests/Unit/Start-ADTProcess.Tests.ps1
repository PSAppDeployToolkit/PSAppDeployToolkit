BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Start-ADTProcess' {
    Context 'Running a process' {
        It 'Returns nothing unless asked' {
            Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow | Should -BeNullOrEmpty
        }

        It 'Returns a result with -PassThru' {
            $result = Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PassThru
            $result | Should -BeOfType ([PSADT.ProcessManagement.ProcessResult])
            $result.ExitCode | Should -Be 0
        }

        It 'Records the command line it built' {
            # Deployments are diagnosed from the log long after the fact, so what was actually run has to
            # travel with the result rather than having to be reconstructed from the parameters.
            $result = Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PassThru
            $result.CommandLine | Should -BeLike '*cmd.exe*'
            $result.CommandLine | Should -BeLike '*exit 0*'
        }

        It 'Captures standard output' {
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'echo captured-out' -CreateNoWindow -PassThru).StdOut | Should -Contain 'captured-out'
        }

        It 'Captures standard error' {
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'echo captured-err 1>&2' -CreateNoWindow -PassThru).StdErr | Should -Contain 'captured-err'
        }

        It 'Keeps the two streams apart' {
            # An installer writing progress to stderr is common enough that conflating the two would have
            # every second deployment looking like it failed.
            $result = Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'echo to-out & echo to-err 1>&2' -CreateNoWindow -PassThru
            $result.StdOut | Should -Contain 'to-out'
            $result.StdOut | Should -Not -Contain 'to-err'
            $result.StdErr | Should -Contain 'to-err'
        }

        It 'Offers both streams together as well' {
            $result = Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'echo to-out & echo to-err 1>&2' -CreateNoWindow -PassThru
            $result.Interleaved | Should -Contain 'to-out'
            $result.Interleaved | Should -Contain 'to-err'
        }

        It 'Runs in the working directory it was given' {
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'cd' -CreateNoWindow -PassThru -WorkingDirectory $TestDrive).StdOut | Should -Contain "$TestDrive"
        }

        It 'Expands environment variables in the file path when asked' {
            # Vendor documentation routinely gives paths as %ProgramFiles%\Thing\setup.exe.
            (Start-ADTProcess -FilePath '%ComSpec%' -ArgumentList '/c', 'exit 0' -CreateNoWindow -PassThru -ExpandEnvironmentVariables).ExitCode | Should -Be 0
        }
    }

    Context 'Exit codes' {
        It 'Fails on an exit code it was not told about' {
            { Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 3' -CreateNoWindow } | Should -Throw -ErrorId 'ProcessExitCodeError,Start-ADTProcess'
        }

        It 'Accepts an exit code nominated as success' {
            # Installers signalling "already installed" with their own code are the norm, not the exception.
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 3' -CreateNoWindow -PassThru -SuccessExitCodes 3).ExitCode | Should -Be 3
        }

        It 'Still reports the real code when it was nominated as success' {
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 3' -CreateNoWindow -PassThru -SuccessExitCodes 0, 3).ExitCode | Should -Be 3
        }

        It 'Hands back the result when the caller silences the failure' {
            # -PassThru with -ErrorAction SilentlyContinue is the documented way to inspect a failure.
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 3' -CreateNoWindow -PassThru -ErrorAction SilentlyContinue).ExitCode | Should -Be 3
        }
    }

    Context 'Not waiting' {
        It 'Returns a handle rather than a result' {
            $handle = Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -NoWait -PassThru
            $handle | Should -BeOfType ([PSADT.ProcessManagement.ProcessHandle])
        }

        It 'Carries something the caller can wait on' {
            $handle = Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -NoWait -PassThru
            $handle.Task | Should -Not -BeNullOrEmpty
            $null = $handle.Task.GetAwaiter().GetResult()
            $handle.IsCompleted | Should -BeTrue
        }
    }

    Context 'Timing out' {
        It 'Terminates a process that takes too long' {
            { Start-ADTProcess -FilePath ping.exe -ArgumentList '-n', '30', '127.0.0.1' -CreateNoWindow -Timeout ([System.TimeSpan]::FromSeconds(1)) } | Should -Throw -ErrorId 'ProcessExecutionCancelled,Start-ADTProcess'
        }

        It 'Gives up rather than waiting the process out' {
            # A timeout that only reported afterwards would leave a deployment hanging for exactly as long
            # as it was trying to avoid.
            $elapsed = [System.Diagnostics.Stopwatch]::StartNew()
            $null = Start-ADTProcess -FilePath ping.exe -ArgumentList '-n', '30', '127.0.0.1' -CreateNoWindow -PassThru -Timeout ([System.TimeSpan]::FromSeconds(1)) -ErrorAction SilentlyContinue
            $elapsed.Stop()
            $elapsed.Elapsed.TotalSeconds | Should -BeLessThan 15
        }

        It 'Reports a timeout rather than a cancellation when it left the process running' {
            # -NoTerminateOnTimeout is for GUI installers that are expected to outlive the wait, so the
            # distinction is what tells the caller whether anything was killed.
            { Start-ADTProcess -FilePath ping.exe -ArgumentList '-n', '30', '127.0.0.1' -CreateNoWindow -Timeout ([System.TimeSpan]::FromSeconds(1)) -NoTerminateOnTimeout } | Should -Throw -ErrorId 'ProcessExecutionTimedOut,Start-ADTProcess'
        }

        It 'Follows -TimeoutAction ahead of the caller''s error action' {
            { Start-ADTProcess -FilePath ping.exe -ArgumentList '-n', '30', '127.0.0.1' -CreateNoWindow -Timeout ([System.TimeSpan]::FromSeconds(1)) -TimeoutAction SilentlyContinue } | Should -Not -Throw
        }

        It 'Reads a bare number as seconds' {
            { Start-ADTProcess -FilePath ping.exe -ArgumentList '-n', '30', '127.0.0.1' -CreateNoWindow -Timeout 1 } | Should -Throw -ErrorId 'ProcessExecutionCancelled,Start-ADTProcess'
        }
    }

    Context 'Input Validation' {
        It 'Refuses a blank file path' {
            { Start-ADTProcess -FilePath '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTProcess'
        }

        It 'Reports a file path that is not there' {
            { Start-ADTProcess -FilePath 'C:\ADTNoSuchDirectory\nothing.exe' -CreateNoWindow } | Should -Throw
        }

        It 'Reports a working directory that is not there' {
            { Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -WorkingDirectory 'C:\ADTNoSuchDirectory' } | Should -Throw -ErrorId 'LiteralPathNotFound,Start-ADTProcess'
        }

        It 'Refuses to both wait and not wait' {
            { Start-ADTProcess -FilePath cmd.exe -NoWait -Timeout ([System.TimeSpan]::FromSeconds(1)) } | Should -Throw -ErrorId 'AmbiguousParameterSet,Start-ADTProcess'
        }

        It 'Refuses a window style alongside no window at all' {
            { Start-ADTProcess -FilePath cmd.exe -CreateNoWindow -WindowStyle Hidden } | Should -Throw -ErrorId 'AmbiguousParameterSet,Start-ADTProcess'
        }

        It 'Refuses a priority class it does not know' {
            { Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PriorityClass 'Urgent' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

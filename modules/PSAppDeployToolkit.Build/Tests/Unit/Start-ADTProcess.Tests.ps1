BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    # A package that is not there, so that msiexec fails at opening it rather than installing anything.
    $script:AbsentPackage = "$TestDrive\NeverExisted.msi"
}

AfterAll {
    Import-ADTModuleUnderTest -Force
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

    Context 'Reporting the exit code' {
        It 'Explains an installer exit code in the installer''s own terms' {
            # msiexec reports failures as Windows Installer codes, which mean nothing on their own, so the
            # message is looked up rather than left as a number. Pointed at a package that is not there,
            # so it fails at opening it rather than installing anything.
            Start-ADTProcess -FilePath 'msiexec.exe' -ArgumentList '/i', $script:AbsentPackage, '/qn' -CreateNoWindow -ErrorAction SilentlyContinue
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*ERROR_INSTALL_PACKAGE_OPEN_FAILED*' }
        }

        It 'Reports a code nominated as needing a reboot' {
            Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 3010' -CreateNoWindow -RebootExitCodes 3010
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*A reboot is required*' }
        }

        It 'Reports a code it was told to ignore as ignored' {
            Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 5' -CreateNoWindow -IgnoreExitCodes 5 -WarningAction SilentlyContinue
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*is being ignored*' }
        }

        It 'Ignores every code when told to ignore all of them' {
            { Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 7' -CreateNoWindow -IgnoreExitCodes '*' -WarningAction SilentlyContinue } | Should -Not -Throw
        }

        It 'Says that ignoring exit codes is on its way out' {
            # Deprecated in favour of -SuccessExitCodes and -RebootExitCodes, so anyone still using it
            # needs to hear about it while it still works.
            Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 5' -CreateNoWindow -IgnoreExitCodes 5 -WarningAction SilentlyContinue
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*obsolete*' -and $Severity -eq 'Warning' }
        }
    }

    Context 'How the process is launched' {
        It 'Keeps the arguments out of the execution log with -SecureArgumentList' {
            # Installers are handed credentials and licence keys on the command line, and the log is
            # written to disk and collected.
            Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0', 'a-secret-value' -CreateNoWindow -SecureArgumentList
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Parameters Hidden*' }
        }

        It 'Keeps the argument values out of the log entirely with -SecureArgumentList' {
            # Not just out of the execution message: the debug dump of every bound parameter has to
            # withhold them too, since that is written on exactly the runs whose logs get collected.
            Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0', 'a-secret-value' -CreateNoWindow -SecureArgumentList
            Should -Not -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*a-secret-value*' }
        }

        It 'Says the streams are unavailable when it was not asked for a hidden window' {
            # Redirecting the streams requires the process to be created without a window, so a caller who
            # wants a window has to be told the output will not be captured.
            Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -WindowStyle Hidden
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*streams will not be available*' }
        }

        It 'Waits for the installer mutex when asked' {
            # Two installers at once is what the mutex exists to prevent, so a caller running one has to
            # be able to queue behind whatever else is going.
            { Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -WaitForMsiExec } | Should -Not -Throw
        }

        It 'Runs without capturing the streams when asked' {
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'echo nothing-captured' -CreateNoWindow -NoStreamLogging -PassThru).ExitCode | Should -Be 0
        }

        It 'Accepts a priority to run at' {
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PriorityClass BelowNormal -PassThru).ExitCode | Should -Be 0
        }

        It 'Accepts an encoding for the streams' {
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'echo encoded' -CreateNoWindow -StreamEncoding ([System.Text.Encoding]::UTF8) -PassThru).StdOut | Should -Contain 'encoded'
        }

        It 'Runs with an unelevated token when asked' {
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -UseUnelevatedToken -PassThru).ExitCode | Should -Be 0
        }

        It 'Reports the installer being busy rather than queueing forever' {
            # The mutex is taken on another thread so that this call finds it held, which is what happens
            # when another installer is already running. Held for as long as the wait below and no longer.
            $shell = [System.Management.Automation.PowerShell]::Create()
            ($shell.Runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()).Open()
            $taken = [System.Threading.ManualResetEventSlim]::new($false)
            $release = [System.Threading.CancellationTokenSource]::new()
            $shell.Runspace.SessionStateProxy.SetVariable('taken', $taken)
            $shell.Runspace.SessionStateProxy.SetVariable('release', $release)
            $async = $shell.AddScript({
                    $mutex = [System.Threading.Mutex]::new($false, 'Global\_MSIExecute')
                    try
                    {
                        if ($mutex.WaitOne(1000))
                        {
                            $taken.Set()
                            [void]$release.Token.WaitHandle.WaitOne(20000)
                            $mutex.ReleaseMutex()
                        }
                    }
                    finally
                    {
                        $mutex.Dispose()
                    }
                }).BeginInvoke()
            try
            {
                if (!$taken.Wait(5000))
                {
                    Set-ItResult -Skipped -Because 'the installer mutex could not be taken to test against'
                }
                { Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -WaitForMsiExec -MsiExecWaitTime ([System.TimeSpan]::FromSeconds(1)) } | Should -Throw -ErrorId 'MsiExecUnavailable,Start-ADTProcess'
            }
            finally
            {
                $release.Cancel()
                $null = $shell.EndInvoke($async)
                $shell.Runspace.Dispose()
                $shell.Dispose()
                $release.Dispose()
                $taken.Dispose()
            }
        }

        It 'Accepts <Switch> as a way of handling child processes' -ForEach @(
            @{ Switch = 'KillChildProcessesWithParent' }
            @{ Switch = 'WaitForChildProcesses' }
        ) {
            $splat = @{ $Switch = $true }
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PassThru @splat).ExitCode | Should -Be 0
        }
    }

    Context 'Running as the active user' {
        BeforeAll {
            $script:ActiveUser = InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTClientServerUser }
        }

        It 'Runs the process in the user''s session' {
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -RunAsActiveUser $script:ActiveUser -PassThru).ExitCode | Should -Be 0
        }

        It 'Hands the process to the shell when asked' {
            # ShellExecute is how a document or a registered file type gets opened as the user would, and
            # it gives up the output streams in exchange.
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -RunAsActiveUser $script:ActiveUser -UseShellExecute -PassThru).ExitCode | Should -Be 0
        }

        It 'Accepts a verb for the shell to use' {
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -RunAsActiveUser $script:ActiveUser -UseShellExecute -Verb 'open' -PassThru).ExitCode | Should -Be 0
        }

        It 'Accepts <Switch> when choosing the token' -ForEach @(
            @{ Switch = 'UseLinkedAdminToken' }
            @{ Switch = 'UseHighestAvailableToken' }
            @{ Switch = 'DenyUserTermination' }
            @{ Switch = 'InheritEnvironmentVariables' }
        ) {
            # Which token the process gets decides what it can do, and each of these picks a different one.
            $splat = @{ $Switch = $true }
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -RunAsActiveUser $script:ActiveUser -PassThru @splat).ExitCode | Should -Be 0
        }
    }

    Context 'Within a deployment session' {
        BeforeEach {
            $script:Deploy = "$TestDrive\Deploy$([System.Guid]::NewGuid().ToString('N'))"
            $null = New-Item -Path "$script:Deploy\Files" -ItemType Directory -Force
            $script:Session = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ProcessSession' -DeployMode Silent -ScriptDirectory $script:Deploy -PassThru -InformationAction SilentlyContinue
        }

        AfterEach {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Runs an installer from the deployment''s own Files folder' {
            # An installer given by name rather than by path is expected to sit alongside the deployment,
            # so that is where it is run from. msiexec is the case that cannot take its directory from the
            # executable, since that is a Windows one.
            Start-ADTProcess -FilePath 'msiexec.exe' -ArgumentList '/i', $script:AbsentPackage, '/qn' -CreateNoWindow -ErrorAction SilentlyContinue
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like "*$($script:Session.DirFiles)*" }
        }

        It 'Records a successful exit code against the deployment' {
            # A deployment's exit code is set from every process it runs, not only the ones that fail, so
            # that a run which succeeds throughout reports as much.
            Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow
            $script:Session.GetExitCode() | Should -Be 0
        }

        It 'Runs in the user''s session from within a deployment' {
            # Reaching the user's session from a deployment is the case that has to make the deployment's
            # own content readable to them first.
            $user = InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTClientServerUser }
            (Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 0' -CreateNoWindow -RunAsActiveUser $user -PassThru).ExitCode | Should -Be 0
        }

        It 'Records a failing exit code against the deployment' {
            # This is how a deployment ends up reporting the installer's own code rather than a generic
            # failure, which is what the reporting on the other side keys off.
            { Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 9' -CreateNoWindow } | Should -Throw
            $script:Session.GetExitCode() | Should -Be 9
        }

        It 'Leaves the deployment''s exit code alone when the caller silenced the failure' {
            # Silencing it says the caller is handling the code themselves, so the deployment must not be
            # marked as failed behind their back.
            Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 9' -CreateNoWindow -ErrorAction SilentlyContinue
            $script:Session.GetExitCode() | Should -Be 0
        }

        It 'Takes the deployment''s reboot exit codes without being told them' {
            # The session carries the codes the package is known to return, so a caller does not have to
            # repeat them at every call.
            $script:Session.AppRebootExitCodes | Should -Contain 3010
            Start-ADTProcess -FilePath cmd.exe -ArgumentList '/c', 'exit 3010' -CreateNoWindow
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*A reboot is required*' }
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

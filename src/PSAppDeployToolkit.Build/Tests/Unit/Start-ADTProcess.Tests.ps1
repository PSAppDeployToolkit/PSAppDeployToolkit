BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
    Import-Module "$PSScriptRoot\..\Support\TestFixtures.psm1" -Force

    # Compile the real fake-installer executable once for the whole file. Start-ADTProcess is
    # exercised against this genuine subprocess so exit-code handling, stream capture, timeout
    # behaviour and the result shape are all validated end-to-end rather than against mocks.
    $script:FakeExe = Get-ADTFakeInstaller -OutputPath (Join-Path $TestDrive 'FakeInstaller.exe')
}

Describe 'Start-ADTProcess' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Input Validation' {
        It 'Has a mandatory FilePath parameter' {
            (Get-Command Start-ADTProcess).Parameters['FilePath'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws ParameterArgumentValidationError when FilePath is <Name>' -ForEach @(
            @{ Name = 'empty'; Value = '' }
            @{ Name = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Start-ADTProcess'
            }
            { Start-ADTProcess -FilePath $Value -CreateNoWindow } | Should @shouldParams
        }

        It 'Throws ParameterArgumentValidationError when FilePath is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Start-ADTProcess'
            }
            { Start-ADTProcess -FilePath $null -CreateNoWindow } | Should @shouldParams
        }

        It 'Throws ParameterArgumentTransformationError when Timeout is not a TimeSpan' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,Start-ADTProcess'
            }
            { Start-ADTProcess -FilePath $script:FakeExe -CreateNoWindow -Timeout 'not-a-timespan' } | Should @shouldParams
        }

        It 'Throws when Timeout is zero (ValidateGreaterThanZero)' {
            { Start-ADTProcess -FilePath $script:FakeExe -CreateNoWindow -Timeout ([System.TimeSpan]::Zero) } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Start-ADTProcess'
        }
    }

    Context 'Exit code handling (real subprocess)' {
        It 'Returns ExitCode 0 and a ProcessResult when the process succeeds' {
            $result = Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--exit-code', '0' -CreateNoWindow -PassThru -ErrorAction Stop
            $result | Should -BeOfType ([PSADT.ProcessManagement.ProcessResult])
            $result.ExitCode | Should -Be 0
        }

        It 'Throws an ExternalException with ProcessExitCodeError when the exit code is a failure' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Runtime.InteropServices.ExternalException]
                ErrorId       = 'ProcessExitCodeError,Start-ADTProcess'
            }
            { Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--exit-code', '5' -CreateNoWindow -ErrorAction Stop } | Should @shouldParams
        }

        It 'Treats a custom SuccessExitCodes value as success' {
            $result = Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--exit-code', '5' -CreateNoWindow -SuccessExitCodes 5 -PassThru -ErrorAction Stop
            $result.ExitCode | Should -Be 5
        }

        It 'Does not throw for the default reboot exit code 3010' {
            $result = Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--exit-code', '3010' -CreateNoWindow -PassThru -ErrorAction Stop
            $result.ExitCode | Should -Be 3010
        }

        It 'Returns the result without throwing when ErrorAction is SilentlyContinue and exit code fails' {
            $result = Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--exit-code', '5' -CreateNoWindow -PassThru -ErrorAction SilentlyContinue
            $result.ExitCode | Should -Be 5
        }

        It 'Ignores a specific failing exit code listed in IgnoreExitCodes' {
            $result = Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--exit-code', '9' -CreateNoWindow -IgnoreExitCodes '9' -PassThru -WarningAction SilentlyContinue -ErrorAction Stop
            $result.ExitCode | Should -Be 9
        }

        It 'Ignores all exit codes when IgnoreExitCodes is a wildcard' {
            $result = Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--exit-code', '9' -CreateNoWindow -IgnoreExitCodes ([System.Char]42) -PassThru -WarningAction SilentlyContinue -ErrorAction Stop
            $result.ExitCode | Should -Be 9
        }
    }

    Context 'Stream capture (real subprocess)' {
        It 'Captures stdout from the process' {
            $result = Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--stdout', 'hello-stdout', '--exit-code', '0' -CreateNoWindow -PassThru -ErrorAction Stop
            $result.StdOut | Should -Contain 'hello-stdout'
        }

        It 'Captures stderr from the process' {
            $result = Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--stderr', 'hello-stderr', '--exit-code', '0' -CreateNoWindow -PassThru -ErrorAction Stop
            $result.StdErr | Should -Contain 'hello-stderr'
        }
    }

    Context 'Side effects (real subprocess)' {
        It 'Runs the process so its file-system side effect occurs' {
            $marker = Join-Path $TestDrive "marker_$([System.Guid]::NewGuid().ToString('N')).txt"
            Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--write-file', $marker, '--exit-code', '0' -CreateNoWindow -ErrorAction Stop
            Test-Path -LiteralPath $marker | Should -BeTrue
        }
    }

    Context 'Timeout handling (real subprocess)' {
        It 'Throws OperationCanceledException with ProcessExecutionCancelled when the process exceeds the timeout' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.OperationCanceledException]
                ErrorId       = 'ProcessExecutionCancelled,Start-ADTProcess'
            }
            { Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--sleep', '5000' -CreateNoWindow -Timeout ([System.TimeSpan]::FromSeconds(1)) -ErrorAction Stop } | Should @shouldParams
        }
    }

    Context 'NoWait handling (real subprocess)' {
        It 'Returns a ProcessHandle with an awaitable task when NoWait and PassThru are specified' {
            $handle = Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--sleep', '200', '--exit-code', '0' -CreateNoWindow -NoWait -PassThru -ErrorAction Stop
            $handle | Should -BeOfType ([PSADT.ProcessManagement.ProcessHandle])
            $handle.Task | Should -Not -BeNullOrEmpty
            # Drain the handle so the subprocess completes and releases the executable.
            $null = $handle.GetAwaiter().GetResult()
        }
    }

    Context 'WhatIf handling' {
        It 'Does not run the process when WhatIf is specified' {
            $marker = Join-Path $TestDrive "whatif_$([System.Guid]::NewGuid().ToString('N')).txt"
            Start-ADTProcess -FilePath $script:FakeExe -ArgumentList '--write-file', $marker, '--exit-code', '0' -CreateNoWindow -WhatIf
            Test-Path -LiteralPath $marker | Should -BeFalse
        }
    }
}

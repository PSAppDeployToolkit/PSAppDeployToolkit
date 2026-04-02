BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Start-ADTProcess' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}

        # Mock Initialize-ADTModuleIfUninitialized: Start-ADTProcess calls this when no session is active,
        # which triggers New-ADTEnvironmentTable -> AccountUtilities (requires admin). Suppress it here.
        Mock -ModuleName PSAppDeployToolkit Initialize-ADTModuleIfUninitialized { }

        # Mock Get-ADTConfig to provide MsiExecWaitTime without requiring an initialized module.
        Mock -ModuleName PSAppDeployToolkit Get-ADTConfig {
            return [PSCustomObject]@{ MSI = [PSCustomObject]@{ MutexWaitTime = 30 } }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'CmdExe', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $CmdExe = "$env:SystemRoot\System32\cmd.exe"
    }

    Context 'Basic Process Execution' {
        It 'Executes a process that exits with code 0 without throwing' {
            { Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 0' -CreateNoWindow } | Should -Not -Throw
        }

        It 'Throws when a process exits with a non-success exit code' {
            { Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 1' -CreateNoWindow } | Should -Throw
        }

        It 'Does not throw when the exit code is in -SuccessExitCodes' {
            { Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 2' -CreateNoWindow -SuccessExitCodes 0, 2 } | Should -Not -Throw
        }

        It 'Does not throw when the exit code matches a value in -SuccessExitCodes' {
            { Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 5' -CreateNoWindow -SuccessExitCodes 0, 5 } | Should -Not -Throw
        }

        It 'Does not throw for any exit code when -ErrorAction SilentlyContinue is specified' {
            { Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 99' -CreateNoWindow -ErrorAction SilentlyContinue } | Should -Not -Throw
        }

        It 'Does not throw when a process exits with a default reboot exit code (treated as success)' {
            # Reboot exit codes (1641, 3010) are considered successful; they log a reboot notice
            # but do NOT throw an exception.
            { Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 3010' -CreateNoWindow } | Should -Not -Throw
        }

        It 'Does not throw when reboot exit code is also listed in -SuccessExitCodes' {
            { Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 3010' -CreateNoWindow -SuccessExitCodes 3010 } | Should -Not -Throw
        }
    }

    Context 'PassThru Output' {
        It 'Returns a non-null result when -PassThru is specified' {
            $result = Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PassThru
            $result | Should -Not -BeNullOrEmpty
        }

        It 'Returns exit code 0 for a successful process' {
            $result = Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PassThru
            $result.ExitCode | Should -Be 0
        }

        It 'Returns the correct non-zero exit code when -ErrorAction SilentlyContinue is specified' {
            $result = Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 42' -CreateNoWindow -PassThru -ErrorAction SilentlyContinue
            $result.ExitCode | Should -Be 42
        }

        It 'Returns a Process object with an Id greater than zero' {
            # ProcessResult exposes the underlying System.Diagnostics.Process via .Process
            $result = Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PassThru
            $result.Process | Should -Not -BeNullOrEmpty
            $result.Process.Id | Should -BeGreaterThan 0
        }

        It 'Captures stdout from the process' {
            $result = Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'echo Hello World' -CreateNoWindow -PassThru
            $result.StdOut | Should -Match 'Hello World'
        }

        It 'Returns null when -PassThru is not specified' {
            $result = Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 0' -CreateNoWindow
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'ArgumentList Handling' {
        It 'Passes multiple arguments correctly via string array' {
            $result = Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'echo TestArgValue' -CreateNoWindow -PassThru
            $result.StdOut | Should -Match 'TestArgValue'
        }

        It 'Passes a single combined argument string correctly' {
            $result = Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c echo SingleArg' -CreateNoWindow -PassThru
            $result.StdOut | Should -Match 'SingleArg'
        }
    }

    Context 'Working Directory' {
        It 'Accepts a valid -WorkingDirectory without throwing' {
            { Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 0' -WorkingDirectory $env:TEMP -CreateNoWindow } | Should -Not -Throw
        }

        It 'Verifies the working directory is used by the process' {
            $result = Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'cd' -WorkingDirectory $env:TEMP -CreateNoWindow -PassThru
            $result.StdOut.Trim() | Should -Be (Get-Item -LiteralPath $env:TEMP).FullName.TrimEnd('\')
        }
    }

    Context 'Environment Variable Expansion' {
        It 'Expands environment variables in ArgumentList when -ExpandEnvironmentVariables is specified' {
            $result = Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'echo %TEMP%' -CreateNoWindow -PassThru -ExpandEnvironmentVariables
            $result.StdOut | Should -Match ([regex]::Escape($env:TEMP))
        }
    }

    Context 'NoWait' {
        It 'Returns immediately without waiting when -NoWait is specified' {
            $elapsed = Measure-Command {
                Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'timeout /t 5 /nobreak' -CreateNoWindow -NoWait
            }
            # Should return in well under 5 seconds
            $elapsed.TotalSeconds | Should -BeLessThan 4
        }
    }

    Context 'WhatIf Support' {
        It 'Does not execute the process when -WhatIf is specified' {
            $markerFile = "$TestDrive\whatif-marker-$(New-Guid).txt"
            Start-ADTProcess -FilePath $CmdExe -ArgumentList "/c echo marker > `"$markerFile`"" -CreateNoWindow -WhatIf
            $markerFile | Should -Not -Exist
        }
    }

    Context 'Priority Class' {
        It 'Accepts -PriorityClass Normal without throwing' {
            { Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PriorityClass Normal } | Should -Not -Throw
        }

        It 'Accepts -PriorityClass BelowNormal without throwing' {
            { Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 0' -CreateNoWindow -PriorityClass BelowNormal } | Should -Not -Throw
        }
    }

    Context 'Input Validation' {
        It 'Throws when -FilePath is null' {
            { Start-ADTProcess -FilePath $null -CreateNoWindow } | Should -Throw
        }

        It 'Throws when -FilePath is an empty string' {
            { Start-ADTProcess -FilePath '' -CreateNoWindow } | Should -Throw
        }

        It 'Throws when -FilePath refers to an executable that cannot be found' {
            { Start-ADTProcess -FilePath 'NonExistentExecutable_Xyz123.exe' -CreateNoWindow } | Should -Throw
        }

        It 'Throws when -SuccessExitCodes is an empty array' {
            { Start-ADTProcess -FilePath $CmdExe -ArgumentList '/c', 'exit 0' -CreateNoWindow -SuccessExitCodes @() } | Should -Throw
        }

    }
}

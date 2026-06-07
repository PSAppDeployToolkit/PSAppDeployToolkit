BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Close-ADTSession' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'No active session' {
        It 'Throws InvalidOperationException with ErrorId ADTSessionBufferEmpty when no session is open' {
            if (Test-ADTSessionActive)
            {
                Set-ItResult -Skipped -Because 'A session is unexpectedly active; cannot assert the no-session path.'
                return
            }
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.InvalidOperationException]
                ErrorId       = 'ADTSessionBufferEmpty,Close-ADTSession'
            }
            { Close-ADTSession } | Should @shouldParams
        }
    }

    Context 'Closing a session' {
        # Mock the exit seam so the final-session closure never exits the test host.
        BeforeAll {
            Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
        }

        BeforeEach {
            $null = Open-ADTSession -DeploymentType Install -DeployMode Silent -AppName 'CloseApp' -AppVendor 'CloseVendor' -AppVersion '1.0.0' -ScriptDirectory $env:TEMP -NoSessionDetection -PassThru
        }

        AfterEach {
            # Defensive cleanup in case an assertion left a session open.
            while (Test-ADTSessionActive)
            {
                Close-ADTSession -ExitCode 0 -NoShellExit
            }
        }

        It 'Deactivates the session after closing' {
            Close-ADTSession -ExitCode 0 -NoShellExit
            Test-ADTSessionActive | Should -BeFalse
        }

        It 'Removes the session from the module session buffer' {
            Close-ADTSession -ExitCode 0 -NoShellExit
            $m = Get-Module PSAppDeployToolkit
            $count = & $m { $Script:ADT.Sessions.Count }
            $count | Should -Be 0
        }

        It 'Returns the exit code when -PassThru is specified' {
            $result = Close-ADTSession -ExitCode 42 -NoShellExit -PassThru
            $result | Should -Be 42
        }

        It 'Returns no output when -PassThru is not specified' {
            $result = Close-ADTSession -ExitCode 0 -NoShellExit
            $result | Should -BeNullOrEmpty
        }

        It 'Forwards the supplied -ExitCode to the exit seam (Exit-ADTInvocation)' {
            Close-ADTSession -ExitCode 7 -NoShellExit
            Should -Invoke -ModuleName PSAppDeployToolkit -CommandName Exit-ADTInvocation -Times 1 -Exactly -ParameterFilter { $ExitCode -eq 7 }
        }

        It 'Sets the global LASTEXITCODE to the supplied exit code' {
            Close-ADTSession -ExitCode 55 -NoShellExit
            $Global:LASTEXITCODE | Should -Be 55
        }

        It 'Records the exit code on the session object (GetExitCode)' {
            $session = Get-ADTSession
            Close-ADTSession -ExitCode 13 -NoShellExit
            $session.GetExitCode() | Should -Be 13
        }
    }

    Context 'Metadata' {
        It 'Declares an OutputType of System.Int32' {
            $outputTypes = (Get-Command Close-ADTSession).OutputType.Type
            $outputTypes | Should -Contain ([System.Int32])
        }

        It 'Is exported from the module' {
            (Get-Module PSAppDeployToolkit).ExportedFunctions.Keys | Should -Contain 'Close-ADTSession'
        }
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    # Closing the last session hands over to Exit-ADTInvocation, which calls [Environment]::Exit outright
    # from a ConsoleHost with a client process open. Mocked throughout, so what is covered here is the
    # session teardown rather than the process exit itself.
    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive

    function Open-Probe
    {
        param
        (
            [Parameter(Mandatory = $false)]
            [System.String]$AppName = 'CloseProbe'
        )

        return Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName $AppName -DeployMode Silent -PassThru -InformationAction SilentlyContinue
    }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Close-ADTSession' {
    Context 'With no session open' {
        It 'Tells the caller there is nothing to close' {
            { Close-ADTSession -NoShellExit } | Should -Throw -ErrorId 'ADTSessionBufferEmpty,Close-ADTSession'
        }
    }

    Context 'Functionality' {
        AfterEach {
            while (Test-ADTSessionActive)
            {
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            }
        }

        It 'Removes the session from the stack' {
            $null = Open-Probe
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            InModuleScope PSAppDeployToolkit { $ADT.Sessions.Count } | Should -Be 0
        }

        It 'Removes only the innermost session when several are nested' {
            $outer = Open-Probe -AppName 'Outer'
            $null = Open-Probe -AppName 'Inner'
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            InModuleScope PSAppDeployToolkit { $ADT.Sessions.Count } | Should -Be 1
            (Get-ADTSession).InstallName | Should -BeExactly $outer.InstallName
        }

        It 'Moves the session into its finalisation phase' {
            $session = Open-Probe
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            $session.InstallPhase | Should -BeExactly 'Finalization'
        }

        It 'Returns the exit code with -PassThru' {
            $null = Open-Probe
            Close-ADTSession -ExitCode 3010 -NoShellExit -PassThru -InformationAction SilentlyContinue | Should -Be 3010
        }

        It 'Returns nothing without -PassThru' {
            $null = Open-Probe
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue | Should -BeNullOrEmpty
        }

        It 'Records the exit code on the session' {
            $session = Open-Probe
            Close-ADTSession -ExitCode 1618 -NoShellExit -InformationAction SilentlyContinue
            $session.GetExitCode() | Should -Be 1618
        }

        It 'Writes the outcome into the session log' {
            $null = Open-Probe -AppName 'LogClose'
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            $log = Get-ChildItem -LiteralPath (Get-ADTConfig).Toolkit.LogPath -Recurse -File -Filter '*LogClose*' | Select-Object -First 1
            [System.IO.File]::ReadAllText($log.FullName) | Should -BeLike '*completed*exit code*'
        }

        It 'Hands over to the exit routine only for the last session' {
            # Exit-ADTInvocation is what ends the process, so a nested close must not reach it.
            $null = Open-Probe -AppName 'Outer'
            $null = Open-Probe -AppName 'Inner'
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            Should -Invoke -ModuleName PSAppDeployToolkit Exit-ADTInvocation -Times 0 -Exactly

            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            Should -Invoke -ModuleName PSAppDeployToolkit Exit-ADTInvocation -Times 1 -Exactly
        }
    }

    Context 'Callbacks' {
        AfterEach {
            while (Test-ADTSessionActive)
            {
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            }
            Clear-ADTModuleCallback -Hookpoint PreClose
            Clear-ADTModuleCallback -Hookpoint PostClose
        }

        It 'Invokes the <Hookpoint> callbacks' -ForEach @(
            @{ Hookpoint = 'PreClose' }
            @{ Hookpoint = 'PostClose' }
        ) {
            $script:CallbackRan = $false
            function Test-CloseCallback
            {
                $script:CallbackRan = $true
            }
            Add-ADTModuleCallback -Hookpoint $Hookpoint -Callback (Get-Command Test-CloseCallback)

            $null = Open-Probe
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            $script:CallbackRan | Should -BeTrue
        }
    }
}

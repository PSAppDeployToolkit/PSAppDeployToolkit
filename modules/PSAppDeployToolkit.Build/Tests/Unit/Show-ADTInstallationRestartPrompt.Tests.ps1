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
Describe 'Show-ADTInstallationRestartPrompt' {
    # Only the silent path without -SilentRestart is exercised. -SilentRestart arms a restart for when the
    # invocation exits, which is not something a test run gets to do to the machine it is running on, so
    # that branch is left uncovered deliberately.
    Context 'In a silent deployment' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'RestartPromptSilent' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Shows nothing and restarts nothing' {
            { Show-ADTInstallationRestartPrompt } | Should -Not -Throw
        }

        It 'Says it skipped the restart' {
            # Silence alone is not enough here: a deployment that expected a restart needs the log to say
            # plainly that it did not happen and why.
            Show-ADTInstallationRestartPrompt
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Skipping restart*' }
        }

        It 'Arms nothing for the end of the deployment' {
            Show-ADTInstallationRestartPrompt
            InModuleScope -ModuleName PSAppDeployToolkit {
                $Module.State.RestartOnExitOptions | Should -BeNullOrEmpty
            }
        }

        It 'Returns nothing' {
            Show-ADTInstallationRestartPrompt | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Refuses a countdown that is not a duration' {
            { Show-ADTInstallationRestartPrompt -Countdown 'soon' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a silent countdown without a silent restart' {
            Test-ADTParameterSetSatisfied -Command (Get-Command Show-ADTInstallationRestartPrompt) -Parameter SilentCountdown, Title, Subtitle | Should -BeFalse
        }

        # A countdown and no countdown at all remain the two ways of shaping the dialog, so naming both
        # is still a contradiction whether or not the call also says what to do in silence.
        It 'Refuses a countdown alongside no countdown <Name>' -ForEach @(
            @{ Name = 'on its own'; Parameter = 'Countdown', 'NoCountdown', 'Title', 'Subtitle' }
            @{ Name = 'with a silent restart'; Parameter = 'Countdown', 'NoCountdown', 'SilentRestart', 'Title', 'Subtitle' }
            @{ Name = 'with a hide cutoff'; Parameter = 'CountdownNoHide', 'NoCountdown', 'Title', 'Subtitle' }
        ) {
            Test-ADTParameterSetSatisfied -Command (Get-Command Show-ADTInstallationRestartPrompt) -Parameter $Parameter | Should -BeFalse
        }

        # -SilentRestart only says what is to happen should the deployment turn out to be silent. An
        # interactive one still shows the dialog, so everything that shapes that dialog has to bind
        # alongside it; each of these named a set the parameter was absent from and could not be asked for.
        It 'Accepts -<Parameter2> alongside a silent restart' -ForEach @(
            @{ Parameter2 = 'Countdown' }
            @{ Parameter2 = 'CountdownNoHide' }
            @{ Parameter2 = 'PersistPrompt' }
            @{ Parameter2 = 'CustomMessage' }
            @{ Parameter2 = 'CustomMessageText' }
            @{ Parameter2 = 'AllowCancel' }
        ) {
            Test-ADTParameterSetSatisfied -Command (Get-Command Show-ADTInstallationRestartPrompt) -Parameter SilentRestart, $Parameter2, Title, Subtitle | Should -BeTrue
        }

        # The dialog the silent restart path falls back to can be the countdown one or the immediate one,
        # and the silent countdown has to survive either choice.
        It 'Accepts a silent countdown <Name>' -ForEach @(
            @{ Name = 'with a countdown dialog'; Parameter = 'SilentRestart', 'SilentCountdown', 'Countdown', 'Title', 'Subtitle' }
            @{ Name = 'with no countdown dialog'; Parameter = 'SilentRestart', 'SilentCountdown', 'NoCountdown', 'Title', 'Subtitle' }
        ) {
            Test-ADTParameterSetSatisfied -Command (Get-Command Show-ADTInstallationRestartPrompt) -Parameter $Parameter | Should -BeTrue
        }

        # Both parameters that shape the shutdown.exe call have to reach every path ending in a restart,
        # and each path sits in a different parameter set. A set one was left out of binds nothing and
        # the caller's intent is dropped without an error, as -ShutdownReasonText was on the silent path.
        It 'Accepts <Parameter2> on the <Name> path' -ForEach @(
            @{ Name = 'countdown'; Parameter2 = 'NoForceCloseApps'; Parameter = 'Countdown', 'NoForceCloseApps', 'Title', 'Subtitle' }
            @{ Name = 'no countdown'; Parameter2 = 'NoForceCloseApps'; Parameter = 'NoCountdown', 'NoForceCloseApps', 'Title', 'Subtitle' }
            @{ Name = 'countdown silent restart'; Parameter2 = 'NoForceCloseApps'; Parameter = 'Countdown', 'SilentRestart', 'NoForceCloseApps', 'Title', 'Subtitle' }
            @{ Name = 'no countdown silent restart'; Parameter2 = 'NoForceCloseApps'; Parameter = 'NoCountdown', 'SilentRestart', 'NoForceCloseApps', 'Title', 'Subtitle' }
            @{ Name = 'countdown'; Parameter2 = 'ShutdownReasonText'; Parameter = 'Countdown', 'ShutdownReasonText', 'Title', 'Subtitle' }
            @{ Name = 'no countdown'; Parameter2 = 'ShutdownReasonText'; Parameter = 'NoCountdown', 'ShutdownReasonText', 'Title', 'Subtitle' }
            @{ Name = 'countdown silent restart'; Parameter2 = 'ShutdownReasonText'; Parameter = 'Countdown', 'SilentRestart', 'ShutdownReasonText', 'Title', 'Subtitle' }
            @{ Name = 'no countdown silent restart'; Parameter2 = 'ShutdownReasonText'; Parameter = 'NoCountdown', 'SilentRestart', 'ShutdownReasonText', 'Title', 'Subtitle' }
        ) {
            Test-ADTParameterSetSatisfied -Command (Get-Command Show-ADTInstallationRestartPrompt) -Parameter $Parameter | Should -BeTrue
        }
    }
}

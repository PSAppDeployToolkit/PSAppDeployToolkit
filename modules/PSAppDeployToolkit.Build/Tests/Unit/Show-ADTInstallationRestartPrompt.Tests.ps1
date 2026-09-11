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
                $ADT.RestartOnExitCountdown | Should -BeNullOrEmpty
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

        # -NoForceCloseApps has to reach every path that ends in a restart, and each of those paths sits
        # in a different parameter set. A set it was left out of would bind nothing and drop the opt-out
        # silently, so the shape of the call is worth asserting rather than assuming.
        It 'Accepts a restart that does not force applications closed for the <Name> path' -ForEach @(
            @{ Name = 'countdown'; Parameter = 'Countdown', 'NoForceCloseApps', 'Title', 'Subtitle' }
            @{ Name = 'no countdown'; Parameter = 'NoCountdown', 'NoForceCloseApps', 'Title', 'Subtitle' }
            @{ Name = 'silent restart'; Parameter = 'SilentRestart', 'NoForceCloseApps', 'Title', 'Subtitle' }
        ) {
            Test-ADTParameterSetSatisfied -Command (Get-Command Show-ADTInstallationRestartPrompt) -Parameter $Parameter | Should -BeTrue
        }
    }
}

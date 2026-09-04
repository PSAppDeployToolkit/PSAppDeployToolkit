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
Describe 'Show-ADTNotifyIcon' {
    # Only the silent path is exercised, for the same reason as the rest of the dialogs. -Force is never
    # used, since it is what overrides the bypass being tested.
    Context 'In a silent deployment' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'NotifyIconSilent' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Shows nothing' {
            { Show-ADTNotifyIcon } | Should -Not -Throw
        }

        It 'Says why it showed nothing' {
            Show-ADTNotifyIcon
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Bypassing*' }
        }

        It 'Leaves no icon behind' {
            # An icon left in the tray outlives the deployment that put it there, so the bypass has to
            # mean nothing was created rather than something created and hidden.
            Show-ADTNotifyIcon
            InModuleScope -ModuleName PSAppDeployToolkit {
                Test-ADTNotifyIconOpen -RunAsActiveUser (Get-ADTClientServerUser) | Should -BeFalse
            }
        }

        It 'Returns nothing' {
            Show-ADTNotifyIcon | Should -BeNullOrEmpty
        }
    }
}

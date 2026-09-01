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
Describe 'Test-ADTNotifyIconOpen' {
    Context 'Functionality' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'NotifyIconOpenProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Reports no icon when none was shown' {
            # Show-ADTBalloonTip routes through the notification icon when one is already up and falls back
            # to a balloon when it is not, so this decides which of the two the user sees.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Test-ADTNotifyIconOpen -RunAsActiveUser (Get-ADTClientServerUser) | Should -BeFalse
            }
        }

        It 'Answers with a boolean' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Test-ADTNotifyIconOpen -RunAsActiveUser (Get-ADTClientServerUser) | Should -BeOfType ([System.Boolean])
            }
        }
    }

    Context 'Input Validation' {
        It 'Requires a user to ask about' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Test-ADTNotifyIconOpen } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }

        It 'Refuses something that is not a user' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Test-ADTNotifyIconOpen -RunAsActiveUser 'not a user' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }
    }
}

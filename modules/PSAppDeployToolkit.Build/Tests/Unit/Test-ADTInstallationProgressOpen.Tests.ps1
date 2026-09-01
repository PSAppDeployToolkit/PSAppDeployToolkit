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
Describe 'Test-ADTInstallationProgressOpen' {
    Context 'Functionality' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ProgressOpenProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Reports no dialog when none was shown' {
            # Show-ADTInstallationProgress asks this before deciding whether to open a dialog or update the
            # one already up, so a wrong answer either stacks dialogs or updates one that is not there.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Test-ADTInstallationProgressOpen -RunAsActiveUser (Get-ADTClientServerUser) | Should -BeFalse
            }
        }

        It 'Answers with a boolean' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Test-ADTInstallationProgressOpen -RunAsActiveUser (Get-ADTClientServerUser) | Should -BeOfType ([System.Boolean])
            }
        }
    }

    Context 'Input Validation' {
        It 'Requires a user to ask about' {
            # The dialog belongs to a user's session, so there is no answer without one.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Test-ADTInstallationProgressOpen } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }

        It 'Refuses something that is not a user' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Test-ADTInstallationProgressOpen -RunAsActiveUser 'not a user' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            }
        }
    }
}

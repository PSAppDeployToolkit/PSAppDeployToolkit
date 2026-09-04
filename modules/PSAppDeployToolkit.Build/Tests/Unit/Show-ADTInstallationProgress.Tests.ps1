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
Describe 'Show-ADTInstallationProgress' {
    # Only the silent path is exercised. A progress dialog shown for real belongs to the user interface
    # effort rather than here.
    Context 'In a silent deployment' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ProgressSilent' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Shows nothing' {
            { Show-ADTInstallationProgress -StatusMessage 'Nothing should appear' } | Should -Not -Throw
        }

        It 'Says why it showed nothing' {
            Show-ADTInstallationProgress -StatusMessage 'Nothing should appear'
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Bypassing*' }
        }

        It 'Leaves no dialog behind' {
            # Deployments call this repeatedly to update the message, and every one of those calls must
            # stay a no-op rather than the first one opening something.
            Show-ADTInstallationProgress -StatusMessage 'Nothing should appear'
            InModuleScope -ModuleName PSAppDeployToolkit {
                Test-ADTInstallationProgressOpen -RunAsActiveUser (Get-ADTClientServerUser) | Should -BeFalse
            }
        }

        It 'Does not object to being called without a message' {
            { Show-ADTInstallationProgress } | Should -Not -Throw
        }

        It 'Returns nothing' {
            Show-ADTInstallationProgress -StatusMessage 'Nothing should appear' | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Refuses a window position it does not know' {
            { Show-ADTInstallationProgress -WindowLocation 'Nowhere' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a message alignment it does not know' {
            { Show-ADTInstallationProgress -MessageAlignment 'Sideways' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

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
Describe 'Show-ADTInstallationPrompt' {
    # Only the silent path is exercised. A prompt shown for real waits on a person answering it, which is
    # left to the user interface effort.
    Context 'In a silent deployment' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'PromptSilent' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Shows nothing' {
            # A prompt in a silent deployment would block it until the timeout, which is the worst of both
            # outcomes: nobody sees it and the deployment stalls anyway.
            { Show-ADTInstallationPrompt -Message 'Nothing should appear' -ButtonRightText 'OK' } | Should -Not -Throw
        }

        It 'Says why it showed nothing' {
            Show-ADTInstallationPrompt -Message 'Nothing should appear' -ButtonRightText 'OK'
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Bypassing*' }
        }

        It 'Returns nothing' {
            Show-ADTInstallationPrompt -Message 'Nothing should appear' -ButtonRightText 'OK' | Should -BeNullOrEmpty
        }

        It 'Requires at least one button' {
            # Checked ahead of the silent bypass, so that a prompt which could never be dismissed is
            # reported when it is written rather than the first time a deployment runs interactively.
            # Asked here rather than alongside the other input checks, because the title and subtitle every
            # parameter set demands are supplied by the session, and without one the call cannot resolve.
            { Show-ADTInstallationPrompt -Message 'Anything' } | Should -Throw -ErrorId 'MandatoryParameterMissing,Show-ADTInstallationPrompt'
        }
        It 'Returns nothing when it was asked for input' {
            # The caller is expecting a string back, so the silent path has to give them nothing to test
            # rather than an empty answer that reads as a real one.
            Show-ADTInstallationPrompt -Message 'Nothing should appear' -ButtonRightText 'OK' -RequestInput | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Requires a message' {
            Test-ADTParameterSetSatisfied -Command (Get-Command Show-ADTInstallationPrompt) | Should -BeFalse
        }


        It 'Refuses a default answer without asking for input' {
            Test-ADTParameterSetSatisfied -Command (Get-Command Show-ADTInstallationPrompt) -Parameter Message, Title, Subtitle, DefaultValue | Should -BeFalse
        }

        It 'Refuses a default answer alongside a secure one' {
            # A secure prompt returns a SecureString, which there is no way to prefill from a plain one.
            { Show-ADTInstallationPrompt -Message 'Anything' -RequestInput -DefaultValue 'preset' -SecureInput } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

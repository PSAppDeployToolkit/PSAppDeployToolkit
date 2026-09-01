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
Describe 'Show-ADTDialogBox' {
    # Only the silent path is exercised. -Force, which overrides the bypass and puts a real dialog box on
    # screen, is never used here.
    Context 'In a silent deployment' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'DialogBoxSilent' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Shows nothing' {
            { Show-ADTDialogBox -Text 'Nothing should appear' } | Should -Not -Throw
        }

        It 'Says why it showed nothing' {
            Show-ADTDialogBox -Text 'Nothing should appear'
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Bypassing*' }
        }

        It 'Returns nothing' {
            # Callers branch on which button came back, so the silent path giving them a button name would
            # have a silent deployment take whichever branch that name implied.
            Show-ADTDialogBox -Text 'Nothing should appear' | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Requires something to say' {
            { Show-ADTDialogBox } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses an icon it cannot draw' {
            { Show-ADTDialogBox -Text 'Anything' -Icon 'Confused' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a set of buttons it does not have' {
            { Show-ADTDialogBox -Text 'Anything' -Buttons 'MaybeLater' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

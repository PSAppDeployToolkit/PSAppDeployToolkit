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
Describe 'Show-ADTBalloonTip' {
    # Only the silent path is exercised. A balloon shown for real needs a person to see it and is left to
    # the user interface effort, so -Force, which overrides the silent bypass, is never used here.
    Context 'In a silent deployment' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'BalloonTipSilent' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Shows nothing' {
            # A silent deployment is one nobody is watching, so anything on screen is a defect rather than
            # a nicety.
            { Show-ADTBalloonTip -BalloonTipText 'Nothing should appear' } | Should -Not -Throw
        }

        It 'Says why it showed nothing' {
            Show-ADTBalloonTip -BalloonTipText 'Nothing should appear'
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*Bypassing*' }
        }

        It 'Returns nothing' {
            Show-ADTBalloonTip -BalloonTipText 'Nothing should appear' | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Requires something to say' {
            Test-ADTMandatoryParameter -Command (Get-Command Show-ADTBalloonTip) -Parameter Text | Should -BeTrue
            Test-ADTMandatoryParameter -Command (Get-Command Show-ADTBalloonTip) -Parameter Title | Should -BeTrue
        }

        It 'Refuses an icon it cannot draw' {
            { Show-ADTBalloonTip -BalloonTipText 'Anything' -BalloonTipIcon 'Exclamation' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Initialize-ADTTestModule -Path $TestDrive
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Test-ADTSessionActive' {
    Context 'Functionality' {
        It 'Returns a boolean' {
            Test-ADTSessionActive | Should -BeOfType ([System.Boolean])
        }

        It 'Reports false with nothing open' {
            Test-ADTSessionActive | Should -BeFalse
        }

        It 'Reports true once a session is opened, and false once it closes' {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ActiveProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
            Test-ADTSessionActive | Should -BeTrue
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            Test-ADTSessionActive | Should -BeFalse
        }

        It 'Stays true while a nested session is still open' {
            # It answers "is any session open", not "is exactly one open", which is what lets a nested
            # deployment close without the outer one being treated as finished.
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'OuterProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'InnerProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
            try
            {
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
                Test-ADTSessionActive | Should -BeTrue
            }
            finally
            {
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            }
            Test-ADTSessionActive | Should -BeFalse
        }
    }
}

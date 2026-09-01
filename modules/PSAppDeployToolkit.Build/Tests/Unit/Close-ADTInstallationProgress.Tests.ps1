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
Describe 'Close-ADTInstallationProgress' {
    Context 'With nothing open' {
        BeforeAll {
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'CloseProgressProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
        }

        AfterAll {
            Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
        }

        It 'Does not object' {
            # Deployments close the progress dialog in their finally blocks whether or not one was ever
            # opened, so this has to be a no-op rather than a failure at the end of every silent run.
            { Close-ADTInstallationProgress } | Should -Not -Throw
        }

        It 'Says why it did nothing' {
            Close-ADTInstallationProgress
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*no progress dialog open*' }
        }

        It 'Returns nothing' {
            Close-ADTInstallationProgress | Should -BeNullOrEmpty
        }
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTModuleExitCode' {
    Context 'Before initialisation' {
        It 'Refuses rather than handing back nothing' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTModuleExitCode } | Should -Throw -ErrorId 'ADTModuleNotInitialized,Get-ADTModuleExitCode'
            }
        }
    }

    Context 'After initialisation' {
        BeforeEach {
            Initialize-ADTTestModule -Path $TestDrive
        }

        It 'Has no exit code until something has exited' {
            # Deliberately absent rather than zero. A zero reads as a deployment that succeeded, and a
            # failure path handing one on was how an error came to be reported as success.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Get-ADTModuleExitCode | Should -BeNullOrEmpty
            }
        }

        It 'Reports what the last closed session exited with' {
            $session = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'ExitCodeProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
            $session.SetExitCode(1641)
            Close-ADTSession -NoShellExit -InformationAction SilentlyContinue
            InModuleScope -ModuleName PSAppDeployToolkit {
                Get-ADTModuleExitCode | Should -Be 1641
            }
        }
    }
}

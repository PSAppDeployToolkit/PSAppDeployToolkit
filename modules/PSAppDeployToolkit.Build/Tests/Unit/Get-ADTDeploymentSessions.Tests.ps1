BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTDeploymentSessions' {
    Context 'Before initialisation' {
        It 'Refuses rather than handing back an empty list' {
            # An empty list would read as "no deployment is running", which is the same answer an
            # initialized module gives between sessions and means something quite different.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTDeploymentSessions } | Should -Throw -ErrorId 'ADTModuleNotInitialized,Get-ADTDeploymentSessions'
            }
        }
    }

    Context 'After initialisation' {
        BeforeAll {
            Initialize-ADTTestModule -Path $TestDrive
        }

        It 'Hands back an empty list before any session opens' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTDeploymentSessions).Count | Should -Be 0
            }
        }

        It 'Hands back the list itself rather than its contents' {
            # Written out unenumerated on purpose. Callers index and count it, and a list that unrolled
            # would arrive as a bare session when one was open and as nothing at all when none were.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Get-ADTDeploymentSessions | Should -BeOfType ([System.Collections.Generic.IList[PSAppDeployToolkit.Foundation.DeploymentSession]])
            }
        }

        It 'Hands back the live list, so an opened session appears in it' {
            # This is how Open-ADTSession registers one, so a copy would leave every later reader looking
            # at a module that believed no deployment was running.
            $null = Open-ADTSession -SessionState $ExecutionContext.SessionState -AppName 'SessionsProbe' -DeployMode Silent -PassThru -InformationAction SilentlyContinue
            try
            {
                InModuleScope -ModuleName PSAppDeployToolkit {
                    (Get-ADTDeploymentSessions).Count | Should -Be 1
                    (Get-ADTDeploymentSessions)[0].AppName | Should -BeExactly 'SessionsProbe'
                }
            }
            finally
            {
                Close-ADTSession -ExitCode 0 -NoShellExit -InformationAction SilentlyContinue
            }
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTDeploymentSessions).Count | Should -Be 0
            }
        }
    }
}

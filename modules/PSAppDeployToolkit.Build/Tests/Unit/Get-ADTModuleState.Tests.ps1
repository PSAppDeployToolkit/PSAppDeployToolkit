BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTModuleState' {
    Context 'Before initialisation' {
        It 'Refuses rather than handing back nothing' {
            # Everything reading the module's state goes through here, so a null returned instead of a
            # refusal would surface as a missing property somewhere unrelated rather than as a caller that
            # asked too early.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTModuleState } | Should -Throw -ErrorId 'ADTModuleNotInitialized,Get-ADTModuleState'
            }
        }

        It 'Is re-reported by whichever public function was called' {
            # It carries its own CmdletBinding, so the refusal starts out named after it. Each public
            # accessor catches and rethrows through its own $PSCmdlet, which puts both the caller's name
            # and the caller's script position on what the user finally sees - an error pointing at a line
            # inside this module tells them nothing about the line they wrote.
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Get-ADTConfig } | Should -Throw -ErrorId 'ADTModuleNotInitialized,Get-ADTConfig'
                { Get-ADTEnvironmentTable } | Should -Throw -ErrorId 'ADTModuleNotInitialized,Get-ADTEnvironmentTable'
                { Get-ADTStringTable } | Should -Throw -ErrorId 'ADTModuleNotInitialized,Get-ADTStringTable'
            }
        }
    }

    Context 'After initialisation' {
        BeforeAll {
            Initialize-ADTTestModule -Path $TestDrive
        }

        It 'Hands back the state the module is holding' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Get-ADTModuleState | Should -BeOfType ([PSAppDeployToolkit.Foundation.ModuleState])
                Get-ADTModuleState | Should -Be $Module.State
            }
        }

        It 'Hands back the same instance every time' {
            # Callers mutate what they are given - the session list and the exit code both live here - so a
            # copy handed out per call would silently discard every write.
            InModuleScope -ModuleName PSAppDeployToolkit {
                [System.Object]::ReferenceEquals((Get-ADTModuleState), (Get-ADTModuleState)) | Should -BeTrue
            }
        }
    }
}

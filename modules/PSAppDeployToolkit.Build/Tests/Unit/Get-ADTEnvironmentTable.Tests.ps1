BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTEnvironmentTable' {
    Context 'Before initialisation' {
        It 'Refuses to hand back an environment that was never built' {
            { Get-ADTEnvironmentTable } | Should -Throw -ErrorId 'ADTEnvironmentDatabaseEmpty,Get-ADTEnvironmentTable'
        }
    }

    Context 'After initialisation' {
        BeforeAll {
            Initialize-ADTTestModule -Path $TestDrive
            $script:Environment = Get-ADTEnvironmentTable
        }

        It 'Returns an environment table' {
            $script:Environment | Should -BeOfType ([PSAppDeployToolkit.Foundation.EnvironmentTable])
        }

        It 'Describes this machine rather than a template' {
            # Compared without regard to case. What identifies the machine is the name, not how it is cased,
            # and the two do not always agree on that: a build agent reports its host name in lower case
            # where a workstation reports it upper.
            $script:Environment.envComputerName | Should -Be ([System.Net.Dns]::GetHostName())
            $script:Environment.envUserName | Should -BeExactly $env:USERNAME
        }

        It 'Resolves the toolkit paths it publishes' {
            $script:Environment.envSystem32Directory | Should -BeExactly ([System.Environment]::SystemDirectory)
            $script:Environment.envWinDir | Should -BeExactly $env:SystemRoot
        }

        It 'Carries the logged-on user it will show UI to' {
            # RunAsActiveUser is what every user-facing function keys off, so an initialised module with a
            # user logged on has to have found them.
            $script:Environment.RunAsActiveUser | Should -Not -BeNullOrEmpty
        }

        It 'Hands back the same table the module holds' {
            $script:Environment | Should -Be (InModuleScope PSAppDeployToolkit { $ADT.Environment })
        }
    }
}

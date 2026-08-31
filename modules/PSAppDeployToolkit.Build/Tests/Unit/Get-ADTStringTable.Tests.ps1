BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTStringTable' {
    Context 'Before initialisation' {
        It 'Refuses to hand back strings that were never loaded' {
            { Get-ADTStringTable } | Should -Throw -ErrorId 'ADTStringTableNotInitialized,Get-ADTStringTable'
        }
    }

    Context 'After initialisation' {
        BeforeAll {
            Initialize-ADTTestModule -Path $TestDrive
            $script:Strings = Get-ADTStringTable
        }

        It 'Returns a hashtable' {
            $script:Strings | Should -BeOfType ([System.Collections.Hashtable])
        }

        It 'Holds the strings for the <Section> dialog' -ForEach @(
            @{ Section = 'CloseAppsPrompt' }
            @{ Section = 'InstallationPrompt' }
            @{ Section = 'ProgressPrompt' }
            @{ Section = 'RestartPrompt' }
            @{ Section = 'BalloonTip' }
        ) {
            # Every dialog reads its wording from here, so a missing section is a dialog that cannot render.
            $script:Strings.ContainsKey($Section) | Should -BeTrue
        }

        It 'Hands back the same table the module holds' {
            $script:Strings | Should -Be (InModuleScope PSAppDeployToolkit { $ADT.Strings })
        }

        It 'Returns a separate copy when given a session state' {
            # The copy is expanded against the caller's variables, so handing back the shared table would
            # bake one caller's values into every later caller's strings.
            $expanded = Get-ADTStringTable -SessionState $ExecutionContext.SessionState
            $expanded | Should -Not -Be $script:Strings
            $expanded.Keys.Count | Should -Be $script:Strings.Keys.Count
        }

        It 'Leaves the shared table unexpanded when a copy is taken' {
            $null = Get-ADTStringTable -SessionState $ExecutionContext.SessionState
            Get-ADTStringTable | Should -Be (InModuleScope PSAppDeployToolkit { $ADT.Strings })
        }
    }
}

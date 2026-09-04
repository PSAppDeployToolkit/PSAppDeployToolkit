BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTConfig' {
    Context 'Before initialisation' {
        It 'Refuses to hand back a config that was never loaded' {
            # The message names Initialize-ADTModule, which is the whole point: a caller reaching for config
            # too early should be told what to do rather than given an empty table.
            { Get-ADTConfig } | Should -Throw -ErrorId 'ADTConfigNotLoaded,Get-ADTConfig'
        }
    }

    Context 'After initialisation' {
        BeforeAll {
            Initialize-ADTTestModule -Path $TestDrive
            $script:Config = Get-ADTConfig
        }

        It 'Returns a hashtable' {
            $script:Config | Should -BeOfType ([System.Collections.Hashtable])
        }

        It 'Holds the <Section> section' -ForEach @(
            @{ Section = 'Toolkit' }
            @{ Section = 'MSI' }
            @{ Section = 'UI' }
            @{ Section = 'Assets' }
        ) {
            $script:Config.ContainsKey($Section) | Should -BeTrue
            $script:Config.$Section | Should -BeOfType ([System.Collections.Hashtable])
        }

        It 'Expanded the environment variables in its paths' {
            # The shipped defaults are written with $env: references, so an unexpanded value would be
            # handed to the file system verbatim.
            $script:Config.Toolkit.RegPath | Should -Not -BeLike '*$env:*'
        }

        It 'Hands back the same table the module holds' {
            # Not a copy: the toolkit mutates config in place during a deployment, and a copy would drop it.
            $script:Config | Should -Be (InModuleScope PSAppDeployToolkit { $ADT.Config })
        }
    }
}

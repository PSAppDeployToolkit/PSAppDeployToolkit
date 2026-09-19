BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTModuleDefaults' {
    Context 'Before initialisation' {
        It 'Answers without the module having been initialized' {
            # The defaults are what initialization reads to build a config from, so reaching them through
            # the state would leave nothing able to produce one.
            Test-ADTModuleInitialized | Should -BeFalse
            InModuleScope -ModuleName PSAppDeployToolkit {
                Get-ADTModuleDefaults | Should -BeOfType ([PSAppDeployToolkit.Foundation.ModuleDefaults])
            }
        }

        It 'Carries a config and a string table, each with a neutral entry' {
            # The empty key is the fallback every lookup lands on when a deployment's own language ships
            # no table of its own.
            InModuleScope -ModuleName PSAppDeployToolkit {
                $defaults = Get-ADTModuleDefaults
                $defaults.Config.ContainsKey([System.String]::Empty) | Should -BeTrue
                $defaults.Strings.ContainsKey([System.String]::Empty) | Should -BeTrue
            }
        }

        It 'Holds each default as a scriptblock rather than an evaluated table' {
            # They are held unevaluated so each caller gets its own copy, since Import-ADTConfig mutates
            # what it is given and a shared table would carry one deployment's paths into the next.
            InModuleScope -ModuleName PSAppDeployToolkit {
                (Get-ADTModuleDefaults).Config[[System.String]::Empty] | Should -BeOfType ([System.Management.Automation.ScriptBlock])
                (Get-ADTModuleDefaults).Strings[[System.String]::Empty] | Should -BeOfType ([System.Management.Automation.ScriptBlock])
            }
        }
    }

    Context 'Functionality' {
        It 'Hands back the same instance every time' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                [System.Object]::ReferenceEquals((Get-ADTModuleDefaults), (Get-ADTModuleDefaults)) | Should -BeTrue
            }
        }
    }
}

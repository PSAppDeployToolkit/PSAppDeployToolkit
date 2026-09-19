BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    # A released module is one file with everything folded into it; a development tree dot-sources the
    # pieces instead. The presence of ImportsFirst.ps1 beside the manifest is what tells the two apart,
    # and it is worked out here rather than assumed so this holds whichever layout is under test.
    $script:ModuleBase = (Get-Module -Name PSAppDeployToolkit).ModuleBase
    $script:IsDevelopmentTree = Test-Path -LiteralPath "$script:ModuleBase\ImportsFirst.ps1" -PathType Leaf
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Test-ADTModuleCompiled' {
    Context 'Functionality' {
        It 'Reports whether the module was built into a single file' {
            # Compared against the layout on disk rather than against a fixed expectation, so this says
            # something on a released module as well as on a development tree.
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Expected = !$script:IsDevelopmentTree } {
                Test-ADTModuleCompiled | Should -Be $Expected
            }
        }

        It 'Answers without the module having been initialized' {
            # It describes how the module was built rather than what it is doing, so it is settled at
            # import and answerable from then on.
            Test-ADTModuleInitialized | Should -BeFalse
            InModuleScope -ModuleName PSAppDeployToolkit {
                Test-ADTModuleCompiled | Should -BeOfType ([System.Boolean])
            }
        }

        It 'Gives the same answer once the module is initialized' {
            # Nothing about initializing can change how the module was built, so an answer that moved
            # would mean it was reading something it should not.
            $before = InModuleScope -ModuleName PSAppDeployToolkit { Test-ADTModuleCompiled }
            Initialize-ADTTestModule -Path $TestDrive
            InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Before = $before } {
                Test-ADTModuleCompiled | Should -Be $Before
            }
        }
    }
}

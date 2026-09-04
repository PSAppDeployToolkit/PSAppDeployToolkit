BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"

    # Forced, because this file needs a module that has not been initialised by whatever ran before it.
    Import-ADTModuleUnderTest -Force
}

AfterAll {
    # Leaves the module as this file found it, since it initialises it below.
    Import-ADTModuleUnderTest -Force
}

Describe 'Test-ADTModuleInitialized' {
    Context 'Before initialisation' {
        It 'Returns false' {
            Test-ADTModuleInitialized | Should -BeFalse
        }

        It 'Returns a boolean' {
            Test-ADTModuleInitialized | Should -BeOfType ([System.Boolean])
        }
    }

    Context 'After initialisation' {
        BeforeAll {
            Initialize-ADTTestModule -Path $TestDrive
        }

        It 'Returns true' {
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Reports the same flag the module holds' {
            Test-ADTModuleInitialized | Should -Be (InModuleScope PSAppDeployToolkit { $ADT.Initialized })
        }

        It 'Returns false again once the module is reloaded' {
            # Reloading is how a test leaves the module as it found it, so the flag has to follow.
            Import-ADTModuleUnderTest -Force
            Test-ADTModuleInitialized | Should -BeFalse
        }
    }
}

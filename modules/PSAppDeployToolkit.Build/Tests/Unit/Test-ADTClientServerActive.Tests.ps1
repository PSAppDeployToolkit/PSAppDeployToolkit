BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force

    Mock -ModuleName PSAppDeployToolkit Exit-ADTInvocation { }
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Test-ADTClientServerActive' {
    # Only the negative case is covered. Standing a client up means putting a dialog on screen and keeping
    # it there, which is left to the user interface effort - a silent deployment creates and disposes one
    # per operation rather than holding it. What can be pinned here is that the question is always
    # answerable, since every caller asks it before deciding whether to reach for the instance.
    Context 'With no client running' {
        It 'Reports no client rather than refusing to answer' {
            InModuleScope -ModuleName PSAppDeployToolkit {
                Test-ADTClientServerActive | Should -BeFalse
            }
        }

        It 'Answers before the module has been initialized' {
            # It reads a script variable rather than the module state, so it is answerable at any point in
            # the module's life. Exit-ADTInvocation asks it while tearing the module down, by which time
            # there is no state left to consult.
            Test-ADTModuleInitialized | Should -BeFalse
            InModuleScope -ModuleName PSAppDeployToolkit {
                { Test-ADTClientServerActive } | Should -Not -Throw
            }
        }

        It 'Still answers once the module is initialized' {
            Initialize-ADTTestModule -Path $TestDrive
            InModuleScope -ModuleName PSAppDeployToolkit {
                Test-ADTClientServerActive | Should -BeFalse
            }
        }

        It 'Agrees with what asking for the instance does' {
            # The two are a pair: callers test first and only then reach for it, so a false here has to
            # mean the reach would have been refused.
            InModuleScope -ModuleName PSAppDeployToolkit {
                Test-ADTClientServerActive | Should -BeFalse
                { Get-ADTClientServerInstance } | Should -Throw -ErrorId 'ClientServerInstanceNotFoundError,Get-ADTClientServerInstance'
            }
        }
    }
}

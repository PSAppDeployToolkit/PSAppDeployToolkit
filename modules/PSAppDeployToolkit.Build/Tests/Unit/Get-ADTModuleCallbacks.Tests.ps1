BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest -Force
}

AfterAll {
    Import-ADTModuleUnderTest -Force
}

Describe 'Get-ADTModuleCallbacks' {
    Context 'Before initialisation' {
        It 'Answers without the module having been initialized' {
            # Callbacks hang off the database rather than the state, because Initialize-ADTModule invokes
            # the OnInit ones itself. Anything reaching for them through the state would have to be seated
            # before initialization could start.
            Test-ADTModuleInitialized | Should -BeFalse
            InModuleScope -ModuleName PSAppDeployToolkit {
                Get-ADTModuleCallbacks | Should -BeOfType ([PSAppDeployToolkit.Foundation.ModuleCallbacks])
            }
        }
    }

    Context 'Functionality' {
        It 'Hands back the same instance every time' {
            # Registering a callback is adding to one of its lists, so a copy per call would discard every
            # registration.
            InModuleScope -ModuleName PSAppDeployToolkit {
                [System.Object]::ReferenceEquals((Get-ADTModuleCallbacks), (Get-ADTModuleCallbacks)) | Should -BeTrue
            }
        }

        It 'Survives the module being reset' {
            # A reset drops the state, not the database, so callbacks registered before one are still there
            # to be invoked on the next initialization.
            Initialize-ADTTestModule -Path $TestDrive
            $before = InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTModuleCallbacks }
            InModuleScope -ModuleName PSAppDeployToolkit { Reset-ADTModuleState }
            $after = InModuleScope -ModuleName PSAppDeployToolkit { Get-ADTModuleCallbacks }
            [System.Object]::ReferenceEquals($before, $after) | Should -BeTrue
        }
    }
}

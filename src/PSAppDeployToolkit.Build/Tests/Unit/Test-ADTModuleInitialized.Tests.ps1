BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Test-ADTModuleInitialized' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'Not Initialized' {
        # Default state after a plain Import-Module.
        It 'Returns a falsy value when module is not initialized' {
            Test-ADTModuleInitialized | Should -Not -BeTrue
        }

        It 'Does not throw' {
            { Test-ADTModuleInitialized } | Should -Not -Throw
        }
    }

    Context 'Initialized' {
        BeforeAll {
            InModuleScope PSAppDeployToolkit {
                $Script:ADTPesterInitBackup = $Script:ADT.Initialized
                $Script:ADT.Initialized = $true
            }
        }

        AfterAll {
            InModuleScope PSAppDeployToolkit {
                $Script:ADT.Initialized = $Script:ADTPesterInitBackup
                $Script:ADTPesterInitBackup = $null
            }
        }

        It 'Returns $true when module is initialized' {
            Test-ADTModuleInitialized | Should -BeTrue
        }

        It 'Returns a boolean' {
            Test-ADTModuleInitialized | Should -BeOfType ([System.Boolean])
        }

        It 'Result matches $Script:ADT.Initialized' {
            $expected = InModuleScope PSAppDeployToolkit { $Script:ADT.Initialized }
            Test-ADTModuleInitialized | Should -Be $expected
        }
    }
}

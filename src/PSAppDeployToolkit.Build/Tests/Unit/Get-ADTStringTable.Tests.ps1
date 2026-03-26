BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTStringTable' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'Not Initialized' {
        # After a plain Import-Module $Script:ADT.Strings is null — function must throw.
        It 'Throws when the string table is not initialized' {
            { Get-ADTStringTable } | Should -Throw
        }
    }

    Context 'Initialized' {
        BeforeAll {
            # Seed a minimal string table directly into module scope.
            InModuleScope PSAppDeployToolkit {
                $Script:ADT.Strings = @{ Greeting = 'Hello'; Farewell = 'Goodbye' }
            }
        }

        AfterAll {
            # Restore to uninitialised state so other tests see the default.
            InModuleScope PSAppDeployToolkit {
                $Script:ADT.Strings = $null
            }
        }

        It 'Returns a non-null result' {
            Get-ADTStringTable | Should -Not -BeNullOrEmpty
        }

        It 'Returns a Hashtable' {
            Get-ADTStringTable | Should -BeOfType ([System.Collections.Hashtable])
        }

        It 'Result is non-empty' {
            (Get-ADTStringTable).Count | Should -BeGreaterThan 0
        }

        It 'Does not throw when called' {
            { Get-ADTStringTable } | Should -Not -Throw
        }

        It 'Contains seeded keys' {
            (Get-ADTStringTable).ContainsKey('Greeting') | Should -BeTrue
        }

        It 'Returns the same reference on successive calls' {
            $a = Get-ADTStringTable
            $b = Get-ADTStringTable
            [object]::ReferenceEquals($a, $b) | Should -BeTrue
        }
    }
}

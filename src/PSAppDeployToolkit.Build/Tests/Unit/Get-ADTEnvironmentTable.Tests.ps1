BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTEnvironmentTable' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'Not initialized — throws' {
        It 'Throws when the environment table is null (module not fully initialized)' {
            # After plain Import-Module without Initialize-ADTModule, $Script:ADT.Environment is null.
            { Get-ADTEnvironmentTable } | Should -Throw
        }
    }

    Context 'Initialized via InModuleScope — happy path' {
        BeforeAll {
            InModuleScope PSAppDeployToolkit {
                $envTable = [System.Collections.Specialized.OrderedDictionary]::new()
                $envTable.Add('PSADTTestEnvKey1', 'Hello')
                $envTable.Add('PSADTTestEnvKey2', 99)
                $Script:ADT.Environment = $envTable
            }
        }

        AfterAll {
            InModuleScope PSAppDeployToolkit {
                $Script:ADT.Environment = $null
            }
        }

        It 'Does not throw when the environment table is populated' {
            { Get-ADTEnvironmentTable } | Should -Not -Throw
        }

        It 'Returns a non-null result' {
            Get-ADTEnvironmentTable | Should -Not -BeNull
        }

        It 'Returns a dictionary with at least one entry' {
            (Get-ADTEnvironmentTable).Count | Should -BeGreaterThan 0
        }

        It 'Contains the seeded key' {
            $result = Get-ADTEnvironmentTable
            $result.Contains('PSADTTestEnvKey1') | Should -BeTrue
        }

        It 'Seeded value matches what was stored' {
            $result = Get-ADTEnvironmentTable
            $result['PSADTTestEnvKey1'] | Should -Be 'Hello'
        }
    }
}

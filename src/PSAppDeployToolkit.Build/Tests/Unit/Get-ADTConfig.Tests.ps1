BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTConfig' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'Not initialized — throws' {
        It 'Throws when the config table is null (module not fully initialized)' {
            # After plain Import-Module without Initialize-ADTModule, $Script:ADT.Config is null.
            { Get-ADTConfig } | Should -Throw
        }
    }

    Context 'Initialized via InModuleScope — happy path' {
        BeforeAll {
            InModuleScope PSAppDeployToolkit {
                $configTable = @{}
                $configTable.Add('Toolkit', @{ CachePath = 'C:\Cache' })
                $configTable.Add('UI', @{ BalloonTipTime = 10000 })
                $Script:ADT.Config = $configTable
            }
        }

        AfterAll {
            InModuleScope PSAppDeployToolkit {
                $Script:ADT.Config = $null
            }
        }

        It 'Does not throw when the config table is populated' {
            { Get-ADTConfig } | Should -Not -Throw
        }

        It 'Returns a non-null result' {
            Get-ADTConfig | Should -Not -BeNull
        }

        It 'Returns a hashtable with at least one entry' {
            (Get-ADTConfig).Count | Should -BeGreaterThan 0
        }

        It 'Contains the seeded Toolkit key' {
            $result = Get-ADTConfig
            $result.ContainsKey('Toolkit') | Should -BeTrue
        }

        It 'Seeded CachePath value matches what was stored' {
            $result = Get-ADTConfig
            $result['Toolkit']['CachePath'] | Should -Be 'C:\Cache'
        }
    }
}

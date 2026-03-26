BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Export-ADTEnvironmentTableToSessionState' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Not initialized — throws' {
        It 'Throws when the environment table is null' {
            # $Script:ADT.Environment is null after plain Import-Module.
            { Export-ADTEnvironmentTableToSessionState } | Should -Throw
        }
    }

    Context 'Initialized via InModuleScope — happy path' {
        BeforeAll {
            InModuleScope PSAppDeployToolkit {
                # Must be a PSCustomObject so PSObject.Properties exposes the entries
                # (OrderedDictionary entries are NOT accessible via PSObject.Properties).
                $Script:ADT.Environment = [PSCustomObject]@{ PSADTTestExportVar = 'ExportedValue' }
            }
        }

        AfterAll {
            InModuleScope PSAppDeployToolkit {
                $Script:ADT.Environment = $null
            }
            # Clean up any exported variable that may have been created.
            Remove-Variable -Name 'PSADTTestExportVar' -Scope Script -ErrorAction SilentlyContinue
        }

        It 'Does not throw when the environment table is populated' {
            { Export-ADTEnvironmentTableToSessionState } | Should -Not -Throw
        }

        It 'Exports the seeded variable into the caller scope' {
            Export-ADTEnvironmentTableToSessionState
            $exported = Get-Variable -Name 'PSADTTestExportVar' -ErrorAction SilentlyContinue
            $exported | Should -Not -BeNull
        }

        It 'Exported variable has the correct value' {
            Export-ADTEnvironmentTableToSessionState
            $exported = Get-Variable -Name 'PSADTTestExportVar' -ErrorAction SilentlyContinue
            $exported.Value | Should -Be 'ExportedValue'
        }

        It 'Exported variable is ReadOnly' {
            Export-ADTEnvironmentTableToSessionState
            $exported = Get-Variable -Name 'PSADTTestExportVar' -ErrorAction SilentlyContinue
            ($exported.Options -band [System.Management.Automation.ScopedItemOptions]::ReadOnly) | Should -Be ([System.Management.Automation.ScopedItemOptions]::ReadOnly)
        }
    }
}

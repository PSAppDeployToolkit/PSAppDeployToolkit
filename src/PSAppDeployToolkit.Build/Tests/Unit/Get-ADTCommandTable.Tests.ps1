BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTCommandTable' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
    }

    Context 'Return Type' {
        It 'Returns a non-null result' {
            Get-ADTCommandTable | Should -Not -BeNullOrEmpty
        }

        It 'Returns an IReadOnlyDictionary[String, CommandInfo]' {
            $result = Get-ADTCommandTable
            $result | Should -BeOfType ([System.Collections.Generic.IReadOnlyDictionary[System.String, System.Management.Automation.CommandInfo]])
        }

        It 'The runtime type is a ReadOnlyDictionary wrapper' {
            $result = Get-ADTCommandTable
            $result.GetType().Name | Should -BeLike '*ReadOnly*'
        }

        It 'Does not throw when called' {
            { Get-ADTCommandTable } | Should -Not -Throw
        }
    }

    Context 'Contents' {
        It 'Result is non-empty' {
            (Get-ADTCommandTable).Count | Should -BeGreaterThan 0
        }

        It 'Contains the public function Get-ADTFreeDiskSpace' {
            (Get-ADTCommandTable).ContainsKey('Get-ADTFreeDiskSpace') | Should -BeTrue
        }

        It 'Contains the public function Convert-ADTValueType' {
            (Get-ADTCommandTable).ContainsKey('Convert-ADTValueType') | Should -BeTrue
        }

        It 'All values in the table are CommandInfo objects' {
            $result = Get-ADTCommandTable
            $allCommandInfo = $result.Values | ForEach-Object { $_ -is [System.Management.Automation.CommandInfo] }
            $allCommandInfo | Should -Not -Contain $false
        }
    }

    Context 'Read-Only Enforcement' {
        It 'Throws when attempting to add an entry (dictionary is read-only)' {
            $result = Get-ADTCommandTable
            { $result.Add('TestKey', $null) } | Should -Throw
        }
    }
}

BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTCommandTable' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Does not throw' {
            # BUG: Get-ADTCommandTable.ps1:47 iterates $Script:CommandTable.Values.GetEnumerator()
            # with foreach. ImmutableArray<T>.Enumerator is a struct that also implements
            # IEnumerable<T>, so PowerShell foreach treats the enumerator struct itself as the
            # iterable collection, yielding one item (the struct) instead of the CommandInfo objects.
            # The fix is to iterate $Script:CommandTable.Values directly (without .GetEnumerator()).
            # This test documents the correct contract and will pass once the bug is fixed.
            { Get-ADTCommandTable } | Should -Not -Throw
        }

        It 'Returns a non-null value' {
            $result = Get-ADTCommandTable
            $result | Should -Not -BeNull
        }

        It 'Returns a non-empty dictionary (more than zero entries)' {
            $result = Get-ADTCommandTable
            $result.Count | Should -BeGreaterThan 0
        }

        It 'Returns an IReadOnlyDictionary[String, CommandInfo] (not mutable)' {
            $result = Get-ADTCommandTable
            ($result -is [System.Collections.Generic.IReadOnlyDictionary[System.String, System.Management.Automation.CommandInfo]]) | Should -BeTrue
        }

        It 'Dictionary is read-only — it exposes no mutating Add overload' {
            # ReadOnlyDictionary implements Add via explicit interface only, so PowerShell finds no
            # callable overload and raises a MethodException rather than NotSupportedException.
            $result = Get-ADTCommandTable
            { $result.Add('FakeCommand', $null) } | Should -Throw -ExceptionType ([System.Management.Automation.MethodException])
        }

        It 'All values in the dictionary are CommandInfo objects' {
            $result = Get-ADTCommandTable
            foreach ($value in $result.Values)
            {
                $value | Should -BeOfType ([System.Management.Automation.CommandInfo])
            }
        }

        It 'All keys match the Name property of their corresponding CommandInfo value' {
            $result = Get-ADTCommandTable
            foreach ($key in $result.Keys)
            {
                $result[$key].Name | Should -Be $key
            }
        }

        It 'Contains the well-known public commands Write-ADTLogEntry and Get-ADTConfig' {
            $result = Get-ADTCommandTable
            $result.ContainsKey('Write-ADTLogEntry') | Should -BeTrue
            $result.ContainsKey('Get-ADTConfig') | Should -BeTrue
        }
    }

    Context 'Metadata' {
        It 'Has no parameters' {
            (Get-Command Get-ADTCommandTable).Parameters.Keys |
                Where-Object { $_ -notin [System.Management.Automation.Cmdlet]::CommonParameters } |
                Should -BeNullOrEmpty
        }

        It 'Is exported from the module' {
            (Get-Module PSAppDeployToolkit).ExportedFunctions.Keys | Should -Contain 'Get-ADTCommandTable'
        }
    }
}

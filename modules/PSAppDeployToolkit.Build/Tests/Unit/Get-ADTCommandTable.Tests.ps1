BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Get-ADTCommandTable' {
    Context 'Functionality' {
        It 'Returns a read-only dictionary of CommandInfo' {
            $table = Get-ADTCommandTable
            $table | Should -BeOfType ([System.Collections.Generic.IReadOnlyDictionary[System.String, System.Management.Automation.CommandInfo]])
            { $table.Add('Anything', $null) } | Should -Throw
        }

        It 'Holds every command the manifest exports' {
            # The table is what extension authors reach for, so it has to match what the module publishes
            # rather than whatever happened to be defined.
            $table = Get-ADTCommandTable
            foreach ($exported in (Get-Command -Module PSAppDeployToolkit).Name)
            {
                $table.ContainsKey($exported) | Should -BeTrue -Because "[$exported] is exported and should be reachable through the command table"
            }
        }

        It 'Also holds the external commands the module itself calls' {
            # The table is a safe lookup for extending modules, so it deliberately carries the built-in
            # cmdlets the module uses as well as its own functions. Its entries are not exports.
            $table = Get-ADTCommandTable
            $table.Count | Should -BeGreaterThan (Get-Command -Module PSAppDeployToolkit).Count
            $table.ContainsKey('Write-Host') | Should -BeTrue
            $table['Write-Host'].ModuleName | Should -Not -BeExactly 'PSAppDeployToolkit'
        }

        It 'Excludes the private functions' {
            # The distinction the function exists to make: the module's internal table carries both, and
            # this one filters the private ones back out.
            $table = Get-ADTCommandTable
            foreach ($private in (InModuleScope -ModuleName PSAppDeployToolkit { $Script:PrivateFuncs }))
            {
                $table.ContainsKey($private) | Should -BeFalse -Because "[$private] is private and should not be reachable through the command table"
            }
        }

        It 'Resolves an entry to something invocable' {
            & (Get-ADTCommandTable)['Out-ADTPowerShellEncodedCommand'] -Command 'Get-Process' | Should -BeExactly 'RwBlAHQALQBQAHIAbwBjAGUAcwBzAA=='
        }
    }
}

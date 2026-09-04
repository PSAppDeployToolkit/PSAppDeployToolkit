BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Select-ADTUniqueObject' {
    Context 'Functionality' {
        It 'Removes duplicates from a homogeneous set' {
            1, 2, 2, 3 | Select-ADTUniqueObject | Should -Be @(1, 2, 3)
        }

        It 'Ignores case by default' {
            # The reason the function exists: Windows PowerShell and PowerShell 7 disagree on the comparer
            # Select-Object -Unique uses, so this pins the behaviour rather than inheriting it.
            'string1', 'string2', 'String2', 'string3' | Select-ADTUniqueObject | Should -Be @('string1', 'string2', 'string3')
        }

        It 'Honours <CaseSensitivity>' -ForEach @(
            @{ CaseSensitivity = 'Ordinal'; Expected = @('a', 'A') }
            @{ CaseSensitivity = 'OrdinalIgnoreCase'; Expected = @('a') }
            @{ CaseSensitivity = 'InvariantCulture'; Expected = @('a', 'A') }
            @{ CaseSensitivity = 'InvariantCultureIgnoreCase'; Expected = @('a') }
        ) {
            'a', 'A' | Select-ADTUniqueObject -CaseSensitivity $CaseSensitivity | Should -Be $Expected
        }

        It 'Keeps the first occurrence rather than the last' {
            'aa', 'AA' | Select-ADTUniqueObject | Should -BeExactly @('aa')
        }

        It 'Accepts an array as an argument as well as from the pipeline' {
            Select-ADTUniqueObject -InputObject @(1, 1, 2) | Should -Be @(1, 2)
        }

        It 'Returns nothing for <Case>' -ForEach @(
            @{ Case = 'a null input'; Value = $null }
            @{ Case = 'an empty collection'; Value = @() }
            @{ Case = 'input that renders as nothing'; Value = @('', '  ') }
        ) {
            # Entries that render as empty are dropped before uniqueness is considered, so an input made
            # only of those yields nothing at all rather than an empty string.
            Select-ADTUniqueObject -InputObject $Value | Should -BeNullOrEmpty
        }

        It 'Handles a mixture of types' {
            $result = Select-ADTUniqueObject -InputObject @(1, 'one', 1, 'one')
            ($result | Measure-Object).Count | Should -Be 2
        }

        It 'Preserves the element type for a homogeneous set' {
            $result = Select-ADTUniqueObject -InputObject @(1, 2, 2)
            $result[0] | Should -BeOfType ([System.Int32])
        }
    }
}

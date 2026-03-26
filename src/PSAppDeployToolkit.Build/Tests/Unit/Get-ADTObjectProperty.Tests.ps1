BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTObjectProperty' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}

        # Note: Get-ADTObjectProperty uses InvokeMember with BindingFlags::GetProperty.
        # This operates on real .NET reflection — PowerShell synthetic properties (e.g.
        # Count on arrays, properties of PSCustomObject) are not accessible this way.
        # Arrays expose .Length, not .Count; use ArrayList/List for a real Count property.
    }

    Context 'String Properties' {
        It 'Returns the Length of a non-empty string' {
            Get-ADTObjectProperty -InputObject 'Hello' -PropertyName 'Length' | Should -Be 5
        }

        It 'Returns the Length of a single-character string' {
            Get-ADTObjectProperty -InputObject 'X' -PropertyName 'Length' | Should -Be 1
        }

        It 'Throws for a whitespace-only string InputObject (ValidateNotNullOrWhiteSpace)' {
            { Get-ADTObjectProperty -InputObject '   ' -PropertyName 'Length' } | Should -Throw
        }
    }

    Context 'DateTime Properties' {
        BeforeAll {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestDate', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
            $TestDate = [datetime]::new(2024, 6, 15, 10, 30, 0)
        }

        It 'Returns the Year from a DateTime' {
            Get-ADTObjectProperty -InputObject $TestDate -PropertyName 'Year' | Should -Be 2024
        }

        It 'Returns the Month from a DateTime' {
            Get-ADTObjectProperty -InputObject $TestDate -PropertyName 'Month' | Should -Be 6
        }

        It 'Returns the Day from a DateTime' {
            Get-ADTObjectProperty -InputObject $TestDate -PropertyName 'Day' | Should -Be 15
        }

        It 'Returns the Hour from a DateTime' {
            Get-ADTObjectProperty -InputObject $TestDate -PropertyName 'Hour' | Should -Be 10
        }

        It 'Returns the Minute from a DateTime' {
            Get-ADTObjectProperty -InputObject $TestDate -PropertyName 'Minute' | Should -Be 30
        }
    }

    Context 'Collection Properties' {
        It 'Returns the Length of an array (reflection exposes Length, not Count)' {
            # System.Object[] has a .Length property in .NET reflection.
            Get-ADTObjectProperty -InputObject @(1, 2, 3) -PropertyName 'Length' | Should -Be 3
        }

        It 'Returns the Count of a Hashtable' {
            $ht = @{ A = 1; B = 2; C = 3 }
            Get-ADTObjectProperty -InputObject $ht -PropertyName 'Count' | Should -Be 3
        }

        It 'Returns the Count of an ArrayList' {
            $list = [System.Collections.ArrayList]@(10, 20, 30)
            Get-ADTObjectProperty -InputObject $list -PropertyName 'Count' | Should -Be 3
        }

        It 'Returns the Count of a generic List' {
            $list = [System.Collections.Generic.List[string]]@('alpha', 'beta')
            Get-ADTObjectProperty -InputObject $list -PropertyName 'Count' | Should -Be 2
        }

        It 'Returns the IsFixedSize boolean property of an ArrayList' {
            $list = [System.Collections.ArrayList]@(1, 2, 3)
            Get-ADTObjectProperty -InputObject $list -PropertyName 'IsFixedSize' | Should -BeFalse
        }
    }

    Context 'StringBuilder Properties' {
        BeforeAll {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'Sb', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
            $Sb = [System.Text.StringBuilder]::new('Hello World')
        }

        It 'Returns the Length of a StringBuilder' {
            Get-ADTObjectProperty -InputObject $Sb -PropertyName 'Length' | Should -Be 11
        }

        It 'Returns the MaxCapacity of a StringBuilder' {
            Get-ADTObjectProperty -InputObject $Sb -PropertyName 'MaxCapacity' | Should -BeGreaterThan 0
        }
    }

    Context 'Indexed Properties (ArgumentList)' {
        It 'Returns the character at index 0 in a string via Chars property' {
            $result = Get-ADTObjectProperty -InputObject 'Hello' -PropertyName 'Chars' -ArgumentList @(0)
            $result | Should -Be 'H'
        }

        It 'Returns the character at the last index in a string' {
            $result = Get-ADTObjectProperty -InputObject 'Hello' -PropertyName 'Chars' -ArgumentList @(4)
            $result | Should -Be 'o'
        }

        It 'Returns a specific item from an ArrayList via Item property' {
            $list = [System.Collections.ArrayList]@('Alpha', 'Beta', 'Gamma')
            $result = Get-ADTObjectProperty -InputObject $list -PropertyName 'Item' -ArgumentList @(1)
            $result | Should -Be 'Beta'
        }

        It 'Returns the first item from an ArrayList' {
            $list = [System.Collections.ArrayList]@('First', 'Second')
            $result = Get-ADTObjectProperty -InputObject $list -PropertyName 'Item' -ArgumentList @(0)
            $result | Should -Be 'First'
        }
    }

    Context 'Return Value Types' {
        It 'Returns a System.Int32 for the Length of a string' {
            $result = Get-ADTObjectProperty -InputObject 'Test' -PropertyName 'Length'
            $result | Should -BeOfType [System.Int32]
        }

        It 'Returns a System.Char for the Chars indexed property' {
            $result = Get-ADTObjectProperty -InputObject 'Test' -PropertyName 'Chars' -ArgumentList @(0)
            $result | Should -BeOfType [System.Char]
        }

        It 'Returns a System.Boolean for boolean properties' {
            $list = [System.Collections.ArrayList]@(1, 2, 3)
            $result = Get-ADTObjectProperty -InputObject $list -PropertyName 'IsFixedSize'
            $result | Should -BeOfType [System.Boolean]
        }

        It 'Returns a System.DateTime for DateTime properties' {
            $dt = [datetime]::new(2024, 1, 1)
            $result = Get-ADTObjectProperty -InputObject $dt -PropertyName 'Date'
            $result | Should -BeOfType [System.DateTime]
        }
    }

    Context 'Error Cases' {
        It 'Throws when accessing a non-existent property name' {
            { Get-ADTObjectProperty -InputObject 'Hello' -PropertyName 'NonExistentProp' -ErrorAction Stop } | Should -Throw
        }

        It 'Throws when accessing a Chars index that is out of bounds' {
            { Get-ADTObjectProperty -InputObject 'Hi' -PropertyName 'Chars' -ArgumentList @(99) -ErrorAction Stop } | Should -Throw
        }

        It 'Throws when accessing a static field as if it were an instance property' {
            # Int32.MaxValue is a static field — not accessible via GetProperty InvokeMember.
            { Get-ADTObjectProperty -InputObject ([int]0) -PropertyName 'MaxValue' -ErrorAction Stop } | Should -Throw
        }
    }

    Context 'Input Validation' {
        It 'Throws when InputObject is null' {
            { Get-ADTObjectProperty -InputObject $null -PropertyName 'Length' } | Should -Throw
        }

        It 'Throws when PropertyName is null' {
            { Get-ADTObjectProperty -InputObject 'Hello' -PropertyName $null } | Should -Throw
        }

        It 'Throws when PropertyName is an empty string' {
            { Get-ADTObjectProperty -InputObject 'Hello' -PropertyName '' } | Should -Throw
        }

        It 'Throws when ArgumentList is explicitly empty' {
            { Get-ADTObjectProperty -InputObject 'Hello' -PropertyName 'Length' -ArgumentList @() } | Should -Throw
        }
    }
}

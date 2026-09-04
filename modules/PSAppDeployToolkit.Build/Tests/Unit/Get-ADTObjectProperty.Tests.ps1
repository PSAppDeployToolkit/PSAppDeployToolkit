BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester. It is also how the deprecation
    # notice is asserted below.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTObjectProperty' {
    Context 'Functionality' {
        It 'Reads a property off an object' {
            Get-ADTObjectProperty -InputObject ([System.DateTime]::new(2026, 3, 14)) -PropertyName 'Year' | Should -Be 2026
        }

        It 'Reads a property off a string' {
            Get-ADTObjectProperty -InputObject 'abcdef' -PropertyName 'Length' | Should -Be 6
        }

        It 'Reads an indexed property using -ArgumentList' {
            # The reason the function takes an argument list at all: Chars is only reachable with an index.
            Get-ADTObjectProperty -InputObject 'abcdef' -PropertyName 'Chars' -ArgumentList 2 | Should -Be ([System.Char]'c')
        }

        It 'Accepts its parameters positionally' {
            Get-ADTObjectProperty 'abcdef' 'Length' | Should -Be 6
        }

        It 'Errors when the property does not exist' {
            { Get-ADTObjectProperty -InputObject 'abcdef' -PropertyName 'NoSuchProperty' -ErrorAction Stop } | Should -Throw
        }

        It 'Warns that it is deprecated' {
            # The function is slated for removal in 4.3.0, so the notice is part of its contract until then.
            $null = Get-ADTObjectProperty -InputObject 'abcdef' -PropertyName 'Length'
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 1 -Exactly -ParameterFilter {
                $Severity -eq 'Warning' -and $Message -match 'deprecated and will be removed'
            }
        }
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester. It is also how the deprecation
    # notice is asserted below.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Invoke-ADTObjectMethod' {
    Context 'Functionality' {
        It 'Invokes a method with no arguments' {
            Invoke-ADTObjectMethod -InputObject '  padded  ' -MethodName 'Trim' | Should -BeExactly 'padded'
        }

        It 'Invokes a method with positional arguments' {
            Invoke-ADTObjectMethod -InputObject 'abcdef' -MethodName 'Substring' -ArgumentList 2, 3 | Should -BeExactly 'cde'
        }

        It 'Invokes a method with named arguments' {
            # The Named parameter set, which passes the hashtable's keys through as parameter names.
            Invoke-ADTObjectMethod -InputObject 'abcdef' -MethodName 'Substring' -Parameter @{ startIndex = 2; length = 3 } | Should -BeExactly 'cde'
        }

        It 'Returns what the method returns, with its own type' {
            Invoke-ADTObjectMethod -InputObject 'abcdef' -MethodName 'IndexOf' -ArgumentList 'c' | Should -BeOfType ([System.Int32])
        }

        It 'Accepts its parameters positionally' {
            Invoke-ADTObjectMethod '  padded  ' 'Trim' | Should -BeExactly 'padded'
        }

        It 'Throws when the method does not exist' {
            { Invoke-ADTObjectMethod -InputObject 'abcdef' -MethodName 'NoSuchMethod' } | Should -Throw
        }

        It 'Rejects -ArgumentList and -Parameter together' {
            # They sit in different parameter sets, so supplying both is ambiguous rather than merged.
            { Invoke-ADTObjectMethod -InputObject 'abcdef' -MethodName 'Substring' -ArgumentList 2 -Parameter @{ startIndex = 2 } } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Warns that it is deprecated' {
            $null = Invoke-ADTObjectMethod -InputObject '  padded  ' -MethodName 'Trim'
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -Times 1 -Exactly -ParameterFilter {
                $Severity -eq 'Warning' -and $Message -match 'deprecated and will be removed'
            }
        }
    }
}

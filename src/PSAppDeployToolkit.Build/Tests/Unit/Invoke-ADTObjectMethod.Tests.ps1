BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Invoke-ADTObjectMethod' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
    }

    BeforeEach {
        # Fresh StringBuilder for each test — mutable so state must be isolated.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'Sb', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $Script:Sb = [System.Text.StringBuilder]::new('Hello')
    }

    Context 'Positional (ArgumentList) Parameter Set' {
        It 'Invokes Append and mutates the StringBuilder' {
            Invoke-ADTObjectMethod -InputObject $Script:Sb -MethodName 'Append' -ArgumentList @(' World')
            $Script:Sb.ToString() | Should -Be 'Hello World'
        }

        It 'Invokes ToString and returns the string value' {
            $result = Invoke-ADTObjectMethod -InputObject $Script:Sb -MethodName 'ToString'
            $result | Should -Be 'Hello'
        }

        It 'Invokes Clear and resets the StringBuilder length to zero' {
            Invoke-ADTObjectMethod -InputObject $Script:Sb -MethodName 'Clear'
            $Script:Sb.Length | Should -Be 0
        }

        It 'Invokes Insert at a specific index' {
            Invoke-ADTObjectMethod -InputObject $Script:Sb -MethodName 'Insert' -ArgumentList @(0, 'Say: ')
            $Script:Sb.ToString() | Should -Be 'Say: Hello'
        }

        It 'Invokes a method on an ArrayList and returns the new item index' {
            # ValidateNotNullOrEmpty rejects empty collections; seed with one item first.
            $list = [System.Collections.ArrayList]@('seed')
            $result = Invoke-ADTObjectMethod -InputObject $list -MethodName 'Add' -ArgumentList @('item')
            $result | Should -Be 1
            $list.Count | Should -Be 2
        }
    }

    Context 'Named (Parameter) Parameter Set' {
        It 'Invokes ArrayList.Add using a named parameter and appends the item' {
            # ValidateNotNullOrEmpty rejects empty collections; seed with one item first.
            $list = [System.Collections.ArrayList]@('seed')
            Invoke-ADTObjectMethod -InputObject $list -MethodName 'Add' -Parameter @{ value = 'named-item' }
            $list[1] | Should -Be 'named-item'
        }

        It 'Invokes Append on StringBuilder using a named parameter' {
            Invoke-ADTObjectMethod -InputObject $Script:Sb -MethodName 'Append' -Parameter @{ value = '!' }
            $Script:Sb.ToString() | Should -Be 'Hello!'
        }
    }

    Context 'Return Values' {
        It 'Returns $null for methods that return void' {
            # StringBuilder.Clear returns the StringBuilder (not void), but ArrayList.Clear returns void.
            $list = [System.Collections.ArrayList]@(1, 2, 3)
            $result = Invoke-ADTObjectMethod -InputObject $list -MethodName 'Clear'
            $result | Should -BeNullOrEmpty
        }

        It 'Returns the correct value type from a method that returns Int32' {
            # ValidateNotNullOrEmpty rejects empty collections; seed with one item first.
            $list = [System.Collections.ArrayList]@('seed')
            $result = Invoke-ADTObjectMethod -InputObject $list -MethodName 'Add' -ArgumentList @('x')
            $result | Should -BeOfType [System.Int32]
        }
    }

    Context 'Error Cases' {
        It 'Throws when calling a method that does not exist on the object' {
            { Invoke-ADTObjectMethod -InputObject $Script:Sb -MethodName 'NonExistentMethod' } | Should -Throw
        }
    }

    Context 'Input Validation' {
        It 'Throws when InputObject is null' {
            { Invoke-ADTObjectMethod -InputObject $null -MethodName 'ToString' } | Should -Throw
        }

        It 'Throws when MethodName is null' {
            { Invoke-ADTObjectMethod -InputObject $Script:Sb -MethodName $null } | Should -Throw
        }

        It 'Throws when MethodName is an empty string' {
            { Invoke-ADTObjectMethod -InputObject $Script:Sb -MethodName '' } | Should -Throw
        }

        It 'Throws when ArgumentList is explicitly empty' {
            { Invoke-ADTObjectMethod -InputObject $Script:Sb -MethodName 'Append' -ArgumentList @() } | Should -Throw
        }

        It 'Throws when Parameter hashtable is empty' {
            { Invoke-ADTObjectMethod -InputObject $Script:Sb -MethodName 'Append' -Parameter @{} } | Should -Throw
        }
    }
}

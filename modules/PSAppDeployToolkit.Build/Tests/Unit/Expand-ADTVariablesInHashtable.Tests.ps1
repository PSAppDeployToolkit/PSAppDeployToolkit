BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mirrors how Import-ADTConfig calls this: the variables to expand and the session state handed over
    # both come from the one scope. A session state captured somewhere else cannot resolve them.
    function Invoke-Expansion
    {
        param
        (
            [Parameter(Mandatory = $true)]
            [System.Collections.Hashtable]$Hashtable
        )

        InModuleScope -ModuleName PSAppDeployToolkit -Parameters @{ Table = $Hashtable } {
            New-Variable -Name ADTExpansionProbe -Value 'expanded-value'
            Expand-ADTVariablesInHashtable -Hashtable $Table -SessionState $ExecutionContext.SessionState
        }
    }
}

Describe 'Expand-ADTVariablesInHashtable' {
    Context 'Functionality' {
        It 'Expands a variable in a top-level value' {
            $table = @{ Value = '$ADTExpansionProbe' }
            Invoke-Expansion -Hashtable $table
            $table.Value | Should -BeExactly 'expanded-value'
        }

        It 'Expands a variable embedded in surrounding text' {
            $table = @{ Value = 'before-$ADTExpansionProbe-after' }
            Invoke-Expansion -Hashtable $table
            $table.Value | Should -BeExactly 'before-expanded-value-after'
        }

        It 'Recurses into nested hashtables' {
            $table = @{ Nested = @{ Deeper = @{ Value = '$ADTExpansionProbe' } } }
            Invoke-Expansion -Hashtable $table
            $table.Nested.Deeper.Value | Should -BeExactly 'expanded-value'
        }

        It 'Mutates the hashtable it was given rather than returning a copy' {
            # Every caller relies on this: the return value is discarded and the original is read back.
            $table = @{ Value = '$ADTExpansionProbe' }
            Invoke-Expansion -Hashtable $table | Should -BeNullOrEmpty
            $table.Value | Should -BeExactly 'expanded-value'
        }

        It 'Leaves a <TypeName> value untouched' -ForEach @(
            @{ TypeName = 'Int32'; Value = 42 }
            @{ TypeName = 'Boolean'; Value = $true }
            @{ TypeName = 'DateTime'; Value = [System.DateTime]::new(2026, 1, 1) }
            @{ TypeName = 'String[]'; Value = @('$ADTExpansionProbe') }
        ) {
            # Only strings and nested hashtables are walked. An array of strings is deliberately included:
            # its elements are not expanded, which is easy to assume otherwise.
            $table = @{ Value = $Value }
            Invoke-Expansion -Hashtable $table
            $table.Value | Should -Be $Value
        }

        It 'Rejects an empty hashtable' {
            # ValidateNotNullOrEmpty on the parameter, so a section with no keys is refused outright rather
            # than walked and left alone.
            { Invoke-Expansion -Hashtable @{} } | Should -Throw -ExpectedMessage "*Cannot validate argument on parameter 'Hashtable'*"
        }

        It 'Throws when a value names a variable that does not exist' {
            # ExpandString raises this from the .NET side, so it terminates regardless of ErrorActionPreference.
            $table = @{ Value = '$ThisVariableIsNotSetAnywhere' }
            { Invoke-Expansion -Hashtable $table } | Should -Throw -ExceptionType ([System.Management.Automation.MethodInvocationException])
        }
    }
}

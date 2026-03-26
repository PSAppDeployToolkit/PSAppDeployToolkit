BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTBoundParametersAndDefaultValues' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}

        # Test helpers: real named functions so that $MyInvocation.MyCommand.ScriptBlock.Ast
        # is a FunctionDefinitionAst that Get-ADTBoundParametersAndDefaultValues can parse.
        function script:Invoke-BoundParamHelper
        {
            [CmdletBinding()]
            param(
                [string]$Name = 'DefaultName',
                [int]$Count = 10,
                [string]$NoDefault
            )
            $null = $Name, $Count, $NoDefault
            return Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation
        }

        function script:Invoke-ExcludeHelper
        {
            [CmdletBinding()]
            param(
                [string]$Name = 'DefaultName',
                [int]$Count = 10
            )
            $null = $Name, $Count
            return Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -Exclude 'Name'
        }
    }

    Context 'Default Values' {
        It 'Includes a parameter with a default string value when not explicitly bound' {
            $result = Invoke-BoundParamHelper
            $result.ContainsKey('Name') | Should -BeTrue
            $result['Name'] | Should -Be 'DefaultName'
        }

        It 'Includes a parameter with a default numeric value when not explicitly bound' {
            $result = Invoke-BoundParamHelper
            $result.ContainsKey('Count') | Should -BeTrue
            $result['Count'] | Should -Be 10
        }
    }

    Context 'Bound Parameters' {
        It 'Returns the explicitly bound value rather than the default' {
            $result = Invoke-BoundParamHelper -Name 'Custom'
            $result['Name'] | Should -Be 'Custom'
        }

        It 'Includes an explicitly bound parameter that has no default' {
            $result = Invoke-BoundParamHelper -NoDefault 'Provided'
            $result.ContainsKey('NoDefault') | Should -BeTrue
            $result['NoDefault'] | Should -Be 'Provided'
        }

        It 'Includes both the bound value and default-valued unbound parameters' {
            $result = Invoke-BoundParamHelper -Name 'Explicit'
            $result['Name'] | Should -Be 'Explicit'
            $result['Count'] | Should -Be 10
        }
    }

    Context 'Unbound Parameters without Defaults' {
        It 'Does not include a parameter that has no default and was not bound' {
            $result = Invoke-BoundParamHelper
            $result.ContainsKey('NoDefault') | Should -BeFalse
        }
    }

    Context 'Exclude Parameter' {
        It 'Removes an excluded parameter from the result even when it has a default' {
            $result = Invoke-ExcludeHelper -Name 'Foo' -Count 5
            $result.ContainsKey('Name') | Should -BeFalse
        }

        It 'Retains non-excluded parameters when Exclude is specified' {
            $result = Invoke-ExcludeHelper -Name 'Foo' -Count 5
            $result.ContainsKey('Count') | Should -BeTrue
            $result['Count'] | Should -Be 5
        }
    }

    Context 'Common Parameters Filtering' {
        It 'Excludes common parameters (e.g. Verbose) from the result by default' {
            $result = Invoke-BoundParamHelper -Verbose
            $result.ContainsKey('Verbose') | Should -BeFalse
        }
    }

    Context 'Return Value Type' {
        It 'Returns a Generic Dictionary[String, Object]' {
            $result = Invoke-BoundParamHelper
            $result | Should -BeOfType ([System.Collections.Generic.Dictionary[System.String, System.Object]])
        }
    }

    Context 'Input Validation' {
        It 'Throws when Invocation is null' {
            { Get-ADTBoundParametersAndDefaultValues -Invocation $null } | Should -Throw
        }
    }
}

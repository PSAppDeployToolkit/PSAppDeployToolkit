BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Helper advanced function that calls Get-ADTBoundParametersAndDefaultValues from its own invocation context.
    function Invoke-TestHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Name', Justification = 'This parameter is read via MyInvocation.BoundParameters and AST, not directly in the body.')]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Count', Justification = 'This parameter is read via MyInvocation.BoundParameters and AST, not directly in the body.')]
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [System.String]$Name,

            [Parameter(Mandatory = $false)]
            [System.Int32]$Count = 10
        )
        return Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation
    }

    function Invoke-ExcludeHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Alpha', Justification = 'This parameter is read via MyInvocation.BoundParameters and AST, not directly in the body.')]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Beta', Justification = 'This parameter is read via MyInvocation.BoundParameters and AST, not directly in the body.')]
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $false)]
            [System.String]$Alpha = 'a',

            [Parameter(Mandatory = $false)]
            [System.String]$Beta = 'b'
        )
        return Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -Exclude 'Beta'
    }

    function Invoke-IncludeHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Alpha', Justification = 'This parameter is read via MyInvocation.BoundParameters and AST, not directly in the body.')]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Beta', Justification = 'This parameter is read via MyInvocation.BoundParameters and AST, not directly in the body.')]
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $false)]
            [System.String]$Alpha = 'a',

            [Parameter(Mandatory = $false)]
            [System.String]$Beta = 'b'
        )
        return Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -Include 'Alpha'
    }
}
Describe 'Get-ADTBoundParametersAndDefaultValues' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Returns a Dictionary[String, Object]' {
            $result = Invoke-TestHelper -Name 'Alice'
            $result | Should -BeOfType ([System.Collections.Generic.Dictionary[System.String, System.Object]])
        }
        It 'Includes explicitly bound parameters' {
            $result = Invoke-TestHelper -Name 'Alice'
            $result.ContainsKey('Name') | Should -BeTrue
            $result['Name'] | Should -Be 'Alice'
        }
        It 'Includes default values for parameters not explicitly bound' {
            $result = Invoke-TestHelper -Name 'Alice'
            $result.ContainsKey('Count') | Should -BeTrue
            $result['Count'] | Should -Be 10
        }
        It 'Overrides default value when parameter is explicitly bound' {
            $result = Invoke-TestHelper -Name 'Alice' -Count 99
            $result['Count'] | Should -Be 99
        }
        It 'Excludes a parameter when -Exclude is supplied' {
            $result = Invoke-ExcludeHelper
            $result.ContainsKey('Alpha') | Should -BeTrue
            $result.ContainsKey('Beta') | Should -BeFalse
        }
        It 'Restricts results to -Include parameters when supplied' {
            $result = Invoke-IncludeHelper
            $result.ContainsKey('Alpha') | Should -BeTrue
            $result.ContainsKey('Beta') | Should -BeFalse
        }
        It '-Exclude takes precedence over -Include when the same parameter name appears in both' {
            function Invoke-PrecedenceHelper
            {
                [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Alpha', Justification = 'This parameter is read via MyInvocation.BoundParameters and AST, not directly in the body.')]
                [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Beta', Justification = 'This parameter is read via MyInvocation.BoundParameters and AST, not directly in the body.')]
                [CmdletBinding()]
                param
                (
                    [Parameter(Mandatory = $false)]
                    [System.String]$Alpha = 'a',

                    [Parameter(Mandatory = $false)]
                    [System.String]$Beta = 'b'
                )
                return Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -Include 'Alpha', 'Beta' -Exclude 'Beta'
            }
            $result = Invoke-PrecedenceHelper -Alpha 'x' -Beta 'y'
            $result.ContainsKey('Alpha') | Should -BeTrue
            $result.ContainsKey('Beta') | Should -BeFalse
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterArgumentValidationError when Invocation is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Get-ADTBoundParametersAndDefaultValues'
            }
            { Get-ADTBoundParametersAndDefaultValues -Invocation $null } | Should @shouldParams
        }
        It 'Throws ParameterArgumentValidationError when Exclude is empty string' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Get-ADTBoundParametersAndDefaultValues'
            }
            { Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -Exclude '' } | Should @shouldParams
        }
    }
}

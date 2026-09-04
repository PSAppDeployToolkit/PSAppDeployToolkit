BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Get-ADTBoundParametersAndDefaultValues' {
    Context 'Functionality' {
        BeforeAll {
            # Kept separate so each test can pass its own extra arguments through to the function.
            function Invoke-Probe
            {
                param
                (
                    [Parameter(Mandatory = $false)]
                    [System.Collections.Hashtable]$Probe = @{},

                    [Parameter(Mandatory = $false)]
                    [System.Collections.Hashtable]$Options = @{}
                )

                $script:ProbeOptions = $Options
                function Get-Probed
                {
                    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Named', Justification = 'The parameters exist so the function under test can read them back out of the invocation.')]
                    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Untagged', Justification = 'The parameters exist so the function under test can read them back out of the invocation.')]
                    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'OtherSet', Justification = 'The parameters exist so the function under test can read them back out of the invocation.')]
                    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Number', Justification = 'The parameters exist so the function under test can read them back out of the invocation.')]
                    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'NoDefault', Justification = 'The parameters exist so the function under test can read them back out of the invocation.')]
                    [CmdletBinding(DefaultParameterSetName = 'Alpha')]
                    param
                    (
                        [Parameter(Mandatory = $false, ParameterSetName = 'Alpha', HelpMessage = 'Tagged')]
                        [System.String]$Named = 'namedDefault',

                        [Parameter(Mandatory = $false, ParameterSetName = 'Alpha')]
                        [System.String]$Untagged = 'untaggedDefault',

                        [Parameter(Mandatory = $false, ParameterSetName = 'Beta')]
                        [System.String]$OtherSet = 'otherDefault',

                        [Parameter(Mandatory = $false)]
                        [System.Int32]$Number = 42,

                        [Parameter(Mandatory = $false)]
                        [System.String]$NoDefault
                    )

                    return Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation @script:ProbeOptions
                }
                return Get-Probed @Probe
            }
        }

        It 'Returns a case-insensitive dictionary' {
            $result = Invoke-Probe
            $result | Should -BeOfType ([System.Collections.Generic.Dictionary[System.String, System.Object]])
            $result['number'] | Should -Be 42
        }

        It 'Fills in the default for a parameter that was not supplied' {
            (Invoke-Probe)['Named'] | Should -BeExactly 'namedDefault'
        }

        It 'Prefers the bound value over the default' {
            (Invoke-Probe -Probe @{ Named = 'supplied' })['Named'] | Should -BeExactly 'supplied'
        }

        It 'Omits a parameter that has neither a value nor a default' {
            (Invoke-Probe).ContainsKey('NoDefault') | Should -BeFalse
        }

        It 'Includes a parameter with no default once it is supplied' {
            (Invoke-Probe -Probe @{ NoDefault = 'now set' })['NoDefault'] | Should -BeExactly 'now set'
        }

        It 'Drops the names given to -Exclude' {
            $result = Invoke-Probe -Options @{ Exclude = 'Named', 'Number' }
            $result.ContainsKey('Named') | Should -BeFalse
            $result.ContainsKey('Number') | Should -BeFalse
            $result.ContainsKey('Untagged') | Should -BeTrue
        }

        It 'Keeps only the names given to -Include' {
            $result = Invoke-Probe -Options @{ Include = 'Number' }
            $result.Keys | Should -Be @('Number')
        }

        It 'Keeps only the parameters in the named parameter set' {
            $result = Invoke-Probe -Options @{ ParameterSetName = 'Beta' }
            $result.ContainsKey('OtherSet') | Should -BeTrue
            $result.ContainsKey('Named') | Should -BeFalse
            $result.ContainsKey('Number') | Should -BeFalse
        }

        It 'Keeps only the parameters carrying the given help message' {
            $result = Invoke-Probe -Options @{ HelpMessage = 'Tagged' }
            $result.Keys | Should -Be @('Named')
        }

        It 'Leaves the common parameters out unless asked for them' {
            (Invoke-Probe -Probe @{ Verbose = $true }).ContainsKey('Verbose') | Should -BeFalse
            (Invoke-Probe -Probe @{ Verbose = $true } -Options @{ CommonParameters = $true }).ContainsKey('Verbose') | Should -BeTrue
        }

        It 'Errors when the invocation has no parameters to read' {
            function Get-NoParameters
            {
                [CmdletBinding()]
                param
                (
                )

                return Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation
            }
            { Get-NoParameters -ErrorAction Stop } | Should -Throw -ErrorId 'InvocationParametersNotFound,Get-ADTBoundParametersAndDefaultValues'
        }
    }
}

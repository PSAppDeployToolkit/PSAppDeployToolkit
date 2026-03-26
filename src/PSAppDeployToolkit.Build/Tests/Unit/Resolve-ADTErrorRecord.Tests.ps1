BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Resolve-ADTErrorRecord' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}

        # A reusable ErrorRecord with a known message and ID, created programmatically.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestErrorRecord', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $TestErrorRecord = [System.Management.Automation.ErrorRecord]::new(
            [System.Exception]::new('Test error message'),
            'TestErrorId',
            [System.Management.Automation.ErrorCategory]::NotSpecified,
            'TestTargetObject'
        )

        # An ErrorRecord whose exception has an inner exception — for IncludeErrorInnerException tests.
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'NestedErrorRecord', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $NestedErrorRecord = [System.Management.Automation.ErrorRecord]::new(
            [System.InvalidOperationException]::new('Outer exception', [System.Exception]::new('Inner exception message')),
            'NestedErrorId',
            [System.Management.Automation.ErrorCategory]::InvalidOperation,
            $null
        )
    }

    Context 'Default Behaviour' {
        It 'Returns a non-empty string' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $TestErrorRecord
            $result | Should -Not -BeNullOrEmpty
        }

        It 'Returns a string that begins with the "Error Record:" header' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $TestErrorRecord
            $result | Should -Match 'Error Record:'
        }

        It 'Output contains the exception message' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $TestErrorRecord
            $result | Should -Match 'Test error message'
        }

        It 'Output contains the FullyQualifiedErrorId' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $TestErrorRecord
            $result | Should -Match 'TestErrorId'
        }

        It 'Returns a System.String' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $TestErrorRecord
            $result | Should -BeOfType [System.String]
        }
    }

    Context 'Exclusion Switches' {
        It 'Does not throw when -ExcludeErrorRecord is specified' {
            { Resolve-ADTErrorRecord -ErrorRecord $TestErrorRecord -ExcludeErrorRecord } | Should -Not -Throw
        }

        It 'Does not throw when -ExcludeErrorException is specified' {
            { Resolve-ADTErrorRecord -ErrorRecord $TestErrorRecord -ExcludeErrorException } | Should -Not -Throw
        }

        It 'Does not throw when -ExcludeErrorInvocation is specified' {
            { Resolve-ADTErrorRecord -ErrorRecord $TestErrorRecord -ExcludeErrorInvocation } | Should -Not -Throw
        }

        It 'Does not include FullyQualifiedErrorId when -ExcludeErrorRecord is specified' {
            # FullyQualifiedErrorId is a property of the ErrorRecord — excluding it removes this field.
            $result = Resolve-ADTErrorRecord -ErrorRecord $TestErrorRecord -ExcludeErrorRecord
            $result | Should -Not -Match 'TestErrorId'
        }
    }

    Context 'Property Selection' {
        It 'Does not throw when -Property * is specified' {
            { Resolve-ADTErrorRecord -ErrorRecord $TestErrorRecord -Property '*' } | Should -Not -Throw
        }

        It 'Includes only the requested property when a specific property name is given' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $TestErrorRecord -Property 'Message'
            $result | Should -Match 'Test error message'
        }
    }

    Context 'Inner Exception' {
        It 'Does not throw when -IncludeErrorInnerException is specified and an inner exception exists' {
            { Resolve-ADTErrorRecord -ErrorRecord $NestedErrorRecord -IncludeErrorInnerException } | Should -Not -Throw
        }

        It 'Output contains inner exception details when -IncludeErrorInnerException is specified' {
            $result = Resolve-ADTErrorRecord -ErrorRecord $NestedErrorRecord -IncludeErrorInnerException
            $result | Should -Match 'Inner exception message'
        }
    }

    Context 'Pipeline Input' {
        It 'Accepts ErrorRecord from the pipeline' {
            $result = $TestErrorRecord | Resolve-ADTErrorRecord
            $result | Should -Match 'Error Record:'
        }
    }

    Context 'Input Validation' {
        It 'Throws when ErrorRecord is null' {
            { Resolve-ADTErrorRecord -ErrorRecord $null } | Should -Throw
        }
    }
}

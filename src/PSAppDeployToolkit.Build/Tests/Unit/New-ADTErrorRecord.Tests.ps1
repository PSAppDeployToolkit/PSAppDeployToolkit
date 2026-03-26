BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'New-ADTErrorRecord' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables {}
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestException', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $TestException = [System.Exception]::new('Test exception message')

        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestCategory', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
        $TestCategory = [System.Management.Automation.ErrorCategory]::NotSpecified
    }

    Context 'Output Type' {
        It 'Returns a System.Management.Automation.ErrorRecord object' {
            New-ADTErrorRecord -Exception $TestException -Category $TestCategory | Should -BeOfType [System.Management.Automation.ErrorRecord]
        }
    }

    Context 'Required Parameters' {
        It 'Preserves the supplied Exception object' {
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory
            $result.Exception | Should -Be $TestException
            $result.Exception.Message | Should -Be 'Test exception message'
        }

        It 'Sets the Category correctly' {
            $result = New-ADTErrorRecord -Exception $TestException -Category ([System.Management.Automation.ErrorCategory]::InvalidArgument)
            $result.CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidArgument)
        }
    }

    Context 'Default Parameter Values' {
        It 'Uses NotSpecified as the default ErrorId' {
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory
            $result.FullyQualifiedErrorId | Should -Match 'NotSpecified'
        }

        It 'Has a null TargetObject by default' {
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory
            $result.TargetObject | Should -BeNullOrEmpty
        }

        It 'Does not set ErrorDetails when RecommendedAction is omitted' {
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory
            $result.ErrorDetails | Should -BeNullOrEmpty
        }
    }

    Context 'Optional Parameters' {
        It 'Sets a custom ErrorId in FullyQualifiedErrorId' {
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory -ErrorId 'CustomErrorId'
            $result.FullyQualifiedErrorId | Should -Match 'CustomErrorId'
        }

        It 'Sets TargetObject to the supplied value' {
            $targetObj = [pscustomobject]@{ Key = 'Value' }
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory -TargetObject $targetObj
            $result.TargetObject | Should -Be $targetObj
        }

        It 'Allows TargetObject to be explicitly null' {
            { New-ADTErrorRecord -Exception $TestException -Category $TestCategory -TargetObject $null } | Should -Not -Throw
        }

        It 'Sets Activity in CategoryInfo correctly' {
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory -Activity 'TestActivity'
            $result.CategoryInfo.Activity | Should -Be 'TestActivity'
        }

        It 'Sets TargetName in CategoryInfo correctly' {
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory -TargetName 'TestTargetName'
            $result.CategoryInfo.TargetName | Should -Be 'TestTargetName'
        }

        It 'Sets TargetType in CategoryInfo correctly' {
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory -TargetType 'System.String'
            $result.CategoryInfo.TargetType | Should -Be 'System.String'
        }

        It 'Sets Reason in CategoryInfo correctly' {
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory -Reason 'TestReason'
            $result.CategoryInfo.Reason | Should -Be 'TestReason'
        }

        It 'Sets RecommendedAction in ErrorDetails correctly' {
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory -RecommendedAction 'Resolve the issue'
            $result.ErrorDetails.RecommendedAction | Should -Be 'Resolve the issue'
        }

        It 'Populates ErrorDetails.Message from the Exception message when RecommendedAction is provided' {
            $result = New-ADTErrorRecord -Exception $TestException -Category $TestCategory -RecommendedAction 'Resolve the issue'
            $result.ErrorDetails.Message | Should -Be $TestException.Message
        }
    }

    Context 'Exception Types' {
        It 'Works with IOException' {
            $ex = [System.IO.IOException]::new('IO error')
            $result = New-ADTErrorRecord -Exception $ex -Category ([System.Management.Automation.ErrorCategory]::ReadError)
            $result.Exception | Should -Be $ex
        }

        It 'Works with ArgumentException' {
            $ex = [System.ArgumentException]::new('Bad argument')
            $result = New-ADTErrorRecord -Exception $ex -Category ([System.Management.Automation.ErrorCategory]::InvalidArgument)
            $result.Exception.Message | Should -Be 'Bad argument'
        }

        It 'Works with UnauthorizedAccessException' {
            $ex = [System.UnauthorizedAccessException]::new('Access denied')
            $result = New-ADTErrorRecord -Exception $ex -Category ([System.Management.Automation.ErrorCategory]::PermissionDenied)
            $result.Exception.Message | Should -Be 'Access denied'
        }

        It 'Works with InvalidOperationException' {
            $ex = [System.InvalidOperationException]::new('Invalid operation')
            $result = New-ADTErrorRecord -Exception $ex -Category ([System.Management.Automation.ErrorCategory]::InvalidOperation)
            $result.Exception | Should -Be $ex
        }
    }

    Context 'All Error Categories' {
        It 'Accepts ErrorCategory: <_>' -ForEach ([System.Management.Automation.ErrorCategory].GetEnumNames()) {
            { New-ADTErrorRecord -Exception $TestException -Category $_ } | Should -Not -Throw
        }
    }

    Context 'Input Validation' {
        It 'Throws when Exception is null' {
            { New-ADTErrorRecord -Exception $null -Category $TestCategory } | Should -Throw
        }

        It 'Throws when Category is an invalid string value' {
            { New-ADTErrorRecord -Exception $TestException -Category 'NonExistentCategory' } | Should -Throw
        }

        It 'Throws when ErrorId is an empty string' {
            { New-ADTErrorRecord -Exception $TestException -Category $TestCategory -ErrorId '' } | Should -Throw
        }

        It 'Throws when ErrorId is whitespace (ValidateNotNullOrWhiteSpace rejects whitespace)' {
            { New-ADTErrorRecord -Exception $TestException -Category $TestCategory -ErrorId ' ' } | Should -Throw
        }

        It 'Throws when Activity is an empty string' {
            { New-ADTErrorRecord -Exception $TestException -Category $TestCategory -Activity '' } | Should -Throw
        }

        It 'Throws when TargetName is an empty string' {
            { New-ADTErrorRecord -Exception $TestException -Category $TestCategory -TargetName '' } | Should -Throw
        }

        It 'Throws when TargetType is an empty string' {
            { New-ADTErrorRecord -Exception $TestException -Category $TestCategory -TargetType '' } | Should -Throw
        }

        It 'Throws when Reason is an empty string' {
            { New-ADTErrorRecord -Exception $TestException -Category $TestCategory -Reason '' } | Should -Throw
        }
    }

    Context 'Composite Tests' {
        It 'Accepts all optional parameters together and returns correct values' {
            $result = New-ADTErrorRecord `
                -Exception $TestException `
                -Category ([System.Management.Automation.ErrorCategory]::InvalidOperation) `
                -ErrorId 'MyError' `
                -TargetObject 'SomeObject' `
                -TargetName 'MyTarget' `
                -TargetType 'System.String' `
                -Activity 'MyActivity' `
                -Reason 'MyReason' `
                -RecommendedAction 'Do something'

            $result | Should -BeOfType [System.Management.Automation.ErrorRecord]
            $result.FullyQualifiedErrorId | Should -Match 'MyError'
            $result.CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidOperation)
            $result.TargetObject | Should -Be 'SomeObject'
            $result.CategoryInfo.TargetName | Should -Be 'MyTarget'
            $result.CategoryInfo.TargetType | Should -Be 'System.String'
            $result.CategoryInfo.Activity | Should -Be 'MyActivity'
            $result.CategoryInfo.Reason | Should -Be 'MyReason'
            $result.ErrorDetails.RecommendedAction | Should -Be 'Do something'
        }
    }
}

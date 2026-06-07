BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'New-ADTErrorRecord' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Returns an [ErrorRecord] with the supplied Exception, Category, and ErrorId' {
            $ex = [System.Exception]::new('Test error')
            $record = New-ADTErrorRecord -Exception $ex -Category NotSpecified -ErrorId 'TestErrorId'
            $record | Should -BeOfType ([System.Management.Automation.ErrorRecord])
            $record.Exception | Should -Be $ex
            $record.FullyQualifiedErrorId | Should -Be 'TestErrorId'
            $record.CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::NotSpecified)
        }
        It 'Uses NotSpecified as the default ErrorId when omitted' {
            $ex = [System.Exception]::new('Test error')
            $record = New-ADTErrorRecord -Exception $ex -Category InvalidArgument
            $record.FullyQualifiedErrorId | Should -Be 'NotSpecified'
        }
        It 'Attaches the TargetObject to the ErrorRecord' {
            $target = [PSCustomObject]@{ Key = 'Value' }
            $ex = [System.Exception]::new('Test error')
            $record = New-ADTErrorRecord -Exception $ex -Category InvalidData -TargetObject $target
            $record.TargetObject | Should -Be $target
        }
        It 'Populates optional CategoryInfo fields when supplied' {
            $ex = [System.Exception]::new('Test error')
            $record = New-ADTErrorRecord -Exception $ex -Category InvalidOperation -ErrorId 'MyId' `
                -TargetName 'MyTarget' -TargetType 'MyType' -Activity 'MyActivity' -Reason 'MyReason'
            $record.CategoryInfo.TargetName | Should -Be 'MyTarget'
            $record.CategoryInfo.TargetType | Should -Be 'MyType'
            $record.CategoryInfo.Activity   | Should -Be 'MyActivity'
            $record.CategoryInfo.Reason     | Should -Be 'MyReason'
        }
        It 'Sets ErrorDetails.RecommendedAction when RecommendedAction is supplied' {
            $ex = [System.Exception]::new('Test error')
            $record = New-ADTErrorRecord -Exception $ex -Category NotSpecified -RecommendedAction 'Try again'
            $record.ErrorDetails.RecommendedAction | Should -Be 'Try again'
        }
        It 'Accepts null as a TargetObject (AllowNull)' {
            $ex = [System.Exception]::new('Test error')
            $record = New-ADTErrorRecord -Exception $ex -Category NotSpecified -TargetObject $null
            $record.TargetObject | Should -BeNull
        }
    }

    Context 'Input Validation' {
        It 'Throws ParameterBindingException when Exception is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,New-ADTErrorRecord'
            }
            { New-ADTErrorRecord -Exception $null -Category NotSpecified } | Should @shouldParams
        }
        It 'Throws ParameterBindingException when Category is an invalid value' {
            $ex = [System.Exception]::new('Test error')
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentTransformationError,New-ADTErrorRecord'
            }
            { New-ADTErrorRecord -Exception $ex -Category 'NotACategory' } | Should @shouldParams
        }
        It 'Throws ParameterArgumentValidationError when ErrorId is empty or whitespace: <Label>' -ForEach @(
            @{ Label = 'empty';      Value = '' }
            @{ Label = 'whitespace'; Value = " `f`n`r`t`v" }
        ) {
            $ex = [System.Exception]::new('Test error')
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,New-ADTErrorRecord'
            }
            { New-ADTErrorRecord -Exception $ex -Category NotSpecified -ErrorId $Value } | Should @shouldParams
        }
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'New-ADTValidateScriptErrorRecord' {
    Context 'Functionality' {
        It 'Builds an ArgumentException naming the parameter' {
            $record = New-ADTValidateScriptErrorRecord -ParameterName 'Thing' -ProvidedValue 'bad' -ExceptionMessage 'The specified input is wrong.'

            $record | Should -BeOfType ([System.Management.Automation.ErrorRecord])
            $record.Exception | Should -BeOfType ([System.ArgumentException])
            $record.Exception.ParamName | Should -BeExactly 'Thing'
            $record.Exception.Message | Should -BeLike 'The specified input is wrong.*'
        }

        It 'Composes the error id from the parameter name' {
            (New-ADTValidateScriptErrorRecord -ParameterName 'Thing' -ProvidedValue 'bad' -ExceptionMessage 'x').FullyQualifiedErrorId | Should -BeExactly 'InvalidThingParameterValue'
        }

        It 'Categorises the failure as an invalid argument' {
            (New-ADTValidateScriptErrorRecord -ParameterName 'Thing' -ProvidedValue 'bad' -ExceptionMessage 'x').CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidArgument)
        }

        It 'Describes the value that was rejected' {
            $record = New-ADTValidateScriptErrorRecord -ParameterName 'Thing' -ProvidedValue 42 -ExceptionMessage 'x'
            $record.TargetObject | Should -Be 42
            $record.CategoryInfo.TargetName | Should -BeExactly '42'
            $record.CategoryInfo.TargetType | Should -BeExactly 'Int32'
        }

        It 'Points the caller at the parameter to review' {
            (New-ADTValidateScriptErrorRecord -ParameterName 'Thing' -ProvidedValue 'bad' -ExceptionMessage 'x').ErrorDetails.RecommendedAction | Should -BeExactly 'Review the supplied Thing parameter value and try again.'
        }

        It 'Keeps an inner exception when one is supplied' {
            $inner = [System.FormatException]::new('inner detail')
            (New-ADTValidateScriptErrorRecord -ParameterName 'Thing' -ProvidedValue 'bad' -ExceptionMessage 'x' -InnerException $inner).Exception.InnerException | Should -Be $inner
        }

        It 'Accepts a <Case> as the rejected value' -ForEach @(
            @{ Case = 'null'; Value = $null }
            @{ Case = 'an empty string'; Value = '' }
            @{ Case = 'a white space string'; Value = '   ' }
        ) {
            # These are exactly the values a ValidateScript block reports on, so the function has to survive
            # them. New-ADTErrorRecord accepts none of them for TargetName or TargetType, which is why they
            # are left off rather than passed through.
            $record = New-ADTValidateScriptErrorRecord -ParameterName 'Thing' -ProvidedValue $Value -ExceptionMessage 'The specified input is null or empty.'
            $record.FullyQualifiedErrorId | Should -BeExactly 'InvalidThingParameterValue'
            $record.Exception.Message | Should -BeLike 'The specified input is null or empty.*'
        }

        It 'Still carries the rejected value even when it cannot be described' {
            # TargetName and TargetType are dropped for an empty value, so TargetObject is the only place
            # left holding what was actually supplied.
            (New-ADTValidateScriptErrorRecord -ParameterName 'Thing' -ProvidedValue '   ' -ExceptionMessage 'x').TargetObject | Should -BeExactly '   '
        }

        It 'Reports its own message when a validator rejects an empty value' {
            # The caller's side of the same bug: this used to surface a failure against TargetName, an
            # internal parameter of New-ADTErrorRecord that the caller has no knowledge of.
            { Initialize-ADTModule -ScriptDirectory '' } | Should -Throw -ErrorId 'InvalidScriptDirectoryParameterValue,Initialize-ADTModule'
        }
    }
}

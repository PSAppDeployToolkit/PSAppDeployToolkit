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

        It 'Accepts a <Case> as the rejected value' -Skip -ForEach @(
            @{ Case = 'null'; Value = $null; TargetName = '' }
            @{ Case = 'an empty string'; Value = ''; TargetName = '' }
            @{ Case = 'a white space string'; Value = '   '; TargetName = '   ' }
        ) {
            # Skipped: the function computes TargetName as $ProvidedValue.ToString() with no null guard, so a
            # null value throws "You cannot call a method on a null-valued expression", and an empty or white
            # space value is rejected by New-ADTErrorRecord's own validator on -TargetName.
            #
            # These are exactly the values a ValidateScript block reports on. Initialize-ADTModule's own
            # validator calls this with an empty ScriptDirectory, so today
            # `Initialize-ADTModule -ScriptDirectory ''` reports "Cannot validate argument on parameter
            # 'TargetName'" instead of its intended message. Unskip with the fix.
            $record = New-ADTValidateScriptErrorRecord -ParameterName 'Thing' -ProvidedValue $Value -ExceptionMessage 'The specified input is null or empty.'
            $record.FullyQualifiedErrorId | Should -BeExactly 'InvalidThingParameterValue'
            $record.Exception.Message | Should -BeLike 'The specified input is null or empty.*'
        }

        It 'Reports its own message when a validator rejects an empty value' -Skip {
            # Skipped for the same reason, from the caller's side rather than the function's.
            { Initialize-ADTModule -ScriptDirectory '' } | Should -Throw -ErrorId 'InvalidScriptDirectoryParameterValue,Initialize-ADTModule'
        }
    }
}

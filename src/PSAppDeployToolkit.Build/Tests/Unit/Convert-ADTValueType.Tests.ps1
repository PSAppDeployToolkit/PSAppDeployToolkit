BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Convert-ADTValueType' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Wraps an out-of-range value into the target type instead of throwing (256 -> SByte = 0)' {
            Convert-ADTValueType -Value 256 -To SByte | Should -Be 0
        }
        It 'Converts <Value> to <To> yielding <Expected>' -ForEach @(
            @{ Value = 7;    To = 'Int32'; Expected = 7 }
            @{ Value = 255;  To = 'Byte';  Expected = 255 }
            @{ Value = 256;  To = 'Byte';  Expected = 0 }
            @{ Value = -1;   To = 'Byte';  Expected = 255 }
        ) {
            Convert-ADTValueType -Value $Value -To $To | Should -Be $Expected
        }
        It 'Returns a System.ValueType' {
            Convert-ADTValueType -Value 1 -To Int32 | Should -BeOfType ([System.ValueType])
        }
        It 'Accepts -Value from the pipeline' {
            256 | Convert-ADTValueType -To Byte | Should -Be 0
        }
    }

    Context 'Input Validation' {
        It 'Should verify that Value is not null' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Convert-ADTValueType'
            }
            { Convert-ADTValueType -Value $null -To Int32 } | Should @shouldParams
        }
        It 'Should reject an invalid -To value' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentTransformationError,Convert-ADTValueType'
            }
            { Convert-ADTValueType -Value 1 -To 'NotAType' } | Should @shouldParams
        }
    }
}

BeforeDiscovery {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Driven from the enumeration so a member added later is covered without this file being touched. The
    # converter method each member names is the oracle for the type it has to produce.
    $script:ValueTypes = foreach ($name in [System.Enum]::GetNames([PSADT.Utilities.ValueTypeConverter+ValueTypes]))
    {
        @{ To = $name; TypeName = [PSADT.Utilities.ValueTypeConverter].GetMethod("To$name").ReturnType.FullName }
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Convert-ADTValueType' {
    Context 'Functionality' {
        It 'Finds value types to check' -ForEach @{ Found = $script:ValueTypes.Count } {
            # A variable set during discovery is not visible at run time, so the count comes in as data.
            $Found | Should -BeGreaterThan 0
        }

        It 'Returns a [<TypeName>] when converting to <To>' -ForEach $script:ValueTypes {
            Convert-ADTValueType -Value 1 -To $To | Should -BeOfType ([System.Type]$TypeName)
        }

        It 'Truncates <Value> to <Expected> when converting to <To>' -ForEach @(
            # The point of the function: the conversion wraps rather than throwing the way a PowerShell cast
            # would. Each expected value is the two's complement truncation of the input.
            @{ To = 'SByte'; Value = 127; Expected = 127 }
            @{ To = 'SByte'; Value = 128; Expected = -128 }
            @{ To = 'SByte'; Value = 256; Expected = 0 }
            @{ To = 'SByte'; Value = -129; Expected = 127 }
            @{ To = 'Byte'; Value = 255; Expected = 255 }
            @{ To = 'Byte'; Value = 256; Expected = 0 }
            @{ To = 'Byte'; Value = -1; Expected = 255 }
            @{ To = 'Int16'; Value = 32767; Expected = 32767 }
            @{ To = 'Int16'; Value = 32768; Expected = -32768 }
            @{ To = 'UInt16'; Value = 65536; Expected = 0 }
            @{ To = 'UInt16'; Value = -1; Expected = 65535 }
            @{ To = 'Int32'; Value = 2147483648; Expected = -2147483648 }
            @{ To = 'UInt32'; Value = 4294967296; Expected = 0 }
            @{ To = 'UInt32'; Value = -1; Expected = 4294967295 }
            @{ To = 'UInt64'; Value = -1; Expected = 18446744073709551615 }
        ) {
            Convert-ADTValueType -Value $Value -To $To | Should -Be $Expected
        }

        It 'Casts where PowerShell would throw' {
            # The same conversion written as a cast, to show the function is doing something a cast cannot.
            { [System.SByte]256 } | Should -Throw
            Convert-ADTValueType -Value 256 -To SByte | Should -Be 0
        }

        It 'Treats the aliased members as the same type' -ForEach @(
            @{ First = 'Short'; Second = 'Int16' }
            @{ First = 'UShort'; Second = 'UInt16' }
            @{ First = 'Int'; Second = 'Int32' }
            @{ First = 'UInt'; Second = 'UInt32' }
            @{ First = 'ULong'; Second = 'UInt64' }
        ) {
            $firstResult = Convert-ADTValueType -Value 300 -To $First
            $secondResult = Convert-ADTValueType -Value 300 -To $Second
            $firstResult.GetType() | Should -Be $secondResult.GetType()
            $firstResult | Should -Be $secondResult
        }

        It 'Accepts pipeline input' {
            1, 2, 3 | Convert-ADTValueType -To Byte | Should -Be @(1, 2, 3)
        }
    }

    Context 'Input Validation' {
        It 'Should reject a value type it does not know' {
            { Convert-ADTValueType -Value 1 -To 'NotAValueType' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentTransformationError,Convert-ADTValueType'
        }
    }
}

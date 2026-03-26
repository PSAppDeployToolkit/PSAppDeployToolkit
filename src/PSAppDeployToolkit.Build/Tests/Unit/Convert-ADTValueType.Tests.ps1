BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Convert-ADTValueType' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry {}
    }

    # All conversions use unchecked casting — values that exceed the target range wrap around
    # rather than throwing. This is the key behavioural difference from PowerShell's own casts.

    Context 'SByte Conversion' {
        It 'Converts a value within SByte range correctly' {
            Convert-ADTValueType -Value 127 -To SByte | Should -Be 127
        }

        It 'Wraps 256 to 0 when converting to SByte' {
            # 256 = 0x100 → truncated to 0x00 = 0 as SByte
            Convert-ADTValueType -Value 256 -To SByte | Should -Be 0
        }

        It 'Wraps 128 to -128 when converting to SByte' {
            # 0x80 = -128 as signed byte
            Convert-ADTValueType -Value 128 -To SByte | Should -Be -128
        }
    }

    Context 'Byte Conversion' {
        It 'Converts a value within Byte range correctly' {
            Convert-ADTValueType -Value 255 -To Byte | Should -Be 255
        }

        It 'Wraps 256 to 0 when converting to Byte' {
            Convert-ADTValueType -Value 256 -To Byte | Should -Be 0
        }

        It 'Wraps -1 to 255 when converting to Byte' {
            # -1 = 0xFFFFFFFFFFFFFFFF → lowest byte = 0xFF = 255
            Convert-ADTValueType -Value -1 -To Byte | Should -Be 255
        }
    }

    Context 'Short / Int16 Conversion' {
        It 'Converts 32767 to Short correctly (max Int16)' {
            Convert-ADTValueType -Value 32767 -To Short | Should -Be 32767
        }

        It 'Wraps 32768 to -32768 when converting to Short' {
            Convert-ADTValueType -Value 32768 -To Short | Should -Be -32768
        }

        It 'Converts using the Int16 alias identically to Short' {
            Convert-ADTValueType -Value 100 -To Int16 | Should -Be 100
        }
    }

    Context 'UShort / UInt16 Conversion' {
        It 'Converts 65535 to UShort correctly (max UInt16)' {
            Convert-ADTValueType -Value 65535 -To UShort | Should -Be 65535
        }

        It 'Wraps 65536 to 0 when converting to UShort' {
            Convert-ADTValueType -Value 65536 -To UShort | Should -Be 0
        }

        It 'Converts using the UInt16 alias identically to UShort' {
            Convert-ADTValueType -Value 200 -To UInt16 | Should -Be 200
        }
    }

    Context 'Int / Int32 Conversion' {
        It 'Converts a value within Int32 range correctly' {
            Convert-ADTValueType -Value 42 -To Int | Should -Be 42
        }

        It 'Converts using the Int32 alias identically to Int' {
            Convert-ADTValueType -Value 42 -To Int32 | Should -Be 42
        }
    }

    Context 'ULong / UInt64 Conversion' {
        It 'Converts -1 to UInt64 max value when converting to ULong' {
            # unchecked cast of -1 to ulong = 0xFFFFFFFFFFFFFFFF = [uint64]::MaxValue
            Convert-ADTValueType -Value -1 -To ULong | Should -Be ([System.UInt64]::MaxValue)
        }

        It 'Converts using the UInt64 alias identically to ULong' {
            Convert-ADTValueType -Value 100 -To UInt64 | Should -Be 100
        }
    }

    Context 'Return Value Types' {
        It 'Returns System.SByte for -To SByte' {
            Convert-ADTValueType -Value 1 -To SByte | Should -BeOfType [System.SByte]
        }

        It 'Returns System.Byte for -To Byte' {
            Convert-ADTValueType -Value 1 -To Byte | Should -BeOfType [System.Byte]
        }

        It 'Returns System.Int16 for -To Short' {
            Convert-ADTValueType -Value 1 -To Short | Should -BeOfType [System.Int16]
        }

        It 'Returns System.Int32 for -To Int' {
            Convert-ADTValueType -Value 1 -To Int | Should -BeOfType [System.Int32]
        }

    }

    Context 'Pipeline Input' {
        It 'Accepts Value from the pipeline' {
            $result = 42 | Convert-ADTValueType -To Int
            $result | Should -Be 42
        }
    }

    Context 'Input Validation' {
        It 'Throws when Value is null' {
            { Convert-ADTValueType -Value $null -To Int } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

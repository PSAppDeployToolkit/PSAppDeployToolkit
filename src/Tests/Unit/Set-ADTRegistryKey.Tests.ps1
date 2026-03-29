BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Set-ADTRegistryKey' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestRegistry', Justification = 'This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.')]
        $TestRegistry = (New-Item -Path 'TestRegistry:\TestLocation' -ItemType Directory).PSPath

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Should create a new registry key if the LiteralPath provided does not exist' {
            Set-ADTRegistryKey -LiteralPath "$TestRegistry\New"
            Test-Path -LiteralPath "$TestRegistry\New" -PathType Container | Should -BeTrue
            Remove-Item -LiteralPath "$TestRegistry\New"

            Set-ADTRegistryKey -LiteralPath "$TestRegistry\New" -Name 'Property' -Value 'value'
            Test-Path -LiteralPath "$TestRegistry\New" -PathType Container | Should -BeTrue
            $regKey = Get-ItemProperty -LiteralPath "$TestRegistry\New"
            $regKey.Property | Should -Be 'value'
        }
        It 'Should create a volatile registry key' {
            Set-ADTRegistryKey -LiteralPath "$TestRegistry\Volatile" -RegistryOptions Volatile

            {
                $key = Get-Item -LiteralPath $TestRegistry
                try
                {
                    $volatileKey = $key.OpenSubKey('Volatile', $true)
                    try
                    {
                        # Attempting to create a nonvolatile registry key under a volatile registry key will throw an error.
                        ## We use this behavior to validate that the registry key created by Set-ADTRegistryKey is volatile.
                        $volatileKey.CreateSubKey('NonVolatile', $true, [Microsoft.Win32.RegistryOptions]::None)
                    }
                    finally
                    {
                        $volatileKey.Dispose()
                    }
                }
                finally
                {
                    $key.Dispose()
                }
            } | Should -Throw -ExceptionType ([System.Management.Automation.MethodInvocationException]) -ErrorId 'IOException'
        }
        It 'Should create a Binary registry property' {
            $binaryData = [System.Byte[]]@(0, 1, 2, 3)
            Set-ADTRegistryKey -LiteralPath $TestRegistry -Name Binary -Value $binaryData -Type Binary

            $key = Get-Item -LiteralPath $TestRegistry
            try
            {
                $key.GetValueNames() | Should -Contain 'Binary'
                $key.GetValueKind('Binary') | Should -Be ([Microsoft.Win32.RegistryValueKind]::Binary)

                $value = $key.GetValue('Binary')
                $value -is [System.Byte[]] | Should -BeTrue
                $value | Should -HaveCount $binaryData.Count
                for ($i = 0; $i -lt $binaryData.Count; $i ++)
                {
                    $value[$i] | Should -Be $binaryData[$i]
                }
            }
            finally
            {
                $key.Dispose()
            }
        }
        It 'Should create a DWord registry property' {
            $testData = 7
            Set-ADTRegistryKey -LiteralPath $TestRegistry -Name DWord -Value $testData -Type DWord

            $key = Get-Item -LiteralPath $TestRegistry
            try
            {
                $key.GetValueNames() | Should -Contain 'DWord'
                $key.GetValueKind('DWord') | Should -Be ([Microsoft.Win32.RegistryValueKind]::DWord)

                $value = $key.GetValue('DWord')
                $value -is [System.Int32] | Should -BeTrue
                $value | Should -Be $testData
            }
            finally
            {
                $key.Dispose()
            }
        }
        It 'Should create a ExpandString registry property' {
            Set-ADTRegistryKey -LiteralPath $TestRegistry -Name ExpandString -Value '%WinDir%\System32\cmd.exe' -Type ExpandString

            $key = Get-Item -LiteralPath $TestRegistry
            try
            {
                $key.GetValueNames() | Should -Contain 'ExpandString'
                $key.GetValueKind('ExpandString') | Should -Be ([Microsoft.Win32.RegistryValueKind]::ExpandString)

                $expandedValue = $key.GetValue('ExpandString')
                $expandedValue -is [System.String] | Should -BeTrue
                $expandedValue | Should -Be "$env:WinDir\System32\cmd.exe"

                $value = $key.GetValue('ExpandString', $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                $value -is [System.String] | Should -BeTrue
                $value | Should -Be '%WinDir%\System32\cmd.exe'
            }
            finally
            {
                $key.Dispose()
            }
        }
        It 'Should create a MultiString registry property' {
            $testData = @('Value1', 'Value2')
            Set-ADTRegistryKey -LiteralPath $TestRegistry -Name MultiString -Value $testData -Type MultiString

            $key = Get-Item -LiteralPath $TestRegistry
            try
            {
                $key.GetValueNames() | Should -Contain 'MultiString'
                $key.GetValueKind('MultiString') | Should -Be ([Microsoft.Win32.RegistryValueKind]::MultiString)

                $value = $key.GetValue('MultiString')
                $value -is [System.String[]] | Should -BeTrue
                $value | Should -HaveCount $testData.Count
                for ($i = 0; $i -lt $testData.Count; $i++)
                {
                    $value[$i] | Should -BeExactly $testData[$i]
                }
            }
            finally
            {
                $key.Dispose()
            }
        }
        It 'Should append to a MultiString registry property value' {
            $testData = @('Value1', 'Value2')
            Set-ADTRegistryKey -LiteralPath $TestRegistry -Name MultiString -Value $testData -Type MultiString
            Set-ADTRegistryKey -LiteralPath $TestRegistry -Name MultiString -Value 'Value3' -Type MultiString -MultiStringValueMode Add
            $testData += 'Value3'

            $key = Get-Item -LiteralPath $TestRegistry
            try
            {
                $key.GetValueNames() | Should -Contain 'MultiString'
                $key.GetValueKind('MultiString') | Should -Be ([Microsoft.Win32.RegistryValueKind]::MultiString)

                $value = $key.GetValue('MultiString')
                $value -is [System.String[]] | Should -BeTrue
                $value | Should -HaveCount $testData.Count
                for ($i = 0; $i -lt $testData.Count; $i++)
                {
                    $value[$i] | Should -BeExactly $testData[$i]
                }
            }
            finally
            {
                $key.Dispose()
            }
        }
        It 'Should remove a MultiString registry property value' {
            $testData = @('Value1', 'Value2', 'Value3')
            Set-ADTRegistryKey -LiteralPath $TestRegistry -Name MultiString -Value $testData -Type MultiString
            Set-ADTRegistryKey -LiteralPath $TestRegistry -Name MultiString -Value 'Value2' -Type MultiString -MultiStringValueMode Remove
            $testData = @('Value1', 'Value3')

            $key = Get-Item -LiteralPath $TestRegistry
            try
            {
                $key.GetValueNames() | Should -Contain 'MultiString'
                $key.GetValueKind('MultiString') | Should -Be ([Microsoft.Win32.RegistryValueKind]::MultiString)

                $value = $key.GetValue('MultiString')
                $value -is [System.String[]] | Should -BeTrue
                $value | Should -HaveCount $testData.Count
                for ($i = 0; $i -lt $testData.Count; $i++)
                {
                    $value[$i] | Should -BeExactly $testData[$i]
                }
            }
            finally
            {
                $key.Dispose()
            }
        }
        It 'Should replace a MultiString registry property' {
            Set-ADTRegistryKey -LiteralPath $TestRegistry -Name MultiString -Value 'Value4' -Type MultiString -MultiStringValueMode Replace

            $key = Get-Item -LiteralPath $TestRegistry
            try
            {
                $key.GetValueNames() | Should -Contain 'MultiString'
                $key.GetValueKind('MultiString') | Should -Be ([Microsoft.Win32.RegistryValueKind]::MultiString)

                $value = $key.GetValue('MultiString')
                $value -is [System.String[]] | Should -BeTrue
                $value | Should -HaveCount 1
                $value[0] | Should -BeExactly 'Value4'
            }
            finally
            {
                $key.Dispose()
            }
        }
        It 'Should create a QWord registry property' {
            $testData = ([System.Int64]([System.Int32]::MaxValue + 1))
            Set-ADTRegistryKey -LiteralPath $TestRegistry -Name QWord -Value $testData -Type QWord

            $key = Get-Item -LiteralPath $TestRegistry
            try
            {
                $key.GetValueNames() | Should -Contain 'QWord'
                $key.GetValueKind('QWord') | Should -Be ([Microsoft.Win32.RegistryValueKind]::QWord)

                $value = $key.GetValue('QWord')
                $value -is [System.Int64] | Should -BeTrue
                $value | Should -Be $testData
            }
            finally
            {
                $key.Dispose()
            }
        }
        It 'Should create a String registry property' {
            $testData = 'test'
            Set-ADTRegistryKey -LiteralPath $TestRegistry -Name String -Value $testData -Type String

            $key = Get-Item -LiteralPath $TestRegistry
            try
            {
                $key.GetValueNames() | Should -Contain 'String'
                $key.GetValueKind('String') | Should -Be ([Microsoft.Win32.RegistryValueKind]::String)

                $value = $key.GetValue('String')
                $value -is [System.String] | Should -BeTrue
                $value | Should -BeExactly $testData
            }
            finally
            {
                $key.Dispose()
            }
        }
    }

    Context 'Input Validation' {
        It 'Should verify that LiteralPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTRegistryKey'
            }
            { Set-ADTRegistryKey -LiteralPath $null } | Should @shouldParams
            { Set-ADTRegistryKey -LiteralPath '' } | Should @shouldParams
            { Set-ADTRegistryKey -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Set-ADTRegistryKey'
            }
            { Set-ADTRegistryKey -Path $TestRegistry -Name $null } | Should @shouldParams
            { Set-ADTRegistryKey -Path $TestRegistry -Name '' } | Should @shouldParams
            { Set-ADTRegistryKey -Path $TestRegistry -Name " `f`n`r`t`v" } | Should @shouldParams
        }
        It 'Should verify that SID is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Set-ADTRegistryKey -LiteralPath $TestRegistry -SID $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Set-ADTRegistryKey'
            { Set-ADTRegistryKey -LiteralPath $TestRegistry -SID '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Set-ADTRegistryKey'
            { Set-ADTRegistryKey -LiteralPath $TestRegistry -SID " `f`n`r`t`v" } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Set-ADTRegistryKey'
        }
        It 'Should verify that MultiStringValueMode is one of: Replace, Add, Remove' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Set-ADTRegistryKey -LiteralPath $TestRegistry -MultiStringValueMode $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Set-ADTRegistryKey'
            { Set-ADTRegistryKey -LiteralPath $TestRegistry -MultiStringValueMode '' } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Set-ADTRegistryKey'
            { Set-ADTRegistryKey -LiteralPath $TestRegistry -MultiStringValueMode " `f`n`r`t`v" } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Set-ADTRegistryKey'
            { Set-ADTRegistryKey -LiteralPath $TestRegistry -MultiStringValueMode 'test' } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Set-ADTRegistryKey'

            { Set-ADTRegistryKey -LiteralPath $TestRegistry -MultiStringValueMode 'Replace' } | Should -Not -Throw
            { Set-ADTRegistryKey -LiteralPath $TestRegistry -MultiStringValueMode 'Add' } | Should -Not -Throw
            { Set-ADTRegistryKey -LiteralPath $TestRegistry -MultiStringValueMode 'Remove' } | Should -Not -Throw
        }
    }
}

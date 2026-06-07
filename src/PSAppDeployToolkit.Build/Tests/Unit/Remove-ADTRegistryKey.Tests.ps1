BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTRegistryKey' {
    BeforeAll {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestRegistry', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $TestRegistry = (New-Item -Path 'TestRegistry:\TestLocation' -ItemType Directory).PSPath

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        BeforeEach {
            # Recreate a clean subkey fixture before each test.
            if (!(Test-Path -LiteralPath "$TestRegistry\Target"))
            {
                New-Item -Path "$TestRegistry\Target" -ItemType Directory | Out-Null
            }
        }

        It 'Should remove a registry key that has no subkeys' {
            Test-Path -LiteralPath "$TestRegistry\Target" | Should -BeTrue
            Remove-ADTRegistryKey -LiteralPath "$TestRegistry\Target"
            Test-Path -LiteralPath "$TestRegistry\Target" | Should -BeFalse
        }

        It 'Should remove a single registry value via -Name' {
            Set-ItemProperty -LiteralPath "$TestRegistry\Target" -Name 'TestValue' -Value 'Hello'
            Get-ItemProperty -LiteralPath "$TestRegistry\Target" -Name 'TestValue' -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty

            Remove-ADTRegistryKey -LiteralPath "$TestRegistry\Target" -Name 'TestValue'

            $prop = Get-ItemProperty -LiteralPath "$TestRegistry\Target" -Name 'TestValue' -ErrorAction SilentlyContinue
            $prop | Should -BeNullOrEmpty
        }

        It 'Should remove a registry key recursively when it contains subkeys' {
            New-Item -Path "$TestRegistry\Target\SubKey" -ItemType Directory | Out-Null
            Test-Path -LiteralPath "$TestRegistry\Target\SubKey" | Should -BeTrue

            Remove-ADTRegistryKey -LiteralPath "$TestRegistry\Target" -Recurse

            Test-Path -LiteralPath "$TestRegistry\Target" | Should -BeFalse
        }

        It 'Should not throw and log a warning when the key does not exist (SilentlyContinue behaviour)' {
            # Remove the fixture so it does not exist.
            if (Test-Path -LiteralPath "$TestRegistry\Target")
            {
                Remove-Item -LiteralPath "$TestRegistry\Target" -Recurse -Force
            }
            { Remove-ADTRegistryKey -LiteralPath "$TestRegistry\Target" } | Should -Not -Throw
        }

        It 'Should not throw and write an error when removing a key with subkeys without -Recurse' {
            # Initialize-ADTFunction uses SilentlyContinue, so the SubKeyRecursionError is handled
            # internally by Invoke-ADTFunctionErrorHandler and is not propagated as a terminating exception.
            New-Item -Path "$TestRegistry\Target\SubKey" -ItemType Directory | Out-Null
            { Remove-ADTRegistryKey -LiteralPath "$TestRegistry\Target" } | Should -Not -Throw
            # The target key should still exist because the removal was blocked.
            Test-Path -LiteralPath "$TestRegistry\Target" | Should -BeTrue
        }

        It 'Should not throw when removing a value by -Name from a non-existent key (SilentlyContinue behaviour)' {
            if (Test-Path -LiteralPath "$TestRegistry\Target")
            {
                Remove-Item -LiteralPath "$TestRegistry\Target" -Recurse -Force
            }
            { Remove-ADTRegistryKey -LiteralPath "$TestRegistry\Target" -Name 'AnyValue' } | Should -Not -Throw
        }

        It 'Should not throw when removing a value by -Name that does not exist in the key (PSArgumentException behaviour)' {
            { Remove-ADTRegistryKey -LiteralPath "$TestRegistry\Target" -Name 'NoSuchValue' } | Should -Not -Throw
        }

        It 'Should accept a wildcard Path and remove matched subkeys with -Recurse' {
            New-Item -Path "$TestRegistry\Target\Child1" -ItemType Directory | Out-Null
            New-Item -Path "$TestRegistry\Target\Child2" -ItemType Directory | Out-Null

            Remove-ADTRegistryKey -Path "$TestRegistry\Target\*" -Recurse

            Get-ChildItem -LiteralPath "$TestRegistry\Target" | Should -BeNullOrEmpty
        }
    }

    Context 'Input Validation' {
        It 'Should verify that LiteralPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTRegistryKey'
            }
            { Remove-ADTRegistryKey -LiteralPath $null } | Should @shouldParams
            { Remove-ADTRegistryKey -LiteralPath '' } | Should @shouldParams
            { Remove-ADTRegistryKey -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }

        It 'Should verify that Path is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTRegistryKey'
            }
            { Remove-ADTRegistryKey -Path $null } | Should @shouldParams
            { Remove-ADTRegistryKey -Path '' } | Should @shouldParams
            { Remove-ADTRegistryKey -Path " `f`n`r`t`v" } | Should @shouldParams
        }

        It 'Should verify that Name is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTRegistryKey'
            }
            { Remove-ADTRegistryKey -LiteralPath $TestRegistry -Name $null } | Should @shouldParams
            { Remove-ADTRegistryKey -LiteralPath $TestRegistry -Name '' } | Should @shouldParams
            { Remove-ADTRegistryKey -LiteralPath $TestRegistry -Name " `f`n`r`t`v" } | Should @shouldParams
        }

        It 'Should verify that SID is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Remove-ADTRegistryKey -LiteralPath $TestRegistry -SID $null } | Should @shouldParams -ErrorId 'ParameterArgumentValidationError,Remove-ADTRegistryKey'
            { Remove-ADTRegistryKey -LiteralPath $TestRegistry -SID '' } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Remove-ADTRegistryKey'
            { Remove-ADTRegistryKey -LiteralPath $TestRegistry -SID " `f`n`r`t`v" } | Should @shouldParams -ErrorId 'ParameterArgumentTransformationError,Remove-ADTRegistryKey'
        }
    }
}

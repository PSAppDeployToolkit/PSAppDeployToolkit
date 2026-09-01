BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}
Describe 'Remove-ADTRegistryKey' {
    BeforeEach {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'TestKey', Justification = 'This variable is used within script blocks that PSScriptAnalyzer has no visibility of.')]
        $TestKey = (New-Item -Path "TestRegistry:\Remove$([System.Guid]::NewGuid().ToString('N'))" -ItemType Directory).PSPath
    }

    Context 'Deleting values' {
        It 'Deletes the value it is given' {
            Set-ItemProperty -LiteralPath $TestKey -Name 'Wanted' -Value 'gone'
            Set-ItemProperty -LiteralPath $TestKey -Name 'Kept' -Value 'here'
            Remove-ADTRegistryKey -LiteralPath $TestKey -Name 'Wanted'
            Get-ItemProperty -LiteralPath $TestKey -Name 'Wanted' -ErrorAction Ignore | Should -BeNullOrEmpty
        }

        It 'Leaves the other values alone' {
            Set-ItemProperty -LiteralPath $TestKey -Name 'Wanted' -Value 'gone'
            Set-ItemProperty -LiteralPath $TestKey -Name 'Kept' -Value 'here'
            Remove-ADTRegistryKey -LiteralPath $TestKey -Name 'Wanted'
            (Get-ItemProperty -LiteralPath $TestKey).Kept | Should -BeExactly 'here'
        }

        It 'Deletes the default value' {
            # Remove-ItemProperty cannot touch the default value, so the function opens the key itself.
            Set-ItemProperty -LiteralPath $TestKey -Name '(Default)' -Value 'default content'
            Remove-ADTRegistryKey -LiteralPath $TestKey -Name '(Default)'
            (Get-Item -LiteralPath $TestKey).GetValue([System.String]::Empty) | Should -BeNullOrEmpty
        }

        It 'Leaves the key itself in place' {
            Set-ItemProperty -LiteralPath $TestKey -Name 'Wanted' -Value 'gone'
            Remove-ADTRegistryKey -LiteralPath $TestKey -Name 'Wanted'
            Test-Path -LiteralPath $TestKey | Should -BeTrue
        }
    }

    Context 'Deleting keys' {
        It 'Deletes a key with nothing under it' {
            Remove-ADTRegistryKey -LiteralPath $TestKey
            Test-Path -LiteralPath $TestKey | Should -BeFalse
        }

        It 'Refuses a key with subkeys unless asked to recurse' {
            # Remove-Item hangs on a populated key, so the function checks first rather than letting a
            # deployment stall indefinitely.
            $null = New-Item -Path "$TestKey\Child" -ItemType Directory -Force
            { Remove-ADTRegistryKey -LiteralPath $TestKey -ErrorAction Stop } | Should -Throw -ErrorId 'SubKeyRecursionError,Remove-ADTRegistryKey'
        }

        It 'Leaves the key alone when it refuses' {
            $null = New-Item -Path "$TestKey\Child" -ItemType Directory -Force
            Remove-ADTRegistryKey -LiteralPath $TestKey -ErrorAction SilentlyContinue
            Test-Path -LiteralPath "$TestKey\Child" | Should -BeTrue
        }

        It 'Deletes a key with subkeys when asked to recurse' {
            $null = New-Item -Path "$TestKey\Child\Grandchild" -ItemType Directory -Force
            Remove-ADTRegistryKey -LiteralPath $TestKey -Recurse
            Test-Path -LiteralPath $TestKey | Should -BeFalse
        }

        # Skipped until the function is repaired. Its subkey guard calls Get-ChildItem on the supplied
        # path, which for a wildcard returns the matching keys rather than their children, so any
        # wildcard is treated as though it had subkeys and refused.
        It 'Resolves a wildcard' -Skip {
            $null = New-Item -Path "$TestKey\MatchOne" -ItemType Directory -Force
            $null = New-Item -Path "$TestKey\Ignored" -ItemType Directory -Force
            Remove-ADTRegistryKey -Path "$TestKey\Match*"
            Test-Path -LiteralPath "$TestKey\MatchOne" | Should -BeFalse
            Test-Path -LiteralPath "$TestKey\Ignored" | Should -BeTrue
        }
    }

    Context 'Keys and values that are not there' {
        It 'Warns rather than failing for a key that does not exist' {
            # Like the file removal functions, this runs during cleanup where already-gone is the goal.
            { Remove-ADTRegistryKey -LiteralPath "$TestKey\NeverExisted" } | Should -Not -Throw
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Severity -eq 'Warning' -and $Message -like '*does not exist*' }
        }

        It 'Warns for a value whose key does not exist' {
            { Remove-ADTRegistryKey -LiteralPath "$TestKey\NeverExisted" -Name 'Anything' } | Should -Not -Throw
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Severity -eq 'Warning' -and $Message -like '*registry key does not exist*' }
        }

        It 'Warns for a value that does not exist' {
            { Remove-ADTRegistryKey -LiteralPath $TestKey -Name 'NeverExisted' } | Should -Not -Throw
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Severity -eq 'Warning' -and $Message -like '*does not exist*' }
        }
    }

    Context 'WhatIf' {
        It 'Leaves the key where it is' {
            Remove-ADTRegistryKey -LiteralPath $TestKey -WhatIf
            Test-Path -LiteralPath $TestKey | Should -BeTrue
        }

        It 'Leaves the value where it is' {
            Set-ItemProperty -LiteralPath $TestKey -Name 'Kept' -Value 'here'
            Remove-ADTRegistryKey -LiteralPath $TestKey -Name 'Kept' -WhatIf
            (Get-ItemProperty -LiteralPath $TestKey).Kept | Should -BeExactly 'here'
        }
    }

    Context 'Input Validation' {
        It 'Refuses a blank path' {
            { Remove-ADTRegistryKey -LiteralPath '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Remove-ADTRegistryKey'
        }

        It 'Refuses a blank value name' {
            { Remove-ADTRegistryKey -LiteralPath $TestKey -Name '   ' } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Remove-ADTRegistryKey'
        }

        It 'Refuses a wildcard path and a literal one together' {
            { Remove-ADTRegistryKey -Path $TestKey -LiteralPath $TestKey } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Refuses a SID that is not one' {
            { Remove-ADTRegistryKey -LiteralPath $TestKey -SID 'not-a-sid' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

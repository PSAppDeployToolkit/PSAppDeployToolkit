BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    $script:TestKeyBase = "HKCU:\SOFTWARE\PSADTTest_$([System.Guid]::NewGuid().ToString('N'))"
    $null = New-Item -Path $script:TestKeyBase -Force
}

AfterAll {
    Remove-Item -LiteralPath $script:TestKeyBase -Recurse -Force -ErrorAction SilentlyContinue
}

Describe 'Remove-ADTRegistryKey' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Remove-ADTRegistryKey calls Convert-ADTRegistryPath internally, which references
        # [PSADT.AccountManagement.AccountUtilities]::CallerSid at compile time.
        # PowerShell resolves all type literals at compile time, requiring admin rights.
        $script:IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }

    BeforeEach {
        if (!$script:IsAdmin) { Set-ItResult -Skipped -Because 'Requires admin rights (AccountUtilities static constructor triggered at compile time)'; return }
    }

    Context 'Remove a leaf key (LiteralPath)' {
        BeforeEach {
            $script:TestKey = "$script:TestKeyBase\$([System.Guid]::NewGuid().ToString('N'))"
            $null = New-Item -Path $script:TestKey -Force
        }

        AfterEach {
            if ($script:TestKey) { Remove-Item -LiteralPath $script:TestKey -Force -ErrorAction SilentlyContinue }
        }

        It 'Does not throw when removing an existing key with no subkeys' {
            { Remove-ADTRegistryKey -LiteralPath $script:TestKey } | Should -Not -Throw
        }

        It 'The key no longer exists after removal' {
            Remove-ADTRegistryKey -LiteralPath $script:TestKey
            Test-Path -LiteralPath $script:TestKey | Should -BeFalse
        }
    }

    Context 'Remove a non-existent key' {
        It 'Does not throw for a key that does not exist' {
            $missing = "HKCU:\SOFTWARE\PSADTTest_NoSuchKey_$([System.Guid]::NewGuid().ToString('N'))"
            { Remove-ADTRegistryKey -LiteralPath $missing } | Should -Not -Throw
        }
    }

    Context 'Keys with subkeys — Recurse requirement' {
        BeforeEach {
            $script:ParentKey = "$script:TestKeyBase\$([System.Guid]::NewGuid().ToString('N'))"
            $script:ChildKey = "$script:ParentKey\SubKey"
            $null = New-Item -Path $script:ChildKey -Force
        }

        AfterEach {
            if ($script:ParentKey) { Remove-Item -LiteralPath $script:ParentKey -Recurse -Force -ErrorAction SilentlyContinue }
        }

        It 'Throws when removing a key that has subkeys without -Recurse' {
            { Remove-ADTRegistryKey -LiteralPath $script:ParentKey -ErrorAction Stop } | Should -Throw
        }

        It 'Does not throw when removing a key with subkeys using -Recurse' {
            { Remove-ADTRegistryKey -LiteralPath $script:ParentKey -Recurse } | Should -Not -Throw
        }

        It 'Key and all subkeys are gone after -Recurse removal' {
            Remove-ADTRegistryKey -LiteralPath $script:ParentKey -Recurse
            Test-Path -LiteralPath $script:ParentKey | Should -BeFalse
        }
    }

    Context 'Remove a specific value with -Name' {
        BeforeEach {
            $script:TestKey = "$script:TestKeyBase\$([System.Guid]::NewGuid().ToString('N'))"
            $null = New-Item -Path $script:TestKey -Force
            Set-ItemProperty -LiteralPath $script:TestKey -Name 'TestVal' -Value 'data'
        }

        AfterEach {
            if ($script:TestKey) { Remove-Item -LiteralPath $script:TestKey -Recurse -Force -ErrorAction SilentlyContinue }
        }

        It 'Does not throw when removing an existing value' {
            { Remove-ADTRegistryKey -LiteralPath $script:TestKey -Name 'TestVal' } | Should -Not -Throw
        }

        It 'The value is gone after removal' {
            Remove-ADTRegistryKey -LiteralPath $script:TestKey -Name 'TestVal'
            $props = (Get-ItemProperty -LiteralPath $script:TestKey).PSObject.Properties | Select-Object -ExpandProperty Name
            $props | Should -Not -Contain 'TestVal'
        }

        It 'The parent key remains after removing a value' {
            Remove-ADTRegistryKey -LiteralPath $script:TestKey -Name 'TestVal'
            Test-Path -LiteralPath $script:TestKey | Should -BeTrue
        }

        It 'Does not throw when removing a value name that does not exist' {
            { Remove-ADTRegistryKey -LiteralPath $script:TestKey -Name 'NoSuchValue' } | Should -Not -Throw
        }
    }

    Context '-WhatIf' {
        BeforeEach {
            $script:TestKey = "$script:TestKeyBase\$([System.Guid]::NewGuid().ToString('N'))"
            $null = New-Item -Path $script:TestKey -Force
        }

        AfterEach {
            if ($script:TestKey) { Remove-Item -LiteralPath $script:TestKey -Force -ErrorAction SilentlyContinue }
        }

        It 'Does not remove the key when -WhatIf is specified' {
            Remove-ADTRegistryKey -LiteralPath $script:TestKey -WhatIf
            Test-Path -LiteralPath $script:TestKey | Should -BeTrue
        }
    }
}

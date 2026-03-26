BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # A relative path that will not exist under any real user profile.
    $script:NonExistentRelPath = "AppData\Local\PSADT_NonExistent_$([System.Guid]::NewGuid().ToString('N'))"
}

Describe 'Remove-ADTFileFromUserProfiles' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
        # Remove-ADTFileFromUserProfiles calls Get-ADTUserProfiles internally, which references
        # [PSADT.AccountManagement.AccountUtilities]::GetWellKnownSid. PowerShell resolves all
        # type literals at compile time, requiring admin rights.
        $script:IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }

    BeforeEach {
        if (!$script:IsAdmin) { Set-ItResult -Skipped -Because 'Requires admin rights (AccountUtilities.GetWellKnownSid requires elevated process)'; return }
    }

    Context '-LiteralPath parameter set' {
        It 'Does not throw for a non-existent path across all user profiles' {
            # Remove-ADTFile on a nonexistent path logs a warning but does not throw.
            { Remove-ADTFileFromUserProfiles -LiteralPath $script:NonExistentRelPath } | Should -Not -Throw
        }

        It 'Does not throw when -WhatIf is specified' {
            { Remove-ADTFileFromUserProfiles -LiteralPath $script:NonExistentRelPath -WhatIf } | Should -Not -Throw
        }

        It 'Does not throw with -ExcludeDefaultUser' {
            { Remove-ADTFileFromUserProfiles -LiteralPath $script:NonExistentRelPath -ExcludeDefaultUser } | Should -Not -Throw
        }
    }

    Context '-Path parameter set (wildcard)' {
        It 'Does not throw for a non-existent wildcard path' {
            { Remove-ADTFileFromUserProfiles -Path "AppData\Local\PSADT_NoMatch_$([System.Guid]::NewGuid().ToString('N'))\*" } | Should -Not -Throw
        }

        It 'Does not throw with -WhatIf using -Path' {
            { Remove-ADTFileFromUserProfiles -Path $script:NonExistentRelPath -WhatIf } | Should -Not -Throw
        }
    }

    Context '-Recurse' {
        It 'Does not throw for a non-existent path with -Recurse' {
            { Remove-ADTFileFromUserProfiles -LiteralPath $script:NonExistentRelPath -Recurse } | Should -Not -Throw
        }
    }

    Context 'Input validation' {
        It 'Throws when -LiteralPath is an empty string' {
            { Remove-ADTFileFromUserProfiles -LiteralPath '' } | Should -Throw
        }
    }
}

BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force

    # Get the current user's profile — their hive is always loaded in HKEY_USERS,
    # so tests using it require no reg.exe hive loading.
    # Get-ADTUserProfiles references AccountUtilities which requires admin rights;
    # wrap in try/catch so non-admin runs leave $null and tests skip via inline checks.
    $currentSID = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
    $script:CurrentProfile = try { @(Get-ADTUserProfiles -SID $currentSID) | Select-Object -First 1 } catch { $null }
    $script:DefaultProfiles = try { @(Get-ADTUserProfiles) } catch { @() }
}

Describe 'Invoke-ADTAllUsersRegistryAction' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Mandatory parameters' {
        It 'Throws when -ScriptBlock is not provided' {
            { Invoke-ADTAllUsersRegistryAction } | Should -Throw
        }
    }

    Context '-WhatIf suppresses scriptblock execution' {
        It 'Does not throw with -WhatIf and an explicit UserProfile (current user)' {
            if ($null -eq $script:CurrentProfile)
            {
                Set-ItResult -Skipped -Because 'Could not locate current user profile'
                return
            }
            { Invoke-ADTAllUsersRegistryAction -UserProfiles $script:CurrentProfile -ScriptBlock { } -WhatIf } | Should -Not -Throw
        }

        It 'Does not execute the ScriptBlock when -WhatIf is specified' {
            if ($null -eq $script:CurrentProfile)
            {
                Set-ItResult -Skipped -Because 'Could not locate current user profile'
                return
            }
            $script:ADTTestInvokeCount = 0
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:CurrentProfile -ScriptBlock { $script:ADTTestInvokeCount++ } -WhatIf
            $script:ADTTestInvokeCount | Should -Be 0
            Remove-Variable -Name ADTTestInvokeCount -Scope Script -ErrorAction SilentlyContinue
        }
    }

    Context 'ScriptBlock receives UserProfileInfo as $_' {
        It '$_ inside ScriptBlock is the UserProfileInfo for the current user' {
            if ($null -eq $script:CurrentProfile)
            {
                Set-ItResult -Skipped -Because 'Could not locate current user profile'
                return
            }
            $script:ADTTestCapturedProfile = $null
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:CurrentProfile -ScriptBlock { $script:ADTTestCapturedProfile = $_ }
            $script:ADTTestCapturedProfile | Should -BeOfType ([PSADT.AccountManagement.UserProfileInfo])
            Remove-Variable -Name ADTTestCapturedProfile -Scope Script -ErrorAction SilentlyContinue
        }
    }

    Context '-SkipUnloadedProfiles' {
        It 'Does not throw with -SkipUnloadedProfiles and all profiles with -WhatIf' {
            if ($script:DefaultProfiles.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'Could not enumerate user profiles (requires admin rights)'
                return
            }
            { Invoke-ADTAllUsersRegistryAction -UserProfiles $script:DefaultProfiles -ScriptBlock { } -SkipUnloadedProfiles -WhatIf } | Should -Not -Throw
        }
    }

    Context 'ScriptBlock as positional parameter' {
        It 'Accepts ScriptBlock as the first positional argument' {
            if ($null -eq $script:CurrentProfile)
            {
                Set-ItResult -Skipped -Because 'Could not locate current user profile'
                return
            }
            { Invoke-ADTAllUsersRegistryAction { } -UserProfiles $script:CurrentProfile -WhatIf } | Should -Not -Throw
        }
    }
}

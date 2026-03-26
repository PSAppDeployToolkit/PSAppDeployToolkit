BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Get-ADTUserProfiles' {
    BeforeAll {
        # Mock Set-ADTPreferenceVariables to avoid changing preference state during tests.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
        # Get-ADTUserProfiles references [PSADT.AccountManagement.AccountUtilities]::GetWellKnownSid in its body.
        # PowerShell resolves all type literals at compile time, requiring admin rights.
        $script:IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }

    BeforeEach {
        if (!$script:IsAdmin) { Set-ItResult -Skipped -Because 'Requires admin rights (AccountUtilities static constructor triggered at compile time)'; return }
    }

    Context 'Default — all profiles including Default User' {
        It 'Does not throw when called with no parameters' {
            { Get-ADTUserProfiles } | Should -Not -Throw
        }

        It 'Returns at least one profile' {
            $result = @(Get-ADTUserProfiles)
            $result.Count | Should -BeGreaterThan 0
        }

        It 'Each result is of type PSADT.Types.UserProfileInfo' {
            $result = @(Get-ADTUserProfiles)
            $result[0] | Should -BeOfType ([PSADT.Types.UserProfileInfo])
        }

        It 'Each result has a non-null SID' {
            $result = @(Get-ADTUserProfiles)
            foreach ($userProfile in $result)
            {
                $userProfile.SID | Should -Not -BeNull
            }
        }

        It 'Each result has a non-null ProfilePath' {
            $result = @(Get-ADTUserProfiles)
            foreach ($userProfile in $result)
            {
                $userProfile.ProfilePath | Should -Not -BeNull
            }
        }

        It 'Includes the Default User profile (NTAccount = Default)' {
            $result = @(Get-ADTUserProfiles)
            $defaultProfile = $result | Where-Object { $_.NTAccount -eq 'Default' }
            $defaultProfile | Should -Not -BeNull
        }
    }

    Context '-ExcludeDefaultUser' {
        It 'Does not throw with -ExcludeDefaultUser' {
            { Get-ADTUserProfiles -ExcludeDefaultUser } | Should -Not -Throw
        }

        It 'No result has NTAccount = Default when -ExcludeDefaultUser is used' {
            $result = @(Get-ADTUserProfiles -ExcludeDefaultUser)
            $defaultProfile = $result | Where-Object { $_.NTAccount -eq 'Default' }
            $defaultProfile | Should -BeNull
        }

        It 'Count without Default User is less than or equal to count with Default User' {
            $withDefault = @(Get-ADTUserProfiles).Count
            $withoutDefault = @(Get-ADTUserProfiles -ExcludeDefaultUser).Count
            $withoutDefault | Should -BeLessOrEqual $withDefault
        }
    }

    Context '-FilterScript parameter set' {
        It 'Does not throw with a -FilterScript that matches all' {
            { Get-ADTUserProfiles -FilterScript { $true } } | Should -Not -Throw
        }

        It '-FilterScript returning $false for all returns empty' {
            $result = @(Get-ADTUserProfiles -FilterScript { $false })
            $result.Count | Should -Be 0
        }

        It '-FilterScript { $true } returns the same count as the default call' {
            $allProfiles = @(Get-ADTUserProfiles)
            $filteredProfiles = @(Get-ADTUserProfiles -FilterScript { $true })
            $filteredProfiles.Count | Should -Be $allProfiles.Count
        }
    }

    Context '-SID parameter set' {
        It 'Does not throw when specifying the current user SID' {
            $currentSID = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
            { Get-ADTUserProfiles -SID $currentSID } | Should -Not -Throw
        }

        It 'Returns the current user profile when specifying the current user SID' {
            $currentSID = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
            $result = @(Get-ADTUserProfiles -SID $currentSID)
            $result.Count | Should -BeGreaterThan 0
            $result[0].SID.Value | Should -Be $currentSID.Value
        }
    }
}

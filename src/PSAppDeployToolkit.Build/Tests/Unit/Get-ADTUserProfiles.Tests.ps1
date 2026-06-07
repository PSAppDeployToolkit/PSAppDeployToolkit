BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Get-ADTUserProfiles' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Output contract' {
        It 'Should not throw' {
            { Get-ADTUserProfiles } | Should -Not -Throw
        }

        It 'Should return one or more PSADT.AccountManagement.UserProfileInfo objects' {
            $result = @(Get-ADTUserProfiles)
            $result.Count | Should -BeGreaterThan 0
        }

        It 'Should return objects of type PSADT.AccountManagement.UserProfileInfo' {
            $result = @(Get-ADTUserProfiles)
            $result[0] | Should -BeOfType ([PSADT.AccountManagement.UserProfileInfo])
        }

        It 'Should include the Default User profile by default' {
            $result = @(Get-ADTUserProfiles)
            $defaultProfile = $result | Where-Object { $_.NTAccount -eq 'Default' }
            $defaultProfile | Should -Not -BeNullOrEmpty
        }

        It 'Should return profiles with a non-null SID property' {
            $result = @(Get-ADTUserProfiles)
            $result | ForEach-Object { $_.SID | Should -Not -BeNullOrEmpty }
        }

        It 'Should return profiles with a non-null ProfilePath property' {
            $result = @(Get-ADTUserProfiles)
            $result | ForEach-Object { $_.ProfilePath | Should -Not -BeNullOrEmpty }
        }

        It 'Should expose the expected shape of properties on each object' {
            $result = @(Get-ADTUserProfiles)
            $first = $result[0]
            $first.PSObject.Properties.Name | Should -Contain 'NTAccount'
            $first.PSObject.Properties.Name | Should -Contain 'SID'
            $first.PSObject.Properties.Name | Should -Contain 'ProfilePath'
        }
    }

    Context 'ExcludeDefaultUser switch' {
        It 'Should exclude the Default User when -ExcludeDefaultUser is specified' {
            $result = @(Get-ADTUserProfiles -ExcludeDefaultUser)
            $defaultProfile = $result | Where-Object { $_.NTAccount -eq 'Default' }
            $defaultProfile | Should -BeNullOrEmpty
        }

        It 'Should return fewer or equal profiles with -ExcludeDefaultUser than without' {
            $withDefault    = @(Get-ADTUserProfiles)
            $withoutDefault = @(Get-ADTUserProfiles -ExcludeDefaultUser)
            $withoutDefault.Count | Should -BeLessOrEqual $withDefault.Count
        }
    }

    Context 'ExcludeNTAccount switch' {
        It 'Should exclude the Default User NTAccount when passed via -ExcludeNTAccount' {
            # The Default profile NTAccount resolves to 'Default'; pick the first real NT account.
            $allProfiles = @(Get-ADTUserProfiles)
            $profilesWithNT = $allProfiles | Where-Object { $_.NTAccount -and $_.NTAccount -ne 'Default' }
            if (!$profilesWithNT)
            {
                Set-ItResult -Skipped -Because 'No profiles with a resolvable NTAccount found on this machine.'
                return
            }
            $excludeAccount = [System.Security.Principal.NTAccount]$profilesWithNT[0].NTAccount
            $result = @(Get-ADTUserProfiles -ExcludeNTAccount $excludeAccount)
            $excluded = $result | Where-Object { $_.NTAccount -eq $excludeAccount.Value }
            $excluded | Should -BeNullOrEmpty
        }

        It 'Should return fewer profiles when a valid -ExcludeNTAccount is specified' {
            $allProfiles = @(Get-ADTUserProfiles)
            $profilesWithNT = $allProfiles | Where-Object { $_.NTAccount -and $_.NTAccount -ne 'Default' }
            if (!$profilesWithNT)
            {
                Set-ItResult -Skipped -Because 'No profiles with a resolvable NTAccount found on this machine.'
                return
            }
            $excludeAccount = [System.Security.Principal.NTAccount]$profilesWithNT[0].NTAccount
            $result = @(Get-ADTUserProfiles -ExcludeNTAccount $excludeAccount)
            $result.Count | Should -BeLessThan $allProfiles.Count
        }
    }

    Context 'IncludeSystemProfiles switch' {
        It 'Should not throw when -IncludeSystemProfiles is specified' {
            { Get-ADTUserProfiles -IncludeSystemProfiles } | Should -Not -Throw
        }

        It 'Should return more or equal profiles with -IncludeSystemProfiles than without' {
            $withoutSystem = @(Get-ADTUserProfiles)
            $withSystem    = @(Get-ADTUserProfiles -IncludeSystemProfiles)
            $withSystem.Count | Should -BeGreaterOrEqual $withoutSystem.Count
        }
    }

    Context 'FilterScript parameter set' {
        It 'Should not throw when -FilterScript always returns false' {
            { Get-ADTUserProfiles -FilterScript { $false } } | Should -Not -Throw
        }

        It 'Should return nothing when -FilterScript always returns false' {
            $result = @(Get-ADTUserProfiles -FilterScript { $false })
            $result.Count | Should -Be 0
        }

        It 'Should return all profiles when -FilterScript always returns true' {
            $unfiltered = @(Get-ADTUserProfiles)
            $filtered   = @(Get-ADTUserProfiles -FilterScript { $true })
            $filtered.Count | Should -Be $unfiltered.Count
        }
    }

    Context 'SID parameter set' {
        It 'Should return the profile for the current user SID when queried by SID' {
            $currentSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
            try
            {
                $result = @(Get-ADTUserProfiles -SID $currentSid)
                # The current user must have a profile entry in ProfileList.
                $result.Count | Should -BeGreaterThan 0
                $result[0].SID.Value | Should -Be $currentSid.Value
            }
            finally
            {
                # WindowsIdentity.User is a SecurityIdentifier; no Dispose needed.
            }
        }

        It 'Should return an object of type PSADT.AccountManagement.UserProfileInfo for the SID parameter set' {
            $currentSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
            $result = @(Get-ADTUserProfiles -SID $currentSid)
            if ($result.Count -eq 0)
            {
                Set-ItResult -Skipped -Because 'Current user SID profile not found in ProfileList.'
                return
            }
            $result[0] | Should -BeOfType ([PSADT.AccountManagement.UserProfileInfo])
        }
    }

    Context 'Input Validation' {
        It 'FilterScript parameter should be mandatory in the FilterScript parameter set' {
            $attrs = (Get-Command Get-ADTUserProfiles).Parameters['FilterScript'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] })
            $attrs.Mandatory | Should -Contain $true
        }

        It 'SID parameter should be mandatory in the Specific parameter set' {
            $attrs = (Get-Command Get-ADTUserProfiles).Parameters['SID'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] })
            $attrs.Mandatory | Should -Contain $true
        }

        It 'ExcludeNTAccount does not accept null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Get-ADTUserProfiles -ExcludeNTAccount $null } | Should @shouldParams
        }
    }

    Context 'Metadata' {
        It 'Should declare OutputType of PSADT.AccountManagement.UserProfileInfo' {
            $outputTypes = (Get-Command Get-ADTUserProfiles).OutputType.Type
            $outputTypes | Should -Contain ([PSADT.AccountManagement.UserProfileInfo])
        }
    }
}

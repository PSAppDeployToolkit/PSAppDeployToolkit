BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Get-ADTUserProfiles' {
    Context 'Functionality' {
        BeforeAll {
            $script:Profiles = @(Get-ADTUserProfiles)
            $callerSid = (Get-ADTCallerSid).Value
            $script:Mine = $script:Profiles | & { process { if ($_.SID.Value.Equals($callerSid)) { return $_ } } } | Select-Object -First 1

            # The profile the filtering tests act on. Deliberately not the caller's: LocalSystem has no
            # ordinary profile and is left out of this list by default, so there would be nothing to act on
            # when the suite runs as it. What those tests need is a profile that is certainly in the set,
            # not a particular person's - so any real one, which means anything but the default template,
            # whose SID is the null one rather than an account's.
            $script:Subject = $script:Profiles | & { process { if (!$_.SID.IsWellKnown([System.Security.Principal.WellKnownSidType]::NullSid)) { return $_ } } } | Select-Object -First 1
        }

        It 'Returns profiles with an account and a path' {
            $script:Profiles.Count | Should -BeGreaterThan 0
            foreach ($userProfile in $script:Profiles)
            {
                $userProfile.NTAccount | Should -Not -BeNullOrEmpty
                $userProfile.SID | Should -BeOfType ([System.Security.Principal.SecurityIdentifier])
                $userProfile.ProfilePath | Should -Not -BeNullOrEmpty
            }
        }

        It 'Includes the account running the test' {
            # Unless that account has no profile to include. LocalSystem's lives under the Windows
            # directory and is a system profile, which this leaves out unless asked for them - so the
            # thing to check there is that it was correctly left out.
            if ((Get-ADTCallerSid).IsWellKnown([System.Security.Principal.WellKnownSidType]::LocalSystemSid))
            {
                $script:Mine | Should -BeNullOrEmpty
                @(Get-ADTUserProfiles -IncludeSystemProfiles) | & { process { if ($_.SID.Value.Equals('S-1-5-18')) { return $_ } } } | Should -Not -BeNullOrEmpty
                return
            }
            $script:Mine | Should -Not -BeNullOrEmpty
            $script:Mine.ProfilePath | Should -BeExactly ([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile))
        }

        It 'Includes the default user unless asked not to' {
            # The default profile is what a first logon is seeded from, so callers copying files into every
            # profile want it, and callers acting on real people do not.
            ($script:Profiles.NTAccount -join ',') | Should -BeLike '*Default*'
            ((Get-ADTUserProfiles -ExcludeDefaultUser).NTAccount -join ',') | Should -Not -BeLike '*Default*'
        }

        It 'Leaves the system profiles out unless asked for them' {
            @(Get-ADTUserProfiles -IncludeSystemProfiles).Count | Should -BeGreaterThan $script:Profiles.Count
        }

        It 'Drops an account named to -ExcludeNTAccount' {
            $remaining = @(Get-ADTUserProfiles -ExcludeNTAccount $script:Subject.NTAccount)
            $remaining.Count | Should -Be ($script:Profiles.Count - 1)
            $remaining.SID.Value | Should -Not -Contain $script:Subject.SID.Value
        }

        It 'Returns just the profile asked for by -SID' {
            $single = @(Get-ADTUserProfiles -SID $script:Subject.SID)
            $single.Count | Should -Be 1
            $single[0].SID.Value | Should -BeExactly $script:Subject.SID.Value
        }

        It 'Applies a -FilterScript' {
            $script:WantedAccount = $script:Subject.NTAccount
            $filtered = @(Get-ADTUserProfiles -FilterScript { $_.NTAccount -eq $script:WantedAccount })
            $filtered.Count | Should -Be 1
        }

        It 'Fills in the shell folder paths only with -LoadProfilePaths' {
            # Reading each profile's shell folders means loading its registry hive, so it is opt-in and the
            # paths are empty without it.
            $script:Subject.DesktopPath | Should -BeNullOrEmpty

            $wantedSid = $script:Subject.SID.Value
            $loaded = @(Get-ADTUserProfiles -LoadProfilePaths) | & { process { if ($_.SID.Value.Equals($wantedSid)) { return $_ } } } | Select-Object -First 1
            $loaded.AppDataPath | Should -Not -BeNullOrEmpty
            $loaded.DesktopPath | Should -Not -BeNullOrEmpty
        }

        It 'Rejects a repeated SID' {
            { Get-ADTUserProfiles -SID $script:Subject.SID, $script:Subject.SID } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Completes without erroring for <Switch>' -ForEach @(
            @{ Switch = 'IncludeServiceProfiles' }
            @{ Switch = 'IncludeSystemProfiles' }
            @{ Switch = 'IncludeIISAppPoolProfiles' }
            @{ Switch = 'IncludeEpmProfiles' }
        ) {
            # A service or app pool SID commonly has no account behind it, which makes the translation
            # throw where the enumeration means to skip the profile quietly.
            $splat = @{ $Switch = $true }
            { Get-ADTUserProfiles @splat -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Asks the translation to fail rather than be ignored' {
            # Skipping itself is covered above by the real service and app pool profiles. This pins that
            # it is decided by which failure came back, asserted on the call as the other needs a live DC.
            Mock -ModuleName PSAppDeployToolkit ConvertTo-ADTNTAccountOrSID { [System.Security.Principal.NTAccount]::new('CONTOSO\someone') }
            $null = Get-ADTUserProfiles
            Should -Invoke -ModuleName PSAppDeployToolkit ConvertTo-ADTNTAccountOrSID -ParameterFilter { $ErrorAction -eq 'Stop' }
            Should -Invoke -ModuleName PSAppDeployToolkit ConvertTo-ADTNTAccountOrSID -ParameterFilter { $ErrorAction -eq 'Ignore' } -Times 0
        }
    }
}

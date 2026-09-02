BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    # The caller's own profile, whose hive is loaded by definition, so nothing has to be mounted for it.
    # Every test runs with -SkipUnloadedProfiles so that no other user's hive is ever loaded either.
    $script:CallerSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
    $script:CallerProfile = [PSADT.AccountManagement.UserProfileInfo]::new(
        $script:CallerSid.Translate([System.Security.Principal.NTAccount]),
        $script:CallerSid,
        [System.IO.DirectoryInfo]::new($env:USERPROFILE))

    # A profile that does not exist, so its hive can never be loaded and it is always skipped.
    $script:AbsentProfile = [PSADT.AccountManagement.UserProfileInfo]::new(
        [System.Security.Principal.NTAccount]::new('TESTONLY\Absent'),
        [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1111111111-2222222222-3333333333-1099'),
        [System.IO.DirectoryInfo]::new("$env:SystemDrive\ADTNoSuchProfile"))
}

Describe 'Invoke-ADTAllUsersRegistryAction' {
    Context 'Functionality' {
        It 'Runs the action for the profile it was given' {
            $seen = [System.Collections.Generic.List[System.String]]::new()
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:CallerProfile -SkipUnloadedProfiles -ScriptBlock { $seen.Add($_.SID.Value) }
            $seen | Should -Be $script:CallerSid.Value
        }

        It 'Hands the action the profile it is acting on' {
            # The whole point is that the action knows whose registry it is writing to, since it has to
            # build the HKEY_USERS path from the SID itself.
            $seen = [System.Collections.Generic.List[System.String]]::new()
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:CallerProfile -SkipUnloadedProfiles -ScriptBlock { $seen.Add($_.NTAccount.Value) }
            $seen | Should -Be $script:CallerSid.Translate([System.Security.Principal.NTAccount]).Value
        }

        It 'Runs every action it was given' {
            $seen = [System.Collections.Generic.List[System.String]]::new()
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:CallerProfile -SkipUnloadedProfiles -ScriptBlock { $seen.Add('first') }, { $seen.Add('second') }
            $seen | Should -Be 'first', 'second'
        }

        It 'Lets the action write to the registry' {
            # This is what the function exists for, so the action has to be able to do the work rather
            # than merely be told which profile it would have been for.
            $key = (New-Item -Path "TestRegistry:\AllUsers$([System.Guid]::NewGuid().ToString('N'))" -ItemType Directory).PSPath
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:CallerProfile -SkipUnloadedProfiles -ScriptBlock { Set-ADTRegistryKey -LiteralPath $key -Name 'WrittenFor' -Value $_.SID.Value }
            (Get-ItemProperty -LiteralPath $key).WrittenFor | Should -BeExactly $script:CallerSid.Value
        }

        It 'Returns whatever the action produced' {
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:CallerProfile -SkipUnloadedProfiles -ScriptBlock { 'produced' } | Should -Be 'produced'
        }
    }

    Context 'Profiles whose hive is not loaded' {
        It 'Skips them when asked to' {
            $seen = [System.Collections.Generic.List[System.String]]::new()
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:AbsentProfile -SkipUnloadedProfiles -ScriptBlock { $seen.Add($_.SID.Value) }
            $seen | Should -BeNullOrEmpty
        }

        It 'Says which profile it skipped' {
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:AbsentProfile -SkipUnloadedProfiles -ScriptBlock { }
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like '*registry hive is not loaded*' }
        }

        It 'Carries on with the profiles it can act on' {
            # One unreachable profile must not stop a deployment applying a setting for everyone else.
            $seen = [System.Collections.Generic.List[System.String]]::new()
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:AbsentProfile, $script:CallerProfile -SkipUnloadedProfiles -ScriptBlock { $seen.Add($_.SID.Value) }
            $seen | Should -Be $script:CallerSid.Value
        }
    }

    Context 'Input Validation' {
        It 'Requires an action to run' {
            Test-ADTParameterSetSatisfied -Command (Get-Command Invoke-ADTAllUsersRegistryAction) -Parameter UserProfiles, SkipUnloadedProfiles | Should -BeFalse
        }

        It 'Refuses the same profile twice' {
            # Acting on one user's hive twice in a run is a caller mistake, and mounting it twice would
            # leave it mounted after the first unload.
            { Invoke-ADTAllUsersRegistryAction -UserProfiles $script:CallerProfile, $script:CallerProfile -SkipUnloadedProfiles -ScriptBlock { } } | Should -Throw -ErrorId 'ParameterArgumentValidationError,Invoke-ADTAllUsersRegistryAction'
        }

        It 'Refuses something that is not a profile' {
            { Invoke-ADTAllUsersRegistryAction -UserProfiles 'not a profile' -SkipUnloadedProfiles -ScriptBlock { } } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

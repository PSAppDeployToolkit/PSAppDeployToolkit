BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

    # The caller's own profile, whose hive is loaded by definition, so nothing has to be mounted for it.
    # Every test runs with -SkipUnloadedProfiles so that no other user's hive is ever loaded either.
    $script:CallerSid = Get-ADTCallerSid
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

    Context 'Profiles whose hive will not load' {
        BeforeAll {
            # A profile whose NTUSER.DAT is present but is not a hive, so reg.exe always refuses it. The
            # refusal is what is under test, so nothing is mounted and the run leaves no hive behind.
            $script:BadHiveProfilePath = New-Item -Path "$TestDrive\BadHiveProfile" -ItemType Directory -Force
            Set-Content -LiteralPath "$($script:BadHiveProfilePath.FullName)\NTUSER.DAT" -Value 'not a registry hive'
            $script:BadHiveProfile = [PSADT.AccountManagement.UserProfileInfo]::new(
                [System.Security.Principal.NTAccount]::new('TESTONLY\BadHive'),
                [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1111111111-2222222222-3333333333-1098'),
                $script:BadHiveProfilePath)
        }

        It 'Does not run the action against a hive it failed to load' {
            # reg.exe reports the refusal by exit code alone, so an unchecked load would leave the action
            # writing to a HKEY_USERS key that was never mounted.
            $seen = [System.Collections.Generic.List[System.String]]::new()
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:BadHiveProfile -ScriptBlock { $seen.Add($_.SID.Value) } -ErrorAction SilentlyContinue
            $seen | Should -BeNullOrEmpty
        }

        It 'Surfaces the failure to the caller' {
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:BadHiveProfile -ScriptBlock { } -ErrorAction SilentlyContinue -ErrorVariable hiveError
            $hiveError | Should -Not -BeNullOrEmpty
        }

        It 'Says which hive it could not load' {
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:BadHiveProfile -ScriptBlock { } -ErrorAction SilentlyContinue
            Should -Invoke -ModuleName PSAppDeployToolkit Write-ADTLogEntry -ParameterFilter { $Message -like "*$($script:BadHiveProfilePath.FullName)\NTUSER.DAT*" }
        }

        It 'Carries on with the profiles it can act on' {
            # One profile refusing to load must not stop a deployment applying a setting for everyone else.
            $seen = [System.Collections.Generic.List[System.String]]::new()
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:BadHiveProfile, $script:CallerProfile -ScriptBlock { $seen.Add($_.SID.Value) } -ErrorAction SilentlyContinue
            $seen | Should -Be $script:CallerSid.Value
        }
    }

    Context 'Previewing with -WhatIf' {
        BeforeAll {
            # reg.exe is stood in for throughout this context, so nothing is ever mounted and the profile
            # only has to look like one from the outside.
            $script:PreviewProfilePath = New-Item -Path "$TestDrive\PreviewProfile" -ItemType Directory -Force
            Set-Content -LiteralPath "$($script:PreviewProfilePath.FullName)\NTUSER.DAT" -Value 'stood in for'
            $script:PreviewProfile = [PSADT.AccountManagement.UserProfileInfo]::new(
                [System.Security.Principal.NTAccount]::new('TESTONLY\Preview'),
                [System.Security.Principal.SecurityIdentifier]::new('S-1-5-21-1111111111-2222222222-3333333333-1097'),
                $script:PreviewProfilePath)
        }

        It 'Previews the change rather than making it' {
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { [PSADT.ProcessManagement.ProcessResult]::new(0, $null, $null, $null) }
            $seen = [System.Collections.Generic.List[System.String]]::new()
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:PreviewProfile -ScriptBlock { $seen.Add($_.SID.Value) } -WhatIf
            $seen | Should -BeNullOrEmpty
        }

        It 'Still mounts and unmounts the hive it would act on' {
            # Mounting is this function's own business rather than the change being previewed, and -WhatIf
            # reaching reg.exe would leave nothing to preview against and no exit code to read.
            Mock -ModuleName PSAppDeployToolkit Start-ADTProcess { [PSADT.ProcessManagement.ProcessResult]::new(0, $null, $null, $null) }
            Invoke-ADTAllUsersRegistryAction -UserProfiles $script:PreviewProfile -ScriptBlock { } -WhatIf
            Should -Invoke -ModuleName PSAppDeployToolkit Start-ADTProcess -Times 2 -Exactly
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

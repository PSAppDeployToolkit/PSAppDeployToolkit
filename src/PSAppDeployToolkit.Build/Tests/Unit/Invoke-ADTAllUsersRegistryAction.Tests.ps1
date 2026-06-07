BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Invoke-ADTAllUsersRegistryAction' {
    BeforeAll {
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Build fake user profiles as real PSADT.AccountManagement.UserProfileInfo objects.
        # The function coerces Get-ADTUserProfiles output back into its typed $UserProfiles
        # variable, so PSCustomObject fakes are rejected; real objects are required.
        function Get-FakeUserProfile
        {
            param ([System.String]$Sid, [System.String]$NTAccount, [System.String]$ProfilePath)
            $nt = [System.Security.Principal.NTAccount]::new($NTAccount)
            $sidObj = [System.Security.Principal.SecurityIdentifier]::new($Sid)
            $dir = [System.IO.DirectoryInfo]::new($ProfilePath)
            $ci = [System.Globalization.CultureInfo]::InvariantCulture
            return [PSADT.AccountManagement.UserProfileInfo]::new($nt, $sidObj, $dir, $dir, $dir, $dir, $dir, $dir, $dir, $dir, $dir, $ci)
        }
    }

    Context 'Per-profile scriptblock execution (hives already loaded)' {
        BeforeEach {
            # Two distinct fake profiles.
            $script:fakeProfiles = @(
                Get-FakeUserProfile -Sid 'S-1-5-21-1111111111-2222222222-3333333333-1001' -NTAccount 'TEST\UserOne' -ProfilePath "$TestDrive\UserOne"
                Get-FakeUserProfile -Sid 'S-1-5-21-1111111111-2222222222-3333333333-1002' -NTAccount 'TEST\UserTwo' -ProfilePath "$TestDrive\UserTwo"
            )
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserProfiles { return $script:fakeProfiles }

            # Pretend every profile's hive is already loaded so reg.exe LOAD/UNLOAD is never invoked.
            Mock -ModuleName PSAppDeployToolkit Test-Path { return $true } -ParameterFilter {
                $LiteralPath -like 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\*'
            }
        }

        It 'Runs the scriptblock once per profile' {
            $script:invokedSids = [System.Collections.Generic.List[System.String]]::new()
            $sb = { $script:invokedSids.Add($_.SID.Value) }
            Invoke-ADTAllUsersRegistryAction -ScriptBlock $sb
            $script:invokedSids.Count | Should -Be 2
        }

        It 'Passes each profile through to the scriptblock as $_' {
            $script:invokedSids = [System.Collections.Generic.List[System.String]]::new()
            $sb = { $script:invokedSids.Add($_.SID.Value) }
            Invoke-ADTAllUsersRegistryAction -ScriptBlock $sb
            $script:invokedSids | Should -Contain 'S-1-5-21-1111111111-2222222222-3333333333-1001'
            $script:invokedSids | Should -Contain 'S-1-5-21-1111111111-2222222222-3333333333-1002'
        }

        It 'Defaults UserProfiles to Get-ADTUserProfiles when none are supplied' {
            Invoke-ADTAllUsersRegistryAction -ScriptBlock { $null = $_ }
            Should -Invoke -ModuleName PSAppDeployToolkit -CommandName Get-ADTUserProfiles -Times 1 -Exactly
        }

        It 'Runs the scriptblock for all profiles when hives are already loaded (no-load path)' {
            # The hive-load branch is gated behind Test-Path returning $false; with it true,
            # the function must not attempt to mount any hive.
            $script:invokedSids = [System.Collections.Generic.List[System.String]]::new()
            { Invoke-ADTAllUsersRegistryAction -ScriptBlock { $script:invokedSids.Add($_.SID.Value) } } | Should -Not -Throw
            $script:invokedSids.Count | Should -Be 2
        }

        It 'Runs the scriptblock once per profile for each scriptblock supplied' {
            $script:invokedSids = [System.Collections.Generic.List[System.String]]::new()
            $sb1 = { $script:invokedSids.Add("A:$($_.SID.Value)") }
            $sb2 = { $script:invokedSids.Add("B:$($_.SID.Value)") }
            Invoke-ADTAllUsersRegistryAction -ScriptBlock $sb1, $sb2
            # 2 profiles x 2 scriptblocks = 4 invocations.
            $script:invokedSids.Count | Should -Be 4
        }
    }

    Context 'Explicit UserProfiles via mocked Get-ADTUserProfiles bypass' {
        BeforeEach {
            $script:singleProfile = @(Get-FakeUserProfile -Sid 'S-1-5-21-1111111111-2222222222-3333333333-1009' -NTAccount 'TEST\Solo' -ProfilePath "$TestDrive\Solo")
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserProfiles { return $script:singleProfile }
            Mock -ModuleName PSAppDeployToolkit Test-Path { return $true } -ParameterFilter {
                $LiteralPath -like 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\*'
            }
        }

        It 'Honors -WhatIf by not running the scriptblock' {
            $script:invokedSids = [System.Collections.Generic.List[System.String]]::new()
            Invoke-ADTAllUsersRegistryAction -ScriptBlock { $script:invokedSids.Add($_.SID.Value) } -WhatIf
            $script:invokedSids.Count | Should -Be 0
        }
    }

    Context 'SkipUnloadedProfiles (hive not loaded)' {
        BeforeEach {
            $script:fakeProfiles = @(
                Get-FakeUserProfile -Sid 'S-1-5-21-1111111111-2222222222-3333333333-1003' -NTAccount 'TEST\Unloaded' -ProfilePath "$TestDrive\Unloaded"
            )
            Mock -ModuleName PSAppDeployToolkit Get-ADTUserProfiles { return $script:fakeProfiles }

            # Pretend the hive is NOT loaded so the function must decide whether to load it.
            Mock -ModuleName PSAppDeployToolkit Test-Path { return $false } -ParameterFilter {
                $LiteralPath -like 'Microsoft.PowerShell.Core\Registry::HKEY_USERS\*'
            }
        }

        It 'Skips the profile and does not run the scriptblock when -SkipUnloadedProfiles is set' {
            $script:invokedSids = [System.Collections.Generic.List[System.String]]::new()
            Invoke-ADTAllUsersRegistryAction -ScriptBlock { $script:invokedSids.Add($_.SID.Value) } -SkipUnloadedProfiles
            $script:invokedSids.Count | Should -Be 0
        }
    }

    Context 'Input Validation' {
        It 'Has a mandatory ScriptBlock parameter' {
            (Get-Command Invoke-ADTAllUsersRegistryAction).Parameters['ScriptBlock'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Contain $true
        }

        It 'Throws ParameterArgumentValidationError when ScriptBlock is null' {
            $shouldParams = @{
                Throw         = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId       = 'ParameterArgumentValidationError,Invoke-ADTAllUsersRegistryAction'
            }
            { Invoke-ADTAllUsersRegistryAction -ScriptBlock $null } | Should @shouldParams
        }

        It 'Throws when the ScriptBlock array contains duplicate entries (ValidateUnique)' {
            $sb = { $null = $_ }
            { Invoke-ADTAllUsersRegistryAction -ScriptBlock $sb, $sb } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest

    # Mock Write-ADTLogEntry due to its expense when running via Pester.
    Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
}

Describe 'Remove-ADTFileFromUserProfiles' {
    BeforeEach {
        # The function has no way to be pointed anywhere other than the real profiles, so the enumeration
        # of them is replaced with profiles rooted under TestDrive. Nothing outside TestDrive is touched.
        $script:ProfileRoot = "$TestDrive\Profiles$([System.Guid]::NewGuid().ToString('N'))"
        $script:Profiles = 1..2 | ForEach-Object {
            $path = "$script:ProfileRoot\User$_"
            $null = New-Item -Path "$path\AppData\Local\Vendor" -ItemType Directory -Force
            Set-Content -LiteralPath "$path\AppData\Local\Vendor\leftover.txt" -Value 'leftover'
            Set-Content -LiteralPath "$path\AppData\Local\Vendor\keep.dat" -Value 'keep'
            [PSADT.AccountManagement.UserProfileInfo]::new(
                [System.Security.Principal.NTAccount]::new("TESTONLY\User$_"),
                [System.Security.Principal.SecurityIdentifier]::new("S-1-5-21-1111111111-2222222222-3333333333-100$_"),
                [System.IO.DirectoryInfo]::new($path))
        }
        Mock -ModuleName PSAppDeployToolkit Get-ADTUserProfiles { return $script:Profiles }
    }

    Context 'Functionality' {
        It 'Removes the file from every profile' {
            Remove-ADTFileFromUserProfiles -Path 'AppData\Local\Vendor\leftover.txt'
            foreach ($userProfile in $script:Profiles)
            {
                Test-Path -LiteralPath "$($userProfile.ProfilePath.FullName)\AppData\Local\Vendor\leftover.txt" | Should -BeFalse
            }
        }

        It 'Leaves the rest of the profile alone' {
            Remove-ADTFileFromUserProfiles -Path 'AppData\Local\Vendor\leftover.txt'
            foreach ($userProfile in $script:Profiles)
            {
                Test-Path -LiteralPath "$($userProfile.ProfilePath.FullName)\AppData\Local\Vendor\keep.dat" | Should -BeTrue
            }
        }

        It 'Resolves a wildcard within each profile' {
            Remove-ADTFileFromUserProfiles -Path 'AppData\Local\Vendor\*.txt'
            Test-Path -LiteralPath "$($script:Profiles[0].ProfilePath.FullName)\AppData\Local\Vendor\leftover.txt" | Should -BeFalse
            Test-Path -LiteralPath "$($script:Profiles[0].ProfilePath.FullName)\AppData\Local\Vendor\keep.dat" | Should -BeTrue
        }

        It 'Removes several paths in the one call' {
            Remove-ADTFileFromUserProfiles -LiteralPath 'AppData\Local\Vendor\leftover.txt', 'AppData\Local\Vendor\keep.dat'
            @(Get-ChildItem -LiteralPath "$($script:Profiles[0].ProfilePath.FullName)\AppData\Local\Vendor" -File).Count | Should -Be 0
        }

        It 'Leaves a folder alone unless asked to recurse' {
            Remove-ADTFileFromUserProfiles -Path 'AppData\Local\Vendor'
            Test-Path -LiteralPath "$($script:Profiles[0].ProfilePath.FullName)\AppData\Local\Vendor" | Should -BeTrue
        }

        It 'Removes a folder when asked to recurse' {
            Remove-ADTFileFromUserProfiles -Path 'AppData\Local\Vendor' -Recurse
            Test-Path -LiteralPath "$($script:Profiles[0].ProfilePath.FullName)\AppData\Local\Vendor" | Should -BeFalse
        }

        It 'Does not object to a path no profile has' {
            # A deployment cleans up after software that may never have run for a given user.
            { Remove-ADTFileFromUserProfiles -Path 'AppData\Local\NeverExisted\nothing.txt' } | Should -Not -Throw
        }

        It 'Removes nothing with -WhatIf' {
            Remove-ADTFileFromUserProfiles -Path 'AppData\Local\Vendor\leftover.txt' -WhatIf
            Test-Path -LiteralPath "$($script:Profiles[0].ProfilePath.FullName)\AppData\Local\Vendor\leftover.txt" | Should -BeTrue
        }
    }

    Context 'Choosing the profiles' {
        It 'Passes the exclusions on to the profile lookup' {
            # The filtering is the profile lookup's job, so what matters here is that the caller's choices
            # reach it rather than being dropped on the floor.
            Remove-ADTFileFromUserProfiles -Path 'AppData\Local\Vendor\leftover.txt' -ExcludeNTAccount 'TESTONLY\User2'
            Should -Invoke -ModuleName PSAppDeployToolkit Get-ADTUserProfiles -ParameterFilter { $ExcludeNTAccount -contains 'TESTONLY\User2' }
        }

        It 'Passes the profile switches on as well' {
            Remove-ADTFileFromUserProfiles -Path 'AppData\Local\Vendor\leftover.txt' -ExcludeDefaultUser -IncludeSystemProfiles -IncludeServiceProfiles
            Should -Invoke -ModuleName PSAppDeployToolkit Get-ADTUserProfiles -ParameterFilter { $ExcludeDefaultUser -and $IncludeSystemProfiles -and $IncludeServiceProfiles }
        }
    }

    Context 'Input Validation' {
        It 'Refuses a wildcard path and a literal one together' {
            { Remove-ADTFileFromUserProfiles -Path 'a' -LiteralPath 'b' } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Requires a path' {
            { Remove-ADTFileFromUserProfiles } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }
}

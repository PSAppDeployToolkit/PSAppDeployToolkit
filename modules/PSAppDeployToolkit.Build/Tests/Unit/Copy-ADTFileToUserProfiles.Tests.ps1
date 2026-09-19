BeforeAll {
    Import-Module "$PSScriptRoot\..\Support\PSAppDeployToolkit.TestHelpers.psm1"
    Import-ADTModuleUnderTest
}

Describe 'Copy-ADTFileToUserProfiles' {
    BeforeAll {
        $SourcePath = (New-Item -Path "$TestDrive\Source" -ItemType Directory).FullName
        New-Item -ItemType File -Force -Path @(
            "$SourcePath\test.txt"
            "$SourcePath\test2.txt"
        ) | Out-Null
        Mock -ModuleName PSAppDeployToolkit Copy-ADTFile {
        }
        Mock -ModuleName PSAppDeployToolkit Get-ADTUserProfiles {
            if ($PesterBoundParameters.ContainsKey('LoadProfilePaths'))
            {
                [PSADT.AccountManagement.UserProfileInfo]::new(
                    'User1',
                    'S-1-0-0',
                    'C:\Users\User1',
                    'C:\Users\User1\AppData\Roaming',
                    'C:\Users\User1\AppData\Local',
                    'C:\Users\User1\Desktop',
                    'C:\Users\User1\Documents',
                    'C:\Users\User1\AppData\Roaming\Microsoft\Windows\Start Menu',
                    'C:\Users\User1\AppData\Local\Temp',
                    'C:\Users\User1\OneDrive',
                    'C:\Users\User1\OneDrive'
                )
                [PSADT.AccountManagement.UserProfileInfo]::new(
                    'User2',
                    'S-1-0-0',
                    'C:\Users\User2',
                    'C:\Users\User2\AppData\Roaming',
                    'C:\Users\User2\AppData\Local',
                    'C:\Users\User2\Desktop',
                    'C:\Users\User2\Documents',
                    'C:\Users\User2\AppData\Roaming\Microsoft\Windows\Start Menu',
                    'C:\Users\User2\AppData\Local\Temp',
                    'C:\Users\User2\OneDrive',
                    'C:\Users\User2\OneDrive'
                )
            }
            else
            {
                [PSADT.AccountManagement.UserProfileInfo]::new(
                    'User1',
                    'S-1-0-0',
                    'C:\Users\User1',
                    $null,
                    $null,
                    $null,
                    $null,
                    $null,
                    $null,
                    $null,
                    $null
                )
                [PSADT.AccountManagement.UserProfileInfo]::new(
                    'User2',
                    'S-1-0-0',
                    'C:\Users\User2',
                    $null,
                    $null,
                    $null,
                    $null,
                    $null,
                    $null,
                    $null,
                    $null
                )
            }
        }

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Calls Copy-ADTFile for each user profile' {
            Copy-ADTFileToUserProfiles -Path "$SourcePath\test.txt" -Destination "AppData\Local\Test"

            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -Times 1 -Exactly -ParameterFilter {
                $Path -eq "$SourcePath\test.txt" -and $Destination -eq "C:\Users\User1\AppData\Local\Test"
            }
            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -Times 1 -Exactly -ParameterFilter {
                $Path -eq "$SourcePath\test.txt" -and $Destination -eq "C:\Users\User2\AppData\Local\Test"
            }
        }

        It 'Calls Copy-ADTFile for each user profile with a non-default BasePath' {
            Copy-ADTFileToUserProfiles -Path "$SourcePath\test.txt" -Destination "Test" -BasePath LocalAppData

            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -Times 1 -Exactly -ParameterFilter {
                $Path -eq "$SourcePath\test.txt" -and $Destination -eq "C:\Users\User1\AppData\Local\Test"
            }
            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -Times 1 -Exactly -ParameterFilter {
                $Path -eq "$SourcePath\test.txt" -and $Destination -eq "C:\Users\User2\AppData\Local\Test"
            }
        }

        It 'Passes through parameters to Copy-ADTFile' {
            Copy-ADTFileToUserProfiles -Path "$SourcePath\test.txt" -Destination "AppData\Local\Test" -FileCopyMode 'Robocopy' -RobocopyParams '/Z' -RobocopyAdditionalParams '/B'

            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -ParameterFilter {
                $FileCopyMode -eq 'Robocopy' -and $RobocopyParams -eq '/Z' -and $RobocopyAdditionalParams -eq '/B'
            }
        }
    }

    Context 'Profile scoping' {
        BeforeAll {
            # Deliberately not User1/User2, so an assertion cannot pass on the mocked profile list instead.
            $script:SpecifiedProfiles = @(
                [PSADT.AccountManagement.UserProfileInfo]::new(
                    'User3',
                    'S-1-0-0',
                    'C:\Users\User3',
                    'C:\Users\User3\AppData\Roaming',
                    'C:\Users\User3\AppData\Local',
                    'C:\Users\User3\Desktop',
                    'C:\Users\User3\Documents',
                    'C:\Users\User3\AppData\Roaming\Microsoft\Windows\Start Menu',
                    'C:\Users\User3\AppData\Local\Temp',
                    'C:\Users\User3\OneDrive',
                    'C:\Users\User3\OneDrive'
                )
                [PSADT.AccountManagement.UserProfileInfo]::new(
                    'User4',
                    'S-1-0-0',
                    'C:\Users\User4',
                    'C:\Users\User4\AppData\Roaming',
                    'C:\Users\User4\AppData\Local',
                    'C:\Users\User4\Desktop',
                    'C:\Users\User4\Documents',
                    'C:\Users\User4\AppData\Roaming\Microsoft\Windows\Start Menu',
                    'C:\Users\User4\AppData\Local\Temp',
                    'C:\Users\User4\OneDrive',
                    'C:\Users\User4\OneDrive'
                )
            )
        }

        It 'Copies to the profiles supplied via -Path and -UserProfiles' {
            Copy-ADTFileToUserProfiles -Path "$SourcePath\test.txt" -Destination 'Test' -UserProfiles $script:SpecifiedProfiles

            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Get-ADTUserProfiles' -Times 0 -Exactly
            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -Times 1 -Exactly -ParameterFilter {
                $Destination -eq 'C:\Users\User3\Test'
            }
            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -Times 1 -Exactly -ParameterFilter {
                $Destination -eq 'C:\Users\User4\Test'
            }
        }

        It 'Copies to the profiles supplied via -LiteralPath and -UserProfiles' {
            Copy-ADTFileToUserProfiles -LiteralPath "$SourcePath\test.txt" -Destination 'Test' -BasePath LocalAppData -UserProfiles $script:SpecifiedProfiles

            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Get-ADTUserProfiles' -Times 0 -Exactly
            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -Times 1 -Exactly -ParameterFilter {
                $Path -eq "$SourcePath\test.txt" -and $Destination -eq 'C:\Users\User3\AppData\Local\Test'
            }
        }

        It 'Forwards the calculated profile filters given with -Path to Get-ADTUserProfiles' {
            Copy-ADTFileToUserProfiles -Path "$SourcePath\test.txt" -Destination 'Test' -ExcludeNTAccount 'CONTOSO\User1' -IncludeSystemProfiles -IncludeServiceProfiles -ExcludeDefaultUser

            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Get-ADTUserProfiles' -Times 1 -Exactly -ParameterFilter {
                ($ExcludeNTAccount.Count -eq 1) -and $ExcludeNTAccount[0].Value.Equals('CONTOSO\User1') -and $IncludeSystemProfiles -and $IncludeServiceProfiles -and $ExcludeDefaultUser
            }
        }

        It 'Forwards the calculated profile filters given with -LiteralPath to Get-ADTUserProfiles' {
            Copy-ADTFileToUserProfiles -LiteralPath "$SourcePath\test.txt" -Destination 'Test' -ExcludeDefaultUser

            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Get-ADTUserProfiles' -Times 1 -Exactly -ParameterFilter {
                $ExcludeDefaultUser -and !$IncludeSystemProfiles -and !$IncludeServiceProfiles
            }
            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -Times 1 -Exactly -ParameterFilter {
                $Path -eq "$SourcePath\test.txt" -and $Destination -eq 'C:\Users\User1\Test'
            }
        }

        It 'Collects every path piped in to -LiteralPath' {
            "$SourcePath\test.txt", "$SourcePath\test2.txt" | Copy-ADTFileToUserProfiles -Destination 'Test' -UserProfiles $script:SpecifiedProfiles

            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -Times 1 -Exactly -ParameterFilter {
                ($Path.Count -eq 2) -and ($Path -contains "$SourcePath\test.txt") -and ($Path -contains "$SourcePath\test2.txt") -and $Destination -eq 'C:\Users\User3\Test'
            }
        }

        It 'Rejects -UserProfiles together with a calculated profile filter' {
            {
                Copy-ADTFileToUserProfiles -Path "$SourcePath\test.txt" -Destination 'Test' -UserProfiles $script:SpecifiedProfiles -ExcludeDefaultUser
            } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }

        It 'Rejects -Path together with -LiteralPath' {
            {
                Copy-ADTFileToUserProfiles -Path "$SourcePath\test.txt" -LiteralPath "$SourcePath\test2.txt" -Destination 'Test'
            } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }

    Context 'Profile path loading' {
        # Loading the shell folder paths mounts the registry hive of every logged off user, so it must
        # only happen for a base path that actually needs one of them.
        It 'Does not load the shell folder paths for the default base path' {
            Copy-ADTFileToUserProfiles -Path "$SourcePath\test.txt" -Destination 'Test'

            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Get-ADTUserProfiles' -Times 1 -Exactly -ParameterFilter {
                !$PesterBoundParameters.ContainsKey('LoadProfilePaths')
            }
            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -Times 1 -Exactly -ParameterFilter {
                $Destination -eq 'C:\Users\User1\Test'
            }
        }

        It 'Does not load the shell folder paths for an explicit -BasePath of Profile' {
            Copy-ADTFileToUserProfiles -Path "$SourcePath\test.txt" -Destination 'Test' -BasePath Profile

            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Get-ADTUserProfiles' -Times 1 -Exactly -ParameterFilter {
                !$PesterBoundParameters.ContainsKey('LoadProfilePaths')
            }
        }

        It 'Loads the shell folder paths for a non-default base path' {
            Copy-ADTFileToUserProfiles -Path "$SourcePath\test.txt" -Destination 'Test' -BasePath Documents

            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Get-ADTUserProfiles' -Times 1 -Exactly -ParameterFilter {
                $LoadProfilePaths
            }
            Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -Times 1 -Exactly -ParameterFilter {
                $Destination -eq 'C:\Users\User1\Documents\Test'
            }
        }
    }
}

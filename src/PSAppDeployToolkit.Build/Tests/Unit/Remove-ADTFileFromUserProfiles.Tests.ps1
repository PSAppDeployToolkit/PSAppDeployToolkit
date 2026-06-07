BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}
Describe 'Remove-ADTFileFromUserProfiles' {
    BeforeAll {
        # Mock Get-ADTUserProfiles to return fake profiles rooted under $TestDrive so no real profile is touched.
        Mock -ModuleName PSAppDeployToolkit Get-ADTUserProfiles {
            [PSADT.AccountManagement.UserProfileInfo]::new(
                'User1',
                'S-1-0-0',
                "$TestDrive\User1",
                $null, $null, $null, $null, $null, $null, $null, $null
            )
            [PSADT.AccountManagement.UserProfileInfo]::new(
                'User2',
                'S-1-0-0',
                "$TestDrive\User2",
                $null, $null, $null, $null, $null, $null, $null, $null
            )
        }

        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }
    }

    Context 'Functionality' {
        It 'Removes the relative path from each user profile' {
            New-Item -Path "$TestDrive\User1\AppData\Roaming\MyApp\config.txt" -ItemType File -Force | Out-Null
            New-Item -Path "$TestDrive\User2\AppData\Roaming\MyApp\config.txt" -ItemType File -Force | Out-Null

            Remove-ADTFileFromUserProfiles -Path 'AppData\Roaming\MyApp\config.txt'

            Test-Path -LiteralPath "$TestDrive\User1\AppData\Roaming\MyApp\config.txt" | Should -BeFalse
            Test-Path -LiteralPath "$TestDrive\User2\AppData\Roaming\MyApp\config.txt" | Should -BeFalse
        }

        It 'Forwards the joined path to Remove-ADTFile once per profile (Path set)' {
            Mock -ModuleName PSAppDeployToolkit Remove-ADTFile { }

            Remove-ADTFileFromUserProfiles -Path 'AppData\Local\MyApp\file.txt'

            Should -Invoke -ModuleName PSAppDeployToolkit -CommandName Remove-ADTFile -Times 1 -Exactly -ParameterFilter {
                $Path -contains "$TestDrive\User1\AppData\Local\MyApp\file.txt"
            }
            Should -Invoke -ModuleName PSAppDeployToolkit -CommandName Remove-ADTFile -Times 1 -Exactly -ParameterFilter {
                $Path -contains "$TestDrive\User2\AppData\Local\MyApp\file.txt"
            }
        }

        It 'Joins the LiteralPath value to each profile and forwards it to Remove-ADTFile via -Path' {
            Mock -ModuleName PSAppDeployToolkit Remove-ADTFile { }

            Remove-ADTFileFromUserProfiles -LiteralPath 'AppData\Local\MyApp\file.txt'

            Should -Invoke -ModuleName PSAppDeployToolkit -CommandName Remove-ADTFile -Times 1 -Exactly -ParameterFilter {
                $Path -contains "$TestDrive\User1\AppData\Local\MyApp\file.txt"
            }
            Should -Invoke -ModuleName PSAppDeployToolkit -CommandName Remove-ADTFile -Times 1 -Exactly -ParameterFilter {
                $Path -contains "$TestDrive\User2\AppData\Local\MyApp\file.txt"
            }
        }

        It 'Joins each supplied path element to every profile' {
            Mock -ModuleName PSAppDeployToolkit Remove-ADTFile { }

            Remove-ADTFileFromUserProfiles -Path 'first.txt', 'second.txt'

            Should -Invoke -ModuleName PSAppDeployToolkit -CommandName Remove-ADTFile -Times 1 -Exactly -ParameterFilter {
                ($Path -contains "$TestDrive\User1\first.txt") -and ($Path -contains "$TestDrive\User1\second.txt")
            }
        }

        It 'Forwards -Recurse through to Remove-ADTFile' {
            Mock -ModuleName PSAppDeployToolkit Remove-ADTFile { }

            Remove-ADTFileFromUserProfiles -Path 'AppData\Local\MyApp' -Recurse

            Should -Invoke -ModuleName PSAppDeployToolkit -CommandName Remove-ADTFile -ParameterFilter { $Recurse -eq $true }
        }

        It 'Forwards profile-selection switches through to Get-ADTUserProfiles' {
            Mock -ModuleName PSAppDeployToolkit Remove-ADTFile { }

            Remove-ADTFileFromUserProfiles -Path 'file.txt' -IncludeSystemProfiles -IncludeServiceProfiles -ExcludeDefaultUser -ExcludeNTAccount 'CONTOSO\jdoe'

            Should -Invoke -ModuleName PSAppDeployToolkit -CommandName Get-ADTUserProfiles -ParameterFilter {
                ($IncludeSystemProfiles -eq $true) -and ($IncludeServiceProfiles -eq $true) -and ($ExcludeDefaultUser -eq $true) -and ($ExcludeNTAccount -contains 'CONTOSO\jdoe')
            }
        }

        It 'Does not call Remove-ADTFile when -WhatIf is specified' {
            Mock -ModuleName PSAppDeployToolkit Remove-ADTFile { }

            Remove-ADTFileFromUserProfiles -Path 'file.txt' -WhatIf

            Should -Invoke -ModuleName PSAppDeployToolkit -CommandName Remove-ADTFile -Times 0 -Exactly
        }
    }

    Context 'Input Validation' {
        It 'Should verify that Path is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTFileFromUserProfiles'
            }
            { Remove-ADTFileFromUserProfiles -Path $null } | Should @shouldParams
            { Remove-ADTFileFromUserProfiles -Path '' } | Should @shouldParams
            { Remove-ADTFileFromUserProfiles -Path " `f`n`r`t`v" } | Should @shouldParams
        }

        It 'Should verify that LiteralPath is not null, empty or whitespace' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
                ErrorId = 'ParameterArgumentValidationError,Remove-ADTFileFromUserProfiles'
            }
            { Remove-ADTFileFromUserProfiles -LiteralPath $null } | Should @shouldParams
            { Remove-ADTFileFromUserProfiles -LiteralPath '' } | Should @shouldParams
            { Remove-ADTFileFromUserProfiles -LiteralPath " `f`n`r`t`v" } | Should @shouldParams
        }

        It 'Should reject duplicate path values' {
            $shouldParams = @{
                Throw = $true
                ExceptionType = [System.Management.Automation.ParameterBindingException]
            }
            { Remove-ADTFileFromUserProfiles -Path 'dup.txt', 'dup.txt' } | Should @shouldParams
        }
    }
}

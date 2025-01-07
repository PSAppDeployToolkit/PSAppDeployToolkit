BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Copy-ADTFileToUserProfiles' {
    BeforeAll {
        $SourcePath = (New-Item -Path "$TestDrive\Source" -ItemType Directory).FullName
        New-Item -ItemType File -Force -Path @(
            "$SourcePath\test.txt"
        ) | Out-Null
        Mock -ModuleName PSAppDeployToolkit Copy-ADTFile {
        }
        Mock -ModuleName PSAppDeployToolkit Get-ADTUserProfiles {
            if ($PesterBoundParameters.LoadProfilePaths)
            {
                [PSADT.Types.UserProfile]::new(
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
                [PSADT.Types.UserProfile]::new(
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
                [PSADT.Types.UserProfile]::new(
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
                [PSADT.Types.UserProfile]::new(
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

        # Mock Set-ADTPreferenceVariables due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Set-ADTPreferenceVariables { }
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
}

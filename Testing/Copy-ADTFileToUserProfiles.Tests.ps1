BeforeAll {
	Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
	Import-Module "$PSScriptRoot\..\src\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'Copy-ADTFileToUserProfiles' {
	BeforeAll {
		Mock -ModuleName PSAppDeployToolkit Copy-ADTFile {
		}
		Mock -ModuleName PSAppDeployToolkit Get-ADTUserProfiles {
			return @(
				@{ ProfilePath = "C:\Users\User1" },
				@{ ProfilePath = "C:\Users\User2" }
			)
		}
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

		It 'Passes through parameters to Copy-ADTFile' {
			Copy-ADTFileToUserProfiles -Path "$SourcePath\test.txt" -Destination "AppData\Local\Test" -FileCopyMode 'Robocopy' -RobocopyParams '/Z' -RobocopyAdditionalParams '/B'

			Should -Invoke -ModuleName 'PSAppDeployToolkit' -CommandName 'Copy-ADTFile' -ParameterFilter {
				$FileCopyMode -eq 'Robocopy' -and $RobocopyParams -eq '/Z' -and $RobocopyAdditionalParams -eq '/B'
			}
		}
	}
}

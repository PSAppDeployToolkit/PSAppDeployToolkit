BeforeAll {
	try {
		$DeployMode = 'NonInteractive'
		$null = . "$PSScriptRoot\..\Toolkit\AppDeployToolkit\AppDeployToolkitMain.ps1" *> $null
		Mock Write-Host {}
		$DebugPreference = 'Continue'
	} catch {
		# Error may be thrown if dot-sourcing main without elevation, but elevation is not required for these tests.
		Write-Warning $_
	}
}

Describe 'Copy-File'-ForEach @(
	@{ UseRobocopy = $false }
	@{ UseRobocopy = $true }
) {
	BeforeAll {
		$SourcePath = (New-Item -Path "$TestDrive\Source" -ItemType Directory).FullName
		$DestinationPath = "$TestDrive\Destination"
		New-Item -ItemType File -Force -Path @(
			"$SourcePath\test.txt"
			"$SourcePath\test3.txt"
			"$SourcePath\Subfolder1\test.txt"
			"$SourcePath\Subfolder1\test1.txt"
			"$SourcePath\Subfolder2\test.txt"
			"$SourcePath\Subfolder2\test2.txt"
			"$SourcePath\Subfolder3\old.txt"
			"$SourcePath\Subfolder3\hidden.txt"
			"$SourcePath\Subfolder3\system.txt"
			"$SourcePath\Subfolder3\hiddensystem.txt"
			"$SourcePath\SubfolderHidden\test.txt"
		) | Out-Null

		Set-Content -Path "$SourcePath\Subfolder3\old.txt" -Value 'old file'
		Set-ItemProperty -Path "$SourcePath\Subfolder3\old.txt" -Name LastWriteTime -Value (Get-Date).AddDays(-1) -PassThru | Set-ItemProperty -Name CreationTime -Value (Get-Date).AddDays(-1)
		Set-ItemProperty -Path "$SourcePath\Subfolder3\hidden.txt" -Name Attributes -Value 'Hidden'
		Set-ItemProperty -Path "$SourcePath\Subfolder3\system.txt" -Name Attributes -Value 'System'
		Set-ItemProperty -Path "$SourcePath\Subfolder3\hiddensystem.txt" -Name Attributes -Value 'Hidden, System'
		Set-ItemProperty -Path "$SourcePath\SubfolderHidden" -Name Attributes -Value 'Hidden'
	}
	BeforeEach {
		if (Test-Path -Path $DestinationPath -PathType Container) {
			Remove-Item -Path $DestinationPath -Recurse -Force
		}
	}
	AfterEach {
		$DestinationFiles = (Get-ChildItem -Path $DestinationPath -Recurse -Force).FullName
		if ($DestinationFiles.Count -gt 0) {
			$DebugMessage = $DestinationFiles -join "`n"
			Write-Debug "Destination files:`n$DebugMessage"
		} else {
			Write-Debug 'No files in destination.'
		}
	}

	Context 'Tests to be repeated with and without destination folder being pre-created' -ForEach @(
		@{ PreCreateDestination = $false }
		@{ PreCreateDestination = $true }
	) {
		BeforeEach {
			if ($PreCreateDestination) {
				New-Item -Path $DestinationPath -ItemType Directory | Out-Null
			}
		}

		It 'Copies a single file ($PreCreateDestination = $<PreCreateDestination>; $UseRobocopy = $<UseRobocopy>)' {
			Copy-File -Path "$SourcePath\test.txt" -Destination $DestinationPath -UseRobocopy $UseRobocopy

			"$DestinationPath\test.txt" | Should -Exist
		}

		It 'Copies a single file with a new filename ($PreCreateDestination = $<PreCreateDestination>; $UseRobocopy = $<UseRobocopy>)' {
			Copy-File -Path "$SourcePath\test.txt" -Destination "$DestinationPath\new.txt" -UseRobocopy $UseRobocopy

			"$DestinationPath\new.txt" | Should -Exist
		}

		It 'Copies a file where only filename is supplied ($PreCreateDestination = $<PreCreateDestination>; $UseRobocopy = $<UseRobocopy>)' {
			Push-Location $SourcePath
			Copy-File -Path 'test.txt' -Destination $DestinationPath -UseRobocopy $UseRobocopy
			Pop-Location

			"$DestinationPath\test.txt" | Should -Exist
		}

		It 'Copies a file where only filename is supplied prefixed with .\ ($PreCreateDestination = $<PreCreateDestination>; $UseRobocopy = $<UseRobocopy>)' {
			Push-Location $SourcePath
			Copy-File -Path '.\test.txt' -Destination $DestinationPath -UseRobocopy $UseRobocopy
			Pop-Location

			"$DestinationPath\test.txt" | Should -Exist
		}

		It 'Copies a file where both source and destination folders are prefixed with .\ ($PreCreateDestination = $<PreCreateDestination>; $UseRobocopy = $<UseRobocopy>)' {
			Push-Location $TestDrive
			Copy-File -Path '.\Source\test.txt' -Destination '.\Destination' -UseRobocopy $UseRobocopy
			Pop-Location

			"$DestinationPath\test.txt" | Should -Exist
		}

		It 'Copies a file where both source and destination folders are prefixed with ..\ ($PreCreateDestination = $<PreCreateDestination>; $UseRobocopy = $<UseRobocopy>)' {
			Push-Location "$SourcePath\Subfolder1"
			Copy-File -Path '..\test.txt' -Destination '..\..\Destination' -UseRobocopy $UseRobocopy
			Pop-Location

			"$DestinationPath\test.txt" | Should -Exist
		}

		It 'Copies a file to and from a UNC path ($PreCreateDestination = $<PreCreateDestination>; $UseRobocopy = $<UseRobocopy>)' {
			Copy-File -Path "$($SourcePath.Replace('C:\', '\\localhost\c$\'))\test.txt" -Destination $DestinationPath.Replace('C:\', '\\localhost\c$\') -UseRobocopy $UseRobocopy

			"$DestinationPath\test.txt" | Should -Exist
		}

		Context 'Tests to be performed with and without recursion/flatten' -ForEach @(
			@{ Recurse = $false; Flatten = $false }
			@{ Recurse = $true; Flatten = $false }
			@{ Recurse = $false; Flatten = $true }
		) {
			It 'Copies a folder ($PreCreateDestination = $<PreCreateDestination>; $Recurse = $<Recurse>; $Flatten = $<Flatten>; $UseRobocopy = $<UseRobocopy>)' {
				Copy-File -Path $SourcePath -Destination $DestinationPath -Recurse:$Recurse -Flatten:$Flatten -UseRobocopy $UseRobocopy

				if ($Flatten) {
					"$DestinationPath\test.txt" | Should -Exist
					"$DestinationPath\test1.txt" | Should -Exist
					"$DestinationPath\test2.txt" | Should -Exist
					"$DestinationPath\test3.txt" | Should -Exist
				} else {
					if ($UseRobocopy) {
						# Known issue - "$DestinationPath\Source\test.txt" will only exist when using Robocopy
						"$DestinationPath\Source\test.txt" | Should -Exist
					}
					if ($Recurse) {
						"$DestinationPath\Source\Subfolder1\test1.txt" | Should -Exist
					} else {
						"$DestinationPath\Source\Subfolder1\test1.txt" | Should -Not -Exist
					}
				}
			}

			It 'Copies files with a * as the source filename ($PreCreateDestination = $<PreCreateDestination>; $Recurse = $<Recurse>; $Flatten = $<Flatten>; $UseRobocopy = $<UseRobocopy>)' {
				Copy-File -Path "$SourcePath\*" -Destination $DestinationPath -Recurse:$Recurse -Flatten:$Flatten -UseRobocopy $UseRobocopy

				"$DestinationPath\test.txt" | Should -Exist
				"$DestinationPath\test3.txt" | Should -Exist

				if ($Flatten) {
					"$DestinationPath\test1.txt" | Should -Exist
					"$DestinationPath\test2.txt" | Should -Exist
					"$DestinationPath\Subfolder1" | Should -Not -Exist
				}
				# Known issue that * includes empty folders in non-recursive native copy
				elseif ($Recurse) {
					"$DestinationPath\Subfolder1\test1.txt" | Should -Exist
					"$DestinationPath\Subfolder2\test2.txt" | Should -Exist
				} else {
					"$DestinationPath\Subfolder1\test1.txt" | Should -Not -Exist
					"$DestinationPath\Subfolder2\test2.txt" | Should -Not -Exist
					# Known issue that * copies empty folders with native copy but not Robocopy
					#"$DestinationPath\Subfolder2" | Should -Exist
				}
			}

			It 'Copies files with a wildcard in the source filename ($PreCreateDestination = $<PreCreateDestination>; $Recurse = $<Recurse>; $Flatten = $<Flatten>; $UseRobocopy = $<UseRobocopy>)' {
				Copy-File -Path "$SourcePath\test*.txt" -Destination $DestinationPath -Recurse:$Recurse -Flatten:$Flatten -UseRobocopy $UseRobocopy

				"$DestinationPath\test.txt" | Should -Exist
				"$DestinationPath\test3.txt" | Should -Exist

				if ($Flatten) {
					"$DestinationPath\test1.txt" | Should -Exist
					"$DestinationPath\test2.txt" | Should -Exist
					"$DestinationPath\Subfolder1" | Should -Not -Exist
				}
				# Known issue that recursive copy of files only works with Robocopy currently
				#elseif ($Recurse) {
				elseif ($Recurse -and $UseRobocopy) {
					"$DestinationPath\Subfolder1\test1.txt" | Should -Exist
					"$DestinationPath\Subfolder2\test2.txt" | Should -Exist
				} else {
					"$DestinationPath\Subfolder1\test1.txt" | Should -Not -Exist
					"$DestinationPath\Subfolder2\test2.txt" | Should -Not -Exist
				}
			}

			It 'Copies files with a wildcard in the source folder path ($PreCreateDestination = $<PreCreateDestination>; $Recurse = $<Recurse>; $Flatten = $<Flatten>; $UseRobocopy = $<UseRobocopy>)' {
				Copy-File -Path "$SourcePath*\test.txt" -Destination $DestinationPath -Recurse:$Recurse -Flatten:$Flatten -UseRobocopy $UseRobocopy

				if ($Flatten) {
					# Flatten does not currently work in this scenario
					#"$DestinationPath\test.txt" | Should -Exist
					"$DestinationPath\Subfolder1" | Should -Not -Exist
				} elseif ($Recurse) {
					# Known issue - using a * in the path reverts to native file copy, but recursive copy of files only works with Robocopy currently
					#"$DestinationPath\Subfolder1\test.txt" | Should -Exist
				} else {
					"$DestinationPath\test.txt" | Should -Exist
					"$DestinationPath\Subfolder1" | Should -Not -Exist
				}
			}

			It 'Copies files with wildcards in the source folder path and filenames ($PreCreateDestination = $<PreCreateDestination>; $Recurse = $<Recurse>; $Flatten = $<Flatten>; $UseRobocopy = $<UseRobocopy>)' {
				Copy-File -Path "$SourcePath*\test*.txt" -Destination $DestinationPath -Recurse:$Recurse -Flatten:$Flatten -UseRobocopy $UseRobocopy

				if ($Flatten) {
					# Flatten does not currently work in this scenario
					#"$DestinationPath\test1.txt" | Should -Exist
					#"$DestinationPath\test2.txt" | Should -Exist
					#"$DestinationPath\test3.txt" | Should -Exist
					"$DestinationPath\Subfolder1" | Should -Not -Exist
				} elseif ($Recurse) {
					"$DestinationPath\test.txt" | Should -Exist
					"$DestinationPath\test3.txt" | Should -Exist
					# Known issue that recurse doesn't currently work in this scenario
					#"$DestinationPath\Subfolder1\test1.txt" | Should -Exist
				} else {
					"$DestinationPath\test.txt" | Should -Exist
					"$DestinationPath\test3.txt" | Should -Exist
					"$DestinationPath\Subfolder1" | Should -Not -Exist
				}
			}
		}
	}

	It 'Overwrites existing newer files ($UseRobocopy = $<UseRobocopy>)' {
		New-Item -Path "$DestinationPath\old.txt" -ItemType File -Force | Set-Content -Value 'new file'

		Copy-File -Path "$SourcePath\Subfolder3\old.txt" -Destination $DestinationPath -UseRobocopy $UseRobocopy

		"$DestinationPath\old.txt" | Should -FileContentMatch 'old file'
	}


	It 'Maintains attributes on copied items ($UseRobocopy = $<UseRobocopy>)' {
		Copy-File -Path "$SourcePath\Subfolder3\*.txt" -Destination $DestinationPath -UseRobocopy $UseRobocopy
		Copy-File -Path "$SourcePath\SubfolderHidden\test.txt" -Destination "$DestinationPath\NewFolder" -UseRobocopy $UseRobocopy

		"$DestinationPath\hidden.txt" | Should -Exist
		"$DestinationPath\system.txt" | Should -Exist
		"$DestinationPath\hiddensystem.txt" | Should -Exist
		"$DestinationPath\NewFolder\test.txt" | Should -Exist
		Get-ItemPropertyValue -Path "$DestinationPath\hidden.txt" -Name Attributes | Should -Match 'Hidden'
		Get-ItemPropertyValue -Path "$DestinationPath\system.txt" -Name Attributes | Should -Match 'System'
		Get-ItemPropertyValue -Path "$DestinationPath\hiddensystem.txt" -Name Attributes | Should -Match 'Hidden'
		Get-ItemPropertyValue -Path "$DestinationPath\hiddensystem.txt" -Name Attributes | Should -Match 'System'
		Get-ItemPropertyValue -Path "$DestinationPath\NewFolder\test.txt" -Name Attributes | Should -Not -Match 'Hidden'
		Get-ItemPropertyValue -Path "$DestinationPath\NewFolder" -Name Attributes | Should -Not -Match 'Hidden'
	}

	It 'Copies an array of items ($UseRobocopy = $<UseRobocopy>)' {
		Copy-File -Path @("$SourcePath\test.txt", "$SourcePath\Subfolder1\test1.txt", "$SourcePath\Subfolder2\test2.txt") -Destination $DestinationPath -UseRobocopy $UseRobocopy

		"$DestinationPath\test.txt" | Should -Exist
		"$DestinationPath\test1.txt" | Should -Exist
		"$DestinationPath\test2.txt" | Should -Exist
	}

	It 'Quits copying files when encountering an error ($UseRobocopy = $<UseRobocopy>)' {
		Copy-File -Path @("$SourcePath\test.txt", "$SourcePath\Subfolder99\test99.txt", "$SourcePath\Subfolder2\test2.txt") -Destination $DestinationPath -UseRobocopy $UseRobocopy -ContinueFileCopyOnError $false

		"$DestinationPath\test.txt" | Should -Exist
		"$DestinationPath\test2.txt" | Should -Not -Exist
	}

	It 'Continues copying files when encountering an error ($UseRobocopy = $<UseRobocopy>)' {
		Copy-File -Path @("$SourcePath\test.txt", "$SourcePath\Subfolder99\test99.txt", "$SourcePath\Subfolder2\test2.txt") -Destination $DestinationPath -UseRobocopy $UseRobocopy -ContinueFileCopyOnError $true

		"$DestinationPath\test.txt" | Should -Exist
		"$DestinationPath\test2.txt" | Should -Exist
	}

	It 'Copies files to and from paths longer than 260 characters ($UseRobocopy = $<UseRobocopy>)' {
		if ((Get-ItemPropertyValue -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'LongPathsEnabled' -ErrorAction SilentlyContinue) -eq 1) {
			Write-Debug 'Long paths are enabled.'
		} else {
			Write-Debug 'Long paths are not enabled.'
		}

		$LongDestinationPath = "$DestinationPath\"
		$LongDestinationPath = $LongDestinationPath.PadRight(265, 'a')

		Write-Debug "Destination path length: $($LongDestinationPath.Length)"

		Copy-File -Path "$SourcePath\test.txt" -Destination $LongDestinationPath -UseRobocopy $UseRobocopy
		Copy-File -Path "$LongDestinationPath\test.txt" -Destination "$LongDestinationPath\test2.txt" -UseRobocopy $UseRobocopy

		"$LongDestinationPath\test.txt" | Should -Exist
		"$LongDestinationPath\test2.txt" | Should -Exist
	}

}

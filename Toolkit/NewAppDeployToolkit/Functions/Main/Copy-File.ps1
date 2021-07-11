#region Function Copy-File
Function Copy-File {
<#
.SYNOPSIS
	Copy a file or group of files to a destination path.
.DESCRIPTION
	Copy a file or group of files to a destination path.
.PARAMETER Path
	Path of the file to copy.
.PARAMETER Destination
	Destination Path of the file to copy.
.PARAMETER Recurse
	Copy files in subdirectories.
.PARAMETER Flatten
	Flattens the files into the root destination directory.
.PARAMETER ContinueOnError
	Continue if an error is encountered. This will continue the deployment script, but will not continue copying files if an error is encountered. Default is: $true.
.PARAMETER ContinueFileCopyOnError
	Continue copying files if an error is encountered. This will continue the deployment script and will warn about files that failed to be copied. Default is: $false.
.EXAMPLE
	Copy-File -Path "$dirSupportFiles\MyApp.ini" -Destination "$envWinDir\MyApp.ini"
.EXAMPLE
	Copy-File -Path "$dirSupportFiles\*.*" -Destination "$envTemp\tempfiles"
	Copy all of the files in a folder to a destination folder.
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string[]]$Path,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Destination,
		[Parameter(Mandatory=$false)]
		[switch]$Recurse = $false,
		[Parameter(Mandatory=$false)]
		[switch]$Flatten,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true,
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueFileCopyOnError = $false
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			If ((-not ([IO.Path]::HasExtension($Destination))) -and (-not (Test-Path -LiteralPath $Destination -PathType 'Container'))) {
				Write-Log -Message "Destination folder does not exist, creating destination folder [$destination]." -Source ${CmdletName}
				$null = New-Item -Path $Destination -Type 'Directory' -Force -ErrorAction 'Stop'
			}

			if ($Flatten) {
				If ($Recurse) {
					Write-Log -Message "Copying file(s) recursively in path [$path] to destination [$destination] root folder, flattened." -Source ${CmdletName}
					If (-not $ContinueFileCopyOnError) {
						$null = Get-ChildItem -Path $path -Recurse | ForEach-Object {
							if(-not($_.PSIsContainer)) {
								Copy-Item -Path ($_.FullName) -Destination $destination -Force -ErrorAction 'Stop'
							}
						}
					}
					Else {
						$null = Get-ChildItem -Path $path -Recurse | ForEach-Object {
							if(-not($_.PSIsContainer)) {
								Copy-Item -Path ($_.FullName) -Destination $destination -Force -ErrorAction 'SilentlyContinue' -ErrorVariable FileCopyError
							}
						}
					}
				}
				Else {
					Write-Log -Message "Copying file in path [$path] to destination [$destination]." -Source ${CmdletName}
					If (-not $ContinueFileCopyOnError) {
						$null = Copy-Item -Path $path -Destination $destination -Force -ErrorAction 'Stop'
					}
					Else {
						$null = Copy-Item -Path $path -Destination $destination -Force -ErrorAction 'SilentlyContinue' -ErrorVariable FileCopyError
					}
				}
			}
			Else {
				$null = $FileCopyError
				If ($Recurse) {
					Write-Log -Message "Copying file(s) recursively in path [$path] to destination [$destination]." -Source ${CmdletName}
					If (-not $ContinueFileCopyOnError) {
						$null = Copy-Item -Path $Path -Destination $Destination -Force -Recurse -ErrorAction 'Stop'
					}
					Else {
						$null = Copy-Item -Path $Path -Destination $Destination -Force -Recurse -ErrorAction 'SilentlyContinue' -ErrorVariable FileCopyError
					}
				}
				Else {
					Write-Log -Message "Copying file in path [$path] to destination [$destination]." -Source ${CmdletName}
					If (-not $ContinueFileCopyOnError) {
						$null = Copy-Item -Path $Path -Destination $Destination -Force -ErrorAction 'Stop'
					}
					Else {
						$null = Copy-Item -Path $Path -Destination $Destination -Force -ErrorAction 'SilentlyContinue' -ErrorVariable FileCopyError
					}
				}
			}

			If ($fileCopyError) {
				Write-Log -Message "The following warnings were detected while copying file(s) in path [$path] to destination [$destination]. `r`n$FileCopyError" -Severity 2 -Source ${CmdletName}
			}
			Else {
				Write-Log -Message "File copy completed successfully." -Source ${CmdletName}
			}
		}
		Catch {
			Write-Log -Message "Failed to copy file(s) in path [$path] to destination [$destination]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to copy file(s) in path [$path] to destination [$destination]: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion

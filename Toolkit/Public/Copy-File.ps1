Function Copy-File {
	<#
.SYNOPSIS

Copy a file or group of files to a destination path.

.DESCRIPTION

Copy a file or group of files to a destination path.

.PARAMETER Path

Path of the file to copy. Multiple paths can be specified

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

.PARAMETER UseRobocopy

Use Robocopy to copy files rather than native PowerShell method. Robocopy overcomes the 260 character limit. Supports * in file names, but not folders, in source paths. Default is configured in the AppDeployToolkitConfig.xml file: $true

.PARAMETER RobocopyAdditionalParams

Additional parameters to pass to Robocopy. Default is: $null

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Copy-File -Path "$dirSupportFiles\MyApp.ini" -Destination "$envWinDir\MyApp.ini"

.EXAMPLE

Copy-File -Path "$dirSupportFiles\*.*" -Destination "$envTemp\tempfiles"

Copy all of the files in a folder to a destination folder.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true, Position = 0)]
		[ValidateNotNullorEmpty()]
		[String[]]$Path,
		[Parameter(Mandatory = $true, Position = 1)]
		[ValidateNotNullorEmpty()]
		[String]$Destination,
		[Parameter(Mandatory = $false)]
		[Switch]$Recurse = $false,
		[Parameter(Mandatory = $false)]
		[Switch]$Flatten,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Boolean]$ContinueOnError = $true,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Boolean]$ContinueFileCopyOnError = $false,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Boolean]$UseRobocopy = $configToolkitUseRobocopy,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[String]$RobocopyAdditionalParams = $null
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

		# Check if Robocopy is on the system
		If ($UseRobocopy) {
			If (Test-Path -Path "$env:SystemRoot\System32\Robocopy.exe" -PathType Leaf) {
				$RobocopyCommand = "$env:SystemRoot\System32\Robocopy.exe"
			} Else {
				$UseRobocopy = $false
				Write-Log 'Robocopy is not available on this system. Falling back to native PowerShell method.' -Source ${CmdletName} -Severity 2
			}
		} Else {
			$UseRobocopy = $false
		}
	}
	Process {
		Foreach ($srcPath in $Path) {
			$UseRobocopyThis = $UseRobocopy
			If ($UseRobocopyThis) {
				Try {
					# Disable Robocopy if $Path has a folder containing a * wildcard
					If ($srcPath -match '\*.*\\') {
						$UseRobocopyThis = $false
						Write-Log 'Asterisk wildcard specified in folder portion of path variable. Falling back to native PowerShell method.' -Source ${CmdletName} -Severity 2
					}
					If ([IO.Path]::HasExtension($Destination) -and [IO.Path]::GetFileNameWithoutExtension($Destination) -and -not (Test-Path -LiteralPath $Destination -PathType Container)) {
						$UseRobocopyThis = $false
						Write-Log 'Destination path appears to be a file. Falling back to native PowerShell method.' -Source ${CmdletName} -Severity 2

					}
					If ($UseRobocopyThis) {
						# Robocopy arguments: NJH = No Job Header; NJS = No Job Summary; NS = No Size; NC = No Class; NP = No Progress; NDL = No Directory List; FP = Full Path; IS = Include Same; XX = Exclude Extra; MT = Number of Threads; R = Number of Retries; W = Wait time between retries in sconds
						$RobocopyParams = '/NJH /NJS /NS /NC /NP /NDL /FP /IS /XX /MT:4 /R:1 /W:1'

						If (Test-Path -LiteralPath $srcPath -PathType Container) {
							# If source exists as a folder, append the last subfolder to the destination, so that Robocopy produces similar results to native Powershell
							# Trim ending backslash from paths which can cause problems
							$RobocopySource = $srcPath.TrimEnd('\')
							$RobocopyDestination = Join-Path $Destination (Split-Path -Path $srcPath -Leaf)
							$RobocopyFile = '*'
						} Else {
							# Else assume source is a file and split args to the format <SourceFolder> <DestinationFolder> <FileName>
							$RobocopySource = (Split-Path -Path $srcPath -Parent)
							$RobocopyDestination = $Destination.TrimEnd('\')
							$RobocopyFile = (Split-Path -Path $srcPath -Leaf)
						}
						If ($Flatten) {
							Write-Log -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination] root folder, flattened." -Source ${CmdletName}
							[Hashtable]$CopyFileSplat = @{
								Path                    = (Join-Path $RobocopySource $RobocopyFile) # This will ensure that the source dir will have \* appended if it was a folder (which prevents creation of a folder at the destination), or keeps the original file name if it was a file
								Destination             = $Destination # Use the original destination path, not $RobocopyDestination which could have had a subfolder appended to it
								Recurse                 = $false # Disable recursion as this will create subfolders in the destination
								Flatten                 = $false # Disable flattening to prevent infinite loops
								ContinueOnError         = $ContinueOnError
								ContinueFileCopyOnError = $ContinueFileCopyOnError
								UseRobocopy             = $UseRobocopy
							}
							if ($RobocopyAdditionalParams) {
								#Ensure that /E is not included in the additional parameters as it will copy recursive folders
								$CopyFileSplat.RobocopyAdditionalParams = $RobocopyAdditionalParams -replace '/E(\s|$)'
							}
							# Copy all files from the root source folder
							Copy-File @CopyFileSplat
							# Copy all files from subfolders
							Get-ChildItem -Path $RobocopySource -Directory -Recurse -Force -ErrorAction 'SilentlyContinue' | ForEach-Object {
								# Append file name to subfolder path and repeat Copy-File
								$CopyFileSplat.Path = Join-Path $_.FullName $RobocopyFile
								Copy-File @CopyFileSplat
							}
							# Skip to next $SrcPath in $Path since we have handed off all copy tasks to separate executions of the function
							Continue
						}
						If ($Recurse) {
							if ($RobocopyParams -notmatch '/E(\s|$)') {
								$RobocopyParams = $RobocopyParams + ' /E'
							}
							Write-Log -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination]." -Source ${CmdletName}
						} Else {
							Write-Log -Message "Copying file(s) in path [$srcPath] to destination [$Destination]." -Source ${CmdletName}
						}
						If (![String]::IsNullOrEmpty($RobocopyAdditionalParams)) {
							$RobocopyParams = "$RobocopyParams $RobocopyAdditionalParams"
						}
						$RobocopyArgs = "$RobocopyParams `"$RobocopySource`" `"$RobocopyDestination`" `"$RobocopyFile`""
						Write-Log -Message "Executing Robocopy command: $RobocopyCommand $RobocopyArgs" -Source ${CmdletName}
						$RobocopyResult = Execute-Process -Path $RobocopyCommand -Parameters $RobocopyArgs -CreateNoWindow -ContinueOnError $true -ExitOnProcessFailure $false -Passthru -IgnoreExitCodes '0,1,2,3,4,5,6,7,8'
						# Trim the leading whitespace from each line of Robocopy output, ignore the last empty line, and join the lines back together
						$RobocopyOutput = ($RobocopyResult.StdOut.Split("`n").TrimStart() | Select-Object -SkipLast 1) -join "`n"
						Write-Log -Message "Robocopy output:`n$RobocopyOutput" -Source ${CmdletName}

						Switch ($RobocopyResult.ExitCode) {
							0 { Write-Log -Message 'Robocopy completed. No files were copied. No failure was encountered. No files were mismatched. The files already exist in the destination directory; therefore, the copy operation was skipped.' -Source ${CmdletName} }
							1 { Write-Log -Message 'Robocopy completed. All files were copied successfully.' -Source ${CmdletName} }
							2 { Write-Log -Message "Robocopy completed. There are some additional files in the destination directory that aren't present in the source directory. No files were copied." -Source ${CmdletName} }
							3 { Write-Log -Message 'Robocopy completed. Some files were copied. Additional files were present. No failure was encountered.' -Source ${CmdletName} }
							4 { Write-Log -Message 'Robocopy completed. Some Mismatched files or directories were detected. Examine the output log. Housekeeping might be required.' -Severity 2 -Source ${CmdletName} }
							5 { Write-Log -Message 'Robocopy completed. Some files were copied. Some files were mismatched. No failure was encountered.' -Source ${CmdletName} }
							6 { Write-Log -Message 'Robocopy completed. Additional files and mismatched files exist. No files were copied and no failures were encountered meaning that the files already exist in the destination directory.' -Severity 2 -Source ${CmdletName} }
							7 { Write-Log -Message 'Robocopy completed. Files were copied, a file mismatch was present, and additional files were present.' -Severity 2 -Source ${CmdletName} }
							8 { Write-Log -Message "Robocopy completed. Several files didn't copy." -Severity 2 -Source ${CmdletName} }
							16 {
								Write-Log -Message 'Serious error. Robocopy did not copy any files. Either a usage error or an error due to insufficient access privileges on the source or destination directories..' -Severity 3 -Source ${CmdletName}
								If (-not $ContinueOnError) {
									Throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
								}
							}
							default {
								Write-Log -Message "Failed to copy file(s) in path [$srcPath] to destination [$Destination]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
								If (-not $ContinueOnError) {
									Throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
								}
							}
						}
					}
				} Catch {
					Write-Log -Message "Failed to copy file(s) in path [$srcPath] to destination [$Destination]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
					If (-not $ContinueOnError) {
						Throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
					}
				}
			}
			If ($UseRobocopyThis -eq $false) {
				Try {
					# If destination has no extension, or if it has an extension only and no name (e.g. a .config folder) and the destination folder does not exist
					If ((-not ([IO.Path]::HasExtension($Destination))) -or ([IO.Path]::HasExtension($Destination) -and -not [IO.Path]::GetFileNameWithoutExtension($Destination)) -and (-not (Test-Path -LiteralPath $Destination -PathType 'Container'))) {
						Write-Log -Message "Destination assumed to be a folder which does not exist, creating destination folder [$Destination]." -Source ${CmdletName}
						$null = New-Item -Path $Destination -Type 'Directory' -Force -ErrorAction 'Stop'
					}
					# If destination appears to be a file name but parent folder does not exist, create it
					$DestinationParent = Split-Path $Destination -Parent
					If ([IO.Path]::HasExtension($Destination) -and [IO.Path]::GetFileNameWithoutExtension($Destination) -and -not (Test-Path -LiteralPath $DestinationParent -PathType 'Container')) {
						Write-Log -Message "Destination assumed to be a file whose parent folder does not exist, creating destination folder [$DestinationParent]." -Source ${CmdletName}
						$null = New-Item -Path $DestinationParent -Type 'Directory' -Force -ErrorAction 'Stop'
					}
					If ($Flatten) {
						Write-Log -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination] root folder, flattened." -Source ${CmdletName}
						If ($ContinueFileCopyOnError) {
							$null = Get-ChildItem -Path $srcPath -File -Recurse -Force -ErrorAction 'SilentlyContinue' | ForEach-Object {
								Copy-Item -Path ($_.FullName) -Destination $Destination -Force -ErrorAction 'SilentlyContinue' -ErrorVariable 'FileCopyError'
							}
						} Else {
							$null = Get-ChildItem -Path $srcPath -File -Recurse -Force -ErrorAction 'SilentlyContinue' | ForEach-Object {
								Copy-Item -Path ($_.FullName) -Destination $Destination -Force -ErrorAction 'Stop'
							}
						}
					} ElseIf ($Recurse) {
						Write-Log -Message "Copying file(s) recursively in path [$srcPath] to destination [$Destination]." -Source ${CmdletName}
						If ($ContinueFileCopyOnError) {
							$null = Copy-Item -Path $srcPath -Destination $Destination -Force -Recurse -ErrorAction 'SilentlyContinue' -ErrorVariable 'FileCopyError'
						} Else {
							$null = Copy-Item -Path $srcPath -Destination $Destination -Force -Recurse -ErrorAction 'Stop'
						}
					} Else {
						Write-Log -Message "Copying file in path [$srcPath] to destination [$Destination]." -Source ${CmdletName}
						If ($ContinueFileCopyOnError) {
							$null = Copy-Item -Path $srcPath -Destination $Destination -Force -ErrorAction 'SilentlyContinue' -ErrorVariable 'FileCopyError'
						} Else {
							$null = Copy-Item -Path $srcPath -Destination $Destination -Force -ErrorAction 'Stop'
						}
					}

					If ($FileCopyError) {
						Write-Log -Message "The following warnings were detected while copying file(s) in path [$srcPath] to destination [$Destination]. `r`n$FileCopyError" -Severity 2 -Source ${CmdletName}
					} Else {
						Write-Log -Message 'File copy completed successfully.' -Source ${CmdletName}
					}
				} Catch {
					Write-Log -Message "Failed to copy file(s) in path [$srcPath] to destination [$Destination]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
					If (-not $ContinueOnError) {
						Throw "Failed to copy file(s) in path [$srcPath] to destination [$Destination]: $($_.Exception.Message)"
					}
				}
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

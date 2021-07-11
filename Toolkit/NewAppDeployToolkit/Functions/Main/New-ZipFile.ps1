#region Function New-ZipFile
Function New-ZipFile {
<#
.SYNOPSIS
	Create a new zip archive or add content to an existing archive.
.DESCRIPTION
	Create a new zip archive or add content to an existing archive by using the Shell object .CopyHere method.
.PARAMETER DestinationArchiveDirectoryPath
	The path to the directory path where the zip archive will be saved.
.PARAMETER DestinationArchiveFileName
	The name of the zip archive.
.PARAMETER SourceDirectoryPath
	The path to the directory to be archived, specified as absolute paths.
.PARAMETER SourceFilePath
	The path to the file to be archived, specified as absolute paths.
.PARAMETER RemoveSourceAfterArchiving
	Remove the source path after successfully archiving the content. Default is: $false.
.PARAMETER OverWriteArchive
	Overwrite the destination archive path if it already exists. Default is: $false.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default: $true.
.EXAMPLE
	New-ZipFile -DestinationArchiveDirectoryPath 'E:\Testing' -DestinationArchiveFileName 'TestingLogs.zip' -SourceDirectory 'E:\Testing\Logs'
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding(DefaultParameterSetName='CreateFromDirectory')]
	Param (
		[Parameter(Mandatory=$true,Position=0)]
		[ValidateNotNullorEmpty()]
		[string]$DestinationArchiveDirectoryPath,
		[Parameter(Mandatory=$true,Position=1)]
		[ValidateNotNullorEmpty()]
		[string]$DestinationArchiveFileName,
		[Parameter(Mandatory=$true,Position=2,ParameterSetName='CreateFromDirectory')]
		[ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Container' })]
		[string[]]$SourceDirectoryPath,
		[Parameter(Mandatory=$true,Position=2,ParameterSetName='CreateFromFile')]
		[ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
		[string[]]$SourceFilePath,
		[Parameter(Mandatory=$false,Position=3)]
		[ValidateNotNullorEmpty()]
		[switch]$RemoveSourceAfterArchiving = $false,
		[Parameter(Mandatory=$false,Position=4)]
		[ValidateNotNullorEmpty()]
		[switch]$OverWriteArchive = $false,
		[Parameter(Mandatory=$false,Position=5)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			## Remove invalid characters from the supplied filename
			$DestinationArchiveFileName = Remove-InvalidFileNameChars -Name $DestinationArchiveFileName
			If ($DestinationArchiveFileName.length -eq 0) {
				Throw "Invalid filename characters replacement resulted into an empty string."
			}
			## Get the full destination path where the archive will be stored
			[string]$DestinationPath = Join-Path -Path $DestinationArchiveDirectoryPath -ChildPath $DestinationArchiveFileName -ErrorAction 'Stop'
			Write-Log -Message "Creating a zip archive with the requested content at destination path [$DestinationPath]." -Source ${CmdletName}

			## If the destination archive already exists, delete it if the -OverWriteArchive option was selected
			If (($OverWriteArchive) -and (Test-Path -LiteralPath $DestinationPath)) {
				Write-Log -Message "An archive at the destination path already exists, deleting file [$DestinationPath]." -Source ${CmdletName}
				$null = Remove-Item -LiteralPath $DestinationPath -Force -ErrorAction 'Stop'
			}

			## If archive file does not exist, then create a zero-byte zip archive
			If (-not (Test-Path -LiteralPath $DestinationPath)) {
				## Create a zero-byte file
				Write-Log -Message "Creating a zero-byte file [$DestinationPath]." -Source ${CmdletName}
				$null = New-Item -Path $DestinationArchiveDirectoryPath -Name $DestinationArchiveFileName -ItemType 'File' -Force -ErrorAction 'Stop'

				## Write the file header for a zip file to the zero-byte file
				[byte[]]$ZipArchiveByteHeader = 80, 75, 5, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
				[IO.FileStream]$FileStream = New-Object -TypeName 'System.IO.FileStream' -ArgumentList ($DestinationPath, ([IO.FileMode]::Create))
				[IO.BinaryWriter]$BinaryWriter = New-Object -TypeName 'System.IO.BinaryWriter' -ArgumentList ($FileStream)
				Write-Log -Message "Write the file header for a zip archive to the zero-byte file [$DestinationPath]." -Source ${CmdletName}
				$null = $BinaryWriter.Write($ZipArchiveByteHeader)
				$BinaryWriter.Close()
				$FileStream.Close()
			}

			## Create a Shell object
			[__comobject]$ShellApp = New-Object -ComObject 'Shell.Application' -ErrorAction 'Stop'
			## Create an object representing the archive file
			[__comobject]$Archive = $ShellApp.NameSpace($DestinationPath)

			## Create the archive file
			If ($PSCmdlet.ParameterSetName -eq 'CreateFromDirectory') {
				## Create the archive file from a source directory
				ForEach ($Directory in $SourceDirectoryPath) {
					Try {
						#  Create an object representing the source directory
						[__comobject]$CreateFromDirectory = $ShellApp.NameSpace($Directory)
						#  Copy all of the files and folders from the source directory to the archive
						$null = $Archive.CopyHere($CreateFromDirectory.Items())
						#  Wait for archive operation to complete. Archive file count property returns 0 if archive operation is in progress.
						Write-Log -Message "Compressing [$($CreateFromDirectory.Count)] file(s) in source directory [$Directory] to destination path [$DestinationPath]..." -Source ${CmdletName}
						Do { Start-Sleep -Milliseconds 250 } While ($Archive.Items().Count -eq 0)
					}
					Finally {
						#  Release the ComObject representing the source directory
						$null = [Runtime.Interopservices.Marshal]::ReleaseComObject($CreateFromDirectory)
					}

					#  If option was selected, recursively delete the source directory after successfully archiving the contents
					If ($RemoveSourceAfterArchiving) {
						Try {
							Write-Log -Message "Recursively deleting the source directory [$Directory] as contents have been successfully archived." -Source ${CmdletName}
							$null = Remove-Item -LiteralPath $Directory -Recurse -Force -ErrorAction 'Stop'
						}
						Catch {
							Write-Log -Message "Failed to recursively delete the source directory [$Directory]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
						}
					}
				}
			}
			Else {
				## Create the archive file from a list of one or more files
				[IO.FileInfo[]]$SourceFilePath = [IO.FileInfo[]]$SourceFilePath
				ForEach ($File in $SourceFilePath) {
					#  Copy the files and folders from the source directory to the archive
					$null = $Archive.CopyHere($File.FullName)
					#  Wait for archive operation to complete. Archive file count property returns 0 if archive operation is in progress.
					Write-Log -Message "Compressing file [$($File.FullName)] to destination path [$DestinationPath]..." -Source ${CmdletName}
					Do { Start-Sleep -Milliseconds 250 } While ($Archive.Items().Count -eq 0)

					#  If option was selected, delete the source file after successfully archiving the content
					If ($RemoveSourceAfterArchiving) {
						Try {
							Write-Log -Message "Deleting the source file [$($File.FullName)] as it has been successfully archived." -Source ${CmdletName}
							$null = Remove-Item -LiteralPath $File.FullName -Force -ErrorAction 'Stop'
						}
						Catch {
							Write-Log -Message "Failed to delete the source file [$($File.FullName)]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
						}
					}
				}
			}

			## If the archive was created in session 0 or by an Admin, then it may only be readable by elevated users.
			#  Apply the parent folder's permissions to the archive file to fix the problem.
			Write-Log -Message "If the archive was created in session 0 or by an Admin, then it may only be readable by elevated users. Apply permissions from parent folder [$DestinationArchiveDirectoryPath] to file [$DestinationPath]." -Source ${CmdletName}
			Try {
				[Security.AccessControl.DirectorySecurity]$DestinationArchiveDirectoryPathAcl = Get-Acl -Path $DestinationArchiveDirectoryPath -ErrorAction 'Stop'
				Set-Acl -Path $DestinationPath -AclObject $DestinationArchiveDirectoryPathAcl -ErrorAction 'Stop'
			}
			Catch {
				Write-Log -Message "Failed to apply parent folder's [$DestinationArchiveDirectoryPath] permissions to file [$DestinationPath]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
			}
		}
		Catch {
			Write-Log -Message "Failed to archive the requested file(s). `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to archive the requested file(s): $($_.Exception.Message)"
			}
		}
		Finally {
			## Release the ComObject representing the archive
			If ($Archive) { $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Archive) }
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion

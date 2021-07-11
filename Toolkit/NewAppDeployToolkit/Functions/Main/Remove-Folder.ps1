#region Function Remove-Folder
Function Remove-Folder {
<#
.SYNOPSIS
	Remove folder and files if they exist.
.DESCRIPTION
	Remove folder and all files with or without recursion in a given path.
.PARAMETER Path
	Path to the folder to remove.
.PARAMETER DisableRecursion
	Disables recursion while deleting.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Remove-Folder -Path "$envWinDir\Downloaded Program Files"
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Path,
		[Parameter(Mandatory=$false)]
		[switch]$DisableRecursion,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullOrEmpty()]
		[boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
			If (Test-Path -LiteralPath $Path -PathType 'Container' -ErrorAction SilentlyContinue) {
				Try {
					If ($DisableRecursion) {
						Write-Log -Message "Deleting folder [$path] without recursion..." -Source ${CmdletName}
						# Without recursion we have to go through the subfolder ourselves because Remove-Item asks for confirmation if we are trying to delete a non-empty folder without -Recurse
						[array]$ListOfChildItems = Get-ChildItem -LiteralPath $Path -Force
						If ($ListOfChildItems) {
							$SubfoldersSkipped = 0
							foreach ($item in $ListOfChildItems) {
								# Check whether this item is a folder
								If (Test-Path -LiteralPath $item.FullName -PathType Container) {
									# Item is a folder. Check if its empty
									# Get list of child items in the folder
									[array]$ItemChildItems = Get-ChildItem -LiteralPath $item.FullName -Force -ErrorAction SilentlyContinue -ErrorVariable '+ErrorRemoveFolder'
									If ($ItemChildItems.Count -eq 0) {
										# The folder is empty, delete it
										Remove-Item -LiteralPath $item.FullName -Force -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorRemoveFolder'
									} else {
										# Folder is not empty, skip it
										$SubfoldersSkipped++
										continue
									}
								} else {
									# Item is a file. Delete it
									Remove-Item -LiteralPath $item.FullName -Force -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorRemoveFolder'
								}
							}
							if ($SubfoldersSkipped -gt 0) {
								Throw "[$SubfoldersSkipped] subfolders are not empty!"
							}
						} Else {
							Remove-Item -LiteralPath $Path -Force -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorRemoveFolder'
						}
					} else {
						Write-Log -Message "Deleting folder [$path] recursively..." -Source ${CmdletName}
						Remove-Item -LiteralPath $Path -Force -Recurse -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorRemoveFolder'
					}

					If ($ErrorRemoveFolder) {
						Throw $ErrorRemoveFolder
					}
				}
				Catch {
					Write-Log -Message "Failed to delete folder(s) and file(s) from path [$path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
					If (-not $ContinueOnError) {
						Throw "Failed to delete folder(s) and file(s) from path [$path]: $($_.Exception.Message)"
					}
				}
			}
			Else {
				Write-Log -Message "Folder [$Path] does not exist." -Source ${CmdletName}
			}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion

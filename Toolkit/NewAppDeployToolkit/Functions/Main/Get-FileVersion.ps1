#region Function Get-FileVersion
Function Get-FileVersion {
<#
.SYNOPSIS
	Gets the version of the specified file
.DESCRIPTION
	Gets the version of the specified file
.PARAMETER File
	Path of the file
.PARAMETER ProductVersion
	Switch that makes the command return ProductVersion instead of FileVersion
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Get-FileVersion -File "$envProgramFilesX86\Adobe\Reader 11.0\Reader\AcroRd32.exe"
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$File,
		[Parameter(Mandatory=$false)]
		[switch]$ProductVersion,
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
		Try {
			Write-Log -Message "Getting version info for file [$file]." -Source ${CmdletName}

			If (Test-Path -LiteralPath $File -PathType 'Leaf') {
				$fileVersionInfo = (Get-Command -Name $file -ErrorAction 'Stop').FileVersionInfo
				If ($ProductVersion) {
					$fileVersion = $fileVersionInfo.ProductVersion
				} else {
					$fileVersion = $fileVersionInfo.FileVersion
				}

				If ($fileVersion) {
					If ($ProductVersion) {
						Write-Log -Message "Product version is [$fileVersion]." -Source ${CmdletName}
					}
					else
					{
						Write-Log -Message "File version is [$fileVersion]." -Source ${CmdletName}
					}

					Write-Output -InputObject $fileVersion
				}
				Else {
					Write-Log -Message 'No version information found.' -Source ${CmdletName}
				}
			}
			Else {
				Throw "File path [$file] does not exist."
			}
		}
		Catch {
			Write-Log -Message "Failed to get version info. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to get version info: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion

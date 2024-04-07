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

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the version of the specified file.

.EXAMPLE

Get-FileVersion -File "$envProgramFilesX86\Adobe\Reader 11.0\Reader\AcroRd32.exe"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true)]
		[ValidateNotNullorEmpty()]
		[String]$File,
		[Parameter(Mandatory = $false)]
		[Switch]$ProductVersion,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message "Getting version info for file [$file]." -Source ${CmdletName}

			If (Test-Path -LiteralPath $File -PathType 'Leaf') {
				$fileVersionInfo = (Get-Command -Name $file -ErrorAction 'Stop').FileVersionInfo
				If ($ProductVersion) {
					$fileVersion = $fileVersionInfo.ProductVersion
				} Else {
					$fileVersion = $fileVersionInfo.FileVersion
				}

				If ($fileVersion) {
					If ($ProductVersion) {
						Write-Log -Message "Product version is [$fileVersion]." -Source ${CmdletName}
					} Else {
						Write-Log -Message "File version is [$fileVersion]." -Source ${CmdletName}
					}

					Write-Output -InputObject ($fileVersion)
				} Else {
					Write-Log -Message 'No version information found.' -Source ${CmdletName}
				}
			} Else {
				Throw "File path [$file] does not exist."
			}
		} Catch {
			Write-Log -Message "Failed to get version info. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to get version info: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

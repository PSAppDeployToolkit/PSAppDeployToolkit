#region Function Install-MSUpdates
Function Install-MSUpdates {
<#
.SYNOPSIS
	Install all Microsoft Updates in a given directory.
.DESCRIPTION
	Install all Microsoft Updates of type ".exe", ".msu", or ".msp" in a given directory (recursively search directory).
.PARAMETER Directory
	Directory containing the updates.
.EXAMPLE
	Install-MSUpdates -Directory "$dirFiles\MSUpdates"
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$Directory
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Write-Log -Message "Recursively installing all Microsoft Updates in directory [$Directory]." -Source ${CmdletName}

		## KB Number pattern match
		$kbPattern = '(?i)kb\d{6,8}'

		## Get all hotfixes and install if required
		[IO.FileInfo[]]$files = Get-ChildItem -LiteralPath $Directory -Recurse -Include ('*.exe','*.msu','*.msp')
		ForEach ($file in $files) {
			If ($file.Name -match 'redist') {
				[version]$redistVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName).ProductVersion
				[string]$redistDescription = [Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName).FileDescription

				Write-Log -Message "Installing [$redistDescription $redistVersion]..." -Source ${CmdletName}
				#  Handle older redistributables (ie, VC++ 2005)
				If ($redistDescription -match 'Win32 Cabinet Self-Extractor') {
					Execute-Process -Path $file.FullName -Parameters '/q' -WindowStyle 'Hidden' -IgnoreExitCodes "*"
				}
				Else {
					Execute-Process -Path $file.FullName -Parameters '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes "*"
				}
			}
			Else {
				#  Get the KB number of the file
				[string]$kbNumber = [regex]::Match($file.Name, $kbPattern).ToString()
				If (-not $kbNumber) { Continue }

				#  Check to see whether the KB is already installed
				If (-not (Test-MSUpdates -KBNumber $kbNumber)) {
					Write-Log -Message "KB Number [$KBNumber] was not detected and will be installed." -Source ${CmdletName}
					Switch ($file.Extension) {
						#  Installation type for executables (i.e., Microsoft Office Updates)
						'.exe' { Execute-Process -Path $file.FullName -Parameters '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes "*" }
						#  Installation type for Windows updates using Windows Update Standalone Installer
						'.msu' { Execute-Process -Path $exeWusa -Parameters "`"$($file.FullName)`" /quiet /norestart" -WindowStyle 'Hidden' -IgnoreExitCodes "*" }
						#  Installation type for Windows Installer Patch
						'.msp' { Execute-MSI -Action 'Patch' -Path $file.FullName -IgnoreExitCodes "*" }
					}
				}
				Else {
					Write-Log -Message "KB Number [$kbNumber] is already installed. Continue..." -Source ${CmdletName}
				}
			}
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion

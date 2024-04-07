Function Copy-ContentToCache {
	<#
.SYNOPSIS
    Copies the toolkit content to a cache folder on the local machine and sets the $dirFiles and $supportFiles directory to the cache path
.DESCRIPTION
    Copies the toolkit content to a cache folder on the local machine and sets the $dirFiles and $supportFiles directory to the cache path
.PARAMETER Path
    The path to the software cache folder
.EXAMPLE
    Copy-ContentToCache -Path 'C:\Windows\Temp\PSAppDeployToolkit'
.NOTES
    This function is provided as a template to copy the toolkit content to a cache folder on the local machine and set the $dirFiles directory to the cache path.
    This can be used in the absence of an Endpoint Management solution that provides a managed cache for source files, e.g. Intune is lacking this functionality whereas ConfigMgr includes this functionality.
    Since this cache folder is effectively unmanaged, it is important to cleanup the cache in the uninstall section for the current version and potentially also in the pre-installation section for previous versions.
    This can be done using [Remove-File -Path "$configToolkitCachePath\$installName" -Recurse -ContinueOnError $true]

.LINK
    https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false, Position = 0, HelpMessage = 'The path to the software cache folder')]
		[ValidateNotNullorEmpty()]
		[String]$Path = "$configToolkitCachePath\$installName"
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			## Create the cache folder if it does not exist
			If (-not (Test-Path -LiteralPath $Path -PathType 'Container')) {
				Try {
					Write-Log -Message "Creating cache folder [$Path]." -Source ${CmdletName}
					$null = New-Item -Path $Path -ItemType 'Directory' -ErrorAction 'Stop'
				} Catch {
					Write-Log -Message "Failed to create cache folder [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
					Throw "Failed to create cache folder [$Path]: $($_.Exception.Message)"
				}
			} Else {
				Write-Log -Message "Cache folder [$Path] already exists." -Source ${CmdletName}
			}

			## Copy the toolkit content to the cache folder
			Write-Log -Message "Copying toolkit content to cache folder [$Path]." -Source ${CmdletName}
			Copy-File -Path (Join-Path $scriptParentPath '*') -Destination $Path -Recurse
			# Set the Files directory to the cache path
			Set-Variable -Name 'dirFiles' -Value "$Path\Files" -Scope 'Script'
			Set-Variable -Name 'dirSupportFiles' -Value "$Path\SupportFiles" -Scope 'Script'
		} Catch {
			Write-Log -Message "Failed to copy toolkit content to cache folder [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			Throw "Failed to copy toolkit content to cache folder [$Path]: $($_.Exception.Message)"
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

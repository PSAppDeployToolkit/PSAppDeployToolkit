Function New-Folder {
	<#
.SYNOPSIS

Create a new folder.

.DESCRIPTION

Create a new folder if it does not exist.

.PARAMETER Path

Path to the new folder to create.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

New-Folder -Path "$envWinDir\System32"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true)]
		[ValidateNotNullorEmpty()]
		[String]$Path,
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
			If (-not (Test-Path -LiteralPath $Path -PathType 'Container')) {
				Write-Log -Message "Creating folder [$Path]." -Source ${CmdletName}
				$null = New-Item -Path $Path -ItemType 'Directory' -ErrorAction 'Stop' -Force
			} Else {
				Write-Log -Message "Folder [$Path] already exists." -Source ${CmdletName}
			}
		} Catch {
			Write-Log -Message "Failed to create folder [$Path]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to create folder [$Path]: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

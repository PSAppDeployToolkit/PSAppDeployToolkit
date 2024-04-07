﻿Function Update-Desktop {
	<#
.SYNOPSIS

Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.

.DESCRIPTION

Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None. This function does not return objects.

.EXAMPLE

Update-Desktop

.NOTES

This function has an alias: Refresh-Desktop

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
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
			Write-Log -Message 'Refreshing the Desktop and the Windows Explorer environment process block.' -Source ${CmdletName}
			[PSADT.Explorer]::RefreshDesktopAndEnvironmentVariables()
		} Catch {
			Write-Log -Message "Failed to refresh the Desktop and the Windows Explorer environment process block. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to refresh the Desktop and the Windows Explorer environment process block: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

Set-Alias -Name 'Refresh-Desktop' -Value 'Update-Desktop' -Scope 'Script' -Force -ErrorAction 'SilentlyContinue'

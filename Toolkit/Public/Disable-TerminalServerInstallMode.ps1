﻿Function Disable-TerminalServerInstallMode {
	<#
.SYNOPSIS

Changes to user install mode for Remote Desktop Session Host/Citrix servers.

.DESCRIPTION

Changes to user install mode for Remote Desktop Session Host/Citrix servers.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Disable-TerminalServerInstallMode

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message 'Changing terminal server into user execute mode...' -Source ${CmdletName}
			$terminalServerResult = & "$envWinDir\System32\change.exe" User /Execute

			If ($global:LastExitCode -ne 1) {
				Throw $terminalServerResult
			}
		} Catch {
			Write-Log -Message "Failed to change terminal server into user execute mode. `r`n$(Resolve-Error) " -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to change terminal server into user execute mode: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

Function Test-NetworkConnection {
	<#
.SYNOPSIS

Tests for an active local network connection, excluding wireless and virtual network adapters.

.DESCRIPTION

Tests for an active local network connection, excluding wireless and virtual network adapters, by querying the Win32_NetworkAdapter WMI class.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if a wired network connection is detected, otherwise returns $false.

.EXAMPLE

Test-NetworkConnection

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Write-Log -Message 'Checking if system is using a wired network connection...' -Source ${CmdletName}

		[PSObject[]]$networkConnected = Get-WmiObject -Class 'Win32_NetworkAdapter' | Where-Object { ($_.NetConnectionStatus -eq 2) -and ($_.NetConnectionID -match 'Local' -or $_.NetConnectionID -match 'Ethernet') -and ($_.NetConnectionID -notmatch 'Wireless') -and ($_.Name -notmatch 'Virtual') } -ErrorAction 'SilentlyContinue'
		[Boolean]$onNetwork = $false
		If ($networkConnected) {
			Write-Log -Message 'Wired network connection found.' -Source ${CmdletName}
			[Boolean]$onNetwork = $true
		} Else {
			Write-Log -Message 'Wired network connection not found.' -Source ${CmdletName}
		}

		Write-Output -InputObject ($onNetwork)
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

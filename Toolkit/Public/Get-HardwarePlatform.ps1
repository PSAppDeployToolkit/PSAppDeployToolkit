﻿Function Get-HardwarePlatform {
	<#
.SYNOPSIS

Retrieves information about the hardware platform (physical or virtual)

.DESCRIPTION

Retrieves information about the hardware platform (physical or virtual)

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the hardware platform (physical or virtual)

.EXAMPLE

Get-HardwarePlatform

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
			Write-Log -Message 'Retrieving hardware platform information.' -Source ${CmdletName}
			$hwBios = Get-WmiObject -Class 'Win32_BIOS' -ErrorAction 'Stop' | Select-Object -Property 'Version', 'SerialNumber'
			$hwMakeModel = Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'Stop' | Select-Object -Property 'Model', 'Manufacturer'

			If ($hwBIOS.Version -match 'VRTUAL') {
				$hwType = 'Virtual:Hyper-V'
			} ElseIf ($hwBIOS.Version -match 'A M I') {
				$hwType = 'Virtual:Virtual PC'
			} ElseIf ($hwBIOS.Version -like '*Xen*') {
				$hwType = 'Virtual:Xen'
			} ElseIf ($hwBIOS.SerialNumber -like '*VMware*') {
				$hwType = 'Virtual:VMWare'
			} ElseIf ($hwBIOS.SerialNumber -like '*Parallels*') {
				$hwType = 'Virtual:Parallels'
			} ElseIf (($hwMakeModel.Manufacturer -like '*Microsoft*') -and ($hwMakeModel.Model -notlike '*Surface*')) {
				$hwType = 'Virtual:Hyper-V'
			} ElseIf ($hwMakeModel.Manufacturer -like '*VMWare*') {
				$hwType = 'Virtual:VMWare'
			} ElseIf ($hwMakeModel.Manufacturer -like '*Parallels*') {
				$hwType = 'Virtual:Parallels'
			} ElseIf ($hwMakeModel.Model -like '*Virtual*') {
				$hwType = 'Virtual'
			} Else {
				$hwType = 'Physical'
			}

			Write-Output -InputObject ($hwType)
		} Catch {
			Write-Log -Message "Failed to retrieve hardware platform information. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to retrieve hardware platform information: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

﻿Function Test-ServiceExists {
	<#
.SYNOPSIS

Check to see if a service exists.

.DESCRIPTION

Check to see if a service exists (using WMI method because Get-Service will generate ErrorRecord if service doesn't exist).

.PARAMETER Name

Specify the name of the service.

Note: Service name can be found by executing "Get-Service | Format-Table -AutoSize -Wrap" or by using the properties screen of a service in services.msc.

.PARAMETER ComputerName

Specify the name of the computer. Default is: the local computer.

.PARAMETER PassThru

Return the WMI service object. To see all the properties use: Test-ServiceExists -Name 'spooler' -PassThru | Get-Member

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Test-ServiceExists -Name 'wuauserv'

.EXAMPLE

Test-ServiceExists -Name 'testservice' -PassThru | Where-Object { $_ } | ForEach-Object { $_.Delete() }

Check if a service exists and then delete it by using the -PassThru parameter.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true)]
		[ValidateNotNullOrEmpty()]
		[String]$Name,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[String]$ComputerName = $env:ComputerName,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Switch]$PassThru,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Boolean]$ContinueOnError = $true
	)
	Begin {
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			$ServiceObject = Get-WmiObject -ComputerName $ComputerName -Class 'Win32_Service' -Filter "Name='$Name'" -ErrorAction 'Stop'
			# If nothing is returned from Win32_Service, check Win32_BaseService
			If (-not $ServiceObject) {
				$ServiceObject = Get-WmiObject -ComputerName $ComputerName -Class 'Win32_BaseService' -Filter "Name='$Name'" -ErrorAction 'Stop'
			}

			If ($ServiceObject) {
				Write-Log -Message "Service [$Name] exists." -Source ${CmdletName}
				If ($PassThru) {
					Write-Output -InputObject ($ServiceObject)
				} Else {
					Write-Output -InputObject ($true)
				}
			} Else {
				Write-Log -Message "Service [$Name] does not exist." -Source ${CmdletName}
				If ($PassThru) {
					Write-Output -InputObject ($ServiceObject)
				} Else {
					Write-Output -InputObject ($false)
				}
			}
		} Catch {
			Write-Log -Message "Failed check to see if service [$Name] exists." -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed check to see if service [$Name] exists: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

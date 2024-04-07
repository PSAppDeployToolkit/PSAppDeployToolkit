Function Test-Battery {
	<#
.SYNOPSIS

Tests whether the local machine is running on AC power or not.

.DESCRIPTION

Tests whether the local machine is running on AC power and returns true/false. For detailed information, use -PassThru option.

.PARAMETER PassThru

Outputs a hashtable containing the following properties:

IsLaptop, IsUsingACPower, ACPowerLineStatus, BatteryChargeStatus, BatteryLifePercent, BatteryLifeRemaining, BatteryFullLifetime

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Hashtable.

Returns a hashtable containing the following properties
- IsLaptop
- IsUsingACPower
- ACPowerLineStatus
- BatteryChargeStatus
- BatteryLifePercent
- BatteryLifeRemaining
- BatteryFullLifetime

.EXAMPLE

Test-Battery

.EXAMPLE

(Test-Battery -PassThru).IsLaptop

Determines if the current system is a laptop or not.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Switch]$PassThru = $false
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

		## Initialize a hashtable to store information about system type and power status
		[Hashtable]$SystemTypePowerStatus = @{ }
	}
	Process {
		Write-Log -Message 'Checking if system is using AC power or if it is running on battery...' -Source ${CmdletName}

		[Windows.Forms.PowerStatus]$PowerStatus = [Windows.Forms.SystemInformation]::PowerStatus

		## Get the system power status. Indicates whether the system is using AC power or if the status is unknown. Possible values:
		#   Offline : The system is not using AC power.
		#   Online  : The system is using AC power.
		#   Unknown : The power status of the system is unknown.
		[String]$PowerLineStatus = $PowerStatus.PowerLineStatus
		$SystemTypePowerStatus.Add('ACPowerLineStatus', $PowerStatus.PowerLineStatus)

		## Get the current battery charge status. Possible values: High, Low, Critical, Charging, NoSystemBattery, Unknown.
		[String]$BatteryChargeStatus = $PowerStatus.BatteryChargeStatus
		$SystemTypePowerStatus.Add('BatteryChargeStatus', $PowerStatus.BatteryChargeStatus)

		## Get the approximate amount, from 0.00 to 1.0, of full battery charge remaining.
		#  This property can report 1.0 when the battery is damaged and Windows can't detect a battery.
		#  Therefore, this property is only indicative of battery charge remaining if 'BatteryChargeStatus' property is not reporting 'NoSystemBattery' or 'Unknown'.
		[Single]$BatteryLifePercent = $PowerStatus.BatteryLifePercent
		If (($BatteryChargeStatus -eq 'NoSystemBattery') -or ($BatteryChargeStatus -eq 'Unknown')) {
			[Single]$BatteryLifePercent = 0.0
		}
		$SystemTypePowerStatus.Add('BatteryLifePercent', $PowerStatus.BatteryLifePercent)

		## The reported approximate number of seconds of battery life remaining. It will report -1 if the remaining life is unknown because the system is on AC power.
		[Int32]$BatteryLifeRemaining = $PowerStatus.BatteryLifeRemaining
		$SystemTypePowerStatus.Add('BatteryLifeRemaining', $PowerStatus.BatteryLifeRemaining)

		## Get the manufacturer reported full charge lifetime of the primary battery power source in seconds.
		#  The reported number of seconds of battery life available when the battery is fully charged, or -1 if it is unknown.
		#  This will only be reported if the battery supports reporting this information. You will most likely get -1, indicating unknown.
		[Int32]$BatteryFullLifetime = $PowerStatus.BatteryFullLifetime
		$SystemTypePowerStatus.Add('BatteryFullLifetime', $PowerStatus.BatteryFullLifetime)

		## Determine if the system is using AC power
		[Boolean]$OnACPower = $false
		Switch ($PowerLineStatus) {
			'Online' {
				Write-Log -Message 'System is using AC power.' -Source ${CmdletName}
				$OnACPower = $true
			}
			'Offline' {
				Write-Log -Message 'System is using battery power.' -Source ${CmdletName}
			}
			'Unknown' {
				If (($BatteryChargeStatus -eq 'NoSystemBattery') -or ($BatteryChargeStatus -eq 'Unknown')) {
					Write-Log -Message "System power status is [$PowerLineStatus] and battery charge status is [$BatteryChargeStatus]. This is most likely due to a damaged battery so we will report system is using AC power." -Source ${CmdletName}
					$OnACPower = $true
				} Else {
					Write-Log -Message "System power status is [$PowerLineStatus] and battery charge status is [$BatteryChargeStatus]. Therefore, we will report system is using battery power." -Source ${CmdletName}
				}
			}
		}
		$SystemTypePowerStatus.Add('IsUsingACPower', $OnACPower)

		## Determine if the system is a laptop
		[Boolean]$IsLaptop = $false
		If (($BatteryChargeStatus -eq 'NoSystemBattery') -or ($BatteryChargeStatus -eq 'Unknown')) {
			$IsLaptop = $false
		} Else {
			$IsLaptop = $true
		}
		#  Chassis Types
		[Int32[]]$ChassisTypes = Get-WmiObject -Class 'Win32_SystemEnclosure' | Where-Object { $_.ChassisTypes } | Select-Object -ExpandProperty 'ChassisTypes'
		Write-Log -Message "The following system chassis types were detected [$($ChassisTypes -join ',')]." -Source ${CmdletName}
		ForEach ($ChassisType in $ChassisTypes) {
			Switch ($ChassisType) {
				9 {
					$IsLaptop = $true
				} # 9=Laptop
				10 {
					$IsLaptop = $true
				} # 10=Notebook
				14 {
					$IsLaptop = $true
				} # 14=Sub Notebook
				3 {
					$IsLaptop = $false
				} # 3=Desktop
			}
		}
		#  Add IsLaptop property to hashtable
		$SystemTypePowerStatus.Add('IsLaptop', $IsLaptop)

		If ($PassThru) {
			Write-Output -InputObject ($SystemTypePowerStatus)
		} Else {
			Write-Output -InputObject ($OnACPower)
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

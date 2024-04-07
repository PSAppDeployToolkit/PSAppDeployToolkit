Function Get-FreeDiskSpace {
	<#
.SYNOPSIS

Retrieves the free disk space in MB on a particular drive (defaults to system drive)

.DESCRIPTION

Retrieves the free disk space in MB on a particular drive (defaults to system drive)

.PARAMETER Drive

Drive to check free disk space on

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Double

Returns the free disk space in MB

.EXAMPLE

Get-FreeDiskSpace -Drive 'C:'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[String]$Drive = $envSystemDrive,
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
			Write-Log -Message "Retrieving free disk space for drive [$Drive]." -Source ${CmdletName}
			$disk = Get-WmiObject -Class 'Win32_LogicalDisk' -Filter "DeviceID='$Drive'" -ErrorAction 'Stop'
			[Double]$freeDiskSpace = [Math]::Round($disk.FreeSpace / 1MB)

			Write-Log -Message "Free disk space for drive [$Drive]: [$freeDiskSpace MB]." -Source ${CmdletName}
			Write-Output -InputObject ($freeDiskSpace)
		} Catch {
			Write-Log -Message "Failed to retrieve free disk space for drive [$Drive]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to retrieve free disk space for drive [$Drive]: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

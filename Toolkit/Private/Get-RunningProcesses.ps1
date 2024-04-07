Function Get-RunningProcesses {
	<#
.SYNOPSIS

Gets the processes that are running from a custom list of process objects and also adds a property called ProcessDescription.

.DESCRIPTION

Gets the processes that are running from a custom list of process objects and also adds a property called ProcessDescription.

.PARAMETER ProcessObjects

Custom object containing the process objects to search for. If not supplied, the function just returns $null

.PARAMETER DisableLogging

Disables function logging

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

Syste.Boolean.

Rettuns $true if the process is running, otherwise $false.

.EXAMPLE

Get-RunningProcesses -ProcessObjects $ProcessObjects

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false, Position = 0)]
		[PSObject[]]$ProcessObjects,
		[Parameter(Mandatory = $false, Position = 1)]
		[Switch]$DisableLogging
	)

	begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}

	process {
		## Confirm input isn't null before proceeding.
		if (!$processObjects -or !$processObjects[0].ProcessName) {
			return
		}
		if (!$DisableLogging) {
			Write-Log -Message "Checking for running applications: [$($processObjects.ProcessName -join ',')]" -Source ${CmdletName}
		}

		## Get all running processes and append properties.
		[Diagnostics.Process[]]$runningProcesses = foreach ($process in (Get-Process -Name $processObjects.ProcessName -ErrorAction SilentlyContinue)) {
			Add-Member -InputObject $process -MemberType NoteProperty -Name ProcessDescription -Force -PassThru -Value $(
				if (![System.String]::IsNullOrWhiteSpace(($objDescription = ($processObjects | Where-Object { $_.ProcessName -eq $process.ProcessName }).ProcessDescription))) {
					# The description of the process provided as a Parameter to the function, e.g. -ProcessName "winword=Microsoft Office Word".
					$objDescription
				} elseif ($process.Description) {
					# If the process already has a description field specified, then use it
					$process.Description
				} else {
					# Fall back on the process name if no description is provided by the process or as a parameter to the function
					$process.ProcessName
				}
			)
		}

		## Return output if there's any.
		if (!$runningProcesses) {
			if (!$DisableLogging) {
				Write-Log -Message 'Specified applications are not running.' -Source ${CmdletName}
			}
			return
		}
		if (!$DisableLogging) {
			Write-Log -Message "The following processes are running: [$(($runningProcesses.ProcessName | Select-Object -Unique) -join ',')]." -Source ${CmdletName}
		}
		return ($runningProcesses | Sort-Object)
	}

	end {
		## Write out the footer
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

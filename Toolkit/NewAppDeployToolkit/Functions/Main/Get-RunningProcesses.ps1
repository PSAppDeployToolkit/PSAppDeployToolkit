#region Function Get-RunningProcesses
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
.EXAMPLE
	Get-RunningProcesses -ProcessObjects $ProcessObjects
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false,Position=0)]
		[psobject[]]$ProcessObjects,
		[Parameter(Mandatory=$false,Position=1)]
		[switch]$DisableLogging
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		If ($processObjects -and $processObjects[0].ProcessName) {
			[string]$runningAppsCheck = $processObjects.ProcessName -join ','
			If (-not($DisableLogging)) {
				Write-Log -Message "Checking for running applications: [$runningAppsCheck]" -Source ${CmdletName}
			}
			## Prepare a filter for Where-Object
			[scriptblock]$whereObjectFilter = {
				ForEach ($processObject in $processObjects) {
					If ($_.ProcessName -ieq $processObject.ProcessName) {
						If ($processObject.ProcessDescription) {
							#  The description of the process provided as a Parameter to the function, e.g. -ProcessName "winword=Microsoft Office Word".
							Add-Member -InputObject $_ -MemberType 'NoteProperty' -Name 'ProcessDescription' -Value $processObject.ProcessDescription -Force -PassThru -ErrorAction 'SilentlyContinue'
						}
						ElseIf ($_.Description) {
							#  If the process already has a description field specified, then use it
							Add-Member -InputObject $_ -MemberType 'NoteProperty' -Name 'ProcessDescription' -Value $_.Description -Force -PassThru -ErrorAction 'SilentlyContinue'
						}
						Else {
							#  Fall back on the process name if no description is provided by the process or as a parameter to the function
							Add-Member -InputObject $_ -MemberType 'NoteProperty' -Name 'ProcessDescription' -Value $_.ProcessName -Force -PassThru -ErrorAction 'SilentlyContinue'
						}
						Write-Output $true
						return;
					}
				}

				Write-Output $false
				return;
			}
			## Get all running processes and escape special characters. Match against the process names to search for to find running processes.
			[Diagnostics.Process[]]$runningProcesses = Get-Process | Where-Object -FilterScript $whereObjectFilter | Sort-Object ProcessName

			If (-not($DisableLogging)) {
				If ($runningProcesses) {
					[string]$runningProcessList = ($runningProcesses.ProcessName | Select-Object -Unique) -join ','
					Write-Log -Message "The following processes are running: [$runningProcessList]." -Source ${CmdletName}
				}
				Else {
					Write-Log -Message 'Specified applications are not running.' -Source ${CmdletName}
				}
			}
			Write-Output -InputObject $runningProcesses
		} Else {
			Write-Output -InputObject $null
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion

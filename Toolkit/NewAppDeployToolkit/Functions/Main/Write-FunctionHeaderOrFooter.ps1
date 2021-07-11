Function Write-FunctionInfo {
<#
.SYNOPSIS
	Write the function header or footer to the log upon first entering or exiting a function.
.DESCRIPTION
	Write the "Function Start" message, the bound parameters the function was invoked with, or the "Function End" message when entering or exiting a function.
	Messages are debug messages so will only be logged if LogDebugMessage option is enabled in XML config file.
.PARAMETER CmdletName
	The name of the function this function is invoked from.
.PARAMETER CmdletBoundParameters
	The bound parameters of the function this function is invoked from.
.PARAMETER Header
	Write the function header.
.PARAMETER Footer
	Write the function footer.
.EXAMPLE
	Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
.EXAMPLE
	Write-FunctionInfo -CmdletName ${CmdletName} -Footer
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(
			Mandatory=$true
		)]
		[ValidateNotNullorEmpty()]
		$CmdletName,

		[Parameter(
			Mandatory=$true,
			ParameterSetName='Header'
		)]
		[AllowEmptyCollection()]
		[hashtable]$CmdletBoundParameters,

		[Parameter(
			Mandatory=$true,
			ParameterSetName='Header'
		)]
		[switch]$Header,

		[Parameter(
			Mandatory=$true,
			ParameterSetName='Footer'
		)]
		[switch]$Footer
	)

	process{
		If ($Header) {
			Write-Log -Message 'Function Start' -Source $CmdletName -DebugMessage
	
			## Get the parameters that the calling function was invoked with
			$CmdletBoundParameters = $CmdletBoundParameters | Format-Table -Property @{
				Label = 'Parameter'
				Expression = {
					"[-$($_.Key)]"
				}
			},

			@{
				Label = 'Value'
				Expression = {
					$_.Value
				}
				Alignment = 'Left'
			},

			@{
				Label = 'Type'
				Expression = {
					$_.Value.GetType().Name
			}
			Alignment = 'Left'
			} -AutoSize -Wrap | Out-String
	
	
			If ($CmdletBoundParameters) {
				Write-Log -Message "Function invoked with bound parameter(s): `r`n$CmdletBoundParameters" -Source $CmdletName -DebugMessage
			}
			Else {
				Write-Log -Message 'Function invoked without any bound parameters.' -Source $CmdletName -DebugMessage
			}

		} ElseIf ($Footer) {
			Write-Log -Message 'Function End' -Source $CmdletName -DebugMessage
		}		
	}
}
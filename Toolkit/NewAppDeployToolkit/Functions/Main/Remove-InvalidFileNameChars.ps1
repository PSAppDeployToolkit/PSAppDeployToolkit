#region Function Remove-InvalidFileNameChars
Function Remove-InvalidFileNameChars {
	<#
	.SYNOPSIS
		Remove invalid characters from the supplied string.
	.DESCRIPTION
		Remove invalid characters from the supplied string and returns a valid filename as a string.
	.PARAMETER Name
		Text to remove invalid filename characters from.
	.EXAMPLE
		Remove-InvalidFileNameChars -Name "Filename/\1"
	.NOTES
		This functions always returns a string however it can be empty if the name only contains invalid characters.
		Do no use this command for an entire path as '\' is not a valid filename character.
	.LINK
		http://psappdeploytoolkit.com
	#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true,ValueFromPipelineByPropertyName=$true)]
		[AllowEmptyString()]
		[string]$Name
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Output -InputObject (([char[]]$Name | Where-Object { $invalidFileNameChars -notcontains $_ }) -join '')
		}
		Catch {
			Write-Log -Message "Failed to remove invalid characters from the supplied filename. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion

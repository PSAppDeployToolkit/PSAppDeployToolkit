Function Remove-InvalidFileNameChars {
	<#
.SYNOPSIS

Remove invalid characters from the supplied string.

.DESCRIPTION

Remove invalid characters from the supplied string and returns a valid filename as a string.

.PARAMETER Name

Text to remove invalid filename characters from.

.INPUTS

System.String

A string containing invalid filename characters.

.OUTPUTS

System.String

Returns the input string with the invalid characters removed.

.EXAMPLE

Remove-InvalidFileNameChars -Name "Filename/\1"

.NOTES

This functions always returns a string however it can be empty if the name only contains invalid characters.
Do no use this command for an entire path as '\' is not a valid filename character.

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
		[AllowEmptyString()]
		[String]$Name
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Output -InputObject (([Char[]]$Name | Where-Object { $invalidFileNameChars -notcontains $_ }) -join '')
		} Catch {
			Write-Log -Message "Failed to remove invalid characters from the supplied filename. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

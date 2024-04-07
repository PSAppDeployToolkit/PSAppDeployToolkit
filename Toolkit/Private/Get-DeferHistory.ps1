Function Get-DeferHistory {
	<#
.SYNOPSIS

Get the history of deferrals from the registry for the current application, if it exists.

.DESCRIPTION

Get the history of deferrals from the registry for the current application, if it exists.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the history of deferrals from the registry for the current application, if it exists.

.EXAMPLE

Get-DeferHistory

.NOTES

This is an internal script function and should typically not be called directly.

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
		Write-Log -Message 'Getting deferral history...' -Source ${CmdletName}
		Get-RegistryKey -Key $regKeyDeferHistory -ContinueOnError $true
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}

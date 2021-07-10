#region Function Get-ObjectProperty
Function Get-ObjectProperty {
<#
.SYNOPSIS
	Get a property from any object.
.DESCRIPTION
	Get a property from any object.
.PARAMETER InputObject
	Specifies an object which has properties that can be retrieved.
.PARAMETER PropertyName
	Specifies the name of a property to retrieve.
.PARAMETER ArgumentList
	Argument to pass to the property being retrieved.
.EXAMPLE
	Get-ObjectProperty -InputObject $Record -PropertyName 'StringData' -ArgumentList @(1)
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,Position=0)]
		[ValidateNotNull()]
		[object]$InputObject,
		[Parameter(Mandatory=$true,Position=1)]
		[ValidateNotNullorEmpty()]
		[string]$PropertyName,
		[Parameter(Mandatory=$false,Position=2)]
		[object[]]$ArgumentList
	)

	Begin { }
	Process {
		## Retrieve property
		Write-Output -InputObject $InputObject.GetType().InvokeMember($PropertyName, [Reflection.BindingFlags]::GetProperty, $null, $InputObject, $ArgumentList, $null, $null, $null)
	}
	End { }
}
#endregion

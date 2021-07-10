#region Function Invoke-ObjectMethod
Function Invoke-ObjectMethod {
<#
.SYNOPSIS
	Invoke method on any object.
.DESCRIPTION
	Invoke method on any object with or without using named parameters.
.PARAMETER InputObject
	Specifies an object which has methods that can be invoked.
.PARAMETER MethodName
	Specifies the name of a method to invoke.
.PARAMETER ArgumentList
	Argument to pass to the method being executed. Allows execution of method without specifying named parameters.
.PARAMETER Parameter
	Argument to pass to the method being executed. Allows execution of method by using named parameters.
.EXAMPLE
	$ShellApp = New-Object -ComObject 'Shell.Application'
	$null = Invoke-ObjectMethod -InputObject $ShellApp -MethodName 'MinimizeAll'
	Minimizes all windows.
.EXAMPLE
	$ShellApp = New-Object -ComObject 'Shell.Application'
	$null = Invoke-ObjectMethod -InputObject $ShellApp -MethodName 'Explore' -Parameter @{'vDir'='C:\Windows'}
	Opens the C:\Windows folder in a Windows Explorer window.
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding(DefaultParameterSetName='Positional')]
	Param (
		[Parameter(Mandatory=$true,Position=0)]
		[ValidateNotNull()]
		[object]$InputObject,
		[Parameter(Mandatory=$true,Position=1)]
		[ValidateNotNullorEmpty()]
		[string]$MethodName,
		[Parameter(Mandatory=$false,Position=2,ParameterSetName='Positional')]
		[object[]]$ArgumentList,
		[Parameter(Mandatory=$true,Position=2,ParameterSetName='Named')]
		[ValidateNotNull()]
		[hashtable]$Parameter
	)

	Begin { }
	Process {
		If ($PSCmdlet.ParameterSetName -eq 'Named') {
			## Invoke method by using parameter names
			Write-Output -InputObject $InputObject.GetType().InvokeMember($MethodName, [Reflection.BindingFlags]::InvokeMethod, $null, $InputObject, ([object[]]($Parameter.Values)), $null, $null, ([string[]]($Parameter.Keys)))
		}
		Else {
			## Invoke method without using parameter names
			Write-Output -InputObject $InputObject.GetType().InvokeMember($MethodName, [Reflection.BindingFlags]::InvokeMethod, $null, $InputObject, $ArgumentList, $null, $null, $null)
		}
	}
	End { }
}
#endregion

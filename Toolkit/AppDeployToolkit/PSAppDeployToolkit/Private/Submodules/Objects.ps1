#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

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

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Object.

The object returned by the method being invoked.

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

https://psappdeploytoolkit.com
#>
    [CmdletBinding(DefaultParameterSetName = 'Positional')]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNull()]
        [Object]$InputObject,
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String]$MethodName,
        [Parameter(Mandatory = $false, Position = 2, ParameterSetName = 'Positional')]
        [Object[]]$ArgumentList,
        [Parameter(Mandatory = $true, Position = 2, ParameterSetName = 'Named')]
        [ValidateNotNull()]
        [Hashtable]$Parameter
    )

    Begin {
    }
    Process {
        If ($PSCmdlet.ParameterSetName -eq 'Named') {
            ## Invoke method by using parameter names
            Write-Output -InputObject ($InputObject.GetType().InvokeMember($MethodName, [Reflection.BindingFlags]::InvokeMethod, $null, $InputObject, ([Object[]]($Parameter.Values)), $null, $null, ([String[]]($Parameter.Keys))))
        }
        Else {
            ## Invoke method without using parameter names
            Write-Output -InputObject ($InputObject.GetType().InvokeMember($MethodName, [Reflection.BindingFlags]::InvokeMethod, $null, $InputObject, $ArgumentList, $null, $null, $null))
        }
    }
    End {
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

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

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Object.

Returns the value of the property being retrieved.

.EXAMPLE

Get-ObjectProperty -InputObject $Record -PropertyName 'StringData' -ArgumentList @(1)

.NOTES

This is an internal script function and should typically not be called directly.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNull()]
        [Object]$InputObject,
        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String]$PropertyName,
        [Parameter(Mandatory = $false, Position = 2)]
        [Object[]]$ArgumentList
    )

    Begin {
    }
    Process {
        ## Retrieve property
        Write-Output -InputObject ($InputObject.GetType().InvokeMember($PropertyName, [Reflection.BindingFlags]::GetProperty, $null, $InputObject, $ArgumentList, $null, $null, $null))
    }
    End {
    }
}

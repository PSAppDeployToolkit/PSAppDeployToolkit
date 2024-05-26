#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Invoke-ADTObjectMethod
{
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
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.Object. The object returned by the method being invoked.

    .EXAMPLE
    $ShellApp = New-Object -ComObject 'Shell.Application'
    $null = Invoke-ADTObjectMethod -InputObject $ShellApp -MethodName 'MinimizeAll'

    Minimizes all windows.

    .EXAMPLE
    $ShellApp = New-Object -ComObject 'Shell.Application'
    $null = Invoke-ADTObjectMethod -InputObject $ShellApp -MethodName 'Explore' -Parameter @{'vDir'='C:\Windows'}

    Opens the C:\Windows folder in a Windows Explorer window.

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding(DefaultParameterSetName = 'Positional')]
    param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.Object]$InputObject,

        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [System.String]$MethodName,

        [Parameter(Mandatory = $false, Position = 2, ParameterSetName = 'Positional')]
        [ValidateNotNullOrEmpty()]
        [System.Object[]]$ArgumentList,

        [Parameter(Mandatory = $true, Position = 2, ParameterSetName = 'Named')]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Parameter
    )

    # Switch on the parameter set name.
    switch ($PSCmdlet.ParameterSetName)
    {
        Named {
            # Invoke method by using parameter names.
            return $InputObject.GetType().InvokeMember($MethodName, [System.Reflection.BindingFlags]::InvokeMethod, $null, $InputObject, [System.Object[]]$Parameter.Values, $null, $null, [System.String[]]$Parameter.Keys)
        }
        Positional {
            # Invoke method without using parameter names.
            return $InputObject.GetType().InvokeMember($MethodName, [System.Reflection.BindingFlags]::InvokeMethod, $null, $InputObject, $ArgumentList, $null, $null, $null)
        }
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTObjectProperty
{
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
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.Object. Returns the value of the property being retrieved.

    .EXAMPLE
    Get-ADTObjectProperty -InputObject $Record -PropertyName 'StringData' -ArgumentList @(1)

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.Object]$InputObject,

        [Parameter(Mandatory = $true, Position = 1)]
        [ValidateNotNullOrEmpty()]
        [System.String]$PropertyName,

        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNullOrEmpty()]
        [System.Object[]]$ArgumentList
    )

    # Retrieve property.
    return $InputObject.GetType().InvokeMember($PropertyName, [Reflection.BindingFlags]::GetProperty, $null, $InputObject, $ArgumentList, $null, $null, $null)
}

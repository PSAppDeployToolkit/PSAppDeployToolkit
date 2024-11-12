#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTObjectMethod
#
#-----------------------------------------------------------------------------

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
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Object

        The object returned by the method being invoked.

    .EXAMPLE
        PS C:\>$ShellApp = New-Object -ComObject 'Shell.Application'
        PS C:\>$null = Invoke-ADTObjectMethod -InputObject $ShellApp -MethodName 'MinimizeAll'

        Minimizes all windows.

    .EXAMPLE
        PS C:\>$ShellApp = New-Object -ComObject 'Shell.Application'
        PS C:\>$null = Invoke-ADTObjectMethod -InputObject $ShellApp -MethodName 'Explore' -Parameter @{'vDir'='C:\Windows'}

        Opens the C:\Windows folder in a Windows Explorer window.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding(DefaultParameterSetName = 'Positional')]
    param
    (
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

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                switch ($PSCmdlet.ParameterSetName)
                {
                    Named
                    {
                        # Invoke method by using parameter names.
                        return $InputObject.GetType().InvokeMember($MethodName, [System.Reflection.BindingFlags]::InvokeMethod, $null, $InputObject, [System.Object[]]$Parameter.Values, $null, $null, [System.String[]]$Parameter.Keys)
                    }
                    Positional
                    {
                        # Invoke method without using parameter names.
                        return $InputObject.GetType().InvokeMember($MethodName, [System.Reflection.BindingFlags]::InvokeMethod, $null, $InputObject, $ArgumentList, $null, $null, $null)
                    }
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

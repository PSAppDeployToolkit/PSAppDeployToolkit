#-----------------------------------------------------------------------------
#
# MARK: Get-ADTObjectProperty
#
#-----------------------------------------------------------------------------

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

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
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
                return $InputObject.GetType().InvokeMember($PropertyName, [Reflection.BindingFlags]::GetProperty, $null, $InputObject, $ArgumentList, $null, $null, $null)
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

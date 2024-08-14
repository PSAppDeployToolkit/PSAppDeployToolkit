#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTUniversalDate
{
    <#

    .SYNOPSIS
    Returns the date/time for the local culture in a universal sortable date time pattern.

    .DESCRIPTION
    Converts the current datetime or a datetime string for the current culture into a universal sortable date time pattern, e.g. 2013-08-22 11:51:52Z

    .PARAMETER DateTime
    Specify the DateTime in the current culture.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the date/time for the local culture in a universal sortable date time pattern.

    .EXAMPLE
    Get-ADTUniversalDate

    Returns the current date in a universal sortable date time pattern.

    .EXAMPLE
    Get-ADTUniversalDate -DateTime '25/08/2013'

    Returns the date for the current culture in a universal sortable date time pattern.

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$DateTime = (Get-Date -Format $Host.CurrentCulture.DateTimeFormat.UniversalDateTimePattern).ToString()
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
                # If a universal sortable date time pattern was provided, remove the Z, otherwise it could get converted to a different time zone.
                $DateTime = [System.DateTime]::Parse($DateTime.TrimEnd('Z'), $Host.CurrentCulture)

                # Convert the date to a universal sortable date time pattern based on the current culture.
                Write-ADTLogEntry -Message "Converting the date [$DateTime] to a universal sortable date time pattern based on the current culture [$($Host.CurrentCulture.Name)]."
                return (Get-Date -Date $DateTime -Format $Host.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern).ToString()
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

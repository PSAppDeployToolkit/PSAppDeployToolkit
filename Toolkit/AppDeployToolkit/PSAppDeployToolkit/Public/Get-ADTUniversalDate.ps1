function Get-ADTUniversalDate
{
    <#

    .SYNOPSIS
    Returns the date/time for the local culture in a universal sortable date time pattern.

    .DESCRIPTION
    Converts the current datetime or a datetime string for the current culture into a universal sortable date time pattern, e.g. 2013-08-22 11:51:52Z

    .PARAMETER DateTime
    Specify the DateTime in the current culture.

    .PARAMETER ContinueOnError
    Continue if an error is encountered. Default: $false.

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

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [ValidateNotNullOrEmpty()]
        [System.String]$DateTime = (Get-Date -Format $Script:ADT.Environment.culture.DateTimeFormat.UniversalDateTimePattern).ToString()
    )

    begin {
        Write-ADTDebugHeader
    }

    process {
        # If a universal sortable date time pattern was provided, remove the Z, otherwise it could get converted to a different time zone.
        [System.DateTime]$DateTime = [System.DateTime]::Parse($DateTime.TrimEnd('Z'), $Script:ADT.Environment.culture)

        # Convert the date to a universal sortable date time pattern based on the current culture.
        Write-ADTLogEntry -Message "Converting the date [$DateTime] to a universal sortable date time pattern based on the current culture [$($Script:ADT.Environment.culture.Name)]."
        return (Get-Date -Date $DateTime -Format $Script:ADT.Environment.culture.DateTimeFormat.UniversalSortableDateTimePattern).ToString()
    }

    end {
        Write-ADTDebugFooter
    }
}

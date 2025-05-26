#-----------------------------------------------------------------------------
#
# MARK: Get-ADTUniversalDate
#
#-----------------------------------------------------------------------------

function Get-ADTUniversalDate
{
    <#
    .SYNOPSIS
        Returns the date/time for the local culture in a universal sortable date time pattern.

    .DESCRIPTION
        Converts the current datetime or a datetime string for the current culture into a universal sortable date time pattern, e.g. 2013-08-22 11:51:52Z.

    .PARAMETER DateTime
        Specify the DateTime in the current culture.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns the date/time for the local culture in a universal sortable date time pattern.

    .EXAMPLE
        Get-ADTUniversalDate

        Returns the current date in a universal sortable date time pattern.

    .EXAMPLE
        Get-ADTUniversalDate -DateTime '25/08/2013'

        Returns the date for the current culture in a universal sortable date time pattern.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTUniversalDate
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$DateTime = [System.DateTime]::Now.ToString([System.Globalization.DateTimeFormatInfo]::CurrentInfo.UniversalSortableDateTimePattern).TrimEnd('Z')
    )

    begin
    {
        # Announce deprecation to callers.
        Write-ADTLogEntry -Message "The function [$($MyInvocation.MyCommand.Name)] is deprecated and will be removed in PSAppDeployToolkit 4.2.0. Please raise a case at [https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues] if you require this function." -Severity 2
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Convert the date to a universal sortable date time pattern based on the current culture.
                Write-ADTLogEntry -Message "Converting the date [$DateTime] to a universal sortable date time pattern based on the current culture [$($Host.CurrentCulture.Name)]."
                return [System.DateTime]::Parse($DateTime, $Host.CurrentCulture).ToUniversalTime().ToString([System.Globalization.DateTimeFormatInfo]::CurrentInfo.UniversalSortableDateTimePattern)
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "The specified date/time [$DateTime] is not in a format recognized by the current culture [$($Host.CurrentCulture.Name)]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

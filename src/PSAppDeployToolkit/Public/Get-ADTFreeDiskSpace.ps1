#-----------------------------------------------------------------------------
#
# MARK: Get-ADTFreeDiskSpace
#
#-----------------------------------------------------------------------------

function Get-ADTFreeDiskSpace
{
    <#
    .SYNOPSIS
        Retrieves the free disk space in MB on a particular drive (defaults to system drive).

    .DESCRIPTION
        The Get-ADTFreeDiskSpace function retrieves the free disk space in MB on a specified drive. If no drive is specified, it defaults to the system drive. This function is useful for monitoring disk space availability.

    .PARAMETER Drive
        The drive to check free disk space on.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Double

        Returns the free disk space in MB.

    .EXAMPLE
        Get-ADTFreeDiskSpace -Drive 'C:'

        This example retrieves the free disk space on the C: drive.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTFreeDiskSpace
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateScript({
                if (!$_.TotalSize)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Drive -ProvidedValue $_ -ExceptionMessage 'The specified drive does not exist or has no media loaded.'))
                }
                return !!$_.TotalSize
            })]
        [System.IO.DriveInfo]$Drive = [System.IO.Path]::GetPathRoot([System.Environment]::SystemDirectory)
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        Write-ADTLogEntry -Message "Retrieving free disk space for drive [$Drive]."
        $freeDiskSpace = [System.Math]::Round($Drive.AvailableFreeSpace / 1MB)
        Write-ADTLogEntry -Message "Free disk space for drive [$Drive]: [$freeDiskSpace MB]."
        return $freeDiskSpace
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

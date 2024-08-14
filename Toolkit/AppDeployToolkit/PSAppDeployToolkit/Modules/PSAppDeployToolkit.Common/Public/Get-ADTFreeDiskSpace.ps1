function Get-ADTFreeDiskSpace
{
    <#

    .SYNOPSIS
    Retrieves the free disk space in MB on a particular drive (defaults to system drive)

    .DESCRIPTION
    Retrieves the free disk space in MB on a particular drive (defaults to system drive)

    .PARAMETER Drive
    Drive to check free disk space on

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.Double. Returns the free disk space in MB

    .EXAMPLE
    Get-ADTFreeDiskSpace -Drive 'C:'

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateScript({
            if (!$_.TotalSize)
            {
                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Drive -ProvidedValue $_ -ExceptionMessage 'The specified drive does not exist or has no media loaded.'))
            }
            return !!$_.TotalSize
        })]
        [System.IO.DriveInfo]$Drive = $env:SystemDrive
    )

    begin {
        Write-ADTDebugHeader
    }

    process {
        Write-ADTLogEntry -Message "Retrieving free disk space for drive [$Drive]."
        $freeDiskSpace = [System.Math]::Round($Drive.AvailableFreeSpace / 1MB)
        Write-ADTLogEntry -Message "Free disk space for drive [$Drive]: [$freeDiskSpace MB]."
        return $freeDiskSpace
    }

    end {
        Write-ADTDebugFooter
    }
}

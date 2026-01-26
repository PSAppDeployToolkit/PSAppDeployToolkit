#-----------------------------------------------------------------------------
#
# MARK: New-ADTLogFileName
#
#-----------------------------------------------------------------------------

function New-ADTLogFileName
{
    <#
    .SYNOPSIS
        Generates a new log file name based off the current deployment session's properties.

    .DESCRIPTION
        Generates a new log file name based off the current deployment session's properties, using the same default format that PSAppDeployTookit uses itself.

    .PARAMETER Discriminator
        The identifier to pre-format the log file name with.

    .PARAMETER FileNameOnly
        If specified, just returns a log file name without and preceeding path.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns a pre-formatted string with the specified discriminator.

    .EXAMPLE
        New-ADTLogFileName -Discriminator Setup

        This example returns a pre-formatted string that can be used as a log file name.

    .NOTES
        An active ADT session is required to use this function.

        Requires: PSADT session should be initialized using Open-ADTSession

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/New-ADTLogFileName
    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Discriminator,

        [Parameter(Mandatory = $true)]
        [System.Management.Automation.SwitchParameter]$FileNameOnly
    )

    # Generate the new log file name based on the active session information.
    try
    {
        $adtSession = Get-ADTSession
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
    if ($FileNameOnly)
    {
        return $adtSession.NewLogFileName($Discriminator)
    }
    return [System.IO.Path]::Combine($adtSession.LogPath, $adtSession.NewLogFileName($Discriminator))
}

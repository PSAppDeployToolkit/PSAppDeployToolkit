#-----------------------------------------------------------------------------
#
# MARK: Get-ADTSession
#
#-----------------------------------------------------------------------------

function Get-ADTSession
{
    <#
    .SYNOPSIS
        Retrieves the most recent ADT session.

    .DESCRIPTION
        The Get-ADTSession function returns the most recent session from the ADT module data. If no sessions are found, it throws an error indicating that an ADT session should be opened using Open-ADTSession before calling this function.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        ADTSession

        Returns the most recent session object from the ADT module data.

    .EXAMPLE
        Get-ADTSession

        This example retrieves the most recent ADT session.

    .NOTES
        An active ADT session is required to use this function.

        Requires: PSADT session should be initialized using Open-ADTSession

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
    )

    # Return the most recent session in the database.
    if (!($adtData = Get-ADTModuleData).Sessions.Count)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("Please ensure that [Open-ADTSession] is called before using any $($MyInvocation.MyCommand.Module.Name) functions.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ADTSessionBufferEmpty'
            TargetObject = $adtData.Sessions
            RecommendedAction = "Please ensure a session is opened via [Open-ADTSession] and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    return $adtData.Sessions[-1]
}

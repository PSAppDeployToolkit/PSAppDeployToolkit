#-----------------------------------------------------------------------------
#
# MARK: Set-ADTDeferHistory
#
#-----------------------------------------------------------------------------

function Set-ADTDeferHistory
{
    <#
    .SYNOPSIS
        Set the history of deferrals in the registry for the current application.

    .DESCRIPTION
        Set the history of deferrals in the registry for the current application.

    .PARAMETER DeferTimesRemaining
        Specify the number of deferrals remaining.

    .PARAMETER DeferDeadline
        Specify the deadline for the deferral.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Set-DeferHistory

    .NOTES
        An active ADT session is required to use this function.

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
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$DeferTimesRemaining,

        [Parameter(Mandatory = $false)]
        [AllowEmptyString()]
        [System.String]$DeferDeadline
    )

    try
    {
        (Get-ADTSession).SetDeferHistory($(if ($PSBoundParameters.ContainsKey('DeferTimesRemaining')) { $DeferTimesRemaining }), $DeferDeadline)
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

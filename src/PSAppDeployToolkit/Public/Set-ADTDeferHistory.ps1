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

    .PARAMETER DeferRunInterval
        Specifies the time span that must elapse before prompting the user again if a process listed in 'CloseProcesses' is still running after a deferral.

        This helps address the issue where Intune retries installations shortly after a user defers, preventing multiple immediate prompts and improving the user experience.

        This parameter is specifically utilized within the `Show-ADTInstallationWelcome` function, and if specified, the current date and time will be used for the DeferRunIntervalLastTime.

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

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTDeferHistory

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$DeferTimesRemaining,

        [Parameter(Mandatory = $false)]
        [AllowEmptyString()]
        [System.String]$DeferDeadline,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$DeferRunInterval
    )

    try
    {
        if ($PSBoundParameters.ContainsKey('DeferRunInterval'))
        {
            (Get-ADTSession).SetDeferHistory($(if ($PSBoundParameters.ContainsKey('DeferTimesRemaining')) { $DeferTimesRemaining }), $DeferDeadline, $DeferRunInterval, (Get-ADTUniversalDate))
        }
        else
        {
            (Get-ADTSession).SetDeferHistory($(if ($PSBoundParameters.ContainsKey('DeferTimesRemaining')) { $DeferTimesRemaining }), $DeferDeadline, $null, $null)
        }
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

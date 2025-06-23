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

    .PARAMETER DeferRunIntervalLastTime
        Specifies the last time the DeferRunInterval value was tested. This is set from within `Show-ADTInstallationWelcome` as required.

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
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTDeferHistory

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$DeferTimesRemaining,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.DateTime]$DeferDeadline,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$DeferRunInterval,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.DateTime]$DeferRunIntervalLastTime
    )

    # Throw if at least one parameter isn't called.
    if (!($PSBoundParameters.Keys.GetEnumerator() | & { process { if (!$Script:PowerShellCommonParameters.Contains($_)) { return $_ } } }))
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("The function [$($MyInvocation.MyCommand.Name)] requires at least one parameter be specified.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
            ErrorId = 'SetDeferHistoryNoParamSpecified'
            TargetObject = $PSBoundParameters
            RecommendedAction = "Please check your usage of [$($MyInvocation.MyCommand.Name)] and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

    # Set the defer history as specified by the caller.
    try
    {
        # Make sure we send proper nulls through at all times.
        (Get-ADTSession).SetDeferHistory(
            $(if ($PSBoundParameters.ContainsKey('DeferTimesRemaining')) { $DeferTimesRemaining }),
            $(if ($PSBoundParameters.ContainsKey('DeferDeadline')) { $DeferDeadline }),
            $(if ($PSBoundParameters.ContainsKey('DeferRunInterval')) { $DeferRunInterval }),
            $(if ($PSBoundParameters.ContainsKey('DeferRunIntervalLastTime')) { $DeferRunIntervalLastTime })
        )
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

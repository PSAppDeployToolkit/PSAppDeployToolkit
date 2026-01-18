#-----------------------------------------------------------------------------
#
# MARK: Get-ADTUserNotificationState
#
#-----------------------------------------------------------------------------

function Get-ADTUserNotificationState
{
    <#
    .SYNOPSIS
        Gets the specified user's notification state.

    .DESCRIPTION
        This function gets the specified user's notification state.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.LibraryInterfaces.QUERY_USER_NOTIFICATION_STATE

        Returns the user's QUERY_USER_NOTIFICATION_STATE value as an enum.

    .EXAMPLE
        Get-ADTUserNotificationState

        Returns the logged on user's notification state.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTUserNotificationState
    #>

    [CmdletBinding()]
    [OutputType([PSADT.LibraryInterfaces.QUERY_USER_NOTIFICATION_STATE])]
    param
    (
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Bypass if no one's logged onto the device.
        if (!($runAsActiveUser = Get-ADTClientServerUser))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
            return
        }

        # Send the request off to the client/server process.
        try
        {
            try
            {
                Write-ADTLogEntry -Message "Detected user notification state [$(($UserNotificationState = Invoke-ADTClientServerOperation -GetUserNotificationState -User $runAsActiveUser))]."
                return [PSADT.LibraryInterfaces.QUERY_USER_NOTIFICATION_STATE]$UserNotificationState
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

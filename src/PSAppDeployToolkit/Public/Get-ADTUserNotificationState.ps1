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
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTUserNotificationState
    #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        # Initialize the module if it's not already. We need this for `Open-ADTClientServerProcess` to function properly.
        $null = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Bypass if no one's logged onto the device.
        if (!($runAsActiveUser = (Get-ADTEnvironmentTable).RunAsActiveUser))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
            return
        }

        # Instantiate a new ClientServerProcess object if one's not already present.
        if (!$Script:ADT.ClientServerProcess)
        {
            Open-ADTClientServerProcess -User $runAsActiveUser
        }

        # Send the request off to the client/server process.
        try
        {
            try
            {
                Write-ADTLogEntry -Message "Detected user notification state [$(($UserNotificationState = $Script:ADT.ClientServerProcess.GetUserNotificationState()))]."
                return $UserNotificationState
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

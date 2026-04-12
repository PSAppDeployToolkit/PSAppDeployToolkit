#-----------------------------------------------------------------------------
#
# MARK: Get-ADTUserToastNotificationMode
#
#-----------------------------------------------------------------------------

function Get-ADTUserToastNotificationMode
{
    <#
    .SYNOPSIS
        Gets the user's toast notification mode.

    .DESCRIPTION
        The `Get-ADTUserToastNotificationMode` function gets the logged on user's toast notification mode, returning the mode, or `$null` if the API is unavailable (older OS, etc).

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Interop.ToastNotificationMode

        Returns the determined mode.

    .EXAMPLE
        Get-ADTUserToastNotificationMode

        Returns the logged on user's toast notification state.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTUserToastNotificationMode
    #>

    [CmdletBinding()]
    [OutputType([PSADT.Interop.ToastNotificationMode])]
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
        Write-ADTLogEntry -Message "Querying the active user's toast notification mode..."
        try
        {
            try
            {
                if (($userToastNotificationMode = Invoke-ADTClientServerOperation -GetUserToastNotificationMode -User $runAsActiveUser) -ge 0)
                {
                    Write-ADTLogEntry -Message "The user's toast notification mode is [$userToastNotificationMode]."
                    return $userToastNotificationMode
                }
                Write-ADTLogEntry -Message "Unable to query the user's toast notification mode."
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

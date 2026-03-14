#-----------------------------------------------------------------------------
#
# MARK: Test-ADTUserInFocusMode
#
#-----------------------------------------------------------------------------

function Test-ADTUserInFocusMode
{
    <#
    .SYNOPSIS
        Tests whether the user is in focus mode.

    .DESCRIPTION
        This function tests whether the user is in focus mode, returning true/false, or null if the API is unavailble (older OS, etc).

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns true/false depending on whether the user is in focus mode or not.

    .EXAMPLE
        Test-ADTUserInFocusMode

        Returns the logged on user's notification state.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Test-ADTUserInFocusMode
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
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
                if (($userInFocusMode = Invoke-ADTClientServerOperation -GetUserFocusModeState -User $runAsActiveUser) -ge 0)
                {
                    Write-ADTLogEntry -Message "Detected user in focus mode [$([System.Boolean]$userInFocusMode)]."
                    return [System.Boolean]$userInFocusMode
                }
                Write-ADTLogEntry -Message "Unable to detect user focus mode."
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

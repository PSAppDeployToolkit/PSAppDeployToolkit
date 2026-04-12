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
        This function tests whether the user is in focus mode, returning `$true`/`$false`, or `$null` if the API is unavailable (older OS, etc).

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        Returns `$null` if there is no active user or the API is unavailable.

    .OUTPUTS
        System.Boolean

        Returns `$true` if the active user is in focus mode and `$false` when they aren't.

    .EXAMPLE
        Test-ADTUserInFocusMode

        Returns whether the logged on user is in focus mode or not, or `$null` if the API is unavailable.

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
        Write-ADTLogEntry -Message "Testing whether the active user is in focus mode..."
        try
        {
            try
            {
                if (($userInFocusMode = Invoke-ADTClientServerOperation -GetUserFocusModeState -User $runAsActiveUser) -ge 0)
                {
                    if (!$userInFocusMode)
                    {
                        Write-ADTLogEntry -Message "The active user is not currently in focus mode."
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "The active user is currently in focus mode."
                    }
                    return [System.Boolean]$userInFocusMode
                }
                Write-ADTLogEntry -Message "Unable to detect user focus mode on this system."
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

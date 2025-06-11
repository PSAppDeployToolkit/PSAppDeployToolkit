#-----------------------------------------------------------------------------
#
# MARK: Update-ADTDesktop
#
#-----------------------------------------------------------------------------

function Update-ADTDesktop
{
    <#
    .SYNOPSIS
        Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.

    .DESCRIPTION
        This function refreshes the Windows Explorer Shell, causing the desktop icons and environment variables to be reloaded. This can be useful after making changes that affect the desktop or environment variables, ensuring that the changes are reflected immediately.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Update-ADTDesktop

        Refreshes the Windows Explorer Shell, reloading the desktop icons and environment variables.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Update-ADTDesktop
    #>

    [CmdletBinding()]
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
        Write-ADTLogEntry -Message 'Refreshing the Desktop and the Windows Explorer environment process block.'
        try
        {
            try
            {
                $null = Invoke-ADTClientServerOperation -RefreshDesktopAndEnvironmentVariables -User $runAsActiveUser
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to refresh the Desktop and the Windows Explorer environment process block."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

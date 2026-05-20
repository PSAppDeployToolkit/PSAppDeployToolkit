#-----------------------------------------------------------------------------
#
# MARK: Close-ADTNotifyIcon
#
#-----------------------------------------------------------------------------

function Close-ADTNotifyIcon
{
    <#
    .SYNOPSIS
        Closes the notification icon created by `Show-ADTNotifyIcon`.

    .DESCRIPTION
        The `Close-ADTNotifyIcon` function closes the notification icon created by `Show-ADTNotifyIcon`. This function is called by the `Close-ADTSession` function to close an open notification icon if found.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Close-ADTNotifyIcon

        This example closes the dialog created by `Show-ADTNotifyIcon`.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Close-ADTNotifyIcon
    #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        # Initialise function.
        $adtSession = Initialize-ADTModuleIfUninitialized -Cmdlet $PSCmdlet -PassThruActiveSession
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Perform pre-requisite checks before closing the notification icon.
        if (!($runAsActiveUser = Get-ADTClientServerUser -AllowSystemFallback))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
            return
        }
        if (!(Test-ADTNotifyIconOpen -RunAsActiveUser $runAsActiveUser))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no notification icon open."
            return
        }
        try
        {
            try
            {
                # Call the underlying function to close the notification icon.
                Write-ADTLogEntry -Message 'Closing the notification icon.'
                Invoke-ADTClientServerOperation -CloseNotifyIcon -User $runAsActiveUser
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
        finally
        {
            # Remove any callback that might be lingering in the backing buffer.
            Remove-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.($MyInvocation.MyCommand.Name)
        }

        # Close the client/server process when we're running sessionless.
        if (!$adtSession -and !(Test-ADTInstallationProgressOpen -RunAsActiveUser $runAsActiveUser))
        {
            Close-ADTClientServerProcess
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

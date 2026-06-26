#-----------------------------------------------------------------------------
#
# MARK: Close-ADTInstallationProgress
#
#-----------------------------------------------------------------------------

function Close-ADTInstallationProgress
{
    <#
    .SYNOPSIS
        Closes the dialog created by `Show-ADTInstallationProgress`.

    .DESCRIPTION
        The `Close-ADTInstallationProgress` function closes the dialog created by `Show-ADTInstallationProgress`. This function is called by the `Close-ADTSession` function to close a running instance of the progress dialog if found.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Close-ADTInstallationProgress

        This example closes the dialog created by `Show-ADTInstallationProgress`.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Close-ADTInstallationProgress

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/src/PSAppDeployToolkit/Public/Close-ADTInstallationProgress.ps1
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

        # Initialise the string table.
        $sessionState = if ($adtSession)
        {
            $adtSession.DeployAppScriptSessionState
        }
        if ($null -eq $sessionState)
        {
            $sessionState = $PSCmdlet.SessionState
        }
        $adtStrings = Get-ADTStringTable -SessionState $SessionState
    }

    process
    {
        # Perform pre-requisite checks before closing the dialog.
        if (!($runAsActiveUser = Get-ADTClientServerUser -AllowSystemFallback))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
            return
        }
        if (!(Test-ADTInstallationProgressOpen -RunAsActiveUser $runAsActiveUser))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no progress dialog open."
            return
        }
        try
        {
            try
            {
                # Call the underlying function to close the progress window.
                Write-ADTLogEntry -Message 'Closing the installation progress dialog.'
                Invoke-ADTClientServerOperation -CloseProgressDialog -User $runAsActiveUser
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
        if (!$adtSession -and !(Test-ADTNotifyIconOpen -RunAsActiveUser $runAsActiveUser))
        {
            Close-ADTClientServerProcess
            return
        }

        # Send out the final toast notification.
        if ((Get-ADTConfig).UI.DialogStyle -eq 'Classic')
        {
            try
            {
                try
                {
                    switch ($adtSession.GetDeploymentStatus())
                    {
                        ([PSAppDeployToolkit.Foundation.DeploymentStatus]::FastRetry)
                        {
                            Show-ADTBalloonTip -Icon Warning -Text $adtStrings.BalloonTip.($_.ToString()).($adtSession.DeploymentType.ToString())
                            break
                        }
                        ([PSAppDeployToolkit.Foundation.DeploymentStatus]::Error)
                        {
                            Show-ADTBalloonTip -Icon Error -Text $adtStrings.BalloonTip.($_.ToString()).($adtSession.DeploymentType.ToString())
                            break
                        }
                        default
                        {
                            Show-ADTBalloonTip -Icon Info -Text $adtStrings.BalloonTip.($_.ToString()).($adtSession.DeploymentType.ToString())
                            break
                        }
                    }
                }
                catch
                {
                    Write-Error -ErrorRecord $_
                }
            }
            catch
            {
                Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -Silent
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

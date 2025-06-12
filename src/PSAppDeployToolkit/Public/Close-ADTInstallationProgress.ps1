#-----------------------------------------------------------------------------
#
# MARK: Close-ADTInstallationProgress
#
#-----------------------------------------------------------------------------

function Close-ADTInstallationProgress
{
    <#
    .SYNOPSIS
        Closes the dialog created by Show-ADTInstallationProgress.

    .DESCRIPTION
        Closes the dialog created by Show-ADTInstallationProgress. This function is called by the Close-ADTSession function to close a running instance of the progress dialog if found.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Close-ADTInstallationProgress

        This example closes the dialog created by Show-ADTInstallationProgress.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Close-ADTInstallationProgress
    #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Return early if we're silent, a window wouldn't have ever opened.
                if ($adtSession -and $adtSession.IsSilent())
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.DeployMode)]"
                    return
                }

                # Bypass if no one's logged on to answer the dialog.
                if (!($runAsActiveUser = Get-ADTClientServerUser))
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
                    return
                }

                # Return early if there's no progress dialog open at all.
                if (!(Invoke-ADTClientServerOperation -ProgressDialogOpen -User $runAsActiveUser))
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no progress dialog open."
                    return
                }

                # Call the underlying function to close the progress window.
                Write-ADTLogEntry -Message 'Closing the installation progress dialog.'
                Invoke-ADTClientServerOperation -CloseProgressDialog -User $runAsActiveUser
                Remove-ADTModuleCallback -Hookpoint OnFinish -Callback $MyInvocation.MyCommand

                # We only send balloon tips when a session is active.
                if (!$adtSession)
                {
                    # Close the client/server process when we're running sessionless.
                    Close-ADTClientServerProcess
                    return
                }

                # Send out the final toast notification.
                switch ($adtSession.GetDeploymentStatus())
                {
                    ([PSADT.Module.DeploymentStatus]::FastRetry)
                    {
                        Show-ADTBalloonTip -BalloonTipIcon Warning -BalloonTipText (Get-ADTStringTable).BalloonTip.($_.ToString()).($adtSession.DeploymentType.ToString()) -NoWait
                        break
                    }
                    ([PSADT.Module.DeploymentStatus]::Error)
                    {
                        Show-ADTBalloonTip -BalloonTipIcon Error -BalloonTipText (Get-ADTStringTable).BalloonTip.($_.ToString()).($adtSession.DeploymentType.ToString()) -NoWait
                        break
                    }
                    default
                    {
                        Show-ADTBalloonTip -BalloonTipIcon Info -BalloonTipText (Get-ADTStringTable).BalloonTip.($_.ToString()).($adtSession.DeploymentType.ToString()) -NoWait
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
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

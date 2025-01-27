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
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        $adtConfig = Get-ADTConfig
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Return early if we're silent, a window wouldn't have ever opened.
                if (!(Test-ADTInstallationProgressRunning))
                {
                    return
                }
                if ($adtSession -and $adtSession.IsSilent())
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.DeployMode)]"
                    return
                }

                # Call the underlying function to close the progress window.
                & $Script:CommandTable."$($MyInvocation.MyCommand.Name)$($adtConfig.UI.DialogStyle)"
                Remove-ADTSessionFinishingCallback -Callback $MyInvocation.MyCommand

                # We only send balloon tips when a session is active.
                if (!$adtSession)
                {
                    return
                }

                # Send out the final toast notification.
                switch ($adtSession.GetDeploymentStatus())
                {
                    ([PSADT.Module.DeploymentStatus]::FastRetry)
                    {
                        Show-ADTBalloonTip -BalloonTipIcon Warning -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $((Get-ADTStringTable).BalloonText.($_.ToString()))"
                        break
                    }
                    ([PSADT.Module.DeploymentStatus]::Error)
                    {
                        Show-ADTBalloonTip -BalloonTipIcon Error -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $((Get-ADTStringTable).BalloonText.($_.ToString()))"
                        break
                    }
                    default
                    {
                        Show-ADTBalloonTip -BalloonTipIcon Info -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $((Get-ADTStringTable).BalloonText.($_.ToString()))"
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

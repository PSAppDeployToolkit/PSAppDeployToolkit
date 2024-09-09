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

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
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
        $adtSession = & $Script:CommandTable.'Initialize-ADTModuleIfUnitialized' -Cmdlet $PSCmdlet
        $adtConfig = & $Script:CommandTable.'Get-ADTConfig'
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Return early if we're silent, a window wouldn't have ever opened.
                if (!(& $Script:CommandTable.'Test-ADTInstallationProgressRunning'))
                {
                    return
                }
                if ($adtSession -and $adtSession.IsSilent())
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.GetPropertyValue('DeployMode'))]"
                    return
                }

                # Call the underlying function to close the progress window.
                & $Script:DialogDispatcher.($adtConfig.UI.DialogStyle).($MyInvocation.MyCommand.Name)
                & $Script:CommandTable.'Remove-ADTSessionFinishingCallback' -Callback $MyInvocation.MyCommand

                # We only send balloon tips when a session is active.
                if (!$adtSession)
                {
                    return
                }

                # Send out the final toast notification.
                switch ($adtSession.GetDeploymentStatus())
                {
                    FastRetry
                    {
                        & $Script:CommandTable.'Show-ADTBalloonTip' -BalloonTipIcon Warning -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $((& $Script:CommandTable.'Get-ADTStringTable').BalloonText.$_)"
                        break
                    }
                    Error
                    {
                        & $Script:CommandTable.'Show-ADTBalloonTip' -BalloonTipIcon Error -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $((& $Script:CommandTable.'Get-ADTStringTable').BalloonText.$_)"
                        break
                    }
                    default
                    {
                        & $Script:CommandTable.'Show-ADTBalloonTip' -BalloonTipIcon Info -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $((& $Script:CommandTable.'Get-ADTStringTable').BalloonText.$_)"
                        break
                    }
                }
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}

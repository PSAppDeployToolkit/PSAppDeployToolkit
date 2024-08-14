function Close-ADTSession
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$ExitCode
    )

    # Get the current session object.
    $adtSession = Get-ADTSession

    # Update the session's exit code with the provided value.
    if ($PSBoundParameters.ContainsKey('ExitCode'))
    {
        $adtSession.SetExitCode($ExitCode)
    }

    # If we're closing the last session, clean up the environment.
    if (($adtData = Get-ADT).Sessions.Count.Equals(1))
    {
        # Only attempt to finalise the dialogs if we're using our own code.
        if (Get-Module -Name PSAppDeployToolkit.Dialogs)
        {
            Close-ADTInstallationProgress
            switch ($adtSession.GetDeploymentStatus())
            {
                FastRetry {
                    Show-ADTBalloonTip -BalloonTipIcon Warning -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $((Get-ADTStrings).BalloonText.$_)" -NoWait
                    break
                }
                Error {
                    Show-ADTBalloonTip -BalloonTipIcon Error -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $((Get-ADTStrings).BalloonText.$_)" -NoWait
                    break
                }
                default {
                    Show-ADTBalloonTip -BalloonTipIcon Info -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $((Get-ADTStrings).BalloonText.$_)" -NoWait
                    break
                }
            }
        }

        # Unblock all PSAppDeployToolkit blocked apps.
        Unblock-ADTAppExecution

        # Only attempt to disable Terminal Services Install Mode if previously set.
        if ($adtData.TerminalServerMode)
        {
            Disable-ADTTerminalServerInstallMode
        }
    }

    # Close out the active session and clean up session state.
    try
    {
        $adtSession.Close()
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
    finally
    {
        [System.Void]$adtData.Sessions.Remove($adtSession)
        if (!$adtData.Sessions.Count -and !$adtSession.RunspaceOrigin)
        {
            exit $adtData.LastExitCode
        }
    }
}

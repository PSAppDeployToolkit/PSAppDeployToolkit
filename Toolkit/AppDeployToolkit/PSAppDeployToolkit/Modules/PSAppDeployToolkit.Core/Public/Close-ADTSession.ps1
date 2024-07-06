function Close-ADTSession
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$ExitCode
    )

    # Get the current session object.
    try
    {
        $adtSession = Get-ADTSession
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }

    # Update the session's exit code with the provided value.
    if ($PSBoundParameters.ContainsKey('ExitCode'))
    {
        $adtSession.SetExitCode($ExitCode)
    }

    # If we're closing the last session, clean up the environment.
    if (($adtData = Get-ADTModuleData).Sessions.Count.Equals(1))
    {
        # Only attempt to finalise the dialogs a dialog module is loaded.
        if (Get-Command -Name Close-ADTInstallationProgress -ErrorAction Ignore)
        {
            Close-ADTInstallationProgress
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

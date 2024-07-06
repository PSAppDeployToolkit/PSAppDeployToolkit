function Close-ADTSession
{
    param (
        [ValidateNotNullOrEmpty()]
        [System.Int32]$ExitCode
    )

    # Cache the module's global data.
    $adtData = Get-ADT

    # Close the Installation Progress Dialog if running.
    if ($adtData.Sessions.Count.Equals(1) -and (Get-Module -Name PSAppDeployToolkit.Dialogs))
    {
        Close-ADTInstallationProgress
    }

    # Close out the active session and clean up session state.
    ($adtSession = Get-ADTSession).Close($ExitCode)
    [System.Void]$adtData.Sessions.Remove($adtSession)

    # If this was the last session, exit out with our code.
    if (!$adtData.Sessions.Count)
    {
        exit $adtData.LastExitCode
    }
}

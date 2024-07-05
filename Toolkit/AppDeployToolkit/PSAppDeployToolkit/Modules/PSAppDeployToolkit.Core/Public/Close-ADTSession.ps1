function Close-ADTSession
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.Int32]]$ExitCode
    )

    # Get the current session object.
    $adtSession = Get-ADTSession

    # If we're closing the last session, clean up the environment.
    if (($adtData = Get-ADT).Sessions.Count.Equals(1))
    {
        if (Get-Module -Name PSAppDeployToolkit.Dialogs)
        {
            Close-ADTInstallationProgress
        }
        Unblock-ADTAppExecution
        if ($adtData.TerminalServerMode)
        {
            Disable-ADTTerminalServerInstallMode
        }
    }

    # Close out the active session and clean up session state.
    try
    {
        $adtSession.Close($ExitCode)
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
    finally
    {
        [System.Void]$adtData.Sessions.Remove($adtSession)
        if (!$adtData.Sessions.Count -and $MyInvocation.PSCommandPath)
        {
            exit $adtData.LastExitCode
        }
    }
}

#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTSilentRestart
#
#-----------------------------------------------------------------------------

function Private:Invoke-ADTSilentRestart
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.UInt32]]$Delay
    )

    # Hand this off to the client/server process to deal with. Run it as this current user though.
    Start-ADTProcess -FilePath ([PSADT.Foundation.EnvironmentInfo]::ClientServerClientLauncherPath) -ArgumentList "/SilentRestart -Delay $Delay" -CreateNoWindow -MsiExecWaitTime 1 -NoWait -InformationAction SilentlyContinue -ErrorAction SilentlyContinue
}
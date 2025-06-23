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
    Start-ADTProcess -FilePath $Script:PSScriptRoot\lib\PSADT.ClientServer.Client.Launcher.exe -ArgumentList "/SilentRestart -Delay $Delay" -CreateNoWindow -MsiExecWaitTime 1 -NoWait -InformationAction SilentlyContinue -ErrorAction SilentlyContinue
}
function Restore-ADTPreviousSession
{
    # Destruct the active session and restore the previous one if available.
    $Host.UI.RawUI.WindowTitle = ($adtSession = Get-ADTSession).OldPSWindowTitle
    $Script:SessionCallers.Remove($adtSession)
    [System.Void]$Script:ADT.Sessions.Remove($adtSession)
}

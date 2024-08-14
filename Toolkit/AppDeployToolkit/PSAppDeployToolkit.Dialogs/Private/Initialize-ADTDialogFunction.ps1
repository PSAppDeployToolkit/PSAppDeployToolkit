function Initialize-ADTDialogFunction
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Initialise the module if there's no session and it hasn't been previously initialised.
    if (!($adtSession = if (Test-ADTSessionActive) {Get-ADTSession}) -and !(Test-ADTModuleInitialised))
    {
        try
        {
            Initialize-ADTModule
        }
        catch
        {
            $Cmdlet.ThrowTerminatingError($_)
        }
    }

    # Return the current session if we happened to get one.
    if ($adtSession)
    {
        return $adtSession
    }
}

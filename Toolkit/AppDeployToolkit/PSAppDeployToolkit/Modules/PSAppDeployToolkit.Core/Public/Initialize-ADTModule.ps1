function Initialize-ADTModule
{
    [CmdletBinding()]
    param
    (
    )

    try
    {
        # Initialise the module's global state.
        $adtData = Get-ADTModuleData
        Initialize-ADTEnvironment
        Import-ADTConfig
        Import-ADTLocalizedStrings
        $adtData.LastExitCode = 0
        $adtData.Initialised = $true
    }
    catch
    {
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

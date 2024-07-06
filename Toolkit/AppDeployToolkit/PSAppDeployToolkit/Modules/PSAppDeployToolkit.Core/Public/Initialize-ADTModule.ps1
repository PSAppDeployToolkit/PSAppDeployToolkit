function Initialize-ADTModule
{
    # Initialise the module's global state.
    $adtData = Get-ADTModuleData
    Initialize-ADTEnvironment
    Import-ADTConfig
    Import-ADTLocalizedStrings
    $adtData.LastExitCode = 0
    $adtData.Initialised = $true
}

function Initialize-ADTModule
{
    # Initialise the module's global state.
    Initialize-ADTEnvironment
    Import-ADTConfig
    Import-ADTLocalizedStrings
    (Get-ADTModuleData).LastExitCode = 0
}

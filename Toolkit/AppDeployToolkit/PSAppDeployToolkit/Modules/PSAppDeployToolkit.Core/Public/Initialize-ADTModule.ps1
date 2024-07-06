function Initialize-ADTModule
{
    Initialize-ADTEnvironment
    Import-ADTConfig
    Import-ADTLocalizedStrings
    (Get-ADT).LastExitCode = 0	
}

function Initialize-ADTModule
{
    Initialize-ADTEnvironment
    Import-ADTConfig
    Import-ADTLocalizedStrings
    $Script:ADT.LastExitCode = 0	
}

function Get-ADTDialogFunction
{
    # Return the underlying function for the configured DialogStyle.
    Get-Command -Name ((Get-Variable -Name MyInvocation -Scope 1 -ValueOnly).MyCommand -replace '^(.+-ADT)(.+$)',"`$1$((Get-ADTConfig).UI.DialogStyle)`$2")
}

function Get-ADTDialogFunction
{
    # If the is classic, use this, otherwise use fluent for anything else.
    if ((Get-ADTConfig).UI.DialogStyle -eq 'Classic')
    {
        return ((Get-Variable -Name MyInvocation -Scope 1 -ValueOnly).MyCommand -replace '^(.+-ADT)(.+$)','$1Classic$2')
    }
    return ((Get-Variable -Name MyInvocation -Scope 1 -ValueOnly).MyCommand -replace '^(.+-ADT)(.+$)','$1Fluent$2')
}

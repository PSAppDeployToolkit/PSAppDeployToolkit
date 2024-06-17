function Get-ADTDialogFunction
{
    # Return the underlying function for the configured DialogStyle.
    Get-Command -Name "$((Get-PSCallStack)[1].Command)$((Get-ADTConfig).UI.DialogStyle)"
}

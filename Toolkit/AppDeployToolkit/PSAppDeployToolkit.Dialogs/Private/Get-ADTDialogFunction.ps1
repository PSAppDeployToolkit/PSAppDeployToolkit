#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Get-ADTDialogFunction
{
    # Return the underlying function for the configured DialogStyle.
    Get-Item -LiteralPath "Function:$((Get-PSCallStack)[1].Command)$((Get-ADTConfig).UI.DialogStyle)"
}

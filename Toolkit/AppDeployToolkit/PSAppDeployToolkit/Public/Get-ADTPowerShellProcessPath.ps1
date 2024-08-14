#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTPowerShellProcessPath
{
    <#

    .NOTES
    This function can be called without an active ADT session.

    #>

    return "$PSHOME\$(if ($PSVersionTable.PSEdition.Equals('Core')) {'pwsh.exe'} else {'powershell.exe'})"
}

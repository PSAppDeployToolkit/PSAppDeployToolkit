function Get-ADTPowerShellProcessPath
{
    return "$PSHOME\$(if ($PSVersionTable.PSEdition.Equals('Core')) {'pwsh.exe'} else {'powershell.exe'})"
}

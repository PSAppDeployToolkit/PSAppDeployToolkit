#-----------------------------------------------------------------------------
#
# MARK: Test-ADTClientServerActive
#
#-----------------------------------------------------------------------------

function Private:Test-ADTClientServerActive
{
    return $null -ne (Get-Variable -Name ClientServerInstance -ValueOnly -Scope Script -ErrorAction Ignore)
}

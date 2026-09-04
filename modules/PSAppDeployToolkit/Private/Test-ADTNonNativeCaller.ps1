#-----------------------------------------------------------------------------
#
# MARK: Test-ADTNonNativeCaller
#
#-----------------------------------------------------------------------------

function Private:Test-ADTNonNativeCaller
{
    return (Get-PSCallStack).Command.Contains('AppDeployToolkitMain.ps1')
}

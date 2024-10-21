#-----------------------------------------------------------------------------
#
# MARK: Test-ADTNonNativeCaller
#
#-----------------------------------------------------------------------------

function Test-ADTNonNativeCaller
{
    return (Get-PSCallStack).Command.Contains('AppDeployToolkitMain.ps1')
}

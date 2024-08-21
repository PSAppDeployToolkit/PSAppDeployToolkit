#-----------------------------------------------------------------------------
#
# MARK: Test-ADTNonNativeCaller
#
#-----------------------------------------------------------------------------

function Test-ADTNonNativeCaller
{
    return (& $Script:CommandTable.'Get-PSCallStack').Command.Contains('AppDeployToolkitMain.ps1')
}

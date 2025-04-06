#-----------------------------------------------------------------------------
#
# MARK: Test-ADTModuleIsReleaseBuild
#
#-----------------------------------------------------------------------------

function Private:Test-ADTModuleIsReleaseBuild
{
    return $Script:Module.Compiled -and $Script:Module.Signed
}

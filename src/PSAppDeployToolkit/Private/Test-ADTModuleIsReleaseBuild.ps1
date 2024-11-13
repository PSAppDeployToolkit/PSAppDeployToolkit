#-----------------------------------------------------------------------------
#
# MARK: Test-ADTModuleIsReleaseBuild
#
#-----------------------------------------------------------------------------

function Test-ADTModuleIsReleaseBuild
{
    return $Script:Module.Compiled -and $Script:Module.Signed
}

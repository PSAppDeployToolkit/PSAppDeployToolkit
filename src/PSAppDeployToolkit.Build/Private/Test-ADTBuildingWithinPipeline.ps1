#-----------------------------------------------------------------------------
#
# MARK: Test-ADTBuildingWithinPipeline
#
#-----------------------------------------------------------------------------

function Test-ADTBuildingWithinPipeline
{
    return (($env:GITHUB_ACTIONS -eq 'true') -or ($env:TF_BUILD -eq 'true'))
}

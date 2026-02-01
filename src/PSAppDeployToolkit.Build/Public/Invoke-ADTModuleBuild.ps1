#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTModuleBuild
#
#-----------------------------------------------------------------------------

function Invoke-ADTModuleBuild
{
    [CmdletBinding()]
    param
    (
    )

    # Go through the motions.
    Initialize-ADTModuleBuild
    try
    {
        Test-ADTBuildEnvironment
        Reset-ADTModuleBuildOutputPath
        Confirm-ADTBuildModulesPresent
        Invoke-ADTDotNetCompilation
        Import-ADTDevelopmentModule
        Confirm-ADTScriptEncoding
        Confirm-ADTScriptFormatting
        Confirm-ADTScriptIntegrity
        Confirm-ADTAdmxTemplateValid
        Confirm-ADTStringTablesValid
        Invoke-ADTPesterUnitTesting
        Invoke-ADTModuleCompilation
        Invoke-ADTPesterIntegrationTesting
        Export-ADTScriptTemplate
        Complete-ADTModuleBuild
    }
    catch
    {
        Complete-ADTModuleBuild -ErrorRecord $_
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

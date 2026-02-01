#-----------------------------------------------------------------------------
#
# MARK: Import-ADTDevelopmentModule
#
#-----------------------------------------------------------------------------

function Import-ADTDevelopmentModule
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # We need the module imported for Pester tests, among other things.
        Write-ADTBuildLogEntry -Message "Importing PSApppDeployToolkit development module."
        $Script:ModuleBuildState.CommandTable = & (Import-Module -Name $Script:ModuleConstants.Paths.ModuleManifest -Force -PassThru) { $CommandTable }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}

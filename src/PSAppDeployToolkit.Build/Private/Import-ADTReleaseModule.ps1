#-----------------------------------------------------------------------------
#
# MARK: Import-ADTReleaseModule
#
#-----------------------------------------------------------------------------

function Import-ADTReleaseModule
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # This assumes the release module has been built ahead of time.
        Write-ADTBuildLogEntry -Message "Importing PSApppDeployToolkit release module."
        Import-Module -Name ([System.IO.Path]::Combine($Script:ModuleConstants.Paths.ModuleOutput, $Script:ModuleConstants.ModuleName)) -Force
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}

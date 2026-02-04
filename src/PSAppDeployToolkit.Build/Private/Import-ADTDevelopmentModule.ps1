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
        if (Get-Module -Name $Script:ModuleConstants.ModuleName)
        {
            throw "A conflicting $($Script:ModuleConstants.ModuleName) module is already imported. Please restart PowerShell and try again."
        }
        $Script:ModuleBuildState.CommandTable = & (Import-Module -FullyQualifiedName $Script:ModuleConstants.ModuleSpecification -Global -Force -PassThru) { $CommandTable }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}

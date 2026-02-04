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
        [Parameter(Mandatory = $false)]
        [ValidateSet('Prerequisites', 'Clean', 'Dependencies', 'DotNet', 'Analyze', 'UnitTests', 'Build', 'IntegrationTests')]
        [System.String[]]$Steps = ('Prerequisites', 'Clean', 'Dependencies', 'DotNet', 'Analyze', 'UnitTests', 'Build', 'IntegrationTests')
    )

    # Go through the motions.
    Initialize-ADTModuleBuild
    $imported = $false
    try
    {
        if ($Steps -contains 'Prerequisites')
        {
            Test-ADTBuildEnvironment
        }
        if ($Steps -contains 'Clean')
        {
            Reset-ADTModuleBuildOutputPath
        }
        if ($Steps -contains 'Dependencies')
        {
            Confirm-ADTBuildModulesPresent
        }
        if ($Steps -contains 'DotNet')
        {
            Invoke-ADTDotNetCompilation
        }
        if ($Steps -contains 'Analyze')
        {
            Confirm-ADTScriptEncoding
            Confirm-ADTScriptFormatting
            Confirm-ADTScriptIntegrity
            Confirm-ADTAdmxTemplateValid
            Confirm-ADTStringTablesValid
        }
        if ($Steps -contains 'UnitTests')
        {
            if (!$imported)
            {
                Import-ADTDevelopmentModule
                $imported = $true
            }
            Invoke-ADTPesterUnitTesting
        }
        if ($Steps -contains 'Build')
        {
            if (!$imported)
            {
                Import-ADTDevelopmentModule
                $imported = $true
            }
            Invoke-ADTModuleCompilation
            Export-ADTScriptTemplate
            if ($Steps -contains 'IntegrationTests')
            {
                Invoke-ADTPesterIntegrationTesting
            }
        }
        Complete-ADTModuleBuild
    }
    catch
    {
        Complete-ADTModuleBuild -ErrorRecord $_
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

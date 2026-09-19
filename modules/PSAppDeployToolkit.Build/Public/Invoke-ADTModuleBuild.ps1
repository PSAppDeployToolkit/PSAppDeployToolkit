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
        [ValidateSet('Clean', 'DotNet', 'Analyze', 'UnitTests', 'Build', 'IntegrationTests')]
        [System.String[]]$Steps = ('Clean', 'DotNet', 'Analyze', 'UnitTests', 'Build', 'IntegrationTests')
    )

    # Go through the motions.
    Initialize-ADTModuleBuild
    $imported = $false
    try
    {
        Test-ADTBuildEnvironment
        if ($Steps -contains 'Clean')
        {
            Reset-ADTModuleBuildOutputPath
        }
        Confirm-ADTBuildModulesPresent
        if ($Steps -contains 'DotNet')
        {
            Invoke-ADTDotNetCompilation
        }
        if ($Steps -contains 'Analyze')
        {
            Confirm-ADTScriptEncoding
            Confirm-ADTScriptFormatting
            Confirm-ADTScriptIntegrity
            if (!$imported)
            {
                Import-ADTDevelopmentModule
                $imported = $true
            }
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
        }
        if ($Steps -contains 'IntegrationTests')
        {
            if (!$imported)
            {
                Import-ADTDevelopmentModule
                $imported = $true
            }
            if ($Steps -notcontains 'Build')
            {
                Invoke-ADTModuleCompilation
                Export-ADTScriptTemplate
            }
            Invoke-ADTPesterIntegrationTesting
        }
        Complete-ADTModuleBuild
    }
    catch
    {
        Complete-ADTModuleBuild -ErrorRecord $_
        $PSCmdlet.ThrowTerminatingError($_)
    }
}

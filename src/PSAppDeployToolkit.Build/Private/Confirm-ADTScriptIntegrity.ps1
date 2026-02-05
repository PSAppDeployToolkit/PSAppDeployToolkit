#-----------------------------------------------------------------------------
#
# MARK: Confirm-ADTScriptIntegrity
#
#-----------------------------------------------------------------------------

function Confirm-ADTScriptIntegrity
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Verify the formatting of all PowerShell script files within the repository.
        Write-ADTBuildLogEntry -Message "Confirming all PowerShell files have no code violations."
        if ([Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic.DiagnosticRecord[]]$result = Invoke-ScriptAnalyzer -Path $Script:ModuleConstants.Paths.SourceRoot -ExcludeRule PSUseShouldProcessForStateChangingFunctions, PSUseSingularNouns -Recurse -Fix:(!(Test-ADTBuildingWithinPipeline)) -Verbose:$false | & { process { if ((!$_.RuleName.Equals('PSUseToExportFieldsInManifest') -or !$_.ScriptName.Equals('PSAppDeployToolkit.Extensions.psd1')) -and (!$_.RuleName.Equals('PSAvoidUsingWriteHost') -or ($_.ScriptPath -notmatch 'PSAppDeployToolkit\.Build'))) { return $_ } } })
        {
            Write-ADTBuildLogEntry -Message "PSScriptAnalyzer returned $($result.Count) script formatting violations." -ForegroundColor DarkRed
            Write-ADTScriptAnalyzerOutput -DiagnosticRecord $result
            throw "The call to Invoke-ScriptAnalyzer returned formatting violations that must be addressed."
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}

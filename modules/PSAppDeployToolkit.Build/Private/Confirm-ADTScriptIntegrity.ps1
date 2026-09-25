#-----------------------------------------------------------------------------
#
# MARK: Confirm-ADTScriptIntegrity
#
#-----------------------------------------------------------------------------

function Confirm-ADTScriptIntegrity
{
    # Internal function for re-calling in catch block due to upstream race conditions.
    # See https://github.com/PowerShell/PSScriptAnalyzer/issues/1867 for more info.
    function Confirm-ADTScriptIntegrityImpl
    {
        # Verify the formatting of all PowerShell script files within the repository.
        if ([Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic.DiagnosticRecord[]]$result = Invoke-ScriptAnalyzer -Path $Script:ModuleConstants.Paths.SourceRoot -ExcludeRule PSUseShouldProcessForStateChangingFunctions, PSUseSingularNouns -Recurse -Fix:(!(Test-ADTBuildingWithinPipeline)) -Verbose:$false | & { process { if ((!$_.RuleName.Equals('PSUseToExportFieldsInManifest') -or !$_.ScriptName.Equals('PSAppDeployToolkit.Extensions.psd1')) -and (!$_.RuleName.Equals('PSAvoidUsingWriteHost') -or ($_.ScriptPath -notmatch 'PSAppDeployToolkit\.Build'))) { return $_ } } })
        {
            Write-ADTBuildLogEntry -Message "PSScriptAnalyzer returned $($result.Count) script formatting violations." -ForegroundColor DarkRed
            Write-ADTScriptAnalyzerOutput -DiagnosticRecord $result
            throw "The call to Invoke-ScriptAnalyzer returned formatting violations that must be addressed."
        }
    }

    # Initialise the module build function and get started.
    Initialize-ADTModuleBuildFunction
    try
    {
        Write-ADTBuildLogEntry -Message "Confirming all PowerShell files have no code violations."
        Confirm-ADTScriptIntegrityImpl
        Complete-ADTModuleBuildFunction
    }
    catch [System.NullReferenceException]
    {
        # Try again due to https://github.com/PowerShell/PSScriptAnalyzer/issues/1867.
        try
        {
            Confirm-ADTScriptIntegrityImpl
            Complete-ADTModuleBuildFunction
        }
        catch [System.NullReferenceException]
        {
            Write-ADTBuildLogEntry -Message "The call to [Invoke-ScriptAnalyzer] threw a NullReferenceException type." -ForegroundColor DarkRed
            Write-ADTBuildLogEntry -Message $_.Exception.ToString() -ForegroundColor DarkRed
            Write-ADTBuildLogEntry -Message $_.ScriptStackTrace -ForegroundColor DarkRed
            Complete-ADTModuleBuildFunction -ErrorRecord $_
            throw
        }
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}

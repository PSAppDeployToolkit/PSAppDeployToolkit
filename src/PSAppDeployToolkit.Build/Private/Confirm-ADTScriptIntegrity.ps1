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
        # Rules resolving Export-ModuleMember parameters off the pipeline thread can throw a transient
        # NullReferenceException (PowerShell/PSScriptAnalyzer#1538), so retry before treating it as fatal.
        Write-ADTBuildLogEntry -Message "Confirming all PowerShell files have no code violations."
        for ($attempt = 1; $attempt -le 3; $attempt++)
        {
            try
            {
                [Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic.DiagnosticRecord[]]$result = Invoke-ScriptAnalyzer -Path $Script:ModuleConstants.Paths.SourceRoot -ExcludeRule PSUseShouldProcessForStateChangingFunctions, PSUseSingularNouns -Recurse -Fix:(!(Test-ADTBuildingWithinPipeline)) -Verbose:$false | & { process { if ((!$_.RuleName.Equals('PSUseToExportFieldsInManifest') -or !$_.ScriptName.Equals('PSAppDeployToolkit.Extensions.psd1')) -and (!$_.RuleName.Equals('PSAvoidUsingWriteHost') -or ($_.ScriptPath -notmatch 'PSAppDeployToolkit\.Build'))) { return $_ } } }
                break
            }
            catch [System.NullReferenceException]
            {
                if ($attempt -ge 3)
                {
                    throw
                }
                Write-ADTBuildLogEntry -Message "The call to [Invoke-ScriptAnalyzer] threw a transient NullReferenceException, retrying..." -ForegroundColor DarkYellow
            }
        }
        if ($result)
        {
            Write-ADTBuildLogEntry -Message "PSScriptAnalyzer returned $($result.Count) script formatting violations." -ForegroundColor DarkRed
            Write-ADTScriptAnalyzerOutput -DiagnosticRecord $result
            throw "The call to Invoke-ScriptAnalyzer returned formatting violations that must be addressed."
        }
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
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}

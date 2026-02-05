#-----------------------------------------------------------------------------
#
# MARK: Write-ADTScriptAnalyzerOutput
#
#-----------------------------------------------------------------------------

function Write-ADTScriptAnalyzerOutput
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic.DiagnosticRecord[]]$DiagnosticRecord
    )

    # Write each result out to the console.
    for ($i = 0; $i -lt $DiagnosticRecord.Count; $i++)
    {
        $output = ($DiagnosticRecord[$i] | Format-List -Property * | Out-String -Width ([System.Int32]::MaxValue)).Trim().Split("`n").Trim() -replace '^', "> "
        Write-ADTBuildLogEntry -Message "Output for Invoke-ScriptAnalyzer DiagnosticRecord [$($i+1)/$($DiagnosticRecord.Count)]" -ForegroundColor DarkRed
        Write-ADTBuildLogEntry -Message $output -ForegroundColor DarkRed
    }
}

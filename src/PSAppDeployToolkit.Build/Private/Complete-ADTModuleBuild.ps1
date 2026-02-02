#-----------------------------------------------------------------------------
#
# MARK: Complete-ADTModuleBuild
#
#-----------------------------------------------------------------------------

function Complete-ADTModuleBuild
{
    param
    (
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ErrorRecord]$ErrorRecord
    )

    # Announce completion of module build and write out closing divider.
    Write-Host -Object ('-' * 79) -ForegroundColor DarkMagenta
    if (!$PSBoundParameters.ContainsKey('ErrorRecord'))
    {
        Write-Host -Object "Module build completed in [$(([System.DateTime]::Now - $Script:ModuleBuildState.StartTime).TotalSeconds)] seconds." -ForegroundColor Green
        Write-Host -Object "Build output saved to [$($Script:ModuleConstants.Paths.BuildOutput)]."
    }
    else
    {
        Write-Host -Object "Module build failed to complete. Review the thrown ErrorRecord and try again." -ForegroundColor Red
    }
    Write-Host -Object "End of log."
}

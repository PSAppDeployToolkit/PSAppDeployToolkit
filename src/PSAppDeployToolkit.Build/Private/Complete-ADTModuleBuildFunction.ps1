#-----------------------------------------------------------------------------
#
# MARK: Complete-ADTModuleBuildFunction
#
#-----------------------------------------------------------------------------

function Complete-ADTModuleBuildFunction
{
    param
    (
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ErrorRecord]$ErrorRecord
    )

    # Announce completion of module function.
    if ($PSBoundParameters.ContainsKey('ErrorRecord'))
    {
        Write-ADTBuildLogEntry -Message $ErrorRecord.Exception.Message -ForegroundColor DarkRed
        Write-ADTBuildLogEntry -Message "Failed to complete build function [$((Get-Variable -Name MyInvocation -Scope 1 -ValueOnly).MyCommand.Name)]." -ForegroundColor Red
    }
    else
    {
        Write-ADTBuildLogEntry -Message "Completed build function [$((Get-Variable -Name MyInvocation -Scope 1 -ValueOnly).MyCommand.Name)] in [$(([System.DateTime]::Now - (Get-Variable -Name functionStart -Scope 1 -ValueOnly)).TotalSeconds)] seconds." -ForegroundColor Green
    }
}

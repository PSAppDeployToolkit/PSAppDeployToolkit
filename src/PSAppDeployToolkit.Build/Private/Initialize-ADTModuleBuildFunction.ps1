#-----------------------------------------------------------------------------
#
# MARK: Initialize-ADTModuleBuild
#
#-----------------------------------------------------------------------------

function Initialize-ADTModuleBuildFunction
{
    # Announce commencement of module build.
    New-Variable -Name functionStart -Value ([System.DateTime]::Now) -Scope 1 -Force
    Write-Host -Object ('-' * 79) -ForegroundColor DarkMagenta
    Write-ADTBuildLogEntry -Message "Performing build function [$((Get-Variable -Name MyInvocation -Scope 1 -ValueOnly).MyCommand.Name)], please wait..."
}

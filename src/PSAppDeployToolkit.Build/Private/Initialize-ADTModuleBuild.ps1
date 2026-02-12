#-----------------------------------------------------------------------------
#
# MARK: Initialize-ADTModuleBuild
#
#-----------------------------------------------------------------------------

function Initialize-ADTModuleBuild
{
    # Announce commencement of module build.
    $Script:ModuleBuildState.StartTime = [System.DateTime]::Now; if (!(Test-ADTBuildingWithinPipeline)) { Show-ADTModuleInitArtwork }
    $domain = if (($w32cs = CimCmdlets\Get-CimInstance -ClassName Win32_ComputerSystem).PartOfDomain) { $w32cs.Domain }
    $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    try
    {
        $userName = $currentUser.Name
    }
    finally
    {
        $currentUser.Dispose()
        $currentUser = $null
    }
    Write-Host -Object "$($Script:ModuleConstants.ModuleName) Module Build System $($MyInvocation.MyCommand.Module.Version). $($MyInvocation.MyCommand.Module.Copyright.Replace("©", "(C)"))"
    Write-Host -Object "Written by: $($MyInvocation.MyCommand.Module.Author -replace '\.+$')."
    Write-Host -Object "Running as: $userName on $([System.Environment]::MachineName)$(if ($domain) {".$($domain)"})."
    Write-Host -Object "Running on: PowerShell $($Host.Version)."
    if (Test-ADTBuildingWithinPipeline)
    {
        Write-Host -Object "Started at: $($Script:ModuleBuildState.StartTime.ToUniversalTime().ToString()) UTC."
    }
    else
    {
        Write-Host -Object "Started at: $($Script:ModuleBuildState.StartTime.ToString())."
    }
    Write-Host -Object "Commencing module build operation."
}

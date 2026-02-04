#-----------------------------------------------------------------------------
#
# MARK: Export-ADTScriptTemplate
#
#-----------------------------------------------------------------------------

function Export-ADTScriptTemplate
{
    # Return early if we're in a GitHub pipeline.
    if (($env:GITHUB_ACTIONS -eq 'true') -and !(Get-Variable -Name MyInvocation -Scope 1 -ValueOnly).MyCommand.Name.Equals('Invoke-ADTCustomModuleBuild'))
    {
        return
    }

    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        Write-ADTBuildLogEntry -Message "Creating frontend templates, this may take a while."
        $spParams = @{
            FilePath = [System.Diagnostics.Process]::GetCurrentProcess().Path
            ArgumentList = "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -Command `$ErrorActionPreference = 'Stop'; Import-Module -FullyQualifiedName @{ ModuleName = '$([System.Management.Automation.WildcardPattern]::Escape($Script:ModuleConstants.Paths.ModuleOutput))\$($Script:ModuleConstants.ModuleName).psd1'; Guid = '8c3c366b-8606-4576-9f2d-4051144f7ca2'; ModuleVersion = '4.2.0' }; $([System.String]::Join('; ', (3, 4).ForEach({"New-ADTTemplate -Destination '$($Script:ModuleConstants.Paths.BuildOutput)' -Name 'Template_v$_' -Version $_ -Force"})))"
            NoNewWindow = $true
            Wait = $true
        }
        if ((Start-Process @spParams -PassThru).ExitCode -ne 0)
        {
            throw "Failed to generate frontend templates."
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}

#-----------------------------------------------------------------------------
#
# MARK: Export-ADTScriptTemplate
#
#-----------------------------------------------------------------------------

function Export-ADTScriptTemplate
{
    # Return early if we're in a GitHub pipeline.
    if ($env:GITHUB_ACTIONS -eq 'true')
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
            ArgumentList = "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -Command `$ErrorActionPreference = 'Stop'; Import-Module -Name '$($Script:ModuleConstants.Paths.ModuleOutput)'; $([System.String]::Join('; ', (3, 4).ForEach({"New-ADTTemplate -Destination '$($Script:ModuleConstants.Paths.BuildOutput)' -Name 'Template_v$_' -Version $_"})))"
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

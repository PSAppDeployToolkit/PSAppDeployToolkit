#-----------------------------------------------------------------------------
#
# MARK: Reset-ADTModuleBuildOutputPath
#
#-----------------------------------------------------------------------------

function Reset-ADTModuleBuildOutputPath
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Remove any existing directory and re-create it. If the directory isn't empty, throw.
        Write-ADTBuildLogEntry -Message "Resetting module build output path [$($Script:ModuleConstants.Paths.BuildOutput)]."
        $null = Remove-Item -LiteralPath $Script:ModuleConstants.Paths.BuildOutput -Force -Recurse -ErrorAction Ignore
        $null = [System.IO.Directory]::CreateDirectory($Script:ModuleConstants.Paths.BuildOutput)
        if ($null -ne (Get-ChildItem -LiteralPath $Script:ModuleConstants.Paths.BuildOutput -ErrorAction Stop))
        {
            throw "The module build output path [$($Script:ModuleConstants.Paths.BuildOutput)] has files that were not able to be removed."
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}

#-----------------------------------------------------------------------------
#
# MARK: Test-ADTBuildEnvironment
#
#-----------------------------------------------------------------------------

function Test-ADTBuildEnvironment
{
    # Initialise the module build function.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Confirm we've got a supported PowerShell environment before proceeding.
        Write-ADTBuildLogEntry -Message "Testing that PowerShell is Windows PowerShell [$($Script:ModuleConstants.MinimumPowerShellVersion)] or higher."
        if ($PSVersionTable.PSVersion -lt $Script:ModuleConstants.MinimumPowerShellVersion)
        {
            throw "A version of PowerShell greater than $($Script:ModuleConstants.MinimumPowerShellVersion) is required."
        }

        # Confirm that the module can be found where we expect it.
        Write-ADTBuildLogEntry -Message "Locating the $($Script:ModuleConstants.ModuleName) module at [$($Script:ModuleConstants.Paths.ModuleManifest)]."
        $null = Get-Item -LiteralPath $Script:ModuleConstants.Paths.ModuleManifest -ErrorAction Stop

        # Confirm that the module manifest is valid.
        Write-ADTBuildLogEntry -Message "Testing the validity of the $($Script:ModuleConstants.ModuleName) module manifest."
        $null = Test-ModuleManifest -Path $Script:ModuleConstants.Paths.ModuleManifest

        # Confirm there's no module of the same name already imported.
        Write-ADTBuildLogEntry -Message "Checking whether $($Script:ModuleConstants.ModuleName) is already an imported module."
        if (Get-Module -Name $Script:ModuleConstants.ModuleName)
        {
            throw "A conflicting $($Script:ModuleConstants.ModuleName) module is already imported. Please restart PowerShell and try again."
        }

        # Confirm there's no conflicting module within the PSModulePath (i.e. installed from the gallery).
        Write-ADTBuildLogEntry -Message "Verifying $($Script:ModuleConstants.ModuleName) is not present within any PSModulePath directory."
        if (Get-ChildItem -LiteralPath $env:PSModulePath.Split(';') -Filter $Script:ModuleConstants.ModuleName -ErrorAction Ignore)
        {
            throw "A conflicting $($Script:ModuleConstants.ModuleName) module was found within a PSModulePath directory. Please uninstall any $($Script:ModuleConstants.ModuleName) modules from the PSGallery and try again."
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}

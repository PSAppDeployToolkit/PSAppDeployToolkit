#-----------------------------------------------------------------------------
#
# MARK: Confirm-ADTBuildModulesPresent
#
#-----------------------------------------------------------------------------

function Confirm-ADTBuildModulesPresent
{
    # Gather all installed modules for determination.
    Initialize-ADTModuleBuildFunction
    try
    {
        # Get all installed modules and see if anything's missing.
        if ($env:GITHUB_ACTIONS -ne 'true')
        {
            Write-ADTBuildLogEntry -Message "Confirming required PowerShell modules are installed."
            $installedModules = Get-InstalledModule; $missingModules = foreach ($requiredModule in $Script:ModuleConstants.RequiredModules)
            {
                # Return this ModuleSpecification if the module is missing.
                if (!($installedModule = $installedModules | & { process { if ($_.Name -eq $requiredModule.Name) { return $_ } } } | Select-Object -First 1))
                {
                    $requiredModule
                    continue
                }

                # Return this ModuleSpecification if the version is less than we need.
                if ($installedModule.Version -lt $requiredModule.Version)
                {
                    $requiredModule
                    continue
                }
            }

            # Return early if the required modules are available.
            if ($missingModules)
            {
                # We've got missing modules... Start by confirming NuGet is available so we can get them installed.
                Write-ADTBuildLogEntry -Message "The following modules are missing or out of date: [$([System.String]::Join(', ', $missingModules.Name))]"
                Write-ADTBuildLogEntry -Message "Confirming NuGet package provider state, please wait..."
                if (!($nugetProvider = Get-PackageProvider -Name NuGet -ListAvailable -ErrorAction Ignore) -or ($nugetProvider.Version -lt [System.Version]::new(2, 8, 5, 201)))
                {
                    Write-ADTBuildLogEntry -Message "Installing/updating NuGet package provider, please wait..."
                    $null = Install-PackageProvider -Name NuGet -Scope $Scope -Force
                }

                # Commence installing modules one by one. We need to do this individually as we can't specify names and differing minimum versions.
                foreach ($missingModule in $missingModules)
                {
                    # Special case for Pester certificate mismatch with older Pester versions (https://github.com/pester/Pester/issues/2389).
                    Write-ADTBuildLogEntry -Message "Installing module [$($missingModule.Name)], please wait..."
                    if (($missingModule.Name -eq 'Pester') -and ($PSVersionTable.PSVersion -le [System.Version]'5.1'))
                    {
                        Install-Module -Name $missingModule.Name -MinimumVersion $missingModule.Version -Scope CurrentUser -Force -SkipPublisherCheck
                    }
                    else
                    {
                        Install-Module -Name $missingModule.Name -MinimumVersion $missingModule.Version -Scope CurrentUser -Force
                    }
                }
            }
            else
            {
                Write-ADTBuildLogEntry -Message "Confirmed all required modules are already present."
            }
        }

        # Import all required modules ahead of time.
        foreach ($requiredModule in $Script:ModuleConstants.RequiredModules)
        {
            Write-ADTBuildLogEntry -Message "Importing module [$($requiredModule.Name)], please wait..."
            Import-Module -FullyQualifiedName $requiredModule -Global -Force
        }
        Complete-ADTModuleBuildFunction
    }
    catch
    {
        Complete-ADTModuleBuildFunction -ErrorRecord $_
        throw
    }
}

#-----------------------------------------------------------------------------
#
# MARK: Get-ADTInstalledModuleFromModulePath
#
#-----------------------------------------------------------------------------

function Get-ADTInstalledModuleFromModulePath
{
    [CmdletBinding(DefaultParameterSetName = 'SpecificVersions')]
    param
    (
        [Parameter(Mandatory = $false, ParameterSetName = 'SpecificVersions')]
        [Parameter(Mandatory = $false, ParameterSetName = 'AllVersions')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Name,

        [Parameter(Mandatory = $false, ParameterSetName = 'SpecificVersions')]
        [ValidateNotNullOrEmpty()]
        [System.Version]$MinimumVersion,

        [Parameter(Mandatory = $false, ParameterSetName = 'SpecificVersions')]
        [ValidateNotNullOrEmpty()]
        [System.Version]$RequiredVersion,

        [Parameter(Mandatory = $false, ParameterSetName = 'SpecificVersions')]
        [ValidateNotNullOrEmpty()]
        [System.Version]$MaximumVersion,

        [Parameter(Mandatory = $false, ParameterSetName = 'AllVersions')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SwitchParameter]$AllVersions
    )

    # Internal worker function to generate the output object.
    function New-ADTInstalledModuleInfo
    {
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.IO.DirectoryInfo]$Directory
        )

        # The output here is to mimic Get-InstalledModule as much as possible, but it's not 1:1.
        # Start off by getting the module's manifest information, if the module has a .psd1 file.
        $moduleInfo = if ([System.IO.File]::Exists("$($Directory.FullName)\$($Directory.Parent.BaseName).psd1"))
        {
            Import-LocalizedData -BaseDirectory $Directory.FullName -FileName "$($Directory.Parent.BaseName).psd1"
        }
        return [pscustomobject]@{
            Name = $Directory.Parent.BaseName
            Version = $Directory.BaseName
            Type = 'Module'
            Description = $(try { $moduleInfo.Description } catch { $null = $null })
            Author = $(try { $moduleInfo.Author } catch { $null = $null })
            CompanyName = $(try { $moduleInfo.CompanyName } catch { $null = $null })
            Copyright = $(try { $moduleInfo.Copyright } catch { $null = $null })
            PublishedDate = $null
            InstalledDate = $Directory.CreationTime
            UpdatedDate = $Directory.LastWriteTime
            LicenseUri = $(try { $moduleInfo.PrivateData.PSData.LicenseUri } catch { $null = $null })
            ProjectUri = $(try { $moduleInfo.PrivateData.PSData.ProjectUri } catch { $null = $null })
            IconUri = $(try { $moduleInfo.PrivateData.PSData.IconUri } catch { $null = $null })
            Tags = $(try { $moduleInfo.PrivateData.PSData.Tags } catch { $null = $null })
            Includes = $null
            PowerShellGetFormatVersion = $null
            ReleaseNotes = $null
            Dependencies = $(try { $moduleInfo.RequiredModules } catch { $null = $null })
            RepositorySourceLocation = $null
            Repository = $null
            PackageManagementProvider = $null
            AdditionalMetadata = $null
            InstalledLocation = $Directory.FullName
        }
    }

    # Get each individual ModulePath values. We reverse the array so we can return objects
    # in order of SystemDirectory/ProgramFiles/UserDirectory like PowerShellGet does.
    $modulePaths = [System.Environment]::GetEnvironmentVariable('PSModulePath').Split(';', [System.StringSplitOptions]::RemoveEmptyEntries).Trim() | & {
        begin
        {
            # Open collector to reverse at the end.
            $collector = [System.Collections.Generic.List[System.String]]::new()
        }
        process
        {
            # Skip over anything null/empty.
            if ([System.String]::IsNullOrWhiteSpace($_))
            {
                return
            }

            # Skip over any paths that don't exist.
            if (![System.IO.Directory]::Exists($_))
            {
                return
            }

            # Add this valid path to the collector.
            $collector.Add($_)
        }
        end
        {
            # Reverse the list and return it to the caller.
            $collector.Reverse()
            return $collector
        }
    }

    # Cycle through each name provided by the caller.
    foreach ($module in $(if ($Name) { $Name } else { (Get-ChildItem -LiteralPath $modulePaths -Directory).BaseName | Sort-Object -Unique }))
    {
        # Open a collector to hold all retrieved objects to return.
        $moduleDirectories = [System.Collections.Generic.List[System.IO.DirectoryInfo]]::new()

        # Cycle through each module path
        foreach ($path in $modulePaths)
        {
            # Get all installed modules that match our criteria.
            $installed = Get-ChildItem -Path "$path\$module" -Directory -ErrorAction Ignore | & {
                process
                {
                    # Skip any directory that isn't a version.
                    [System.Version]$version = $null; if (![System.Version]::TryParse($_.BaseName, [ref]$version))
                    {
                        return
                    }

                    # Handle the various versioning options.
                    if ($AllVersions -or (!$MinimumVersion -and !$RequiredVersion -and !$MaximumVersion))
                    {
                        return $_
                    }
                    if ($MinimumVersion -and ($version -ge $MinimumVersion))
                    {
                        return $_
                    }
                    if ($RequiredVersion -and ($version -eq $RequiredVersion))
                    {
                        return $_
                    }
                    if ($MaximumVersion -and ($version -le $MaximumVersion))
                    {
                        return $_
                    }
                }
            }

            # Add these to our collector if we have anything.
            if ($installed)
            {
                $moduleDirectories.AddRange([System.IO.DirectoryInfo[]]($installed | Sort-Object -Property @{ Expression = { [System.Version]$_.BaseName } } -Unique))
            }
        }

        # Process each found directory.
        if ($moduleDirectories.Count)
        {
            # For AllVersions, we return unique entries, but in the order we've collected them.
            if ($AllVersions)
            {
                # These get returned uniquely, but in the order we've collected them.
                foreach ($directory in ($moduleDirectories | Select-Object -Unique))
                {
                    $PSCmdlet.WriteObject((New-ADTInstalledModuleInfo -Directory $directory))
                }
                continue
            }

            # Handle the version-specific stuff next.
            $moduleDirectories = [System.IO.DirectoryInfo[]]($moduleDirectories | Sort-Object -Property @{ Expression = { [System.Version]$_.BaseName } } -Unique)
            if ($MinimumVersion)
            {
                # Return the highest version greater or equal to the minimum.
                if ($directory = $moduleDirectories | & { process { if ([System.Version]$_.BaseName -ge $MinimumVersion) { return $_ } } } | Select-Object -Last 1)
                {
                    $PSCmdlet.WriteObject((New-ADTInstalledModuleInfo -Directory $directory))
                }
                continue
            }
            if ($RequiredVersion)
            {
                # Return the exact version if we've got it.
                if ($directory = $moduleDirectories | & { process { if ([System.Version]$_.BaseName -eq $RequiredVersion) { return $_ } } } | Select-Object -First 1)
                {
                    $PSCmdlet.WriteObject((New-ADTInstalledModuleInfo -Directory $directory))
                }
                continue
            }
            if ($MaximumVersion)
            {
                # Return the highest version lesser or equal to the maximum
                if ($directory = $moduleDirectories | & { process { if ([System.Version]$_.BaseName -le $MaximumVersion) { return $_ } } } | Select-Object -Last 1)
                {
                    $PSCmdlet.WriteObject((New-ADTInstalledModuleInfo -Directory $directory))
                }
                continue
            }
            if (!$MinimumVersion -and !$RequiredVersion -and !$MaximumVersion)
            {
                # Return the highest version we've got.
                $PSCmdlet.WriteObject((New-ADTInstalledModuleInfo -Directory $moduleDirectories[-1]))
                continue
            }
        }
    }
}

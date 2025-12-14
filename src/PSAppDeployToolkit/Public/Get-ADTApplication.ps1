#-----------------------------------------------------------------------------
#
# MARK: Get-ADTApplication
#
#-----------------------------------------------------------------------------

function Get-ADTApplication
{
    <#
    .SYNOPSIS
        Retrieves information about installed applications.

    .DESCRIPTION
        Retrieves information about installed applications by querying the registry. You can specify an application name, a product code, or both. Returns information about application publisher, name & version, product code, uninstall string, install source, location, date, and application architecture.

    .PARAMETER Name
        The name of the application to retrieve information for. Performs a contains match on the application display name by default.

    .PARAMETER NameMatch
        Specifies the type of match to perform on the application name. Valid values are 'Contains', 'Exact', 'Wildcard', and 'Regex'. The default value is 'Contains'.

    .PARAMETER ProductCode
        The product code of the application to retrieve information for.

    .PARAMETER ApplicationType
        Specifies the type of application to remove. Valid values are 'All', 'MSI', and 'EXE'. The default value is 'All'.

    .PARAMETER IncludeUpdatesAndHotfixes
        Include matches against updates and hotfixes in results.

    .PARAMETER FilterScript
        A script used to filter the results as they're processed.

    .PARAMETER Appx
        Switch to the Appx application search mode.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.InstalledApplication

        Returns a custom type with information about an installed application:
        - PSPath
        - PSParentPath
        - PSChildName
        - ProductCode
        - DisplayName
        - DisplayVersion
        - UninstallString
        - QuietUninstallString
        - InstallSource
        - InstallLocation
        - InstallDate
        - Publisher
        - HelpLink
        - EstimatedSize
        - SystemComponent
        - WindowsInstaller
        - Is64BitApplication

    .EXAMPLE
        Get-ADTApplication

        This example retrieves information about all installed applications.

    .EXAMPLE
        Get-ADTApplication -Name 'Acrobat'

        Returns all applications that contain the name 'Acrobat' in the DisplayName.

    .EXAMPLE
        Get-ADTApplication -Name 'Adobe Acrobat Reader' -NameMatch 'Exact'

        Returns all applications that match the name 'Adobe Acrobat Reader' exactly.

    .EXAMPLE
        Get-ADTApplication -ProductCode '{AC76BA86-7AD7-1033-7B44-AC0F074E4100}'

        Returns the application with the specified ProductCode.

    .EXAMPLE
        Get-ADTApplication -Name 'Acrobat' -ApplicationType 'MSI' -FilterScript { $_.Publisher -match 'Adobe' }

        Returns all MSI applications that contain the name 'Acrobat' in the DisplayName and 'Adobe' in the Publisher name.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTApplication
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Appx', Justification = "Parameter is solely required to switch the parameter set.")]
    [CmdletBinding(DefaultParameterSetName = 'Arp')]
    [OutputType([PSADT.Types.InstalledApplication])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Name,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Contains', 'Exact', 'Wildcard', 'Regex')]
        [System.String]$NameMatch = 'Contains',

        [Parameter(Mandatory = $false, ParameterSetName = 'Arp')]
        [ValidateNotNullOrEmpty()]
        [System.Guid[]]$ProductCode,

        [Parameter(Mandatory = $false, ParameterSetName = 'Arp')]
        [ValidateSet('All', 'MSI', 'EXE')]
        [System.String]$ApplicationType = 'All',

        [Parameter(Mandatory = $false, ParameterSetName = 'Arp')]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes,

        [Parameter(Mandatory = $false, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$FilterScript,

        [Parameter(Mandatory = $true, ParameterSetName = 'Appx')]
        [System.Management.Automation.SwitchParameter]$Appx
    )

    begin
    {
        # Announce start.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $updatesSkippedCounter = 0
        $appxProvisionedKey = 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications'
        $appxKey = 'Microsoft.PowerShell.Core\Registry::HKEY_CLASSES_ROOT\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages'
        $uninstallKeyPaths = $(
            'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall'
            'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall'
            if ([System.Environment]::Is64BitProcess)
            {
                'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall'
            }
        )

        # If we're filtering by name, set up the relevant FilterScript.
        $nameFilterScript = if ($Name)
        {
            switch ($NameMatch)
            {
                Contains
                {
                    { foreach ($eachName in $Name) { if ($appDisplayName -like "*$eachName*") { $true; break } } }
                    break
                }
                Exact
                {
                    { foreach ($eachName in $Name) { if ($appDisplayName -eq $eachName) { $true; break } } }
                    break
                }
                Wildcard
                {
                    { foreach ($eachName in $Name) { if ($appDisplayName -like $eachName) { $true; break } } }
                    break
                }
                Regex
                {
                    { foreach ($eachName in $Name) { if ($appDisplayName -match $eachName) { $true; break } } }
                    break
                }
            }
        }

        # Define compiled regex for use throughout main loop.
        $updatesAndHotFixesRegex = [System.Text.RegularExpressions.Regex]::new('((?i)kb\d+|(Cumulative|Security) Update|Hotfix)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Compiled)
    }

    process
    {
        # Create a custom object with the desired properties for the installed applications and sanitize property details.
        Write-ADTLogEntry -Message "Getting information for installed $($PSCmdlet.ParameterSetName) applications$(if ($FilterScript) {' matching the provided FilterScript'})..."
        $installedApplication = if ($PSCmdlet.ParameterSetName -eq 'Appx')
        {
            foreach ($item in (Get-ChildItem -LiteralPath $appxKey -ErrorAction Ignore))
            {
                try
                {
                    try
                    {
                        if (-not ($packageRoot = $item.GetValue("Path")))
                        {
                            continue
                        }

                        # Extract the information from the manifest.
                        $manifest = Get-ADTPackageManifest -LiteralPath "$packageRoot\AppxManifest.xml"

                        # Decide which name to put through the filter.
                        $appDisplayName = if (![System.String]::IsNullOrWhiteSpace($manifest.DisplayName)) { $manifest.DisplayName } else { $manifest.Name }

                        # Apply name filter if specified.
                        if ($nameFilterScript -and !(& $nameFilterScript))
                        {
                            continue
                        }

                        # Calculate the application size.
                        $packageRootSize = 0; [System.IO.Directory]::GetFiles($packageRoot, "*", [System.IO.SearchOption]::AllDirectories) | . { process { $packageRootSize += $_.Length } }

                        # Consider applications in Windows\SystemApps non removable.
                        $nonRemovable = $packageRoot -like '*\Windows\SystemApps\*'

                        # Try to obtain information if the package was provisioned.
                        $provisionedPackage = $null
                        if (-not $nonRemovable)
                        {
                            if (Test-Path -LiteralPath "$appxProvisionedKey\$($manifest.FullName)")
                            {
                                $provisionedPackage = $manifest.FullName
                            }
                            else
                            {
                                foreach ($familyMember in (Get-ChildItem -LiteralPath $appxProvisionedKey | Where-Object { $_.PSChildName.StartsWith($manifest.Name) -and $_.PSChildName.EndsWith($manifest.PublisherId) }))
                                {
                                    $familyManifest = Get-ADTPackageManifest -LiteralPath ([System.Environment]::ExpandEnvironmentVariables($familyMember.GetValue('Path')))
                                    if ($familyManifest.IsBundle -and $manifest.FullName -in ($familyManifest.BundledApplications + $familyManifest.BundledResources))
                                    {
                                        $provisionedPackage = $familyManifest.FullName
                                        break
                                    }
                                }
                            }
                        }

                        # Construct different uninstall commands based off the provisioning status
                        $uninstallString = 'powershell.exe -NonInteractive -NoProfile -WindowStyle Hidden -Command "' + $(
                            if ([System.String]::IsNullOrWhitespace($provisionedPackage))
                            {
                                "Remove-AppxPackage -AllUsers -Package '$($manifest.FullName)' -ErrorAction Stop"
                            }
                            else
                            {
                                "Remove-AppxProvisionedPackage -Online -AllUsers -PackageName '$($manifest.FullName)' -ErrorAction Stop"
                            }
                        ) + '"'

                        # Build out the app object here before we filter as the caller needs to be able to filter on the object's properties.
                        $app = [PSADT.Types.InstalledAppxPackage]::new(
                            $item.PSPath,
                            $item.PSParentPath,
                            $item.PSChildName,
                            $appDisplayName,
                            $manifest.Version,
                            $uninstallString,
                            $null,
                            $packageRoot,
                            [PSADT.RegistryManagement.RegistryUtilities]::GetRegistryKeyLastWriteTime($item.PSPath).Date,
                            $(if (![System.String]::IsNullOrWhiteSpace($manifest.PublisherDisplayName)) { $manifest.PublisherDisplayName } else { $manifest.PublisherDistinguishedName }),
                            $packageRootSize,
                            $manifest.Architecture.Contains("64") -or $manifest.Architecture -eq "neutral",
                            $manifest.FullName,
                            $manifest.FamilyName,
                            $manifest.PublisherId,
                            $manifest.Architecture,
                            $manifest.IsBundle,
                            $manifest.IsResource,
                            $manifest.IsFramework,
                            $nonRemovable,
                            $provisionedPackage
                        )

                        # Build out an object and return it to the pipeline if there's no FilterScript or the FilterScript returns something.
                        if (!$FilterScript -or (ForEach-Object -InputObject $app -Process $FilterScript -ErrorAction Ignore))
                        {
                            Write-ADTLogEntry -Message "Found package [$($app.DisplayName)$(if ($app.DisplayVersion -and !$app.DisplayName.Contains($app.DisplayVersion)) {" $($app.DisplayVersion)"})]."
                            $app
                        }
                    }
                    catch
                    {
                        Write-Error -ErrorRecord $_
                    }
                }
                catch
                {
                    Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to process the uninstall data [$item]: $($_.Exception.Message)." -ErrorAction SilentlyContinue
                }
            }
        }
        else
        {
            foreach ($item in (Get-ChildItem -LiteralPath $uninstallKeyPaths -ErrorAction Ignore))
            {
                try
                {
                    try
                    {
                        # Set up initial variables.
                        $defUriValue = [System.Uri][System.String]::Empty
                        $installDate = [System.DateTime]::MinValue
                        $defaultGuid = [System.Guid]::Empty

                        # Exclude anything without any properties.
                        if (!$item.GetValueNames())
                        {
                            continue
                        }

                        # Exclude anything without a DisplayName field.
                        if (!($appDisplayName = $item.GetValue('DisplayName', $null)) -or [System.String]::IsNullOrWhiteSpace($appDisplayName))
                        {
                            continue
                        }

                        # Bypass any updates or hotfixes.
                        if (!$IncludeUpdatesAndHotfixes -and $updatesAndHotFixesRegex.Matches($appDisplayName).Count)
                        {
                            $updatesSkippedCounter++
                            continue
                        }

                        # Apply name filter if specified.
                        if ($nameFilterScript -and !(& $nameFilterScript))
                        {
                            continue
                        }

                        # Grab all available uninstall string.
                        if (($uninstallString = $item.GetValue('UninstallString', $null)) -and [System.String]::IsNullOrWhiteSpace($uninstallString.Replace('"', $null)))
                        {
                            $uninstallString = $null
                        }
                        if (($quietUninstallString = $item.GetValue('QuietUninstallString', $null)) -and [System.String]::IsNullOrWhiteSpace($quietUninstallString.Replace('"', $null)))
                        {
                            $quietUninstallString = $null
                        }

                        # Apply application type filter if specified.
                        $windowsInstaller = $item.GetValue('WindowsInstaller', $false) -or ($uninstallString -match 'msiexec') -or ($quietUninstallString -match 'msiexec')
                        if ((($ApplicationType -eq 'MSI') -and !$windowsInstaller) -or (($ApplicationType -eq 'EXE') -and $windowsInstaller))
                        {
                            continue
                        }

                        # Apply ProductCode filter if specified.
                        $appMsiGuid = if ($windowsInstaller -and [System.Guid]::TryParse($item.PSChildName, [ref]$defaultGuid)) { $defaultGuid }
                        if ($ProductCode -and (!$appMsiGuid -or ($ProductCode -notcontains $appMsiGuid)))
                        {
                            continue
                        }

                        # Determine the install date. If the key has a valid property, we use it. If not, we get the LastWriteDate for the key from the registry.
                        if (![System.DateTime]::TryParseExact($item.GetValue('InstallDate', $null), 'yyyyMMdd', [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::None, [ref]$installDate))
                        {
                            $installDate = [PSADT.RegistryManagement.RegistryUtilities]::GetRegistryKeyLastWriteTime($item.PSPath).Date
                        }

                        # Build hashtable of calculated properties based on their presence in the registry and the value's validity.
                        $appProperties = @{}; 'DisplayVersion', 'Publisher', 'EstimatedSize' | & {
                            process
                            {
                                if (![System.String]::IsNullOrWhiteSpace(($value = $item.GetValue($_, $null))))
                                {
                                    $appProperties.Add($_, $value)
                                }
                            }
                        }

                        # Process the source/location directory paths.
                        'InstallSource', 'InstallLocation' | & {
                            process
                            {
                                if (![System.String]::IsNullOrWhiteSpace(($value = $item.GetValue($_, [System.String]::Empty).TrimStart('"').TrimEnd('"'))) -and [PSADT.FileSystem.FileSystemUtilities]::IsValidFilePath($value))
                                {
                                    $appProperties.Add($_, $value)
                                }
                            }
                        }

                        # Process the HelpLink, accepting only valid URLs.
                        if ([System.Uri]::TryCreate($item.GetValue('HelpLink', [System.String]::Empty), [System.UriKind]::Absolute, [ref]$defUriValue))
                        {
                            $appProperties.Add('HelpLink', $defUriValue)
                        }

                        # Build out the app object here before we filter as the caller needs to be able to filter on the object's properties.
                        $app = if (!$windowsInstaller)
                        {
                            [PSADT.Types.InstalledArpApplication]::new(
                                $item.PSPath,
                                $item.PSParentPath,
                                $item.PSChildName,
                                $appDisplayName,
                                $appProperties['DisplayVersion'],
                                $uninstallString,
                                $appProperties['InstallSource'],
                                $appProperties['InstallLocation'],
                                $installDate,
                                $appProperties['Publisher'],
                                $appProperties['EstimatedSize'],
                                ([System.Environment]::Is64BitProcess -and ($item.PSPath -notmatch '^Microsoft\.PowerShell\.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node')),
                                $quietUninstallString,
                                $appProperties['HelpLink'],
                                $item.GetValue('SystemComponent', $false)
                            )
                        }
                        else
                        {
                            [PSADT.Types.InstalledMsiApplication]::new(
                                $item.PSPath,
                                $item.PSParentPath,
                                $item.PSChildName,
                                $appDisplayName,
                                $appProperties['DisplayVersion'],
                                $uninstallString,
                                $appProperties['InstallSource'],
                                $appProperties['InstallLocation'],
                                $installDate,
                                $appProperties['Publisher'],
                                $appProperties['EstimatedSize'],
                                ([System.Environment]::Is64BitProcess -and ($item.PSPath -notmatch '^Microsoft\.PowerShell\.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node')),
                                $quietUninstallString,
                                $appProperties['HelpLink'],
                                $item.GetValue('SystemComponent', $false),
                                $appMsiGuid
                            )
                        }

                        # Build out an object and return it to the pipeline if there's no FilterScript or the FilterScript returns something.
                        if (!$FilterScript -or (ForEach-Object -InputObject $app -Process $FilterScript -ErrorAction Ignore))
                        {
                            Write-ADTLogEntry -Message "Found installed application [$($app.DisplayName)$(if ($app.DisplayVersion -and !$app.DisplayName.Contains($app.DisplayVersion)) {" $($app.DisplayVersion)"})]."
                            $app
                        }

                        # Write to log the number of entries skipped due to them being considered updates.
                        if (!$IncludeUpdatesAndHotfixes -and $updatesSkippedCounter)
                        {
                            if ($updatesSkippedCounter -eq 1)
                            {
                                Write-ADTLogEntry -Message 'Skipped 1 entry while searching, because it was considered a Microsoft update.'
                            }
                            else
                            {
                                Write-ADTLogEntry -Message "Skipped $updatesSkippedCounter entries while searching, because they were considered Microsoft updates."
                            }
                        }
                    }
                    catch
                    {
                        Write-Error -ErrorRecord $_
                    }
                }
                catch
                {
                    Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to process the uninstall data [$item]: $($_.Exception.Message)." -ErrorAction SilentlyContinue
                }
            }
        }

        # Return any accumulated apps to the caller.
        if ($installedApplication)
        {
            return $installedApplication
        }
        Write-ADTLogEntry -Message 'Found no application based on the supplied FilterScript.'
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

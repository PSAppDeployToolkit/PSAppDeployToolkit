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

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ProductCode', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'ApplicationType', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    [OutputType([PSADT.Types.InstalledApplication])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Name,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Contains', 'Exact', 'Wildcard', 'Regex')]
        [System.String]$NameMatch = 'Contains',

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Guid[]]$ProductCode,

        [Parameter(Mandatory = $false)]
        [ValidateSet('All', 'MSI', 'EXE', 'APPX')]
        [System.String]$ApplicationType = 'All',

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes,

        [Parameter(Mandatory = $false, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$FilterScript
    )

    begin
    {
        # Announce start.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $updatesSkippedCounter = 0
        $appxKey = 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications'
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
        Write-ADTLogEntry -Message "Getting information for installed applications$(if ($FilterScript) {' matching the provided FilterScript'})..."
        $installedApplication = if ($ApplicationType -eq 'APPX')
        {
            foreach ($item in (Get-ChildItem -LiteralPath $appxKey -ErrorAction Ignore))
            {
                try
                {
                    try
                    {
                        $manifest = [PSADT.PackageManagement.AppxUtilities]::GetProvisionedPackageManifest($item.PSChildName)
                        $appDisplayName = $manifest.Name

                        # Apply name filter if specified.
                        if ($nameFilterScript -and !(& $nameFilterScript))
                        {
                            continue
                        }

                        # Extract the package root from the manifest path.
                        $packageRoot = [System.IO.DirectoryInfo]::new(
                            $(
                                if ($manifest.IsBundle)
                                {
                                    [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetDirectoryName($manifest.Path))
                                }
                                else
                                {
                                    [System.IO.Path]::GetDirectoryName($manifest.Path)
                                }
                            )
                        )

                        # Calculate the application size.
                        $packageRootSize = 0; $packageRoot.GetFiles("*", [System.IO.SearchOption]::AllDirectories) | . { process { $packageRootSize += $_.Length } }

                        # Build out the app object here before we filter as the caller needs to be able to filter on the object's properties.
                        $app = [PSADT.Types.InstalledApplication]::new(
                            $item.PSPath,
                            $item.PSParentPath,
                            $item.PSChildName,
                            $null,
                            $appDisplayName,
                            $manifest.Version,
                            "$(Get-ADTPowerShellProcessPath) -NonInteractive -NoProfile -WindowStyle Hidden -Command `"Remove-AppxProvisionedPackage -Online -AllUsers -PackageName '$($manifest.FullNameIdentifier)' -ErrorAction Stop`"",
                            $null,
                            $null,
                            $packageRoot,
                            [PSADT.RegistryManagement.RegistryUtilities]::GetRegistryKeyLastWriteTime($item.PSPath).Date,
                            $manifest.Publisher,
                            $null,
                            $packageRootSize,
                            $false,
                            $false,
                            $manifest.Architecture -in @([PSADT.PackageManagement.ProcessorArchitecture]::X64, [PSADT.PackageManagement.ProcessorArchitecture]::Arm64, [PSADT.PackageManagement.ProcessorArchitecture]::Neutral)
                        )

                        # Build out an object and return it to the pipeline if there's no filterscript or the filterscript returns something.
                        if (!$FilterScript -or (ForEach-Object -InputObject $app -Process $FilterScript -ErrorAction Ignore))
                        {
                            Write-ADTLogEntry -Message "Found installed application [$($app.DisplayName)$(if ($app.DisplayVersion -and !$app.DisplayName.Contains($app.DisplayVersion)) {" $($app.DisplayVersion)"})]."
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
                        $app = [PSADT.Types.InstalledApplication]::new(
                            $item.PSPath,
                            $item.PSParentPath,
                            $item.PSChildName,
                            $appMsiGuid,
                            $appDisplayName,
                            $appProperties['DisplayVersion'],
                            $uninstallString,
                            $quietUninstallString,
                            $appProperties['InstallSource'],
                            $appProperties['InstallLocation'],
                            $installDate,
                            $appProperties['Publisher'],
                            $appProperties['HelpLink'],
                            $appProperties['EstimatedSize'],
                            $item.GetValue('SystemComponent', $false),
                            $windowsInstaller,
                            ([System.Environment]::Is64BitProcess -and ($item.PSPath -notmatch '^Microsoft\.PowerShell\.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node'))
                        )

                        # Build out an object and return it to the pipeline if there's no filterscript or the filterscript returns something.
                        if (!$FilterScript -or (ForEach-Object -InputObject $app -Process $FilterScript -ErrorAction Ignore))
                        {
                            Write-ADTLogEntry -Message "Found installed application [$($app.DisplayName)$(if ($app.DisplayVersion -and !$app.DisplayName.Contains($app.DisplayVersion)) {" $($app.DisplayVersion)"})]."
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


        # Write to log the number of entries skipped due to them being considered updates.
        if (!$IncludeUpdatesAndHotfixes -and $updatesSkippedCounter)
        {
            if ($updatesSkippedCounter -eq 1)
            {
                Write-ADTLogEntry -Message 'Skipped 1 entry while searching, because it was considered a Microsoft update.'
            }
            else
            {
                Write-ADTLogEntry -Message "Skipped $UpdatesSkippedCounter entries while searching, because they were considered Microsoft updates."
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

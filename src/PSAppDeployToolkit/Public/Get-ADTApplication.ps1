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
        - UninstallKey
        - UninstallParentKey
        - UninstallSubKey
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
        [ValidateSet('All', 'MSI', 'EXE')]
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
                    { foreach ($eachName in $Name) { if ($appRegProps.DisplayName -like "*$eachName*") { $true; break } } }
                    break
                }
                Exact
                {
                    { foreach ($eachName in $Name) { if ($appRegProps.DisplayName -eq $eachName) { $true; break } } }
                    break
                }
                Wildcard
                {
                    { foreach ($eachName in $Name) { if ($appRegProps.DisplayName -like $eachName) { $true; break } } }
                    break
                }
                Regex
                {
                    { foreach ($eachName in $Name) { if ($appRegProps.DisplayName -match $eachName) { $true; break } } }
                    break
                }
            }
        }
    }

    process
    {
        # Create a custom object with the desired properties for the installed applications and sanitize property details.
        Write-ADTLogEntry -Message "Getting information for installed applications$(if ($FilterScript) {' matching the provided FilterScript'})..."
        $installedApplication = foreach ($item in (Get-ChildItem -LiteralPath $uninstallKeyPaths -ErrorAction Ignore))
        {
            try
            {
                try
                {
                    # Set up initial variables.
                    $appRegProps = Get-ItemProperty -LiteralPath $item.PSPath
                    $psPropNames = $appRegProps.PSObject.Properties | Select-Object -ExpandProperty Name
                    $defUriValue = [System.Uri][System.String]::Empty
                    $installDate = [System.DateTime]::MinValue
                    $defaultGuid = [System.Guid]::Empty

                    # Exclude anything without any properties.
                    if (!$psPropNames)
                    {
                        continue
                    }

                    # Exclude anything without a DisplayName field.
                    if (!$psPropNames.Contains('DisplayName') -or [System.String]::IsNullOrWhiteSpace($appRegProps.DisplayName))
                    {
                        continue
                    }

                    # Bypass any updates or hotfixes.
                    if (!$IncludeUpdatesAndHotfixes -and ($appRegProps.DisplayName -match '((?i)kb\d+|(Cumulative|Security) Update|Hotfix)'))
                    {
                        $updatesSkippedCounter++
                        continue
                    }

                    # Apply application type filter if specified.
                    $windowsInstaller = !!$(if ($psPropNames.Contains('WindowsInstaller')) { $appRegProps.WindowsInstaller })
                    if ((($ApplicationType -eq 'MSI') -and !$windowsInstaller) -or (($ApplicationType -eq 'EXE') -and $windowsInstaller))
                    {
                        continue
                    }

                    # Apply ProductCode filter if specified.
                    $appMsiGuid = if ($windowsInstaller -and [System.Guid]::TryParse($appRegProps.PSChildName, [ref]$defaultGuid)) { $defaultGuid }
                    if ($ProductCode -and (!$appMsiGuid -or ($ProductCode -notcontains $appMsiGuid)))
                    {
                        continue
                    }

                    # Apply name filter if specified.
                    if ($nameFilterScript -and !(& $nameFilterScript))
                    {
                        continue
                    }

                    # Determine the install date. If the key has a valid property, we use it. If not, we get the LastWriteDate for the key from the registry.
                    if (!$psPropNames.Contains('InstallDate') -or ![System.DateTime]::TryParseExact($appRegProps.InstallDate, "yyyyMMdd", [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::None, [ref]$installDate))
                    {
                        $installDate = [PSADT.Utilities.RegistryUtilities]::GetRegistryKeyLastWriteTime($item.PSPath).Date
                    }

                    # Build out the app object here before we filter as the caller needs to be able to filter on the object's properties.
                    $app = [PSADT.Types.InstalledApplication]::new(
                        $appRegProps.PSPath,
                        $appRegProps.PSParentPath,
                        $appRegProps.PSChildName,
                        $appMsiGuid,
                        $appRegProps.DisplayName,
                        $(if ($psPropNames.Contains('DisplayVersion') -and ![System.String]::IsNullOrWhiteSpace($appRegProps.DisplayVersion)) { $appRegProps.DisplayVersion }),
                        $(if ($psPropNames.Contains('UninstallString') -and ![System.String]::IsNullOrWhiteSpace($appRegProps.UninstallString)) { $appRegProps.UninstallString }),
                        $(if ($psPropNames.Contains('QuietUninstallString') -and ![System.String]::IsNullOrWhiteSpace($appRegProps.QuietUninstallString)) { $appRegProps.QuietUninstallString }),
                        $(if ($psPropNames.Contains('InstallSource') -and ![System.String]::IsNullOrWhiteSpace($appRegProps.InstallSource)) { $appRegProps.InstallSource }),
                        $(if ($psPropNames.Contains('InstallLocation') -and ![System.String]::IsNullOrWhiteSpace($appRegProps.InstallLocation)) { $appRegProps.InstallLocation }),
                        $installDate,
                        $(if ($psPropNames.Contains('Publisher') -and ![System.String]::IsNullOrWhiteSpace($appRegProps.Publisher)) { $appRegProps.Publisher }),
                        $(if ($psPropNames.Contains('HelpLink') -and ![System.String]::IsNullOrWhiteSpace($appRegProps.HelpLink) -and [System.Uri]::TryCreate($appRegProps.HelpLink, [System.UriKind]::Absolute, [ref]$defUriValue)) { $defUriValue }),
                        $(if ($psPropNames.Contains('EstimatedSize') -and ![System.String]::IsNullOrWhiteSpace($appRegProps.EstimatedSize)) { $appRegProps.EstimatedSize }),
                        !!$(if ($psPropNames.Contains('SystemComponent')) { $appRegProps.SystemComponent }),
                        $windowsInstaller,
                        ([System.Environment]::Is64BitProcess -and ($appRegProps.PSPath -notmatch '^Microsoft\.PowerShell\.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node'))
                    )

                    # Build out an object and return it to the pipeline if there's no filterscript or the filterscript returns something.
                    if (!$FilterScript -or (ForEach-Object -InputObject $app -Process $FilterScript -ErrorAction Ignore))
                    {
                        Write-ADTLogEntry -Message "Found installed application [$($app.DisplayName)]$(if ($app.DisplayVersion) {" version [$($app.DisplayVersion)]"})."
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

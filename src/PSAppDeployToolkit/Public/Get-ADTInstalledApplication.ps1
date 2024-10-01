#-----------------------------------------------------------------------------
#
# MARK: Get-ADTInstalledApplication
#
#-----------------------------------------------------------------------------

function Get-ADTInstalledApplication
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
        - Publisher
        - DisplayName
        - DisplayVersion
        - ProductCode
        - UninstallString
        - InstallSource
        - InstallLocation
        - InstallDate
        - Architecture

    .EXAMPLE
        Get-ADTInstalledApplication

        This example retrieves information about all installed applications.

    .EXAMPLE
        Get-ADTInstalledApplication -Name 'Acrobat'

        Returns all applications that contain the name 'Acrobat' in the DisplayName.

    .EXAMPLE
        Get-ADTInstalledApplication -Name 'Adobe Acrobat Reader' -NameMatch 'Exact'

        Returns all applications that match the name 'Adobe Acrobat Reader' exactly.

    .EXAMPLE
        Get-ADTInstalledApplication -ProductCode '{AC76BA86-7AD7-1033-7B44-AC0F074E4100}'

        Returns the application with the specified ProductCode.

    .EXAMPLE
        Get-ADTInstalledApplication -Name 'Acrobat' -ApplicationType 'MSI' -FilterScript { $_.Publisher -match 'Adobe' }

        Returns all MSI applications that contain the name 'Acrobat' in the DisplayName and 'Adobe' in the Publisher name.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'NameMatch', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
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
        [System.String[]]$ProductCode,

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
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $updatesSkippedCounter = 0
        $uninstallKeyPaths = $(
            'Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
            'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
            if ([System.Environment]::Is64BitProcess)
            {
                'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
            }
        )

        # If we're filtering by name, set up the relevant FilterScript.
        $nameFilterScript = if ($Name)
        {
            switch ($NameMatch)
            {
                Contains
                {
                    { $Name -like "*$($_)*" }
                }
                Exact
                {
                    { $Name -eq $_ }
                }
                Wildcard
                {
                    { $Name -like $_ }
                }
                Regex
                {
                    { $Name -match $_ }
                }
            }
        }
    }

    process
    {
        & $Script:CommandTable.'Write-ADTLogEntry' -Message "Getting information for installed applications$(if ($FilterScript) {' matching the provided FilterScript'})..."
        try
        {
            try
            {
                # Create a custom object with the desired properties for the installed applications and sanitize property details.
                $installedApplication = & $Script:CommandTable.'Get-ItemProperty' -Path $uninstallKeyPaths -ErrorAction Ignore | & {
                    process
                    {
                        # Exclude anything without a DisplayName field.
                        if (!$_.PSObject.Properties.Name.Contains('DisplayName') -or [System.String]::IsNullOrWhiteSpace($_.DisplayName))
                        {
                            return
                        }

                        # Apply name filter if specified.
                        if ($nameFilterScript -and !(& $Script:CommandTable.'ForEach-Object' -InputObject $_.DisplayName -Process $nameFilterScript))
                        {
                            return
                        }

                        # Bypass any updates or hotfixes.
                        if (!$IncludeUpdatesAndHotfixes -and ($_.DisplayName -match '((?i)kb\d+|(Cumulative|Security) Update|Hotfix)'))
                        {
                            $updatesSkippedCounter++
                            return
                        }

                        # Apply ProductCode filter if specified.
                        $appMsiGuid = $(if ($_.PSChildName -match '^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$') { $_.PSChildName })
                        if ($appMsiGuid -and $ProductCode -and ($ProductCode -notcontains $appMsiGuid))
                        {
                            return
                        }

                        # Apply application type filter if specified.
                        $windowsInstaller = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty WindowsInstaller -ErrorAction Ignore
                        if (($ApplicationType -ne 'All') -and (($ApplicationType -eq 'MSI') -eq !$windowsInstaller))
                        {
                            return
                        }

                        # Build out the app object here before we filter as the caller needs to be able to filter on the object's properties.
                        $app = [PSADT.Types.InstalledApplication]@{
                            UninstallKey         = $_.PSPath
                            UninstallParentKey   = $_.PSParentPath
                            UninstallSubKey      = $_.PSChildName
                            ProductCode          = $appMsiGuid
                            DisplayName          = $_.DisplayName
                            DisplayVersion       = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty DisplayVersion -ErrorAction Ignore
                            UninstallString      = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty UninstallString -ErrorAction Ignore
                            QuietUninstallString = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty QuietUninstallString -ErrorAction Ignore
                            InstallSource        = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty InstallSource -ErrorAction Ignore
                            InstallLocation      = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty InstallLocation -ErrorAction Ignore
                            InstallDate          = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty InstallDate -ErrorAction Ignore
                            Publisher            = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty Publisher -ErrorAction Ignore
                            SystemComponent      = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty SystemComponent -ErrorAction Ignore
                            WindowsInstaller     = $windowsInstaller
                            Is64BitApplication   = [System.Environment]::Is64BitProcess -and ($_.PSPath -notmatch '^Microsoft\.PowerShell\.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node')
                        }

                        # Build out an object and return it to the pipeline if there's no filterscript or the filterscript returns something.
                        if (!$FilterScript -or (& $Script:CommandTable.'ForEach-Object' -InputObject $app -Process $FilterScript -ErrorAction Ignore))
                        {
                            & $Script:CommandTable.'Write-ADTLogEntry' -Message "Found installed application [$($app.DisplayName)]$(if ($app.DisplayVersion) {" version [$($app.DisplayVersion)]"})."
                            return $app
                        }
                    }
                }

                # Write to log the number of entries skipped due to them being considered updates.
                if (!$IncludeUpdatesAndHotfixes -and $updatesSkippedCounter)
                {
                    if ($updatesSkippedCounter -eq 1)
                    {
                        & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Skipped 1 entry while searching, because it was considered a Microsoft update.'
                    }
                    else
                    {
                        & $Script:CommandTable.'Write-ADTLogEntry' -Message "Skipped $UpdatesSkippedCounter entries while searching, because they were considered Microsoft updates."
                    }
                }

                # Return any accumulated apps to the caller.
                if ($installedApplication)
                {
                    return $installedApplication
                }
                & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Found no application based on the supplied FilterScript.'
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}

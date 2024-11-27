﻿#-----------------------------------------------------------------------------
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

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
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
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $msiProductCodeRegex = Get-ADTMsiProductCodeRegexPattern
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
                    { foreach ($eachName in $Name) { if ($_.DisplayName -like "*$eachName*") { $true; break } } }
                    break
                }
                Exact
                {
                    { foreach ($eachName in $Name) { if ($_.DisplayName -eq $eachName) { $true; break } } }
                    break
                }
                Wildcard
                {
                    { foreach ($eachName in $Name) { if ($_.DisplayName -like $eachName) { $true; break } } }
                    break
                }
                Regex
                {
                    { foreach ($eachName in $Name) { if ($_.DisplayName -match $eachName) { $true; break } } }
                    break
                }
            }
        }
    }

    process
    {
        Write-ADTLogEntry -Message "Getting information for installed applications$(if ($FilterScript) {' matching the provided FilterScript'})..."
        try
        {
            try
            {
                # Create a custom object with the desired properties for the installed applications and sanitize property details.
                $installedApplication = Get-ItemProperty -Path $uninstallKeyPaths -ErrorAction Ignore | & {
                    process
                    {
                        # Exclude anything without a DisplayName field.
                        if (!$_.PSObject.Properties.Name.Contains('DisplayName') -or [System.String]::IsNullOrWhiteSpace($_.DisplayName))
                        {
                            return
                        }

                        # Bypass any updates or hotfixes.
                        if (!$IncludeUpdatesAndHotfixes -and ($_.DisplayName -match '((?i)kb\d+|(Cumulative|Security) Update|Hotfix)'))
                        {
                            $updatesSkippedCounter++
                            return
                        }

                        # Apply name filter if specified.
                        if ($nameFilterScript -and !(& $nameFilterScript))
                        {
                            return
                        }

                        # Apply ProductCode filter if specified.
                        $appMsiGuid = if ($_.PSChildName -match $msiProductCodeRegex) { $_.PSChildName }
                        if ($appMsiGuid -and $ProductCode -and ($ProductCode -notcontains $appMsiGuid))
                        {
                            return
                        }

                        # Apply application type filter if specified.
                        $windowsInstaller = !!($_ | Select-Object -ExpandProperty WindowsInstaller -ErrorAction Ignore)
                        if (($ApplicationType -ne 'All') -and (($ApplicationType -eq 'MSI') -ne $windowsInstaller))
                        {
                            return
                        }

                        # Build out the app object here before we filter as the caller needs to be able to filter on the object's properties.
                        $app = [PSADT.Types.InstalledApplication]::new(
                            $_.PSPath,
                            $_.PSParentPath,
                            $_.PSChildName,
                            $appMsiGuid,
                            $_.DisplayName,
                            ($_ | Select-Object -ExpandProperty DisplayVersion -ErrorAction Ignore),
                            ($_ | Select-Object -ExpandProperty UninstallString -ErrorAction Ignore),
                            ($_ | Select-Object -ExpandProperty QuietUninstallString -ErrorAction Ignore),
                            ($_ | Select-Object -ExpandProperty InstallSource -ErrorAction Ignore),
                            ($_ | Select-Object -ExpandProperty InstallLocation -ErrorAction Ignore),
                            ($_ | Select-Object -ExpandProperty InstallDate -ErrorAction Ignore),
                            ($_ | Select-Object -ExpandProperty Publisher -ErrorAction Ignore),
                            ($_ | Select-Object -ExpandProperty SystemComponent -ErrorAction Ignore),
                            $windowsInstaller,
                            ([System.Environment]::Is64BitProcess -and ($_.PSPath -notmatch '^Microsoft\.PowerShell\.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node'))
                        )

                        # Build out an object and return it to the pipeline if there's no filterscript or the filterscript returns something.
                        if (!$FilterScript -or (ForEach-Object -InputObject $app -Process $FilterScript -ErrorAction Ignore))
                        {
                            Write-ADTLogEntry -Message "Found installed application [$($app.DisplayName)]$(if ($app.DisplayVersion) {" version [$($app.DisplayVersion)]"})."
                            return $app
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
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

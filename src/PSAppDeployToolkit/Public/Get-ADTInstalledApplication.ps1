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

    .PARAMETER FilterScript
        A script used to filter the results as they're processed.

    .PARAMETER IncludeUpdatesAndHotfixes
        Include matches against updates and hotfixes in results.

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
        Get-ADTInstalledApplication -FilterScript { $_.DisplayName -eq 'Adobe Flash' }

        This example retrieves information about installed applications with the name 'Adobe Flash'.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'FilterScript', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    [OutputType([PSADT.Types.InstalledApplication])]
    param
    (
        [Parameter(Mandatory = $false, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ScriptBlock]$FilterScript,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes
    )

    begin
    {
        # Announce start.
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $updatesSkippedCounter = 0
        $uninstallKeyPaths = @('Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*')
        if ([System.Environment]::Is64BitProcess)
        {
            $uninstallKeyPaths += 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
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

                        # Bypass any updates or hotfixes.
                        if (!$IncludeUpdatesAndHotfixes -and ($_.DisplayName -match '((?i)kb\d+|(Cumulative|Security) Update|Hotfix)'))
                        {
                            $updatesSkippedCounter++
                            return
                        }

                        # Build out the app object here before we filter as the caller needs to be able to filter on the object's properties.
                        $app = [PSADT.Types.InstalledApplication]@{
                            UninstallSubkey      = $_.PSChildName
                            ProductCode          = $(if ($_.PSChildName -match '^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$') { $_.PSChildName })
                            DisplayName          = $_.DisplayName
                            DisplayVersion       = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty DisplayVersion -ErrorAction Ignore
                            UninstallString      = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty UninstallString -ErrorAction Ignore
                            QuietUninstallString = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty QuietUninstallString -ErrorAction Ignore
                            InstallSource        = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty InstallSource -ErrorAction Ignore
                            InstallLocation      = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty InstallLocation -ErrorAction Ignore
                            InstallDate          = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty InstallDate -ErrorAction Ignore
                            Publisher            = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty Publisher -ErrorAction Ignore
                            SystemComponent      = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty SystemComponent -ErrorAction Ignore
                            WindowsInstaller     = $_ | & $Script:CommandTable.'Select-Object' -ExpandProperty WindowsInstaller -ErrorAction Ignore
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

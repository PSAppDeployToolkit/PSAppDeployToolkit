#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTInstalledApplication
{
    <#

    .SYNOPSIS
    Retrieves information about installed applications.

    .DESCRIPTION
    Retrieves information about installed applications by querying the registry. You can specify an application name, a product code, or both.

    Returns information about application publisher, name & version, product code, uninstall string, install source, location, date, and application architecture.

    .PARAMETER Name
    The name of the application to retrieve information for. Performs a contains match on the application display name by default.

    .PARAMETER Exact
    Specifies that the named application must be matched using the exact name.

    .PARAMETER WildCard
    Specifies that the named application must be matched using a wildcard search.

    .PARAMETER RegEx
    Specifies that the named application must be matched using a regular expression search.

    .PARAMETER ProductCode
    The product code of the application to retrieve information for.

    .PARAMETER IncludeUpdatesAndHotfixes
    Include matches against updates and hotfixes in results.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    PSADT.Types.InstalledApplication. Returns a custom type with information about an installed application:
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
    Get-ADTInstalledApplication -Name 'Adobe Flash'

    .EXAMPLE
    Get-ADTInstalledApplication -ProductCode '{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Name,

        [Parameter(Mandatory = $false)]
        [ValidatePattern('^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$')]
        [System.String[]]$ProductCode,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Exact,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$WildCard,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$RegEx,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes
    )

    begin
    {
        # Announce start.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Enumerate the installed applications from the registry for applications that have the "DisplayName" property.
        $regUninstallPaths = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*', 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
        $regKeyApplication = & $Script:CommandTable.'Get-ItemProperty' -Path $regUninstallPaths | & $Script:CommandTable.'Where-Object' {$_.PSObject.Properties.Name.Contains('DisplayName') -and ![System.String]::IsNullOrWhiteSpace($_.DisplayName)}

        # Set up variables needed in main loop.
        $updatesSkippedCounter = 0
        $msiProductCodeRegex = '^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$'
        $wow6432PSPathRegex = '^Microsoft\.PowerShell\.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node'
        $updatesHotfixRegex = '((?i)kb\d+|(Cumulative|Security) Update|Hotfix)'
        $stringControlChars = '[^\p{L}\p{Nd}\p{Z}\p{P}]'

        # Ensure provided data in unique.
        ('Name','ProductCode').Where({$PSBoundParameters.ContainsKey($_)}).ForEach({
            $PSBoundParameters.$_ = (& $Script:CommandTable.'Set-Variable' -Name $_ -Value ((& $Script:CommandTable.'Get-Variable' -Name $_ -ValueOnly) | & $Script:CommandTable.'Select-Object' -Unique) -PassThru).Value
        })

        # Function to build return object. Can be called in multiple places.
        function Out-InstalledAppObject
        {
            return [PSADT.Types.InstalledApplication]@{
                UninstallSubkey    = $regKeyApp.PSChildName
                ProductCode        = $(if ($regKeyApp.PSChildName -match $msiProductCodeRegex) {$regKeyApp.PSChildName})
                DisplayName        = $appDisplayName
                DisplayVersion     = $appDisplayVersion
                UninstallString    = $regKeyApp | & $Script:CommandTable.'Select-Object' -ExpandProperty UninstallString -ErrorAction Ignore
                InstallSource      = $regKeyApp | & $Script:CommandTable.'Select-Object' -ExpandProperty InstallSource -ErrorAction Ignore
                InstallLocation    = $regKeyApp | & $Script:CommandTable.'Select-Object' -ExpandProperty InstallLocation -ErrorAction Ignore
                InstallDate        = $regKeyApp | & $Script:CommandTable.'Select-Object' -ExpandProperty InstallDate -ErrorAction Ignore
                Publisher          = $appPublisher
                Is64BitApplication = $Is64BitApp
            }
        }
    }
    process
    {
        if ($Name)
        {
            Write-ADTLogEntry -Message "Getting information for installed Application Name(s) [$($Name -join ', ')]..."
        }
        if ($ProductCode)
        {
            Write-ADTLogEntry -Message "Getting information for installed Product Code [$ProductCode]..."
        }
        try
        {
            try
            {
                # Create a custom object with the desired properties for the installed applications and sanitize property details.
                $installedApplication = foreach ($regKeyApp in $regKeyApplication)
                {
                    # Bypass any updates or hotfixes.
                    if (!$IncludeUpdatesAndHotfixes -and ($regKeyApp.DisplayName -match $updatesHotfixRegex))
                    {
                        $updatesSkippedCounter++
                        continue
                    }

                    # Remove any control characters which may interfere with logging and creating file path names from these variables.
                    $appDisplayName = $regKeyApp.DisplayName -replace $stringControlChars
                    $appDisplayVersion = ($regKeyApp | & $Script:CommandTable.'Select-Object' -ExpandProperty DisplayVersion -ErrorAction Ignore) -replace $stringControlChars
                    $appPublisher = ($regKeyApp | & $Script:CommandTable.'Select-Object' -ExpandProperty Publisher -ErrorAction Ignore) -replace $stringControlChars
                    $Is64BitApp = [System.Environment]::Is64BitOperatingSystem -and ($regKeyApp.PSPath -notmatch $wow6432PSPathRegex)

                    # Verify if there is a match with the product code passed to the script.
                    if ($ProductCode -contains $regKeyApp.PSChildName)
                    {
                        Write-ADTLogEntry -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] matching product code [$ProductCode]."
                        Out-InstalledAppObject
                    }

                    # Verify if there is a match with the application name(s) passed to the script.
                    foreach ($application in $Name)
                    {
                        if ($Exact -and ($regKeyApp.DisplayName -eq $application))
                        {
                            Write-ADTLogEntry -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using exact name matching for search term [$application]."
                            Out-InstalledAppObject
                        }
                        elseif ($WildCard -and ($regKeyApp.DisplayName -like $application))
                        {
                            Write-ADTLogEntry -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using wildcard matching for search term [$application]."
                            Out-InstalledAppObject
                        }
                        elseif ($RegEx -and ($regKeyApp.DisplayName -match $application))
                        {
                            Write-ADTLogEntry -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using regex matching for search term [$application]."
                            Out-InstalledAppObject
                        }
                        elseif ($regKeyApp.DisplayName -match [System.Text.RegularExpressions.Regex]::Escape($application))
                        {
                            Write-ADTLogEntry -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using contains matching for search term [$application]."
                            Out-InstalledAppObject
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

                if ($installedApplication)
                {
                    return $installedApplication
                }
                Write-ADTLogEntry -Message 'Found no application based on the supplied parameters.'
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
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

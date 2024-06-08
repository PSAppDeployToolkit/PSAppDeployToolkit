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
    PSObject. Returns a PSObject with information about an installed application
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

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Name,

        [ValidatePattern('^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$')]
        [System.String[]]$ProductCode,

        [System.Management.Automation.SwitchParameter]$Exact,
        [System.Management.Automation.SwitchParameter]$WildCard,
        [System.Management.Automation.SwitchParameter]$RegEx,
        [System.Management.Automation.SwitchParameter]$IncludeUpdatesAndHotfixes
    )

    begin {
        # Announce start.
        $adtEnv = Get-ADTEnvironment
        Write-ADTDebugHeader
        if ($Name)
        {
            Write-ADTLogEntry -Message "Getting information for installed Application Name(s) [$($Name -join ', ')]..."
        }
        if ($ProductCode)
        {
            Write-ADTLogEntry -Message "Getting information for installed Product Code [$ProductCode]..."
        }

        # Enumerate the installed applications from the registry for applications that have the "DisplayName" property.
        $regKeyApplication = Get-ItemProperty -Path ($adtEnv.regKeyApplications -replace '$','\*') |
            Where-Object {$_.PSObject.Properties.Name.Contains('DisplayName') -and ![System.String]::IsNullOrWhiteSpace($_.DisplayName)}

        # Set up variables needed in main loop.
        $updatesSkippedCounter = 0
        $wow6432PSPathRegex = '^Microsoft\.PowerShell\.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node'
        $updatesHotfixRegex = '((?i)kb\d+|(Cumulative|Security) Update|Hotfix)'
        $stringControlChars = '[^\p{L}\p{Nd}\p{Z}\p{P}]'

        # Ensure provided data in unique.
        ('Name','ProductCode').Where({$PSBoundParameters.ContainsKey($_)}).ForEach({
            $PSBoundParameters.$_ = (Set-Variable -Name $_ -Value ((Get-Variable -Name $_ -ValueOnly) | Select-Object -Unique) -PassThru).Value
        })
    }
    process {
        # Create a custom object with the desired properties for the installed applications and sanitize property details
        $installedApplication = foreach ($regKeyApp in $regKeyApplication)
        {
            # Bypass any updates or hotfixes
            if (!$IncludeUpdatesAndHotfixes -and ($regKeyApp.DisplayName -match $updatesHotfixRegex))
            {
                $updatesSkippedCounter++
                continue
            }

            # Remove any control characters which may interfere with logging and creating file path names from these variables.
            $appDisplayName = $regKeyApp.DisplayName -replace $stringControlChars
            $appDisplayVersion = ($regKeyApp | Select-Object -ExpandProperty DisplayVersion -ErrorAction Ignore) -replace $stringControlChars
            $appPublisher = ($regKeyApp | Select-Object -ExpandProperty Publisher -ErrorAction Ignore) -replace $stringControlChars
            $Is64BitApp = $adtEnv.is64Bit -and ($regKeyApp.PSPath -notmatch $wow6432PSPathRegex)

            # Verify if there is a match with the product code passed to the script.
            if ($ProductCode -contains $regKeyApp.PSChildName)
            {
                Write-ADTLogEntry -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] matching product code [$ProductCode]."
                [pscustomobject]@{
                    UninstallSubkey    = $regKeyApp.PSChildName
                    ProductCode        = $(if ($regKeyApp.PSChildName -match $adtEnv.MSIProductCodeRegExPattern) {$regKeyApp.PSChildName})
                    DisplayName        = $appDisplayName
                    DisplayVersion     = $appDisplayVersion
                    UninstallString    = $regKeyApp | Select-Object -ExpandProperty UninstallString -ErrorAction Ignore
                    InstallSource      = $regKeyApp | Select-Object -ExpandProperty InstallSource -ErrorAction Ignore
                    InstallLocation    = $regKeyApp | Select-Object -ExpandProperty InstallLocation -ErrorAction Ignore
                    InstallDate        = $regKeyApp | Select-Object -ExpandProperty InstallDate -ErrorAction Ignore
                    Publisher          = $appPublisher
                    Is64BitApplication = $Is64BitApp
                }
            }

            ## Verify if there is a match with the application name(s) passed to the script
            foreach ($application in $Name)
            {
                $applicationMatched = if ($Exact -and ($regKeyApp.DisplayName -eq $application))
                {
                    Write-ADTLogEntry -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using exact name matching for search term [$application]." -PassThru
                }
                elseif ($WildCard -and ($regKeyApp.DisplayName -like $application))
                {
                    Write-ADTLogEntry -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using wildcard matching for search term [$application]." -PassThru
                }
                elseif ($RegEx -and ($regKeyApp.DisplayName -match $application))
                {
                    Write-ADTLogEntry -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using regex matching for search term [$application]." -PassThru
                }
                elseif ($regKeyApp.DisplayName -match [System.Text.RegularExpressions.Regex]::Escape($application))
                {
                    Write-ADTLogEntry -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using contains matching for search term [$application]." -PassThru
                }

                if ($applicationMatched)
                {
                    [pscustomobject]@{
                        UninstallSubkey    = $regKeyApp.PSChildName
                        ProductCode        = $(if ($regKeyApp.PSChildName -match $adtEnv.MSIProductCodeRegExPattern) {$regKeyApp.PSChildName})
                        DisplayName        = $appDisplayName
                        DisplayVersion     = $appDisplayVersion
                        UninstallString    = $regKeyApp.UninstallString
                        InstallSource      = $regKeyApp.InstallSource
                        InstallLocation    = $regKeyApp.InstallLocation
                        InstallDate        = $regKeyApp.InstallDate
                        Publisher          = $appPublisher
                        Is64BitApplication = $Is64BitApp
                    }
                }
            }
        }

        ## Write to log the number of entries skipped due to them being considered updates
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

    end {
        Write-ADTDebugFooter
    }
}

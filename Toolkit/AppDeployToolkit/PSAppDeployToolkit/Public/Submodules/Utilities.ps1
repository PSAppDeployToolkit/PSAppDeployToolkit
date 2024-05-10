#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Remove-InvalidFileNameChars {
    <#
.SYNOPSIS

Remove invalid characters from the supplied string.

.DESCRIPTION

Remove invalid characters from the supplied string and returns a valid filename as a string.

.PARAMETER Name

Text to remove invalid filename characters from.

.INPUTS

System.String

A string containing invalid filename characters.

.OUTPUTS

System.String

Returns the input string with the invalid characters removed.

.EXAMPLE

Remove-InvalidFileNameChars -Name "Filename/\1"

.NOTES

This functions always returns a string however it can be empty if the name only contains invalid characters.
Do no use this command for an entire path as '\' is not a valid filename character.

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyString()]
        [String]$Name
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Output -InputObject (([Char[]]$Name | Where-Object { $invalidFileNameChars -notcontains $_ }) -join '')
        }
        Catch {
            Write-Log -Message "Failed to remove invalid characters from the supplied filename. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-HardwarePlatform {
    <#
.SYNOPSIS

Retrieves information about the hardware platform (physical or virtual)

.DESCRIPTION

Retrieves information about the hardware platform (physical or virtual)

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the hardware platform (physical or virtual)

.EXAMPLE

Get-HardwarePlatform

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Retrieving hardware platform information.' -Source ${CmdletName}
            $hwBios = Get-WmiObject -Class 'Win32_BIOS' -ErrorAction 'Stop' | Select-Object -Property 'Version', 'SerialNumber'
            $hwMakeModel = Get-WmiObject -Class 'Win32_ComputerSystem' -ErrorAction 'Stop' | Select-Object -Property 'Model', 'Manufacturer'

            If ($hwBIOS.Version -match 'VRTUAL') {
                $hwType = 'Virtual:Hyper-V'
            }
            ElseIf ($hwBIOS.Version -match 'A M I') {
                $hwType = 'Virtual:Virtual PC'
            }
            ElseIf ($hwBIOS.Version -like '*Xen*') {
                $hwType = 'Virtual:Xen'
            }
            ElseIf ($hwBIOS.SerialNumber -like '*VMware*') {
                $hwType = 'Virtual:VMWare'
            }
            ElseIf ($hwBIOS.SerialNumber -like '*Parallels*') {
                $hwType = 'Virtual:Parallels'
            }
            ElseIf (($hwMakeModel.Manufacturer -like '*Microsoft*') -and ($hwMakeModel.Model -notlike '*Surface*')) {
                $hwType = 'Virtual:Hyper-V'
            }
            ElseIf ($hwMakeModel.Manufacturer -like '*VMWare*') {
                $hwType = 'Virtual:VMWare'
            }
            ElseIf ($hwMakeModel.Manufacturer -like '*Parallels*') {
                $hwType = 'Virtual:Parallels'
            }
            ElseIf ($hwMakeModel.Model -like '*Virtual*') {
                $hwType = 'Virtual'
            }
            Else {
                $hwType = 'Physical'
            }

            Write-Output -InputObject ($hwType)
        }
        Catch {
            Write-Log -Message "Failed to retrieve hardware platform information. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to retrieve hardware platform information: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-FreeDiskSpace {
    <#
.SYNOPSIS

Retrieves the free disk space in MB on a particular drive (defaults to system drive)

.DESCRIPTION

Retrieves the free disk space in MB on a particular drive (defaults to system drive)

.PARAMETER Drive

Drive to check free disk space on

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Double

Returns the free disk space in MB

.EXAMPLE

Get-FreeDiskSpace -Drive 'C:'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Drive = $envSystemDrive,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message "Retrieving free disk space for drive [$Drive]." -Source ${CmdletName}
            $disk = Get-WmiObject -Class 'Win32_LogicalDisk' -Filter "DeviceID='$Drive'" -ErrorAction 'Stop'
            [Double]$freeDiskSpace = [Math]::Round($disk.FreeSpace / 1MB)

            Write-Log -Message "Free disk space for drive [$Drive]: [$freeDiskSpace MB]." -Source ${CmdletName}
            Write-Output -InputObject ($freeDiskSpace)
        }
        Catch {
            Write-Log -Message "Failed to retrieve free disk space for drive [$Drive]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to retrieve free disk space for drive [$Drive]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-InstalledApplication {
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

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject

Returns a PSObject with information about an installed application
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

Get-InstalledApplication -Name 'Adobe Flash'

.EXAMPLE
    Get-InstalledApplication -ProductCode '{1AD147D0-BE0E-3D6C-AC11-64F6DC4163F1}'
.Outputs
    For every detected matching Application the Function puts out a custom Object containing the following Properties:
    DisplayName, DisplayVersion, InstallDate, Publisher, Is64BitApplication, ProductCode, InstallLocation, UninstallSubkey, UninstallString, InstallSource.
.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String[]]$Name,
        [Parameter(Mandatory = $false)]
        [Switch]$Exact = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$WildCard = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$RegEx = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$ProductCode,
        [Parameter(Mandatory = $false)]
        [Switch]$IncludeUpdatesAndHotfixes
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        If ($name) {
            Write-Log -Message "Getting information for installed Application Name(s) [$($name -join ', ')]..." -Source ${CmdletName}
        }
        If ($productCode) {
            Write-Log -Message "Getting information for installed Product Code [$ProductCode]..." -Source ${CmdletName}
        }

        ## Enumerate the installed applications from the registry for applications that have the "DisplayName" property
        [PSObject[]]$regKeyApplication = ForEach ($regKey in $regKeyApplications) {
            If (Test-Path -LiteralPath $regKey -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorUninstallKeyPath') {
                [PSObject[]]$UninstallKeyApps = Get-ChildItem -LiteralPath $regKey -ErrorAction 'SilentlyContinue' -ErrorVariable '+ErrorUninstallKeyPath'
                ForEach ($UninstallKeyApp in $UninstallKeyApps) {
                    Try {
                        [PSObject]$regKeyApplicationProps = Get-ItemProperty -LiteralPath $UninstallKeyApp.PSPath -ErrorAction 'Stop'
                        If ($regKeyApplicationProps | Select-Object -ExpandProperty DisplayName -ErrorAction Ignore) {
                            $regKeyApplicationProps
                        }
                    }
                    Catch {
                        Write-Log -Message "Unable to enumerate properties from registry key path [$($UninstallKeyApp.PSPath)]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
                        Continue
                    }
                }
            }
        }
        If ($ErrorUninstallKeyPath) {
            Write-Log -Message "The following error(s) took place while enumerating installed applications from the registry. `r`n$(Resolve-Error -ErrorRecord $ErrorUninstallKeyPath)" -Severity 2 -Source ${CmdletName}
        }

        $UpdatesSkippedCounter = 0
        ## Create a custom object with the desired properties for the installed applications and sanitize property details
        [PSObject[]]$installedApplication = @()
        ForEach ($regKeyApp in $regKeyApplication) {
            Try {
                ## Bypass any updates or hotfixes
                If ((-not $IncludeUpdatesAndHotfixes) -and (($regKeyApp.DisplayName -match '(?i)kb\d+') -or ($regKeyApp.DisplayName -match 'Cumulative Update') -or ($regKeyApp.DisplayName -match 'Security Update') -or ($regKeyApp.DisplayName -match 'Hotfix'))) {
                    $UpdatesSkippedCounter += 1
                    Continue
                }

                ## Remove any control characters which may interfere with logging and creating file path names from these variables
                [String]$appDisplayName = $regKeyApp.DisplayName -replace '[^\p{L}\p{Nd}\p{Z}\p{P}]', ''
                [String]$appDisplayVersion = ($regKeyApp | Select-Object -ExpandProperty DisplayVersion -ErrorAction SilentlyContinue) -replace '[^\p{L}\p{Nd}\p{Z}\p{P}]', ''
                [String]$appPublisher = ($regKeyApp | Select-Object -ExpandProperty Publisher -ErrorAction SilentlyContinue) -replace '[^\p{L}\p{Nd}\p{Z}\p{P}]', ''


                ## Determine if application is a 64-bit application
                [Boolean]$Is64BitApp = If (($is64Bit) -and ($regKeyApp.PSPath -notmatch '^Microsoft\.PowerShell\.Core\\Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node')) {
                    $true
                }
                Else {
                    $false
                }

                If ($ProductCode) {
                    ## Verify if there is a match with the product code passed to the script
                    If ($regKeyApp.PSChildName -match [RegEx]::Escape($productCode)) {
                        Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] matching product code [$productCode]." -Source ${CmdletName}
                        $installedApplication += New-Object -TypeName 'PSObject' -Property @{
                            UninstallSubkey    = $regKeyApp.PSChildName
                            ProductCode        = If ($regKeyApp.PSChildName -match $MSIProductCodeRegExPattern) {
                                $regKeyApp.PSChildName
                            }
                            Else {
                                [String]::Empty
                            }
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

                If ($name) {
                    ## Verify if there is a match with the application name(s) passed to the script
                    ForEach ($application in $Name) {
                        $applicationMatched = $false
                        If ($exact) {
                            #  Check for an exact application name match
                            If ($regKeyApp.DisplayName -eq $application) {
                                $applicationMatched = $true
                                Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using exact name matching for search term [$application]." -Source ${CmdletName}
                            }
                        }
                        ElseIf ($WildCard) {
                            #  Check for wildcard application name match
                            If ($regKeyApp.DisplayName -like $application) {
                                $applicationMatched = $true
                                Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using wildcard matching for search term [$application]." -Source ${CmdletName}
                            }
                        }
                        ElseIf ($RegEx) {
                            #  Check for a regex application name match
                            If ($regKeyApp.DisplayName -match $application) {
                                $applicationMatched = $true
                                Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using regex matching for search term [$application]." -Source ${CmdletName}
                            }
                        }
                        #  Check for a contains application name match
                        ElseIf ($regKeyApp.DisplayName -match [RegEx]::Escape($application)) {
                            $applicationMatched = $true
                            Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using contains matching for search term [$application]." -Source ${CmdletName}
                        }

                        If ($applicationMatched) {
                            $installedApplication += New-Object -TypeName 'PSObject' -Property @{
                                UninstallSubkey    = $regKeyApp.PSChildName
                                ProductCode        = If ($regKeyApp.PSChildName -match $MSIProductCodeRegExPattern) {
                                    $regKeyApp.PSChildName
                                }
                                Else {
                                    [String]::Empty
                                }
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
            }
            Catch {
                Write-Log -Message "Failed to resolve application details from registry for [$appDisplayName]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                Continue
            }
        }

        If (-not $IncludeUpdatesAndHotfixes) {
            ## Write to log the number of entries skipped due to them being considered updates
            If ($UpdatesSkippedCounter -eq 1) {
                Write-Log -Message 'Skipped 1 entry while searching, because it was considered a Microsoft update.' -Source ${CmdletName}
            }
            Else {
                Write-Log -Message "Skipped $UpdatesSkippedCounter entries while searching, because they were considered Microsoft updates." -Source ${CmdletName}
            }
        }

        If (-not $installedApplication) {
            Write-Log -Message 'Found no application based on the supplied parameters.' -Source ${CmdletName}
        }

        Write-Output -InputObject ($installedApplication)
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-UserProfiles {
    <#
.SYNOPSIS

Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine and also the Default User (which does not log on).

.DESCRIPTION

Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine and also the Default User (which does  not log on).

Please note that the NTAccount property may be empty for some user profiles but the SID and ProfilePath properties will always be populated.

.PARAMETER ExcludeNTAccount

Specify NT account names in Domain\Username format to exclude from the list of user profiles.

.PARAMETER ExcludeSystemProfiles

Exclude system profiles: SYSTEM, LOCAL SERVICE, NETWORK SERVICE. Default is: $true.

.PARAMETER ExcludeServiceProfiles

Exclude service profiles where NTAccount begins with NT SERVICE. Default is: $true.

.PARAMETER ExcludeDefaultUser

Exclude the Default User. Default is: $false.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject. Returns a PSObject with the following properties: NTAccount, SID, ProfilePath

.EXAMPLE

Get-UserProfiles

Returns the following properties for each user profile on the system: NTAccount, SID, ProfilePath

.EXAMPLE

Get-UserProfiles -ExcludeNTAccount 'CONTOSO\Robot','CONTOSO\ntadmin'

.EXAMPLE

[String[]]$ProfilePaths = Get-UserProfiles | Select-Object -ExpandProperty 'ProfilePath'

Returns the user profile path for each user on the system. This information can then be used to make modifications under the user profile on the filesystem.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String[]]$ExcludeNTAccount,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ExcludeSystemProfiles = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ExcludeServiceProfiles = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$ExcludeDefaultUser = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Getting the User Profile Path, User Account SID, and the User Account Name for all users that log onto the machine.' -Source ${CmdletName}

            ## Get the User Profile Path, User Account Sid, and the User Account Name for all users that log onto the machine
            [String]$UserProfileListRegKey = 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList'
            [PSObject[]]$UserProfiles = Get-ChildItem -LiteralPath $UserProfileListRegKey -ErrorAction 'Stop' |
                ForEach-Object {
                    Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction 'Stop' | Where-Object { ($_.ProfileImagePath) } |
                        Select-Object @{ Label = 'NTAccount'; Expression = { $(ConvertTo-NTAccountOrSID -SID $_.PSChildName).Value } }, @{ Label = 'SID'; Expression = { $_.PSChildName } }, @{ Label = 'ProfilePath'; Expression = { $_.ProfileImagePath } }
                    } |
                    Where-Object { $_.NTAccount } # This removes the "defaultuser0" account, which is a Windows 10 bug
            If ($ExcludeSystemProfiles) {
                [String[]]$SystemProfiles = 'S-1-5-18', 'S-1-5-19', 'S-1-5-20'
                [PSObject[]]$UserProfiles = $UserProfiles | Where-Object { $SystemProfiles -notcontains $_.SID }
            }
            If ($ExcludeServiceProfiles) {
                [PSObject[]]$UserProfiles = $UserProfiles | Where-Object { $_.NTAccount -notlike 'NT SERVICE\*' }
            }
            If ($ExcludeNTAccount) {
                [PSObject[]]$UserProfiles = $UserProfiles | Where-Object { $ExcludeNTAccount -notcontains $_.NTAccount }
            }

            ## Find the path to the Default User profile
            If (-not $ExcludeDefaultUser) {
                [String]$UserProfilesDirectory = Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name 'ProfilesDirectory' -ErrorAction 'Stop' | Select-Object -ExpandProperty 'ProfilesDirectory'

                #  On Windows Vista or higher
                If (([Version]$envOSVersion).Major -gt 5) {
                    # Path to Default User Profile directory on Windows Vista or higher: By default, C:\Users\Default
                    [string]$DefaultUserProfileDirectory = Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name 'Default' -ErrorAction 'Stop' | Select-Object -ExpandProperty 'Default'
                }
                #  On Windows XP or lower
                Else {
                    #  Default User Profile Name: By default, 'Default User'
                    [string]$DefaultUserProfileName = Get-ItemProperty -LiteralPath $UserProfileListRegKey -Name 'DefaultUserProfile' -ErrorAction 'Stop' | Select-Object -ExpandProperty 'DefaultUserProfile'

                    #  Path to Default User Profile directory: By default, C:\Documents and Settings\Default User
                    [String]$DefaultUserProfileDirectory = Join-Path -Path $UserProfilesDirectory -ChildPath $DefaultUserProfileName
                }

                ## Create a custom object for the Default User profile.
                #  Since the Default User is not an actual User account, it does not have a username or a SID.
                #  We will make up a SID and add it to the custom object so that we have a location to load the default registry hive into later on.
                [PSObject]$DefaultUserProfile = New-Object -TypeName 'PSObject' -Property @{
                    NTAccount   = 'Default User'
                    SID         = 'S-1-5-21-Default-User'
                    ProfilePath = $DefaultUserProfileDirectory
                }

                ## Add the Default User custom object to the User Profile list.
                $UserProfiles += $DefaultUserProfile
            }

            Write-Output -InputObject ($UserProfiles)
        }
        Catch {
            Write-Log -Message "Failed to create a custom object representing all user profiles on the machine. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-FileVersion {
    <#
.SYNOPSIS

Gets the version of the specified file

.DESCRIPTION

Gets the version of the specified file

.PARAMETER File

Path of the file

.PARAMETER ProductVersion

Switch that makes the command return ProductVersion instead of FileVersion

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the version of the specified file.

.EXAMPLE

Get-FileVersion -File "$envProgramFilesX86\Adobe\Reader 11.0\Reader\AcroRd32.exe"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$File,
        [Parameter(Mandatory = $false)]
        [Switch]$ProductVersion,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message "Getting version info for file [$file]." -Source ${CmdletName}

            If (Test-Path -LiteralPath $File -PathType 'Leaf') {
                $fileVersionInfo = (Get-Command -Name $file -ErrorAction 'Stop').FileVersionInfo
                If ($ProductVersion) {
                    $fileVersion = $fileVersionInfo.ProductVersion
                }
                Else {
                    $fileVersion = $fileVersionInfo.FileVersion
                }

                If ($fileVersion) {
                    If ($ProductVersion) {
                        Write-Log -Message "Product version is [$fileVersion]." -Source ${CmdletName}
                    }
                    Else {
                        Write-Log -Message "File version is [$fileVersion]." -Source ${CmdletName}
                    }

                    Write-Output -InputObject ($fileVersion)
                }
                Else {
                    Write-Log -Message 'No version information found.' -Source ${CmdletName}
                }
            }
            Else {
                Throw "File path [$file] does not exist."
            }
        }
        Catch {
            Write-Log -Message "Failed to get version info. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to get version info: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Update-Desktop {
    <#
.SYNOPSIS

Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.

.DESCRIPTION

Refresh the Windows Explorer Shell, which causes the desktop icons and the environment variables to be reloaded.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None. This function does not return objects.

.EXAMPLE

Update-Desktop

.NOTES

This function has an alias: Refresh-Desktop

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Refreshing the Desktop and the Windows Explorer environment process block.' -Source ${CmdletName}
            [PSADT.Explorer]::RefreshDesktopAndEnvironmentVariables()
        }
        Catch {
            Write-Log -Message "Failed to refresh the Desktop and the Windows Explorer environment process block. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to refresh the Desktop and the Windows Explorer environment process block: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
Set-Alias -Name 'Refresh-Desktop' -Value 'Update-Desktop' -Scope 'Script' -Force -ErrorAction 'SilentlyContinue'


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Update-SessionEnvironmentVariables {
    <#
.SYNOPSIS

Updates the environment variables for the current PowerShell session with any environment variable changes that may have occurred during script execution.

.DESCRIPTION

Environment variable changes that take place during script execution are not visible to the current PowerShell session.

Use this function to refresh the current PowerShell session with all environment variable settings.

.PARAMETER LoadLoggedOnUserEnvironmentVariables

If script is running in SYSTEM context, this option allows loading environment variables from the active console user. If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None. This function does not return objects.

.EXAMPLE

Update-SessionEnvironmentVariables

.NOTES

This function has an alias: Refresh-SessionEnvironmentVariables

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$LoadLoggedOnUserEnvironmentVariables = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        [ScriptBlock]$GetEnvironmentVar = {
            Param (
                $Key,
                $Scope
            )
            [Environment]::GetEnvironmentVariable($Key, $Scope)
        }
    }
    Process {
        Try {
            Write-Log -Message 'Refreshing the environment variables for this PowerShell session.' -Source ${CmdletName}

            If ($LoadLoggedOnUserEnvironmentVariables -and $RunAsActiveUser) {
                [String]$CurrentUserEnvironmentSID = $RunAsActiveUser.SID
            }
            Else {
                [String]$CurrentUserEnvironmentSID = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
            }
            [String]$MachineEnvironmentVars = 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
            [String]$UserEnvironmentVars = "Registry::HKEY_USERS\$CurrentUserEnvironmentSID\Environment"

            ## Update all session environment variables. Ordering is important here: $UserEnvironmentVars comes second so that we can override $MachineEnvironmentVars.
            $MachineEnvironmentVars, $UserEnvironmentVars | Get-Item | Where-Object { $_ } | ForEach-Object { $envRegPath = $_.PSPath; $_ | Select-Object -ExpandProperty 'Property' | ForEach-Object { Set-Item -LiteralPath "env:$($_)" -Value (Get-ItemProperty -LiteralPath $envRegPath -Name $_).$_ } }

            ## Set PATH environment variable separately because it is a combination of the user and machine environment variables
            [String[]]$PathFolders = 'Machine', 'User' | ForEach-Object { (& $GetEnvironmentVar -Key 'PATH' -Scope $_) } | Where-Object { $_ } | ForEach-Object { $_.Trim(';').Split(';').Trim().Trim('"') } | Select-Object -Unique
            $env:PATH = $PathFolders -join ';'
        }
        Catch {
            Write-Log -Message "Failed to refresh the environment variables for this PowerShell session. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to refresh the environment variables for this PowerShell session: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
Set-Alias -Name 'Refresh-SessionEnvironmentVariables' -Value 'Update-SessionEnvironmentVariables' -Scope 'Script' -Force -ErrorAction 'SilentlyContinue'


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-SchedulerTask {
    <#
.SYNOPSIS

Retrieve all details for scheduled tasks on the local computer.

.DESCRIPTION

Retrieve all details for scheduled tasks on the local computer using schtasks.exe. All property names have spaces and colons removed.

.PARAMETER TaskName

Specify the name of the scheduled task to retrieve details for. Uses regex match to find scheduled task.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSOjbect. This function returns a PSObject with all scheduled task properties.

.EXAMPLE

Get-SchedulerTask

To display a list of all scheduled task properties.

.EXAMPLE

Get-SchedulerTask | Out-GridView

To display a grid view of all scheduled task properties.

.EXAMPLE

Get-SchedulerTask | Select-Object -Property TaskName

To display a list of all scheduled task names.

.NOTES

This function has an alias: Get-ScheduledTask if Get-ScheduledTask is not defined

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$TaskName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        [PSObject[]]$ScheduledTasks = @()
    }
    Process {
        Try {
            Write-Log -Message 'Retrieving Scheduled Tasks...' -Source ${CmdletName}
            [String[]]$exeSchtasksResults = & $exeSchTasks /Query /V /FO CSV
            If ($global:LastExitCode -ne 0) {
                Throw "Failed to retrieve scheduled tasks using [$exeSchTasks]."
            }
            [PSObject[]]$SchtasksResults = $exeSchtasksResults | ConvertFrom-Csv -Header 'HostName', 'TaskName', 'Next Run Time', 'Status', 'Logon Mode', 'Last Run Time', 'Last Result', 'Author', 'Task To Run', 'Start In', 'Comment', 'Scheduled Task State', 'Idle Time', 'Power Management', 'Run As User', 'Delete Task If Not Rescheduled', 'Stop Task If Runs X Hours and X Mins', 'Schedule', 'Schedule Type', 'Start Time', 'Start Date', 'End Date', 'Days', 'Months', 'Repeat: Every', 'Repeat: Until: Time', 'Repeat: Until: Duration', 'Repeat: Stop If Still Running' -ErrorAction 'Stop'

            If ($SchtasksResults) {
                ForEach ($SchtasksResult in $SchtasksResults) {
                    If ($SchtasksResult.TaskName -match $TaskName) {
                        $SchtasksResult | Get-Member -MemberType 'Properties' |
                            ForEach-Object -Begin {
                                [Hashtable]$Task = @{}
                            } -Process {
                                ## Remove spaces and colons in property names. Do not set property value if line being processed is a column header (this will only work on English language machines).
                            ($Task.($($_.Name).Replace(' ', '').Replace(':', ''))) = If ($_.Name -ne $SchtasksResult.($_.Name)) {
                                    $SchtasksResult.($_.Name)
                                }
                            } -End {
                                ## Only add task to the custom object if all property values are not empty
                                If (($Task.Values | Select-Object -Unique | Measure-Object).Count) {
                                    $ScheduledTasks += New-Object -TypeName 'PSObject' -Property $Task
                                }
                            }
                    }
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to retrieve scheduled tasks. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to retrieve scheduled tasks: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-Output -InputObject ($ScheduledTasks)
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}
# If Get-ScheduledTask doesn't exist, add alias Get-ScheduledTask
If (-not (Get-Command -Name 'Get-ScheduledTask' -ErrorAction 'SilentlyContinue')) {
    New-Alias -Name 'Get-ScheduledTask' -Value 'Get-SchedulerTask'
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-UniversalDate {
    <#
.SYNOPSIS

Returns the date/time for the local culture in a universal sortable date time pattern.

.DESCRIPTION

Converts the current datetime or a datetime string for the current culture into a universal sortable date time pattern, e.g. 2013-08-22 11:51:52Z

.PARAMETER DateTime

Specify the DateTime in the current culture.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default: $false.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the date/time for the local culture in a universal sortable date time pattern.

.EXAMPLE

Get-UniversalDate

Returns the current date in a universal sortable date time pattern.

.EXAMPLE

Get-UniversalDate -DateTime '25/08/2013'

Returns the date for the current culture in a universal sortable date time pattern.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        #  Get the current date
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$DateTime = ((Get-Date -Format ($culture).DateTimeFormat.UniversalDateTimePattern).ToString()),
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            ## If a universal sortable date time pattern was provided, remove the Z, otherwise it could get converted to a different time zone.
            If ($DateTime -match 'Z$') {
                $DateTime = $DateTime -replace 'Z$', ''
            }
            [DateTime]$DateTime = [DateTime]::Parse($DateTime, $culture)

            ## Convert the date to a universal sortable date time pattern based on the current culture
            Write-Log -Message "Converting the date [$DateTime] to a universal sortable date time pattern based on the current culture [$($culture.Name)]." -Source ${CmdletName}
            [String]$universalDateTime = (Get-Date -Date $DateTime -Format ($culture).DateTimeFormat.UniversalSortableDateTimePattern -ErrorAction 'Stop').ToString()
            Write-Output -InputObject ($universalDateTime)
        }
        Catch {
            Write-Log -Message "The specified date/time [$DateTime] is not in a format recognized by the current culture [$($culture.Name)]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "The specified date/time [$DateTime] is not in a format recognized by the current culture: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Set-PinnedApplication {
    <#
.SYNOPSIS

Pins or unpins a shortcut to the start menu or task bar.

.DESCRIPTION

Pins or unpins a shortcut to the start menu or task bar.

This should typically be run in the user context, as pinned items are stored in the user profile.

.PARAMETER Action

Action to be performed. Options: 'PinToStartMenu','UnpinFromStartMenu','PinToTaskbar','UnpinFromTaskbar'.

.PARAMETER FilePath

Path to the shortcut file to be pinned or unpinned.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Set-PinnedApplication -Action 'PinToStartMenu' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"

.EXAMPLE

Set-PinnedApplication -Action 'UnpinFromTaskbar' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"

.NOTES

Windows 10 logic borrowed from Stuart Pearson (https://pinto10blog.wordpress.com/2016/09/10/pinto10/)

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateSet('PinToStartMenu', 'UnpinFromStartMenu', 'PinToTaskbar', 'UnpinFromTaskbar')]
        [String]$Action,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$FilePath
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        #region Function Get-PinVerb
        Function Get-PinVerb {
            [CmdletBinding()]
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [Int32]$VerbId
            )

            [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

            Write-Log -Message "Get localized pin verb for verb id [$VerbID]." -Source ${CmdletName}
            [String]$PinVerb = [PSADT.FileVerb]::GetPinVerb($VerbId)
            Write-Log -Message "Verb ID [$VerbID] has a localized pin verb of [$PinVerb]." -Source ${CmdletName}
            Write-Output -InputObject ($PinVerb)
        }
        #endregion

        #region Function Invoke-Verb
        Function Invoke-Verb {
            [CmdletBinding()]
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [String]$FilePath,
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [String]$Verb
            )

            Try {
                [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
                $Verb = $Verb.Replace('&', '')
                $path = Split-Path -Path $FilePath -Parent -ErrorAction 'Stop'
                $folder = $shellApp.Namespace($path)
                $item = $folder.ParseName((Split-Path -Path $FilePath -Leaf -ErrorAction 'Stop'))
                $itemVerb = $item.Verbs() | Where-Object { $_.Name.Replace('&', '') -eq $Verb } -ErrorAction 'Stop'

                If ($null -eq $itemVerb) {
                    Write-Log -Message "Performing action [$Verb] is not programmatically supported for this file [$FilePath]." -Severity 2 -Source ${CmdletName}
                }
                Else {
                    Write-Log -Message "Performing action [$Verb] on [$FilePath]." -Source ${CmdletName}
                    $itemVerb.DoIt()
                }
            }
            Catch {
                Write-Log -Message "Failed to perform action [$Verb] on [$FilePath]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
            }
        }
        #endregion

        If (([Version]$envOSVersion).Major -ge 10) {
            Write-Log -Message 'Detected Windows 10 or higher, using Windows 10 verb codes.' -Source ${CmdletName}
            [Hashtable]$Verbs = @{
                'PinToStartMenu'     = 51201
                'UnpinFromStartMenu' = 51394
                'PinToTaskbar'       = 5386
                'UnpinFromTaskbar'   = 5387
            }
        }
        Else {
            [Hashtable]$Verbs = @{
                'PinToStartMenu'     = 5381
                'UnpinFromStartMenu' = 5382
                'PinToTaskbar'       = 5386
                'UnpinFromTaskbar'   = 5387
            }
        }

    }
    Process {
        Try {
            Write-Log -Message "Execute action [$Action] for file [$FilePath]." -Source ${CmdletName}

            If (-not (Test-Path -LiteralPath $FilePath -PathType 'Leaf' -ErrorAction 'Stop')) {
                Throw "Path [$filePath] does not exist."
            }

            If (-not ($Verbs.$Action)) {
                Throw "Action [$Action] not supported. Supported actions are [$($Verbs.Keys -join ', ')]."
            }

            If ($Action.Contains('StartMenu')) {
                If ([Int32]$envOSVersionMajor -ge 10)   {
                    If ((Get-Item -Path $FilePath).Extension -ne '.lnk') {
                        Throw 'Only shortcut files (.lnk) are supported on Windows 10 and higher.'
                    }
                    ElseIf (-not ($FilePath.StartsWith($envUserStartMenu, 'OrdinalIgnoreCase') -or $FilePath.StartsWith($envCommonStartMenu, 'OrdinalIgnoreCase'))) {
                        Throw "Only shortcut files (.lnk) in [$envUserStartMenu] and [$envCommonStartMenu] are supported on Windows 10 and higher."
                    }
                }

                [String]$PinVerbAction = Get-PinVerb -VerbId ($Verbs.$Action)
                If (-not $PinVerbAction) {
                    Throw "Failed to get a localized pin verb for action [$Action]. Action is not supported on this operating system."
                }

                Invoke-Verb -FilePath $FilePath -Verb $PinVerbAction
            }
            ElseIf ($Action.Contains('Taskbar')) {
                If ([Int32]$envOSVersionMajor -ge 10) {
                    $FileNameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
                    $PinExists = Test-Path -Path "$envAppData\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\$($FileNameWithoutExtension).lnk"

                    If (($Action -eq 'PinToTaskbar') -and ($PinExists)) {
                        If ($(Invoke-ObjectMethod -InputObject $Shell -MethodName 'CreateShortcut' -ArgumentList "$envAppData\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\$($FileNameWithoutExtension).lnk").TargetPath -eq $FilePath) {
                            Write-Log -Message "Pin [$FileNameWithoutExtension] already exists." -Source ${CmdletName}
                            Return
                        }
                    }
                    ElseIf (($Action -eq 'UnpinFromTaskbar') -and ($PinExists -eq $false)) {
                        Write-Log -Message "Pin [$FileNameWithoutExtension] does not exist." -Source ${CmdletName}
                        Return
                    }

                    $ExplorerCommandHandler = Get-RegistryKey -Key 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\Windows.taskbarpin' -Value 'ExplorerCommandHandler'
                    $classesStarKey = (Get-Item "Registry::HKEY_USERS\$($RunasActiveUser.SID)\SOFTWARE\Classes").OpenSubKey('*', $true)
                    $shellKey = $classesStarKey.CreateSubKey('shell', $true)
                    $specialKey = $shellKey.CreateSubKey('{:}', $true)
                    $specialKey.SetValue('ExplorerCommandHandler', $ExplorerCommandHandler)

                    $Folder = Invoke-ObjectMethod -InputObject $ShellApp -MethodName 'Namespace' -ArgumentList $(Split-Path -Path $FilePath -Parent)
                    $Item = Invoke-ObjectMethod -InputObject $Folder -MethodName 'ParseName' -ArgumentList $(Split-Path -Path $FilePath -Leaf)

                    $Item.InvokeVerb('{:}')

                    $shellKey.DeleteSubKey('{:}')
                    If ($shellKey.SubKeyCount -eq 0 -and $shellKey.ValueCount -eq 0) {
                        $classesStarKey.DeleteSubKey('shell')
                    }
                }
                Else {
                    [String]$PinVerbAction = Get-PinVerb -VerbId ($Verbs.$Action)
                    If (-not $PinVerbAction) {
                        Throw "Failed to get a localized pin verb for action [$Action]. Action is not supported on this operating system."
                    }

                    Invoke-Verb -FilePath $FilePath -Verb $PinVerbAction
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to execute action [$Action]. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
        }
        Finally {
            Try {
                If ($shellKey) {
                    $shellKey.Close()
                }
            }
            Catch {
            }
            Try {
                If ($classesStarKey) {
                    $classesStarKey.Close()
                }
            }
            Catch {
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-WindowTitle {
    <#
.SYNOPSIS

Search for an open window title and return details about the window.

.DESCRIPTION

Search for a window title. If window title searched for returns more than one result, then details for each window will be displayed.

Returns the following properties for each window: WindowTitle, WindowHandle, ParentProcess, ParentProcessMainWindowHandle, ParentProcessId.

Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

.PARAMETER WindowTitle

The title of the application window to search for using regex matching.

.PARAMETER GetAllWindowTitles

Get titles for all open windows on the system.

.PARAMETER DisableFunctionLogging

Disables logging messages to the script log file.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Management.Automation.PSObject

Returns a PSObject with the following properties: WindowTitle, WindowHandle, ParentProcess, ParentProcessMainWindowHandle, ParentProcessId.

.EXAMPLE

Get-WindowTitle -WindowTitle 'Microsoft Word'

Gets details for each window that has the words "Microsoft Word" in the title.

.EXAMPLE

Get-WindowTitle -GetAllWindowTitles

Gets details for all windows with a title.

.EXAMPLE

Get-WindowTitle -GetAllWindowTitles | Where-Object { $_.ParentProcess -eq 'WINWORD' }

Get details for all windows belonging to Microsoft Word process with name "WINWORD".

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ParameterSetName = 'SearchWinTitle')]
        [AllowEmptyString()]
        [String]$WindowTitle,
        [Parameter(Mandatory = $true, ParameterSetName = 'GetAllWinTitles')]
        [ValidateNotNullorEmpty()]
        [Switch]$GetAllWindowTitles = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$DisableFunctionLogging = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            If ($PSCmdlet.ParameterSetName -eq 'SearchWinTitle') {
                If (-not $DisableFunctionLogging) {
                    Write-Log -Message "Finding open window title(s) [$WindowTitle] using regex matching." -Source ${CmdletName}
                }
            }
            ElseIf ($PSCmdlet.ParameterSetName -eq 'GetAllWinTitles') {
                If (-not $DisableFunctionLogging) {
                    Write-Log -Message 'Finding all open window title(s).' -Source ${CmdletName}
                }
            }

            ## Get all window handles for visible windows
            [IntPtr[]]$VisibleWindowHandles = [PSADT.UiAutomation]::EnumWindows() | Where-Object { [PSADT.UiAutomation]::IsWindowVisible($_) }

            ## Discover details about each visible window that was discovered
            ForEach ($VisibleWindowHandle in $VisibleWindowHandles) {
                If (-not $VisibleWindowHandle) {
                    Continue
                }
                ## Get the window title
                [String]$VisibleWindowTitle = [PSADT.UiAutomation]::GetWindowText($VisibleWindowHandle)
                If ($VisibleWindowTitle) {
                    ## Get the process that spawned the window
                    [Diagnostics.Process]$Process = Get-Process -ErrorAction 'Stop' | Where-Object { $_.Id -eq [PSADT.UiAutomation]::GetWindowThreadProcessId($VisibleWindowHandle) }
                    If ($Process) {
                        ## Build custom object with details about the window and the process
                        [PSObject]$VisibleWindow = New-Object -TypeName 'PSObject' -Property @{
                            WindowTitle                   = $VisibleWindowTitle
                            WindowHandle                  = $VisibleWindowHandle
                            ParentProcess                 = $Process.ProcessName
                            ParentProcessMainWindowHandle = $Process.MainWindowHandle
                            ParentProcessId               = $Process.Id
                        }

                        ## Only save/return the window and process details which match the search criteria
                        If ($PSCmdlet.ParameterSetName -eq 'SearchWinTitle') {
                            $MatchResult = $VisibleWindow.WindowTitle -match $WindowTitle
                            If ($MatchResult) {
                                [PSObject[]]$VisibleWindows += $VisibleWindow
                            }
                        }
                        ElseIf ($PSCmdlet.ParameterSetName -eq 'GetAllWinTitles') {
                            [PSObject[]]$VisibleWindows += $VisibleWindow
                        }
                    }
                }
            }
        }
        Catch {
            If (-not $DisableFunctionLogging) {
                Write-Log -Message "Failed to get requested window title(s). `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            }
        }
    }
    End {
        Write-Output -InputObject ($VisibleWindows)

        If ($DisableFunctionLogging) {
            . $RevertScriptLogging
        }
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Send-Keys {
    <#
.SYNOPSIS

Send a sequence of keys to one or more application windows.

.DESCRIPTION

Send a sequence of keys to one or more application window. If window title searched for returns more than one window, then all of them will receive the sent keys.

Function does not work in SYSTEM context unless launched with "psexec.exe -s -i" to run it as an interactive process under the SYSTEM account.

.PARAMETER WindowTitle

The title of the application window to search for using regex matching.

.PARAMETER GetAllWindowTitles

Get titles for all open windows on the system.

.PARAMETER WindowHandle

Send keys to a specific window where the Window Handle is already known.

.PARAMETER Keys

The sequence of keys to send. Info on Key input at: http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

.PARAMETER WaitSeconds

An optional number of seconds to wait after the sending of the keys.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Send-Keys -WindowTitle 'foobar - Notepad' -Key 'Hello world'

Send the sequence of keys "Hello world" to the application titled "foobar - Notepad".

.EXAMPLE

Send-Keys -WindowTitle 'foobar - Notepad' -Key 'Hello world' -WaitSeconds 5

Send the sequence of keys "Hello world" to the application titled "foobar - Notepad" and wait 5 seconds.

.EXAMPLE

Send-Keys -WindowHandle ([IntPtr]17368294) -Key 'Hello world'

Send the sequence of keys "Hello world" to the application with a Window Handle of '17368294'.

.NOTES

.LINK

http://msdn.microsoft.com/en-us/library/System.Windows.Forms.SendKeys(v=vs.100).aspx

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false, Position = 0)]
        [AllowEmptyString()]
        [ValidateNotNull()]
        [String]$WindowTitle,
        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateNotNullorEmpty()]
        [Switch]$GetAllWindowTitles = $false,
        [Parameter(Mandatory = $false, Position = 2)]
        [ValidateNotNullorEmpty()]
        [IntPtr]$WindowHandle,
        [Parameter(Mandatory = $false, Position = 3)]
        [ValidateNotNullorEmpty()]
        [String]$Keys,
        [Parameter(Mandatory = $false, Position = 4)]
        [ValidateNotNullorEmpty()]
        [Int32]$WaitSeconds
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        [ScriptBlock]$SendKeys = {
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [IntPtr]$WindowHandle
            )
            Try {
                ## Bring the window to the foreground
                [Boolean]$IsBringWindowToFrontSuccess = [PSADT.UiAutomation]::BringWindowToFront($WindowHandle)
                If (-not $IsBringWindowToFrontSuccess) {
                    Throw 'Failed to bring window to foreground.'
                }

                ## Send the Key sequence
                If ($Keys) {
                    If (-not [PSADT.UiAutomation]::IsWindowEnabled($WindowHandle)) {
                        Throw 'Unable to send keys to window because it may be disabled due to a modal dialog being shown.'
                    }
                    Write-Log -Message "Sending key(s) [$Keys] to window title [$($Window.WindowTitle)] with window handle [$WindowHandle]." -Source ${CmdletName}
                    [Windows.Forms.SendKeys]::SendWait($Keys)
                    If ($WaitSeconds) {
                        Write-Log -Message "Sleeping for [$WaitSeconds] seconds." -Source ${CmdletName}
                        Start-Sleep -Seconds $WaitSeconds
                    }
                }
            }
            Catch {
                Write-Log -Message "Failed to send keys to window title [$($Window.WindowTitle)] with window handle [$WindowHandle]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            }
        }
    }
    Process {
        Try {
            If ($WindowHandle) {
                [PSObject]$Window = Get-WindowTitle -GetAllWindowTitles | Where-Object { $_.WindowHandle -eq $WindowHandle }
                If (-not $Window) {
                    Write-Log -Message "No windows with Window Handle [$WindowHandle] were discovered." -Severity 2 -Source ${CmdletName}
                    Return
                }
                & $SendKeys -WindowHandle $Window.WindowHandle
            }
            Else {
                [Hashtable]$GetWindowTitleSplat = @{}
                If ($GetAllWindowTitles) {
                    $GetWindowTitleSplat.Add( 'GetAllWindowTitles', $GetAllWindowTitles)
                }
                Else {
                    $GetWindowTitleSplat.Add( 'WindowTitle', $WindowTitle)
                }
                [PSObject[]]$AllWindows = Get-WindowTitle @GetWindowTitleSplat
                If (-not $AllWindows) {
                    Write-Log -Message 'No windows with the specified details were discovered.' -Severity 2 -Source ${CmdletName}
                    Return
                }

                ForEach ($Window in $AllWindows) {
                    & $SendKeys -WindowHandle $Window.WindowHandle
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to send keys to specified window. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Test-Battery {
    <#
.SYNOPSIS

Tests whether the local machine is running on AC power or not.

.DESCRIPTION

Tests whether the local machine is running on AC power and returns true/false. For detailed information, use -PassThru option.

.PARAMETER PassThru

Outputs a hashtable containing the following properties:

IsLaptop, IsUsingACPower, ACPowerLineStatus, BatteryChargeStatus, BatteryLifePercent, BatteryLifeRemaining, BatteryFullLifetime

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Hashtable.

Returns a hashtable containing the following properties
- IsLaptop
- IsUsingACPower
- ACPowerLineStatus
- BatteryChargeStatus
- BatteryLifePercent
- BatteryLifeRemaining
- BatteryFullLifetime

.EXAMPLE

Test-Battery

.EXAMPLE

(Test-Battery -PassThru).IsLaptop

Determines if the current system is a laptop or not.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$PassThru = $false
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        ## Initialize a hashtable to store information about system type and power status
        [Hashtable]$SystemTypePowerStatus = @{ }
    }
    Process {
        Write-Log -Message 'Checking if system is using AC power or if it is running on battery...' -Source ${CmdletName}

        [Windows.Forms.PowerStatus]$PowerStatus = [Windows.Forms.SystemInformation]::PowerStatus

        ## Get the system power status. Indicates whether the system is using AC power or if the status is unknown. Possible values:
        #   Offline : The system is not using AC power.
        #   Online  : The system is using AC power.
        #   Unknown : The power status of the system is unknown.
        [String]$PowerLineStatus = $PowerStatus.PowerLineStatus
        $SystemTypePowerStatus.Add('ACPowerLineStatus', $PowerStatus.PowerLineStatus)

        ## Get the current battery charge status. Possible values: High, Low, Critical, Charging, NoSystemBattery, Unknown.
        [String]$BatteryChargeStatus = $PowerStatus.BatteryChargeStatus
        $SystemTypePowerStatus.Add('BatteryChargeStatus', $PowerStatus.BatteryChargeStatus)

        ## Get the approximate amount, from 0.00 to 1.0, of full battery charge remaining.
        #  This property can report 1.0 when the battery is damaged and Windows can't detect a battery.
        #  Therefore, this property is only indicative of battery charge remaining if 'BatteryChargeStatus' property is not reporting 'NoSystemBattery' or 'Unknown'.
        [Single]$BatteryLifePercent = $PowerStatus.BatteryLifePercent
        If (($BatteryChargeStatus -eq 'NoSystemBattery') -or ($BatteryChargeStatus -eq 'Unknown')) {
            [Single]$BatteryLifePercent = 0.0
        }
        $SystemTypePowerStatus.Add('BatteryLifePercent', $PowerStatus.BatteryLifePercent)

        ## The reported approximate number of seconds of battery life remaining. It will report -1 if the remaining life is unknown because the system is on AC power.
        [Int32]$BatteryLifeRemaining = $PowerStatus.BatteryLifeRemaining
        $SystemTypePowerStatus.Add('BatteryLifeRemaining', $PowerStatus.BatteryLifeRemaining)

        ## Get the manufacturer reported full charge lifetime of the primary battery power source in seconds.
        #  The reported number of seconds of battery life available when the battery is fully charged, or -1 if it is unknown.
        #  This will only be reported if the battery supports reporting this information. You will most likely get -1, indicating unknown.
        [Int32]$BatteryFullLifetime = $PowerStatus.BatteryFullLifetime
        $SystemTypePowerStatus.Add('BatteryFullLifetime', $PowerStatus.BatteryFullLifetime)

        ## Determine if the system is using AC power
        [Boolean]$OnACPower = $false
        Switch ($PowerLineStatus) {
            'Online' {
                Write-Log -Message 'System is using AC power.' -Source ${CmdletName}
                $OnACPower = $true
            }
            'Offline' {
                Write-Log -Message 'System is using battery power.' -Source ${CmdletName}
            }
            'Unknown' {
                If (($BatteryChargeStatus -eq 'NoSystemBattery') -or ($BatteryChargeStatus -eq 'Unknown')) {
                    Write-Log -Message "System power status is [$PowerLineStatus] and battery charge status is [$BatteryChargeStatus]. This is most likely due to a damaged battery so we will report system is using AC power." -Source ${CmdletName}
                    $OnACPower = $true
                }
                Else {
                    Write-Log -Message "System power status is [$PowerLineStatus] and battery charge status is [$BatteryChargeStatus]. Therefore, we will report system is using battery power." -Source ${CmdletName}
                }
            }
        }
        $SystemTypePowerStatus.Add('IsUsingACPower', $OnACPower)

        ## Determine if the system is a laptop
        [Boolean]$IsLaptop = $false
        If (($BatteryChargeStatus -eq 'NoSystemBattery') -or ($BatteryChargeStatus -eq 'Unknown')) {
            $IsLaptop = $false
        }
        Else {
            $IsLaptop = $true
        }
        #  Chassis Types
        [Int32[]]$ChassisTypes = Get-WmiObject -Class 'Win32_SystemEnclosure' | Where-Object { $_.ChassisTypes } | Select-Object -ExpandProperty 'ChassisTypes'
        Write-Log -Message "The following system chassis types were detected [$($ChassisTypes -join ',')]." -Source ${CmdletName}
        ForEach ($ChassisType in $ChassisTypes) {
            Switch ($ChassisType) {
                9 {
                    $IsLaptop = $true
                } # 9=Laptop
                10 {
                    $IsLaptop = $true
                } # 10=Notebook
                14 {
                    $IsLaptop = $true
                } # 14=Sub Notebook
                3 {
                    $IsLaptop = $false
                } # 3=Desktop
            }
        }
        #  Add IsLaptop property to hashtable
        $SystemTypePowerStatus.Add('IsLaptop', $IsLaptop)

        If ($PassThru) {
            Write-Output -InputObject ($SystemTypePowerStatus)
        }
        Else {
            Write-Output -InputObject ($OnACPower)
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Test-NetworkConnection {
    <#
.SYNOPSIS

Tests for an active local network connection, excluding wireless and virtual network adapters.

.DESCRIPTION

Tests for an active local network connection, excluding wireless and virtual network adapters, by querying the Win32_NetworkAdapter WMI class.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if a wired network connection is detected, otherwise returns $false.

.EXAMPLE

Test-NetworkConnection

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Write-Log -Message 'Checking if system is using a wired network connection...' -Source ${CmdletName}

        [PSObject[]]$networkConnected = Get-WmiObject -Class 'Win32_NetworkAdapter' | Where-Object { ($_.NetConnectionStatus -eq 2) -and ($_.NetConnectionID -match 'Local' -or $_.NetConnectionID -match 'Ethernet') -and ($_.NetConnectionID -notmatch 'Wireless') -and ($_.Name -notmatch 'Virtual') } -ErrorAction 'SilentlyContinue'
        [Boolean]$onNetwork = $false
        If ($networkConnected) {
            Write-Log -Message 'Wired network connection found.' -Source ${CmdletName}
            [Boolean]$onNetwork = $true
        }
        Else {
            Write-Log -Message 'Wired network connection not found.' -Source ${CmdletName}
        }

        Write-Output -InputObject ($onNetwork)
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Test-PowerPoint {
    <#
.SYNOPSIS

Tests whether PowerPoint is running in either fullscreen slideshow mode or presentation mode.

.DESCRIPTION

Tests whether someone is presenting using PowerPoint in either fullscreen slideshow mode or presentation mode.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if PowerPoint is running in either fullscreen slideshow mode or presentation mode, otherwise returns $false.

.EXAMPLE

Test-PowerPoint

.NOTES

This function can only execute detection logic if the process is in interactive mode.

There is a possiblity of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show".

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Checking if PowerPoint is in either fullscreen slideshow mode or presentation mode...' -Source ${CmdletName}
            Try {
                [Diagnostics.Process[]]$PowerPointProcess = Get-Process -ErrorAction 'Stop' | Where-Object { $_.ProcessName -eq 'POWERPNT' }
                If ($PowerPointProcess) {
                    [Boolean]$IsPowerPointRunning = $true
                    Write-Log -Message 'PowerPoint application is running.' -Source ${CmdletName}
                }
                Else {
                    [Boolean]$IsPowerPointRunning = $false
                    Write-Log -Message 'PowerPoint application is not running.' -Source ${CmdletName}
                }
            }
            Catch {
                Throw
            }

            [Nullable[Boolean]]$IsPowerPointFullScreen = $false
            If ($IsPowerPointRunning) {
                ## Detect if PowerPoint is in fullscreen mode or Presentation Mode, detection method only works if process is interactive
                If ([Environment]::UserInteractive) {
                    #  Check if "POWERPNT" process has a window with a title that begins with "PowerPoint Slide Show" or "Powerpoint-" for non-English language systems.
                    #  There is a possiblity of a false positive if the PowerPoint filename starts with "PowerPoint Slide Show"
                    [PSObject]$PowerPointWindow = Get-WindowTitle -GetAllWindowTitles | Where-Object { $_.WindowTitle -match '^PowerPoint Slide Show' -or $_.WindowTitle -match '^PowerPoint-' } | Where-Object { $_.ParentProcess -eq 'POWERPNT' } | Select-Object -First 1
                    If ($PowerPointWindow) {
                        [Nullable[Boolean]]$IsPowerPointFullScreen = $true
                        Write-Log -Message 'Detected that PowerPoint process [POWERPNT] has a window with a title that beings with [PowerPoint Slide Show] or [PowerPoint-].' -Source ${CmdletName}
                    }
                    Else {
                        Write-Log -Message 'Detected that PowerPoint process [POWERPNT] does not have a window with a title that beings with [PowerPoint Slide Show] or [PowerPoint-].' -Source ${CmdletName}
                        Try {
                            [Int32[]]$PowerPointProcessIDs = $PowerPointProcess | Select-Object -ExpandProperty 'Id' -ErrorAction 'Stop'
                            Write-Log -Message "PowerPoint process [POWERPNT] has process id(s) [$($PowerPointProcessIDs -join ', ')]." -Source ${CmdletName}
                        }
                        Catch {
                            Write-Log -Message "Unable to retrieve process id(s) for [POWERPNT] process. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
                        }
                    }

                    ## If previous detection method did not detect PowerPoint in fullscreen mode, then check if PowerPoint is in Presentation Mode (check only works on Windows Vista or higher)
                    If ((-not $IsPowerPointFullScreen) -and (([Version]$envOSVersion).Major -gt 5)) {
                        #  Note: below method does not detect PowerPoint presentation mode if the presentation is on a monitor that does not have current mouse input control
                        [String]$UserNotificationState = [PSADT.UiAutomation]::GetUserNotificationState()
                        Write-Log -Message "Detected user notification state [$UserNotificationState]." -Source ${CmdletName}
                        Switch ($UserNotificationState) {
                            'PresentationMode' {
                                Write-Log -Message 'Detected that system is in [Presentation Mode].' -Source ${CmdletName}
                                [Nullable[Boolean]]$IsPowerPointFullScreen = $true
                            }
                            'FullScreenOrPresentationModeOrLoginScreen' {
                                If (([String]$PowerPointProcessIDs) -and ($PowerPointProcessIDs -contains [PSADT.UIAutomation]::GetWindowThreadProcessID([PSADT.UIAutomation]::GetForeGroundWindow()))) {
                                    Write-Log -Message 'Detected that fullscreen foreground window matches PowerPoint process id.' -Source ${CmdletName}
                                    [Nullable[Boolean]]$IsPowerPointFullScreen = $true
                                }
                            }
                        }
                    }
                }
                Else {
                    [Nullable[Boolean]]$IsPowerPointFullScreen = $null
                    Write-Log -Message 'Unable to run check to see if PowerPoint is in fullscreen mode or Presentation Mode because current process is not interactive. Configure script to run in interactive mode in your deployment tool. If using SCCM Application Model, then make sure "Allow users to view and interact with the program installation" is selected. If using SCCM Package Model, then make sure "Allow users to interact with this program" is selected.' -Severity 2 -Source ${CmdletName}
                }
            }
        }
        Catch {
            [Nullable[Boolean]]$IsPowerPointFullScreen = $null
            Write-Log -Message "Failed check to see if PowerPoint is running in fullscreen slideshow mode. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-Log -Message "PowerPoint is running in fullscreen mode [$IsPowerPointFullScreen]." -Source ${CmdletName}
        Write-Output -InputObject ($IsPowerPointFullScreen)
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Update-GroupPolicy {
    <#
.SYNOPSIS

Performs a gpupdate command to refresh Group Policies on the local machine.

.DESCRIPTION

Performs a gpupdate command to refresh Group Policies on the local machine.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Update-GroupPolicy

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        [String[]]$GPUpdateCmds = '/C echo N | gpupdate.exe /Target:Computer /Force', '/C echo N | gpupdate.exe /Target:User /Force'
        [Int32]$InstallCount = 0
        ForEach ($GPUpdateCmd in $GPUpdateCmds) {
            Try {
                If ($InstallCount -eq 0) {
                    [String]$InstallMsg = 'Updating Group Policies for the Machine'
                }
                Else {
                    [String]$InstallMsg = 'Updating Group Policies for the User'
                }
                Write-Log -Message "$($InstallMsg)..." -Source ${CmdletName}
                [PSObject]$ExecuteResult = Execute-Process -Path "$envWinDir\System32\cmd.exe" -Parameters $GPUpdateCmd -WindowStyle 'Hidden' -PassThru -ExitOnProcessFailure $false

                If ($ExecuteResult.ExitCode -ne 0) {
                    If ($ExecuteResult.ExitCode -eq 60002) {
                        Throw "Execute-Process function failed with exit code [$($ExecuteResult.ExitCode)]."
                    }
                    Else {
                        Throw "gpupdate.exe failed with exit code [$($ExecuteResult.ExitCode)]."
                    }
                }
                $InstallCount++
            }
            Catch {
                Write-Log -Message "$($InstallMsg) failed. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
                If (-not $ContinueOnError) {
                    Throw "$($InstallMsg) failed: $($_.Exception.Message)"
                }
                Continue
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Set-ActiveSetup {
    <#
.SYNOPSIS

Creates an Active Setup entry in the registry to execute a file for each user upon login.

.DESCRIPTION

Active Setup allows handling of per-user changes registry/file changes upon login.
A registry key is created in the HKLM registry hive which gets replicated to the HKCU hive when a user logs in.
If the "Version" value of the Active Setup entry in HKLM is higher than the version value in HKCU, the file referenced in "StubPath" is executed.

This Function:
    - Creates the registry entries in HKLM:SOFTWARE\Microsoft\Active Setup\Installed Components\$installName.
    - Creates StubPath value depending on the file extension of the $StubExePath parameter.
    - Handles Version value with YYYYMMDDHHMMSS granularity to permit re-installs on the same day and still trigger Active Setup after Version increase.
    - Copies/overwrites the StubPath file to $StubExePath destination path if file exists in 'Files' subdirectory of script directory.
    - Executes the StubPath file for the current user based on $ExecuteForCurrentUser (no need to logout/login to trigger Active Setup).

.PARAMETER StubExePath

Use this parameter to specify the destination path of the file that will be executed upon user login.

Note: Place the file you want users to execute in the '\Files' subdirectory of the script directory and the toolkit will install it to the path specificed in this parameter.

.PARAMETER Arguments

Arguments to pass to the file being executed.

.PARAMETER Description

Description for the Active Setup. Users will see "Setting up personalized settings for: $Description" at logon. Default is: $installName.

.PARAMETER Key

Name of the registry key for the Active Setup entry. Default is: $installName.

.PARAMETER Wow6432Node

Specify this switch to use Active Setup entry under Wow6432Node on a 64-bit OS. Default is: $false.

.PARAMETER Version

Optional. Specify version for Active setup entry. Active Setup is not triggered if Version value has more than 8 consecutive digits. Use commas to get around this limitation. Default: YYYYMMDDHHMMSS

Note:
    - Do not use this parameter if it is not necessary. PSADT will handle this parameter automatically using the time of the installation as the version number.
    - In Windows 10, Scripts and EXEs might be blocked by AppLocker. Ensure that the path given to -StubExePath will permit end users to run Scripts and EXEs unelevated.

.PARAMETER Locale

Optional. Arbitrary string used to specify the installation language of the file being executed. Not replicated to HKCU.

.PARAMETER PurgeActiveSetupKey

Remove Active Setup entry from HKLM registry hive. Will also load each logon user's HKCU registry hive to remove Active Setup entry. Function returns after purging.

.PARAMETER DisableActiveSetup

Disables the Active Setup entry so that the StubPath file will not be executed. This also disables -ExecuteForCurrentUser

.PARAMETER ExecuteForCurrentUser

Specifies whether the StubExePath should be executed for the current user. Since this user is already logged in, the user won't have the application started without logging out and logging back in. Default: $true

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if Active Setup entry was created or updated, $false if Active Setup entry was not created or updated.

.EXAMPLE

Set-ActiveSetup -StubExePath 'C:\Users\Public\Company\ProgramUserConfig.vbs' -Arguments '/Silent' -Description 'Program User Config' -Key 'ProgramUserConfig' -Locale 'en'

.EXAMPLE

Set-ActiveSetup -StubExePath "$envWinDir\regedit.exe" -Arguments "/S `"%SystemDrive%\Program Files (x86)\PS App Deploy\PSAppDeployHKCUSettings.reg`"" -Description 'PS App Deploy Config' -Key 'PS_App_Deploy_Config' -ContinueOnError $true

.EXAMPLE

Set-ActiveSetup -Key 'ProgramUserConfig' -PurgeActiveSetupKey

Deletes "ProgramUserConfig" active setup entry from all registry hives.

.NOTES

Original code borrowed from: Denis St-Pierre (Ottawa, Canada), Todd MacNaught (Ottawa, Canada)

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$StubExePath,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Arguments,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Description = $installName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$Key = $installName,
        [Parameter(Mandatory = $false)]
        [Switch]$Wow6432Node = $false,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Version = ((Get-Date -Format 'yyMM,ddHH,mmss').ToString()), # Ex: 1405,1515,0522
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [String]$Locale,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [Switch]$DisableActiveSetup = $false,
        [Parameter(Mandatory = $true, ParameterSetName = 'Purge')]
        [Switch]$PurgeActiveSetupKey,
        [Parameter(Mandatory = $false, ParameterSetName = 'Create')]
        [ValidateNotNullorEmpty()]
        [Boolean]$ExecuteForCurrentUser = $true,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            if ($Wow6432Node -and $Is64Bit) {
                [String]$ActiveSetupKey = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Active Setup\Installed Components\$Key"
                [String]$HKCUActiveSetupKey = "Registry::HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Active Setup\Installed Components\$Key"
            }
            else {
                [String]$ActiveSetupKey = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Active Setup\Installed Components\$Key"
                [String]$HKCUActiveSetupKey = "Registry::HKEY_CURRENT_USER\Software\Microsoft\Active Setup\Installed Components\$Key"
            }

            ## Delete Active Setup registry entry from the HKLM hive and for all logon user registry hives on the system
            If ($PurgeActiveSetupKey) {
                Write-Log -Message "Removing Active Setup entry [$ActiveSetupKey]." -Source ${CmdletName}
                Remove-RegistryKey -Key $ActiveSetupKey -Recurse

                Write-Log -Message "Removing Active Setup entry [$HKCUActiveSetupKey] for all log on user registry hives on the system." -Source ${CmdletName}
                [ScriptBlock]$RemoveHKCUActiveSetupKey = {
                    If (Get-RegistryKey -Key $HKCUActiveSetupKey -SID $RunAsActiveUser.SID) {
                        Remove-RegistryKey -Key $HKCUActiveSetupKey -SID $RunAsActiveUser.SID -Recurse
                    }
                }
                Invoke-HKCURegistrySettingsForAllUsers -RegistrySettings $RemoveHKCUActiveSetupKey -UserProfiles (Get-UserProfiles -ExcludeDefaultUser)
                Return
            }

            ## Verify a file with a supported file extension was specified in $StubExePath
            [String[]]$StubExePathFileExtensions = '.exe', '.vbs', '.cmd', '.ps1', '.js'
            [String]$StubExeExt = [IO.Path]::GetExtension($StubExePath)
            If ($StubExePathFileExtensions -notcontains $StubExeExt) {
                Throw "Unsupported Active Setup StubPath file extension [$StubExeExt]."
            }

            ## Copy file to $StubExePath from the 'Files' subdirectory of the script directory (if it exists there)
            [String]$StubExePath = [Environment]::ExpandEnvironmentVariables($StubExePath)
            [String]$ActiveSetupFileName = [IO.Path]::GetFileName($StubExePath)
            [String]$StubExeFile = Join-Path -Path $dirFiles -ChildPath $ActiveSetupFileName
            If (Test-Path -LiteralPath $StubExeFile -PathType 'Leaf') {
                #  This will overwrite the StubPath file if $StubExePath already exists on target
                Copy-File -Path $StubExeFile -Destination $StubExePath -ContinueOnError $false
            }

            ## Check if the $StubExePath file exists
            If (-not (Test-Path -LiteralPath $StubExePath -PathType 'Leaf')) {
                Throw "Active Setup StubPath file [$ActiveSetupFileName] is missing."
            }

            ## Define Active Setup StubPath according to file extension of $StubExePath
            Switch ($StubExeExt) {
                '.exe' {
                    [String]$CUStubExePath = "$StubExePath"
                    [String]$CUArguments = $Arguments
                    [String]$StubPath = "`"$CUStubExePath`""
                }
                '.js' {
                    [String]$CUStubExePath = "$envWinDir\System32\cscript.exe"
                    [String]$CUArguments = "//nologo `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.vbs' {
                    [String]$CUStubExePath = "$envWinDir\System32\cscript.exe"
                    [String]$CUArguments = "//nologo `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.cmd' {
                    [String]$CUStubExePath = "$envWinDir\System32\cmd.exe"
                    [String]$CUArguments = "/C `"$StubExePath`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
                '.ps1' {
                    [String]$CUStubExePath = [System.Diagnostics.Process]::GetCurrentProcess().Path
                    [String]$CUArguments = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden -Command `"& {& `\`"$StubExePath`\`"}`""
                    [String]$StubPath = "`"$CUStubExePath`" $CUArguments"
                }
            }
            If ($Arguments) {
                [String]$StubPath = "$StubPath $Arguments"
                If ($StubExeExt -ne '.exe') {
                    [String]$CUArguments = "$CUArguments $Arguments"
                }
            }

            [ScriptBlock]$TestActiveSetup = {
                Param (
                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullorEmpty()]
                    [String]$HKLMKey,
                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullorEmpty()]
                    [String]$HKCUKey,
                    [Parameter(Mandatory = $false)]
                    [ValidateNotNullorEmpty()]
                    [String]$UserSID
                )
                If ($UserSID) {
                    $HKCUProps = (Get-RegistryKey -Key $HKCUKey -SID $UserSID -ContinueOnError $true)
                }
                Else {
                    $HKCUProps = (Get-RegistryKey -Key $HKCUKey -ContinueOnError $true)
                }

                $HKLMProps = (Get-RegistryKey -Key $HKLMKey -ContinueOnError $true)
                [String]$HKCUVer = $HKCUProps.Version
                [String]$HKLMVer = $HKLMProps.Version
                [Int32]$HKLMInst = $HKLMProps.IsInstalled

                # HKLM entry not present. Nothing to run.
                If (-not $HKLMProps) {
                    Write-Log 'HKLM active setup entry is not present.' -Source ${CmdletName}
                    Return ($false)
                }
                # HKLM entry present, but disabled. Nothing to run.
                If ($HKLMInst -eq 0) {
                    Write-Log 'HKLM active setup entry is present, but it is disabled (IsInstalled set to 0).' -Source ${CmdletName}
                    Return ($false)
                }
                # HKLM entry present and HKCU entry is not. Run the StubPath.
                If (-not $HKCUProps) {
                    Write-Log 'HKLM active setup entry is present. HKCU active setup entry is not present.' -Source ${CmdletName}
                    Return ($true)
                }
                # Both entries present. HKLM entry does not have Version property. Nothing to run.
                If (-not $HKLMVer) {
                    Write-Log 'HKLM and HKCU active setup entries are present. HKLM Version property is missing.' -Source ${CmdletName}
                    Return ($false)
                }
                # Both entries present. HKLM entry has Version property, but HKCU entry does not. Run the StubPath.
                If (-not $HKCUVer) {
                    Write-Log 'HKLM and HKCU active setup entries are present. HKCU Version property is missing.' -Source ${CmdletName}
                    Return ($true)
                }

                # Remove invalid characters from Version property. Only digits and commas are allowed.
                [String]$HKLMValidVer = ''
                For ($i = 0; $i -lt $HKLMVer.Length; $i++) {
                    If ([Char]::IsDigit($HKLMVer[$i]) -or ($HKLMVer[$i] -eq ',')) {
                        $HKLMValidVer += $HKLMVer[$i]
                    }
                }

                [String]$HKCUValidVer = ''
                For ($i = 0; $i -lt $HKCUVer.Length; $i++) {
                    If ([Char]::IsDigit($HKCUVer[$i]) -or ($HKCUVer[$i] -eq ',')) {
                        $HKCUValidVer += $HKCUVer[$i]
                    }
                }

                # After cleanup, the HKLM Version property is empty. Considering it missing. HKCU is present so nothing to run.
                If (-not $HKLMValidVer) {
                    Write-Log 'HKLM and HKCU active setup entries are present. HKLM Version property is invalid.' -Source ${CmdletName}
                    Return ($false)
                }

                # After cleanup, the HKCU Version property is empty while HKLM Version property is not. Run the StubPath.
                If (-not $HKCUValidVer) {
                    Write-Log 'HKLM and HKCU active setup entries are present. HKCU Version property is invalid.' -Source ${CmdletName}
                    Return ($true)
                }

                ## Both entries present, with a Version property. Compare the Versions.
                # Convert the version property to Version type and compare
                [Version]$VersionHKLMValidVer = $null
                [Version]$VersionHKCUValidVer = $null
                Try {
                    [Version]$VersionHKLMValidVer = [Version]$HKLMValidVer.Replace(',','.')
                    [Version]$VersionHKCUValidVer = [Version]$HKCUValidVer.Replace(',','.')

                    If ($VersionHKLMValidVer -gt $VersionHKCUValidVer) {
                        # HKLM is greater, run the StubPath.
                        Write-Log "HKLM and HKCU active setup entries are present. Both contain Version properties, and the HKLM Version is greater." -Source ${CmdletName}
                        Return ($true)
                    }
                    Else {
                        # The HKCU version is equal or higher than HKLM version, Nothing to run
                        Write-Log 'HKLM and HKCU active setup entries are present. Both contain Version properties. However, they are either the same or the HKCU Version property is higher.' -Source ${CmdletName}
                        Return ($false)
                    }
                }
                Catch {
                    # Failed to convert version property to Version type.
                }

                # Check whether the Versions were split into the same number of strings
                # Split the version by commas
                [String[]]$SplitHKLMValidVer = $HKLMValidVer.Split(',')
                [String[]]$SplitHKCUValidVer = $HKCUValidVer.Split(',')
                If ($SplitHKLMValidVer.Count -ne $SplitHKCUValidVer.Count) {
                    # The versions are different length - more commas
                    If ($SplitHKLMValidVer.Count -gt $SplitHKCUValidVer.Count) {
                        # HKLM is longer, Run the StubPath
                        Write-Log "HKLM and HKCU active setup entries are present. Both contain Version properties. However, the HKLM Version has more version fields." -Source ${CmdletName}
                        Return ($true)
                    }
                    Else {
                        # HKCU is longer, Nothing to run
                        Write-Log "HKLM and HKCU active setup entries are present. Both contain Version properties. However, the HKCU Version has more version fields." -Source ${CmdletName}
                        Return ($false)
                    }
                }

                # The Versions have the same number of strings. Compare them
                Try {
                    For ($i = 0; $i -lt $SplitHKLMValidVer.Count; $i++) {
                        # Parse the version is UINT64
                        [UInt64]$ParsedHKLMVer = [UInt64]::Parse($SplitHKLMValidVer[$i])
                        [UInt64]$ParsedHKCUVer = [UInt64]::Parse($SplitHKCUValidVer[$i])
                        # The HKCU ver is lower, Run the StubPath
                        If ($ParsedHKCUVer -lt $ParsedHKLMVer) {
                            Write-Log 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid. However, HKCU Version property is lower.' -Source ${CmdletName}
                            Return ($true)
                        }
                    }
                    # The HKCU version is equal or higher than HKLM version, Nothing to run
                    Write-Log 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid. However, they are either the same or HKCU Version property is higher.' -Source ${CmdletName}
                    Return ($false)
                }
                Catch {
                    # Failed to parse strings as UInt64, Run the StubPath
                    Write-Log 'HKLM and HKCU active setup entries are present. Both Version properties are present and valid. However, parsing string numerics to 64-bit integers failed.' -Severity 2 -Source ${CmdletName}
                    Return ($true)
                }
            }

            ## Create the Active Setup entry in the registry
            [ScriptBlock]$SetActiveSetupRegKeys = {
                Param (
                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullorEmpty()]
                    [String]$ActiveSetupRegKey,
                    [Parameter(Mandatory = $false)]
                    [ValidateNotNullorEmpty()]
                    [String]$SID
                )
                If ($SID) {
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name '(Default)' -Value $Description -SID $SID -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Version' -Value $Version -SID $SID -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'StubPath' -Value $StubPath -Type 'String' -SID $SID -ContinueOnError $false
                    If ($Locale) {
                        Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Locale' -Value $Locale -SID $SID -ContinueOnError $false
                    }
                    # Only Add IsInstalled to HKLM
                    If ($ActiveSetupRegKey.Contains('HKEY_LOCAL_MACHINE')) {
                        If ($DisableActiveSetup) {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 0 -Type 'DWord' -SID $SID -ContinueOnError $false
                        }
                        Else {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 1 -Type 'DWord' -SID $SID -ContinueOnError $false
                        }
                    }
                }
                Else {
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name '(Default)' -Value $Description -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Version' -Value $Version -ContinueOnError $false
                    Set-RegistryKey -Key $ActiveSetupRegKey -Name 'StubPath' -Value $StubPath -Type 'String' -ContinueOnError $false
                    If ($Locale) {
                        Set-RegistryKey -Key $ActiveSetupRegKey -Name 'Locale' -Value $Locale -ContinueOnError $false
                    }
                    # Only Add IsInstalled to HKLM
                    If ($ActiveSetupRegKey.Contains('HKEY_LOCAL_MACHINE')) {
                        If ($DisableActiveSetup) {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 0 -Type 'DWord' -ContinueOnError $false
                        }
                        Else {
                            Set-RegistryKey -Key $ActiveSetupRegKey -Name 'IsInstalled' -Value 1 -Type 'DWord' -ContinueOnError $false
                        }
                    }
                }
            }

            Write-Log -Message "Adding Active Setup Key for local machine: [$ActiveSetupKey]." -Source ${CmdletName}
            & $SetActiveSetupRegKeys -ActiveSetupRegKey $ActiveSetupKey

            ## Execute the StubPath file for the current user as long as not in Session 0
            If ($ExecuteForCurrentUser) {
                If ($SessionZero) {
                    If ($RunAsActiveUser) {
                        # Skip if Active Setup reg key is present and Version is equal or higher
                        [Boolean]$InstallNeeded = (& $TestActiveSetup -HKLMKey $ActiveSetupKey -HKCUKey $HKCUActiveSetupKey -UserSID $RunAsActiveUser.SID)
                        If ($InstallNeeded) {
                            Write-Log -Message "Session 0 detected: Executing Active Setup StubPath file for currently logged in user [$($RunAsActiveUser.NTAccount)]." -Source ${CmdletName}
                            If ($CUArguments) {
                                Execute-ProcessAsUser -Path $CUStubExePath -Parameters $CUArguments -Wait -ContinueOnError $true
                            }
                            Else {
                                Execute-ProcessAsUser -Path $CUStubExePath -Wait -ContinueOnError $true
                            }

                            Write-Log -Message "Adding Active Setup Key for the current user: [$HKCUActiveSetupKey]." -Source ${CmdletName}
                            & $SetActiveSetupRegKeys -ActiveSetupRegKey $HKCUActiveSetupKey -SID $RunAsActiveUser.SID
                        }
                        Else {
                            Write-Log -Message "Session 0 detected: Skipping executing Active Setup StubPath file for currently logged in user [$($RunAsActiveUser.NTAccount)]." -Source ${CmdletName} -Severity 2
                        }
                    }
                    Else {
                        Write-Log -Message 'Session 0 detected: No logged in users detected. Active Setup StubPath file will execute when users first log into their account.' -Source ${CmdletName}
                    }
                }
                Else {
                    # Skip if Active Setup reg key is present and Version is equal or higher
                    [Boolean]$InstallNeeded = (& $TestActiveSetup -HKLMKey $ActiveSetupKey -HKCUKey $HKCUActiveSetupKey)
                    If ($InstallNeeded) {
                        Write-Log -Message 'Executing Active Setup StubPath file for the current user.' -Source ${CmdletName}
                        If ($CUArguments) {
                            Execute-Process -FilePath $CUStubExePath -Parameters $CUArguments -ExitOnProcessFailure $false
                        }
                        Else {
                            Execute-Process -FilePath $CUStubExePath -ExitOnProcessFailure $false
                        }

                        Write-Log -Message "Adding Active Setup Key for the current user: [$HKCUActiveSetupKey]." -Source ${CmdletName}
                        & $SetActiveSetupRegKeys -ActiveSetupRegKey $HKCUActiveSetupKey
                    }
                    Else {
                        Write-Log -Message 'Skipping executing Active Setup StubPath file for current user.' -Source ${CmdletName} -Severity 2
                    }
                }
            }
        }
        Catch {
            Write-Log -Message "Failed to set Active Setup registry entry. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed to set Active Setup registry entry: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-LoggedOnUser {
    <#
.SYNOPSIS

Get session details for all local and RDP logged on users.

.DESCRIPTION

Get session details for all local and RDP logged on users using Win32 APIs. Get the following session details:
    NTAccount, SID, UserName, DomainName, SessionId, SessionName, ConnectState, IsCurrentSession, IsConsoleSession, IsUserSession, IsActiveUserSession
    IsRdpSession, IsLocalAdmin, LogonTime, IdleTime, DisconnectTime, ClientName, ClientProtocolType, ClientDirectory, ClientBuildNumber

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Get-LoggedOnUser

.NOTES

Description of ConnectState property:

Value        Description
-----        -----------
Active       A user is logged on to the session.
ConnectQuery The session is in the process of connecting to a client.
Connected    A client is connected to the session.
Disconnected The session is active, but the client has disconnected from it.
Down         The session is down due to an error.
Idle         The session is waiting for a client to connect.
Initializing The session is initializing.
Listening    The session is listening for connections.
Reset        The session is being reset.
Shadowing    This session is shadowing another session.

Description of IsActiveUserSession property:

- If a console user exists, then that will be the active user session.
- If no console user exists but users are logged in, such as on terminal servers, then the first logged-in non-console user that has ConnectState either 'Active' or 'Connected' is the active user.

Description of IsRdpSession property:
- Gets a value indicating whether the user is associated with an RDP client session.

Description of IsLocalAdmin property:
- Checks whether the user is a member of the Administrators group

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Try {
            Write-Log -Message 'Getting session information for all logged on users.' -Source ${CmdletName}
            Write-Output -InputObject ([PSADT.QueryUser]::GetUserSessionInfo("$env:ComputerName"))
        }
        Catch {
            Write-Log -Message "Failed to get session information for all logged on users. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-PendingReboot {
    <#
.SYNOPSIS

Get the pending reboot status on a local computer.

.DESCRIPTION

Check WMI and the registry to determine if the system has a pending reboot operation from any of the following:
a) Component Based Servicing (Vista, Windows 2008)
b) Windows Update / Auto Update (XP, Windows 2003 / 2008)
c) SCCM 2012 Clients (DetermineIfRebootPending WMI method)
d) App-V Pending Tasks (global based Appv 5.0 SP2)
e) Pending File Rename Operations (XP, Windows 2003 / 2008)

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject

Returns a custom object with the following properties
- ComputerName
- LastBootUpTime
- IsSystemRebootPending
- IsCBServicingRebootPending
- IsWindowsUpdateRebootPending
- IsSCCMClientRebootPending
- IsFileRenameRebootPending
- PendingFileRenameOperations
- ErrorMsg

.EXAMPLE

Get-PendingReboot

Returns custom object with following properties:
- ComputerName
- LastBootUpTime
- IsSystemRebootPending
- IsCBServicingRebootPending
- IsWindowsUpdateRebootPending
- IsSCCMClientRebootPending
- IsFileRenameRebootPending
- PendingFileRenameOperations
- ErrorMsg

.EXAMPLE

(Get-PendingReboot).IsSystemRebootPending

Returns boolean value determining whether or not there is a pending reboot operation.

.NOTES

ErrorMsg only contains something if an error occurred

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header

        ## Initialize variables
        [String]$private:ComputerName = $envComputerNameFQDN
        $PendRebootErrorMsg = $null
    }
    Process {
        Write-Log -Message "Getting the pending reboot status on the local computer [$ComputerName]." -Source ${CmdletName}

        ## Get the date/time that the system last booted up
        Try {
            [Nullable[DateTime]]$LastBootUpTime = (Get-Date -ErrorAction 'Stop') - ([Timespan]::FromMilliseconds([Math]::Abs([Environment]::TickCount)))
        }
        Catch {
            [Nullable[DateTime]]$LastBootUpTime = $null
            [String[]]$PendRebootErrorMsg += "Failed to get LastBootUpTime: $($_.Exception.Message)"
            Write-Log -Message "Failed to get LastBootUpTime. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Determine if a Windows Vista/Server 2008 and above machine has a pending reboot from a Component Based Servicing (CBS) operation
        Try {
            If (([Version]$envOSVersion).Major -ge 5) {
                If (Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending' -ErrorAction 'Stop') {
                    [Nullable[Boolean]]$IsCBServicingRebootPending = $true
                }
                Else {
                    [Nullable[Boolean]]$IsCBServicingRebootPending = $false
                }
            }
        }
        Catch {
            [Nullable[Boolean]]$IsCBServicingRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsCBServicingRebootPending: $($_.Exception.Message)"
            Write-Log -Message "Failed to get IsCBServicingRebootPending. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Determine if there is a pending reboot from a Windows Update
        Try {
            If (Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired' -ErrorAction 'Stop') {
                [Nullable[Boolean]]$IsWindowsUpdateRebootPending = $true
            }
            Else {
                [Nullable[Boolean]]$IsWindowsUpdateRebootPending = $false
            }
        }
        Catch {
            [Nullable[Boolean]]$IsWindowsUpdateRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsWindowsUpdateRebootPending: $($_.Exception.Message)"
            Write-Log -Message "Failed to get IsWindowsUpdateRebootPending. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Determine if there is a pending reboot from a pending file rename operation
        [Boolean]$IsFileRenameRebootPending = $false
        $PendingFileRenameOperations = $null
        If (Test-RegistryValue -Key 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager' -Value 'PendingFileRenameOperations') {
            #  If PendingFileRenameOperations value exists, set $IsFileRenameRebootPending variable to $true
            [Boolean]$IsFileRenameRebootPending = $true
            #  Get the value of PendingFileRenameOperations
            Try {
                [String[]]$PendingFileRenameOperations = Get-ItemProperty -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager' -ErrorAction 'Stop' | Select-Object -ExpandProperty 'PendingFileRenameOperations' -ErrorAction 'Stop'
            }
            Catch {
                [String[]]$PendRebootErrorMsg += "Failed to get PendingFileRenameOperations: $($_.Exception.Message)"
                Write-Log -Message "Failed to get PendingFileRenameOperations. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            }
        }

        ## Determine SCCM 2012 Client reboot pending status
        Try {
            [Boolean]$IsSccmClientNamespaceExists = $false
            [PSObject]$SCCMClientRebootStatus = Invoke-WmiMethod -ComputerName $ComputerName -Namespace 'ROOT\CCM\ClientSDK' -Class 'CCM_ClientUtilities' -Name 'DetermineIfRebootPending' -ErrorAction 'Stop'
            [Boolean]$IsSccmClientNamespaceExists = $true
            If ($SCCMClientRebootStatus.ReturnValue -ne 0) {
                Throw "'DetermineIfRebootPending' method of 'ROOT\CCM\ClientSDK\CCM_ClientUtilities' class returned error code [$($SCCMClientRebootStatus.ReturnValue)]"
            }
            Else {
                Write-Log -Message 'Successfully queried SCCM client for reboot status.' -Source ${CmdletName}
                [Nullable[Boolean]]$IsSCCMClientRebootPending = $false
                If ($SCCMClientRebootStatus.IsHardRebootPending -or $SCCMClientRebootStatus.RebootPending) {
                    [Nullable[Boolean]]$IsSCCMClientRebootPending = $true
                    Write-Log -Message 'Pending SCCM reboot detected.' -Source ${CmdletName}
                }
                Else {
                    Write-Log -Message 'Pending SCCM reboot not detected.' -Source ${CmdletName}
                }
            }
        }
        Catch [System.Management.ManagementException] {
            [Nullable[Boolean]]$IsSCCMClientRebootPending = $null
            [Boolean]$IsSccmClientNamespaceExists = $false
            Write-Log -Message 'Failed to get IsSCCMClientRebootPending. Failed to detect the SCCM client WMI class.' -Severity 3 -Source ${CmdletName}
        }
        Catch {
            [Nullable[Boolean]]$IsSCCMClientRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsSCCMClientRebootPending: $($_.Exception.Message)"
            Write-Log -Message "Failed to get IsSCCMClientRebootPending. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Determine if there is a pending reboot from an App-V global Pending Task. (User profile based tasks will complete on logoff/logon)
        Try {
            If (Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Software\Microsoft\AppV\Client\PendingTasks' -ErrorAction 'Stop') {
                [Nullable[Boolean]]$IsAppVRebootPending = $true
            }
            Else {
                [Nullable[Boolean]]$IsAppVRebootPending = $false
            }
        }
        Catch {
            [Nullable[Boolean]]$IsAppVRebootPending = $null
            [String[]]$PendRebootErrorMsg += "Failed to get IsAppVRebootPending: $($_.Exception.Message)"
            Write-Log -Message "Failed to get IsAppVRebootPending. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
        }

        ## Determine if there is a pending reboot for the system
        [Boolean]$IsSystemRebootPending = $false
        If ($IsCBServicingRebootPending -or $IsWindowsUpdateRebootPending -or $IsSCCMClientRebootPending -or $IsFileRenameRebootPending) {
            [Boolean]$IsSystemRebootPending = $true
        }

        ## Create a custom object containing pending reboot information for the system
        [PSObject]$PendingRebootInfo = New-Object -TypeName 'PSObject' -Property @{
            ComputerName                 = $ComputerName
            LastBootUpTime               = $LastBootUpTime
            IsSystemRebootPending        = $IsSystemRebootPending
            IsCBServicingRebootPending   = $IsCBServicingRebootPending
            IsWindowsUpdateRebootPending = $IsWindowsUpdateRebootPending
            IsSCCMClientRebootPending    = $IsSCCMClientRebootPending
            IsAppVRebootPending          = $IsAppVRebootPending
            IsFileRenameRebootPending    = $IsFileRenameRebootPending
            PendingFileRenameOperations  = $PendingFileRenameOperations
            ErrorMsg                     = $PendRebootErrorMsg
        }
        Write-Log -Message "Pending reboot status on the local computer [$ComputerName]: `r`n$($PendingRebootInfo | Format-List | Out-String)" -Source ${CmdletName}
    }
    End {
        Write-Output -InputObject ($PendingRebootInfo | Select-Object -Property 'ComputerName', 'LastBootUpTime', 'IsSystemRebootPending', 'IsCBServicingRebootPending', 'IsWindowsUpdateRebootPending', 'IsSCCMClientRebootPending', 'IsAppVRebootPending', 'IsFileRenameRebootPending', 'PendingFileRenameOperations', 'ErrorMsg')

        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Configure-EdgeExtension {
    <#
    .SYNOPSIS
    Configures an extension for Microsoft Edge using the ExtensionSettings policy
    .DESCRIPTION
    This function configures an extension for Microsoft Edge using the ExtensionSettings policy: https://learn.microsoft.com/en-us/deployedge/microsoft-edge-manage-extensions-ref-guide
    This enables Edge Extensions to be installed and managed like applications, enabling extensions to be pushed to specific devices or users alongside existing GPO/Intune extension policies.
    This should not be used in conjunction with Edge Management Service which leverages the same registry key to configure Edge extensions.
    .PARAMETER Add
    Adds an extension configuration
    .PARAMETER Remove
    Removes an extension configuration
    .PARAMETER ExtensionID
    The ID of the extension to install.
    .PARAMETER InstallationMode
    The installation mode of the extension. Allowed values: blocked, allowed, removed, force_installed, normal_installed
    .PARAMETER UpdateUrl
    The update URL of the extension. This is the URL where the extension will check for updates.
    .PARAMETER MinimumVersionRequired
    The minimum version of the extension required for installation.
    .EXAMPLE
    Configure-EdgeExtension -Add -ExtensionID "extensionID" -InstallationMode "force_installed" -UpdateUrl "https://edge.microsoft.com/extensionwebstorebase/v1/crx"
    .EXAMPLE
    Configure-EdgeExtension -Remove -ExtensionID "extensionID"
    .NOTES
    This function is provided as a template to install an extension for Microsoft Edge. This should not be used in conjunction with Edge Management Service which leverages the same registry key to configure Edge extensions.
    #>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, ParameterSetName = 'Add')]
        [Switch]$Add,

        [Parameter(Mandatory = $true, ParameterSetName = 'Remove')]
        [Switch]$Remove,

        [Parameter(Mandatory = $true, ParameterSetName = 'Add')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Remove')]
        [String]$ExtensionID,

        [Parameter(Mandatory = $true, ParameterSetName = 'Add')]
        [ValidateSet('blocked', 'allowed', 'removed', 'force_installed', 'normal_installed')]
        [String]$InstallationMode,

        [Parameter(Mandatory = $true, ParameterSetName = 'Add')]
        [String]$UpdateUrl,

        [Parameter(Mandatory = $false, ParameterSetName = 'Add')]
        [String]$MinimumVersionRequired
    )
    If ($Add) {
        If ($MinimumVersionRequired) {
            Write-Log -Message "Configuring extension with ID [$extensionID] with mode [Add] using installation mode [$InstallationMode] and update URL [$UpdateUrl] with minimum version required [$MinimumVersionRequired]." -Severity 1
        }
        Else {
            Write-Log -Message "Configuring extension with ID [$extensionID] with mode [Add] using installation mode [$InstallationMode] and update URL [$UpdateUrl]." -Severity 1
        }
    }
    Else {
        Write-Log -Message "Configuring extension with ID [$extensionID] with mode [Add]." -Severity 1
    }

    $regKeyEdgeExtensions = 'HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge'
    # Check if the ExtensionSettings registry key exists if not create it
    If (!(Test-RegistryValue -Key $regKeyEdgeExtensions -Value ExtensionSettings)) {
        Set-RegistryKey -Key $regKeyEdgeExtensions -Name ExtensionSettings -Value "" | Out-Null
    }
    Else {
        # Get the installed extensions
        $installedExtensions = Get-RegistryKey -Key $regKeyEdgeExtensions -Value ExtensionSettings | ConvertFrom-Json -ErrorAction SilentlyContinue
        Write-Log -Message "Configured extensions: [$($installedExtensions | ConvertTo-Json -Compress -ErrorAction SilentlyContinue)]." -Severity 1
    }

    Try {
        If ($Remove) {
            If ($installedExtensions.$($extensionID)) {
                # If the deploymentmode is Remove, remove the extension from the list
                Write-Log -Message "Removing extension with ID [$extensionID]." -Severity 1
                $installedExtensions.PSObject.Properties.Remove($extensionID)
                $jsonExtensionSettings = $installedExtensions | ConvertTo-Json -Compress
                Set-RegistryKey -Key $regKeyEdgeExtensions -Name "ExtensionSettings" -Value $jsonExtensionSettings | Out-Null
            }
            Else { # If the extension is not configured
                Write-Log -Message "Extension with ID [$extensionID] is not configured. Removal not required." -Severity 1
            }
        }
        # Configure the extension
        ElseIf ($Add) {
            Write-Log -Message "Configuring extension ID [$extensionID]." -Severity 1
            If (!$installedExtensions) {
                $installedExtensions = @{}
            }
            If ($MinimumVersionRequired) {
                $installedExtensions | Add-Member -Name $($extensionID) -Value $(@{ "installation_mode" = $InstallationMode; "update_url" = $UpdateUrl; "minimum_version_required" = $MinimumVersionRequired }) -MemberType NoteProperty -Force
            }
            Else {
                $installedExtensions | Add-Member -Name $($extensionID) -Value $(@{ "installation_mode" = $InstallationMode; "update_url" = $UpdateUrl }) -MemberType NoteProperty -Force
            }
            $jsonExtensionSettings = $installedExtensions | ConvertTo-Json -Compress
            Set-RegistryKey -Key $regKeyEdgeExtensions -Name "ExtensionSettings" -Value $jsonExtensionSettings | Out-Null
        }
    }
    Catch {
        Write-Log -Message "Failed to configure extension with ID $extensionID. `r`n$(Resolve-Error)" -Severity 3
        Exit-Script -ExitCode 60001
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-SidTypeAccountName
{
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [Security.Principal.WellKnownSidType]$WellKnownSidType
    )

    # Translate the SidType into its user-readable name.
    return [System.Security.Principal.SecurityIdentifier]::new($WellKnownSidType, $null).Translate([System.Security.Principal.NTAccount]).Value
}

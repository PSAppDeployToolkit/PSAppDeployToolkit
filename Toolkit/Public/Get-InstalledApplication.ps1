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
					} Catch {
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
				} Else {
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
							} Else {
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
						} ElseIf ($WildCard) {
							#  Check for wildcard application name match
							If ($regKeyApp.DisplayName -like $application) {
								$applicationMatched = $true
								Write-Log -Message "Found installed application [$appDisplayName] version [$appDisplayVersion] using wildcard matching for search term [$application]." -Source ${CmdletName}
							}
						} ElseIf ($RegEx) {
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
								} Else {
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
			} Catch {
				Write-Log -Message "Failed to resolve application details from registry for [$appDisplayName]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				Continue
			}
		}

		If (-not $IncludeUpdatesAndHotfixes) {
			## Write to log the number of entries skipped due to them being considered updates
			If ($UpdatesSkippedCounter -eq 1) {
				Write-Log -Message 'Skipped 1 entry while searching, because it was considered a Microsoft update.' -Source ${CmdletName}
			} Else {
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

Function Remove-MSIApplications {
    <#
.SYNOPSIS

Removes all MSI applications matching the specified application name.

.DESCRIPTION

Removes all MSI applications matching the specified application name.
Enumerates the registry for installed applications matching the specified application name and uninstalls that application using the product code, provided the uninstall string matches "msiexec".

.PARAMETER Name

The name of the application to uninstall. Performs a contains match on the application display name by default.

.PARAMETER Exact

Specifies that the named application must be matched using the exact name.

.PARAMETER WildCard

Specifies that the named application must be matched using a wildcard search.

.PARAMETER Parameters

Overrides the default parameters specified in the XML configuration file. Uninstall default is: "REBOOT=ReallySuppress /QN".

.PARAMETER AddParameters

Adds to the default parameters specified in the XML configuration file. Uninstall default is: "REBOOT=ReallySuppress /QN".

.PARAMETER FilterApplication

Two-dimensional array that contains one or more (property, value, match-type) sets that should be used to filter the list of results returned by Get-ADTInstalledApplication to only those that should be uninstalled.
Properties that can be filtered upon: ProductCode, DisplayName, DisplayVersion, UninstallString, InstallSource, InstallLocation, InstallDate, Publisher, Is64BitApplication

.PARAMETER ExcludeFromUninstall

Two-dimensional array that contains one or more (property, value, match-type) sets that should be excluded from uninstall if found.
Properties that can be excluded: ProductCode, DisplayName, DisplayVersion, UninstallString, InstallSource, InstallLocation, InstallDate, Publisher, Is64BitApplication

.PARAMETER IncludeUpdatesAndHotfixes

Include matches against updates and hotfixes in results.

.PARAMETER LoggingOptions

Overrides the default logging options specified in the XML configuration file. Default options are: "/L*v".

.PARAMETER private:LogName

Overrides the default log file name. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.
For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

.PARAMETER LogName

Overrides the default log file name. The default log file name is generated from the MSI file name. If LogName does not end in .log, it will be automatically appended.
For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

.PARAMETER PassThru

Returns ExitCode, STDOut, and STDErr output from the process.

.PARAMETER ContinueOnError

Continue if an error occured while trying to start the processes. Default: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

PSObject

Returns an object with the following properties:
- ExitCode
- StdOut
- StdErr

.EXAMPLE

Remove-MSIApplications -Name 'Adobe Flash'

Removes all versions of software that match the name "Adobe Flash"

.EXAMPLE

Remove-MSIApplications -Name 'Adobe'

Removes all versions of software that match the name "Adobe"

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -FilterApplication @(
        @('Is64BitApplication', $false, 'Exact'),
        @('Publisher', 'Oracle Corporation', 'Exact')
    )

Removes all versions of software that match the name "Java 8 Update" where the software is 32-bits and the publisher is "Oracle Corporation".

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -FilterApplication @(, @('Publisher', 'Oracle Corporation', 'Exact')) -ExcludeFromUninstall @(, @('DisplayName', 'Java 8 Update 45', 'Contains'))

Removes all versions of software that match the name "Java 8 Update" and also have "Oracle Corporation" as the Publisher; however, it does not uninstall "Java 8 Update 45" of the software.
NOTE: If only specifying a single row in the two-dimensional arrays, the array must have the extra parentheses and leading comma as in this example.

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -ExcludeFromUninstall @(, @('DisplayName', 'Java 8 Update 45', 'Contains'))

Removes all versions of software that match the name "Java 8 Update"; however, it does not uninstall "Java 8 Update 45" of the software.
NOTE: If only specifying a single row in the two-dimensional array, the array must have the extra parentheses and leading comma as in this example.

.EXAMPLE

Remove-MSIApplications -Name 'Java 8 Update' -ExcludeFromUninstall @(
    @('Is64BitApplication', $true, 'Exact'),
    @('DisplayName', 'Java 8 Update 45', 'Exact'),
    @('DisplayName', 'Java 8 Update 4*', 'WildCard'),
    @('DisplayName', 'Java \d Update \d{3}', 'RegEx'),
    @('DisplayName', 'Java 8 Update', 'Contains'))

Removes all versions of software that match the name "Java 8 Update"; however, it does not uninstall 64-bit versions of the software, Update 45 of the software, or any Update that starts with 4.

.NOTES

More reading on how to create arrays if having trouble with -FilterApplication or -ExcludeFromUninstall parameter: http://blogs.msdn.com/b/powershell/archive/2007/01/23/array-literals-in-powershell.aspx

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [Switch]$Exact = $false,
        [Parameter(Mandatory = $false)]
        [Switch]$WildCard = $false,
        [Parameter(Mandatory = $false)]
        [Alias('Arguments')]
        [ValidateNotNullorEmpty()]
        [String]$Parameters,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$AddParameters,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Array]$FilterApplication = @(@()),
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Array]$ExcludeFromUninstall = @(@()),
        [Parameter(Mandatory = $false)]
        [Switch]$IncludeUpdatesAndHotfixes = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [String]$LoggingOptions,
        [Parameter(Mandatory = $false)]
        [Alias('LogName')]
        [String]$private:LogName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Switch]$PassThru = $false,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullorEmpty()]
        [Boolean]$ContinueOnError = $true
    )

    Begin {
        Initialize-ADTFunction -Cmdlet $PSCmdlet
    }
    Process {
        ## Build the hashtable with the options that will be passed to Get-ADTInstalledApplication using splatting
        [Hashtable]$GetInstalledApplicationSplat = @{ Name = $name }
        If ($Exact) {
            $GetInstalledApplicationSplat.Add( 'Exact', $Exact)
        }
        ElseIf ($WildCard) {
            $GetInstalledApplicationSplat.Add( 'WildCard', $WildCard)
        }
        If ($IncludeUpdatesAndHotfixes) {
            $GetInstalledApplicationSplat.Add( 'IncludeUpdatesAndHotfixes', $IncludeUpdatesAndHotfixes)
        }

        [PSADT.Types.InstalledApplication[]]$installedApplications = Get-ADTInstalledApplication @GetInstalledApplicationSplat

        Write-ADTLogEntry -Message "Found [$($installedApplications.Count)] application(s) that matched the specified criteria [$Name]."

        ## Filter the results from Get-ADTInstalledApplication
        [Collections.ArrayList]$removeMSIApplications = New-Object -TypeName 'System.Collections.ArrayList'
        If (($null -ne $installedApplications) -and ($installedApplications.Count)) {
            ForEach ($installedApplication in $installedApplications) {
                If ([String]::IsNullOrEmpty($installedApplication.ProductCode)) {
                    Write-ADTLogEntry -Message "Skipping removal of application [$($installedApplication.DisplayName)] because unable to discover MSI ProductCode from application's registry Uninstall subkey [$($installedApplication.UninstallSubkey)]." -Severity 2
                    Continue
                }

                #  Filter the results from Get-ADTInstalledApplication to only those that should be uninstalled
                [Boolean]$addAppToRemoveList = $true
                If (($null -ne $FilterApplication) -and ($FilterApplication.Count)) {
                    Write-ADTLogEntry -Message 'Filter the results to only those that should be uninstalled as specified in parameter [-FilterApplication].'
                    ForEach ($Filter in $FilterApplication) {
                        If ($Filter[2] -eq 'RegEx') {
                            If ($installedApplication.($Filter[0]) -match $Filter[1]) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-ADTLogEntry -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of regex match against [-FilterApplication] criteria."
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                        ElseIf ($Filter[2] -eq 'Contains') {
                            If ($installedApplication.($Filter[0]) -match [RegEx]::Escape($Filter[1])) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-ADTLogEntry -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of contains match against [-FilterApplication] criteria."
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                        ElseIf ($Filter[2] -eq 'WildCard') {
                            If ($installedApplication.($Filter[0]) -like $Filter[1]) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-ADTLogEntry -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of wildcard match against [-FilterApplication] criteria."
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                        ElseIf ($Filter[2] -eq 'Exact') {
                            If ($installedApplication.($Filter[0]) -eq $Filter[1]) {
                                [Boolean]$addAppToRemoveList = $true
                                Write-ADTLogEntry -Message "Preserve removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of exact match against [-FilterApplication] criteria."
                            }
                            Else {
                                [Boolean]$addAppToRemoveList = $false
                                Break
                            }
                        }
                    }
                }

                #  Filter the results from Get-ADTInstalledApplication to remove those that should never be uninstalled
                If (($null -ne $ExcludeFromUninstall) -and ($ExcludeFromUninstall.Count)) {
                    ForEach ($Exclude in $ExcludeFromUninstall) {
                        If ($Exclude[2] -eq 'RegEx') {
                            If ($installedApplication.($Exclude[0]) -match $Exclude[1]) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-ADTLogEntry -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of regex match against [-ExcludeFromUninstall] criteria."
                                Break
                            }
                        }
                        ElseIf ($Exclude[2] -eq 'Contains') {
                            If ($installedApplication.($Exclude[0]) -match [RegEx]::Escape($Exclude[1])) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-ADTLogEntry -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of contains match against [-ExcludeFromUninstall] criteria."
                                Break
                            }
                        }
                        ElseIf ($Exclude[2] -eq 'WildCard') {
                            If ($installedApplication.($Exclude[0]) -like $Exclude[1]) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-ADTLogEntry -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of wildcard match against [-ExcludeFromUninstall] criteria."
                                Break
                            }
                        }
                        ElseIf ($Exclude[2] -eq 'Exact') {
                            If ($installedApplication.($Exclude[0]) -eq $Exclude[1]) {
                                [Boolean]$addAppToRemoveList = $false
                                Write-ADTLogEntry -Message "Skipping removal of application [$($installedApplication.DisplayName) $($installedApplication.Version)] because of exact match against [-ExcludeFromUninstall] criteria."
                                Break
                            }
                        }
                    }
                }

                If ($addAppToRemoveList) {
                    Write-ADTLogEntry -Message "Adding application to list for removal: [$($installedApplication.DisplayName) $($installedApplication.Version)]."
                    $removeMSIApplications.Add($installedApplication)
                }
            }
        }

        ## Build the hashtable with the options that will be passed to Start-ADTMsiProcess using splatting
        [Hashtable]$ExecuteMSISplat = @{
            Action          = 'Uninstall'
            Path            = ''
            ContinueOnError = $ContinueOnError
        }
        If ($Parameters) {
            $ExecuteMSISplat.Add( 'Parameters', $Parameters)
        }
        ElseIf ($AddParameters) {
            $ExecuteMSISplat.Add( 'AddParameters', $AddParameters)
        }
        If ($LoggingOptions) {
            $ExecuteMSISplat.Add( 'LoggingOptions', $LoggingOptions)
        }
        If ($LogName) {
            $ExecuteMSISplat.Add( 'LogName', $LogName)
        }
        If ($PassThru) {
            $ExecuteMSISplat.Add( 'PassThru', $PassThru)
        }
        If ($IncludeUpdatesAndHotfixes) {
            $ExecuteMSISplat.Add( 'IncludeUpdatesAndHotfixes', $IncludeUpdatesAndHotfixes)
        }

        $ExecuteResults = If (($null -ne $removeMSIApplications) -and ($removeMSIApplications.Count)) {
            ForEach ($removeMSIApplication in $removeMSIApplications) {
                Write-ADTLogEntry -Message "Removing application [$($removeMSIApplication.DisplayName) $($removeMSIApplication.Version)]."
                $ExecuteMSISplat.Path = $removeMSIApplication.ProductCode
                Start-ADTMsiProcess @ExecuteMSISplat
            }
        }
        Else {
            Write-ADTLogEntry -Message 'No applications found for removal. Continue...'
        }
    }
    End {
        If ($PassThru -and $ExecuteResults) {
            Write-Output -InputObject ($ExecuteResults)
        }
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

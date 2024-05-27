#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Test-MSUpdates {
    <#
.SYNOPSIS

Test whether a Microsoft Windows update is installed.

.DESCRIPTION

Test whether a Microsoft Windows update is installed.

.PARAMETER KBNumber

KBNumber of the update.

.PARAMETER ContinueOnError

Suppress writing log message to console on failure to write message to log file. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.Boolean

Returns $true if the update is installed, otherwise returns $false.

.EXAMPLE

Test-MSUpdates -KBNumber 'KB2549864'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true, Position = 0, HelpMessage = 'Enter the KB Number for the Microsoft Update')]
        [ValidateNotNullorEmpty()]
        [String]$KBNumber,
        [Parameter(Mandatory = $false, Position = 1)]
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
            Write-Log -Message "Checking if Microsoft Update [$kbNumber] is installed." -Source ${CmdletName}

            ## Default is not found
            [Boolean]$kbFound = $false

            ## Check for update using built in PS cmdlet which uses WMI in the background to gather details
            Get-HotFix -Id $kbNumber -ErrorAction 'SilentlyContinue' | ForEach-Object { $kbFound = $true }

            If (-not $kbFound) {
                Write-Log -Message 'Unable to detect Windows update history via Get-Hotfix cmdlet. Trying via COM object.' -Source ${CmdletName}

                ## Check for update using ComObject method (to catch Office updates)
                [__ComObject]$UpdateSession = New-Object -ComObject 'Microsoft.Update.Session'
                [__ComObject]$UpdateSearcher = $UpdateSession.CreateUpdateSearcher()
                #  Indicates whether the search results include updates that are superseded by other updates in the search results
                $UpdateSearcher.IncludePotentiallySupersededUpdates = $false
                #  Indicates whether the UpdateSearcher goes online to search for updates.
                $UpdateSearcher.Online = $false
                [Int32]$UpdateHistoryCount = $UpdateSearcher.GetTotalHistoryCount()
                If ($UpdateHistoryCount -gt 0) {
                    [PSObject]$UpdateHistory = $UpdateSearcher.QueryHistory(0, $UpdateHistoryCount) |
                        Select-Object -Property 'Title', 'Date',
                        @{Name = 'Operation'; Expression = { Switch ($_.Operation) {
                                    1 {
                                        'Installation'
                                    }; 2 {
                                        'Uninstallation'
                                    }; 3 {
                                        'Other'
                                    }
                                } }
                        },
                        @{Name = 'Status'; Expression = { Switch ($_.ResultCode) {
                                    0 {
                                        'Not Started'
                                    }; 1 {
                                        'In Progress'
                                    }; 2 {
                                        'Successful'
                                    }; 3 {
                                        'Incomplete'
                                    }; 4 {
                                        'Failed'
                                    }; 5 {
                                        'Aborted'
                                    }
                                } }
                        },
                        'Description' |
                        Sort-Object -Property 'Date' -Descending
                    ForEach ($Update in $UpdateHistory) {
                        If (($Update.Operation -ne 'Other') -and ($Update.Title -match "\($KBNumber\)")) {
                            $LatestUpdateHistory = $Update
                            Break
                        }
                    }
                    If (($LatestUpdateHistory.Operation -eq 'Installation') -and ($LatestUpdateHistory.Status -eq 'Successful')) {
                        Write-Log -Message "Discovered the following Microsoft Update: `r`n$($LatestUpdateHistory | Format-List | Out-String)" -Source ${CmdletName}
                        $kbFound = $true
                    }
                    $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($UpdateSession)
                    $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($UpdateSearcher)
                }
                Else {
                    Write-Log -Message 'Unable to detect Windows update history via COM object.' -Source ${CmdletName}
                }
            }

            ## Return Result
            If (-not $kbFound) {
                Write-Log -Message "Microsoft Update [$kbNumber] is not installed." -Source ${CmdletName}
                Write-Output -InputObject ($false)
            }
            Else {
                Write-Log -Message "Microsoft Update [$kbNumber] is installed." -Source ${CmdletName}
                Write-Output -InputObject ($true)
            }
        }
        Catch {
            Write-Log -Message "Failed discovering Microsoft Update [$kbNumber]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
            If (-not $ContinueOnError) {
                Throw "Failed discovering Microsoft Update [$kbNumber]: $($_.Exception.Message)"
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

Function Install-MSUpdates {
    <#
.SYNOPSIS

Install all Microsoft Updates in a given directory.

.DESCRIPTION

Install all Microsoft Updates of type ".exe", ".msu", or ".msp" in a given directory (recursively search directory).

.PARAMETER Directory

Directory containing the updates.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Install-MSUpdates -Directory "$dirFiles\MSUpdates"

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$Directory
    )

    Begin {
        ## Get the name of this function and write header
        [String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
    }
    Process {
        Write-Log -Message "Recursively installing all Microsoft Updates in directory [$Directory]." -Source ${CmdletName}

        ## KB Number pattern match
        $kbPattern = '(?i)kb\d{6,8}'

        ## Get all hotfixes and install if required
        [IO.FileInfo[]]$files = Get-ChildItem -LiteralPath $Directory -Recurse -Include ('*.exe', '*.msu', '*.msp')
        ForEach ($file in $files) {
            If ($file.Name -match 'redist') {
                [Version]$redistVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName).ProductVersion
                [String]$redistDescription = [Diagnostics.FileVersionInfo]::GetVersionInfo($file.FullName).FileDescription

                Write-Log -Message "Installing [$redistDescription $redistVersion]..." -Source ${CmdletName}
                #  Handle older redistributables (ie, VC++ 2005)
                If ($redistDescription -match 'Win32 Cabinet Self-Extractor') {
                    Execute-Process -Path $file.FullName -Parameters '/q' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                }
                Else {
                    Execute-Process -Path $file.FullName -Parameters '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                }
            }
            Else {
                #  Get the KB number of the file
                [String]$kbNumber = [RegEx]::Match($file.Name, $kbPattern).ToString()
                If (-not $kbNumber) {
                    Continue
                }

                #  Check to see whether the KB is already installed
                If (-not (Test-MSUpdates -KBNumber $kbNumber)) {
                    Write-Log -Message "KB Number [$KBNumber] was not detected and will be installed." -Source ${CmdletName}
                    Switch ($file.Extension) {
                        #  Installation type for executables (i.e., Microsoft Office Updates)
                        '.exe' {
                            Execute-Process -Path $file.FullName -Parameters '/quiet /norestart' -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                        }
                        #  Installation type for Windows updates using Windows Update Standalone Installer
                        '.msu' {
                            Execute-Process -Path $Script:ADT.Environment.exeWusa -Parameters "`"$($file.FullName)`" /quiet /norestart" -WindowStyle 'Hidden' -IgnoreExitCodes '*'
                        }
                        #  Installation type for Windows Installer Patch
                        '.msp' {
                            Execute-MSI -Action 'Patch' -Path $file.FullName -IgnoreExitCodes '*'
                        }
                    }
                }
                Else {
                    Write-Log -Message "KB Number [$kbNumber] is already installed. Continue..." -Source ${CmdletName}
                }
            }
        }
    }
    End {
        Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
    }
}

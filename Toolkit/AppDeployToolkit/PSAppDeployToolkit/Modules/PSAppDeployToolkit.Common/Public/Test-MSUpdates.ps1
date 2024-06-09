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
        Write-ADTDebugHeader
    }
    Process {
        Try {
            Write-ADTLogEntry -Message "Checking if Microsoft Update [$kbNumber] is installed."

            ## Default is not found
            [Boolean]$kbFound = $false

            ## Check for update using built in PS cmdlet which uses WMI in the background to gather details
            Get-HotFix -Id $kbNumber -ErrorAction 'Ignore' | ForEach-Object { $kbFound = $true }

            If (-not $kbFound) {
                Write-ADTLogEntry -Message 'Unable to detect Windows update history via Get-Hotfix cmdlet. Trying via COM object.'

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
                        Write-ADTLogEntry -Message "Discovered the following Microsoft Update: `r`n$($LatestUpdateHistory | Format-List | Out-String)"
                        $kbFound = $true
                    }
                    $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($UpdateSession)
                    $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($UpdateSearcher)
                }
                Else {
                    Write-ADTLogEntry -Message 'Unable to detect Windows update history via COM object.'
                }
            }

            ## Return Result
            If (-not $kbFound) {
                Write-ADTLogEntry -Message "Microsoft Update [$kbNumber] is not installed."
                Write-Output -InputObject ($false)
            }
            Else {
                Write-ADTLogEntry -Message "Microsoft Update [$kbNumber] is installed."
                Write-Output -InputObject ($true)
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed discovering Microsoft Update [$kbNumber]. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed discovering Microsoft Update [$kbNumber]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}

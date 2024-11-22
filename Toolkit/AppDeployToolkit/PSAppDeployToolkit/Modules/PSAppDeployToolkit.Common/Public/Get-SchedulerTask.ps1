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
        $adtEnv = Get-ADTEnvironment
        Write-ADTDebugHeader

        [PSObject[]]$ScheduledTasks = @()
    }
    Process {
        Try {
            Write-ADTLogEntry -Message 'Retrieving Scheduled Tasks...'
            [String[]]$exeSchtasksResults = & $adtEnv.exeSchTasks /Query /V /FO CSV
            If ($global:LastExitCode -ne 0) {
                Throw "Failed to retrieve scheduled tasks using [$($adtEnv.exeSchTasks)]."
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
            Write-ADTLogEntry -Message "Failed to retrieve scheduled tasks.`n$(Resolve-ADTError)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to retrieve scheduled tasks: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-Output -InputObject ($ScheduledTasks)
        Write-ADTDebugFooter
    }
}

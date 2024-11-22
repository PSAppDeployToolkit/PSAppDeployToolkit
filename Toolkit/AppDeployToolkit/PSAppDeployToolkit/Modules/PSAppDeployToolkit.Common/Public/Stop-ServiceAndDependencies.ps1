Function Stop-ServiceAndDependencies {
    <#
.SYNOPSIS

Stop Windows service and its dependencies.

.DESCRIPTION

Stop Windows service and its dependencies.

.PARAMETER Name

Specify the name of the service.

.PARAMETER SkipServiceExistsTest

Choose to skip the test to check whether or not the service exists if it was already done outside of this function.

.PARAMETER SkipDependentServices

Choose to skip checking for and stopping dependent services. Default is: $false.

.PARAMETER PendingStatusWait

The amount of time to wait for a service to get out of a pending state before continuing. Default is 60 seconds.

.PARAMETER PassThru

Return the System.ServiceProcess.ServiceController service object.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.ServiceProcess.ServiceController.

Returns the service object.

.EXAMPLE

Stop-ServiceAndDependencies -Name 'wuauserv'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$SkipServiceExistsTest,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$SkipDependentServices,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Timespan]$PendingStatusWait = (New-TimeSpan -Seconds 60),
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Switch]$PassThru,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )
    Begin {
        Write-ADTDebugHeader
    }
    Process {
        Try {
            ## Check to see if the service exists
            If ((-not $SkipServiceExistsTest) -and (-not (Test-ADTServiceExists -Name $Name -ContinueOnError $false))) {
                Write-ADTLogEntry -Message "Service [$Name] does not exist." -Severity 2
                Throw "Service [$Name] does not exist."
            }

            ## Get the service object
            Write-ADTLogEntry -Message "Getting the service object for service [$Name]."
            [ServiceProcess.ServiceController]$Service = Get-Service -Name $Name -ErrorAction 'Stop'
            ## Wait up to 60 seconds if service is in a pending state
            [String[]]$PendingStatus = 'ContinuePending', 'PausePending', 'StartPending', 'StopPending'
            If ($PendingStatus -contains $Service.Status) {
                Switch ($Service.Status) {
                    'ContinuePending' {
                        $DesiredStatus = 'Running'
                    }
                    'PausePending' {
                        $DesiredStatus = 'Paused'
                    }
                    'StartPending' {
                        $DesiredStatus = 'Running'
                    }
                    'StopPending' {
                        $DesiredStatus = 'Stopped'
                    }
                }
                Write-ADTLogEntry -Message "Waiting for up to [$($PendingStatusWait.TotalSeconds)] seconds to allow service pending status [$($Service.Status)] to reach desired status [$DesiredStatus]."
                $Service.WaitForStatus([ServiceProcess.ServiceControllerStatus]$DesiredStatus, $PendingStatusWait)
                $Service.Refresh()
            }
            ## Discover if the service is currently running
            Write-ADTLogEntry -Message "Service [$($Service.ServiceName)] with display name [$($Service.DisplayName)] has a status of [$($Service.Status)]."
            If ($Service.Status -ne 'Stopped') {
                #  Discover all dependent services that are running and stop them
                If (-not $SkipDependentServices) {
                    Write-ADTLogEntry -Message "Discovering all dependent service(s) for service [$Name] which are not 'Stopped'."
                    [ServiceProcess.ServiceController[]]$DependentServices = Get-Service -Name $Service.ServiceName -DependentServices -ErrorAction 'Stop' | Where-Object { $_.Status -ne 'Stopped' }
                    If ($DependentServices) {
                        ForEach ($DependentService in $DependentServices) {
                            Write-ADTLogEntry -Message "Stopping dependent service [$($DependentService.ServiceName)] with display name [$($DependentService.DisplayName)] and a status of [$($DependentService.Status)]."
                            Try {
                                Stop-Service -InputObject (Get-Service -Name $DependentService.ServiceName -ErrorAction 'Stop') -Force -WarningAction 'Ignore' -ErrorAction 'Stop'
                            }
                            Catch {
                                Write-ADTLogEntry -Message "Failed to stop dependent service [$($DependentService.ServiceName)] with display name [$($DependentService.DisplayName)] and a status of [$($DependentService.Status)]. Continue..." -Severity 2
                                Continue
                            }
                        }
                    }
                    Else {
                        Write-ADTLogEntry -Message "Dependent service(s) were not discovered for service [$Name]."
                    }
                }
                #  Stop the parent service
                Write-ADTLogEntry -Message "Stopping parent service [$($Service.ServiceName)] with display name [$($Service.DisplayName)]."
                [ServiceProcess.ServiceController]$Service = Stop-Service -InputObject (Get-Service -Name $Service.ServiceName -ErrorAction 'Stop') -Force -PassThru -WarningAction 'Ignore' -ErrorAction 'Stop'
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to stop the service [$Name].`n$(Resolve-ADTError)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to stop the service [$Name]: $($_.Exception.Message)"
            }
        }
        Finally {
            #  Return the service object if option selected
            If ($PassThru -and $Service) {
                Write-Output -InputObject ($Service)
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}

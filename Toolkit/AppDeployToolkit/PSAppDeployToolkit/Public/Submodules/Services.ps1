#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Test-ServiceExists {
    <#
.SYNOPSIS

Check to see if a service exists.

.DESCRIPTION

Check to see if a service exists (using WMI method because Get-Service will generate ErrorRecord if service doesn't exist).

.PARAMETER Name

Specify the name of the service.

Note: Service name can be found by executing "Get-Service | Format-Table -AutoSize -Wrap" or by using the properties screen of a service in services.msc.

.PARAMETER ComputerName

Specify the name of the computer. Default is: the local computer.

.PARAMETER PassThru

Return the WMI service object. To see all the properties use: Test-ServiceExists -Name 'spooler' -PassThru | Get-Member

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Test-ServiceExists -Name 'wuauserv'

.EXAMPLE

Test-ServiceExists -Name 'testservice' -PassThru | Where-Object { $_ } | ForEach-Object { $_.Delete() }

Check if a service exists and then delete it by using the -PassThru parameter.

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
        [String]$ComputerName = $env:ComputerName,
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
            $ServiceObject = Get-WmiObject -ComputerName $ComputerName -Class 'Win32_Service' -Filter "Name='$Name'" -ErrorAction 'Stop'
            # If nothing is returned from Win32_Service, check Win32_BaseService
            If (-not $ServiceObject) {
                $ServiceObject = Get-WmiObject -ComputerName $ComputerName -Class 'Win32_BaseService' -Filter "Name='$Name'" -ErrorAction 'Stop'
            }

            If ($ServiceObject) {
                Write-ADTLogEntry -Message "Service [$Name] exists."
                If ($PassThru) {
                    Write-Output -InputObject ($ServiceObject)
                }
                Else {
                    Write-Output -InputObject ($true)
                }
            }
            Else {
                Write-ADTLogEntry -Message "Service [$Name] does not exist."
                If ($PassThru) {
                    Write-Output -InputObject ($ServiceObject)
                }
                Else {
                    Write-Output -InputObject ($false)
                }
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed check to see if service [$Name] exists." -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed check to see if service [$Name] exists: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

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
            If ((-not $SkipServiceExistsTest) -and (-not (Test-ServiceExists -Name $Name -ContinueOnError $false))) {
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
            Write-ADTLogEntry -Message "Failed to stop the service [$Name]. `r`n$(Resolve-Error)" -Severity 3
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


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Start-ServiceAndDependencies {
    <#
.SYNOPSIS

Start Windows service and its dependencies.

.DESCRIPTION

Start Windows service and its dependencies.

.PARAMETER Name

Specify the name of the service.

.PARAMETER SkipServiceExistsTest

Choose to skip the test to check whether or not the service exists if it was already done outside of this function.

.PARAMETER SkipDependentServices

Choose to skip checking for and starting dependent services. Default is: $false.

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

Start-ServiceAndDependencies -Name 'wuauserv'

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
            If ((-not $SkipServiceExistsTest) -and (-not (Test-ServiceExists -Name $Name -ContinueOnError $false))) {
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
            ## Discover if the service is currently stopped
            Write-ADTLogEntry -Message "Service [$($Service.ServiceName)] with display name [$($Service.DisplayName)] has a status of [$($Service.Status)]."
            If ($Service.Status -ne 'Running') {
                #  Start the parent service
                Write-ADTLogEntry -Message "Starting parent service [$($Service.ServiceName)] with display name [$($Service.DisplayName)]."
                [ServiceProcess.ServiceController]$Service = Start-Service -InputObject (Get-Service -Name $Service.ServiceName -ErrorAction 'Stop') -PassThru -WarningAction 'Ignore' -ErrorAction 'Stop'

                #  Discover all dependent services that are stopped and start them
                If (-not $SkipDependentServices) {
                    Write-ADTLogEntry -Message "Discover all dependent service(s) for service [$Name] which are not 'Running'."
                    [ServiceProcess.ServiceController[]]$DependentServices = Get-Service -Name $Service.ServiceName -DependentServices -ErrorAction 'Stop' | Where-Object { $_.Status -ne 'Running' }
                    If ($DependentServices) {
                        ForEach ($DependentService in $DependentServices) {
                            Write-ADTLogEntry -Message "Starting dependent service [$($DependentService.ServiceName)] with display name [$($DependentService.DisplayName)] and a status of [$($DependentService.Status)]."
                            Try {
                                Start-Service -InputObject (Get-Service -Name $DependentService.ServiceName -ErrorAction 'Stop') -WarningAction 'Ignore' -ErrorAction 'Stop'
                            }
                            Catch {
                                Write-ADTLogEntry -Message "Failed to start dependent service [$($DependentService.ServiceName)] with display name [$($DependentService.DisplayName)] and a status of [$($DependentService.Status)]. Continue..." -Severity 2
                                Continue
                            }
                        }
                    }
                    Else {
                        Write-ADTLogEntry -Message "Dependent service(s) were not discovered for service [$Name]."
                    }
                }
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to start the service [$Name]. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to start the service [$Name]: $($_.Exception.Message)"
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


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Get-ServiceStartMode {
    <#
.SYNOPSIS

Get the service startup mode.

.DESCRIPTION

Get the service startup mode.

.PARAMETER Name

Specify the name of the service.

.PARAMETER ComputerName

Specify the name of the computer. Default is: the local computer.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.ServiceProcess.ServiceController.

Returns the service object.

.EXAMPLE

Get-ServiceStartMode -Name 'wuauserv'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdLetBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [String]$ComputerName = $env:ComputerName,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )
    Begin {
        Write-ADTDebugHeader
    }
    Process {
        Try {
            Write-ADTLogEntry -Message "Getting the service [$Name] startup mode."
            [String]$ServiceStartMode = (Get-WmiObject -ComputerName $ComputerName -Class 'Win32_Service' -Filter "Name='$Name'" -Property 'StartMode' -ErrorAction 'Stop').StartMode
            ## If service start mode is set to 'Auto', change value to 'Automatic' to be consistent with 'Set-ServiceStartMode' function
            If ($ServiceStartMode -eq 'Auto') {
                $ServiceStartMode = 'Automatic'
            }

            ## If on Windows Vista or higher, check to see if service is set to Automatic (Delayed Start)
            If (($ServiceStartMode -eq 'Automatic') -and ($Script:ADT.Environment.envOSVersionMajor -gt 5)) {
                [String]$ServiceRegistryPath = "Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\$Name"
                [Int32]$DelayedAutoStart = Get-ItemProperty -LiteralPath $ServiceRegistryPath -ErrorAction Ignore | Select-Object -ExpandProperty 'DelayedAutoStart' -ErrorAction Ignore
                If ($DelayedAutoStart -eq 1) {
                    $ServiceStartMode = 'Automatic (Delayed Start)'
                }
            }

            Write-ADTLogEntry -Message "Service [$Name] startup mode is set to [$ServiceStartMode]."
            Write-Output -InputObject ($ServiceStartMode)
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to get the service [$Name] startup mode. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to get the service [$Name] startup mode: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Set-ServiceStartMode {
    <#
.SYNOPSIS

Set the service startup mode.

.DESCRIPTION

Set the service startup mode.

.PARAMETER Name

Specify the name of the service.

.PARAMETER StartMode

Specify startup mode for the service. Options: Automatic, Automatic (Delayed Start), Manual, Disabled, Boot, System.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not return any objects.

.EXAMPLE

Set-ServiceStartMode -Name 'wuauserv' -StartMode 'Automatic (Delayed Start)'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdLetBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]$Name,
        [Parameter(Mandatory = $true)]
        [ValidateSet('Automatic', 'Automatic (Delayed Start)', 'Manual', 'Disabled', 'Boot', 'System')]
        [String]$StartMode,
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Boolean]$ContinueOnError = $true
    )
    Begin {
        Write-ADTDebugHeader
    }
    Process {
        Try {
            ## If on lower than Windows Vista and 'Automatic (Delayed Start)' selected, then change to 'Automatic' because 'Delayed Start' is not supported.
            If (($StartMode -eq 'Automatic (Delayed Start)') -and ($Script:ADT.Environment.envOSVersionMajor -lt 6)) {
                $StartMode = 'Automatic'
            }

            Write-ADTLogEntry -Message "Set service [$Name] startup mode to [$StartMode]."

            ## Set the name of the start up mode that will be passed to sc.exe
            [String]$ScExeStartMode = $StartMode
            Switch ($StartMode) {
                'Automatic' {
                    $ScExeStartMode = 'Auto'; Break
                }
                'Automatic (Delayed Start)' {
                    $ScExeStartMode = 'Delayed-Auto'; Break
                }
                'Manual' {
                    $ScExeStartMode = 'Demand'; Break
                }
            }

            ## Set the start up mode using sc.exe. Note: we found that the ChangeStartMode method in the Win32_Service WMI class set services to 'Automatic (Delayed Start)' even when you specified 'Automatic' on Win7, Win8, and Win10.
            $ChangeStartMode = & "$env:WinDir\System32\sc.exe" config $Name start= $ScExeStartMode

            If ($global:LastExitCode -ne 0) {
                Throw "sc.exe failed with exit code [$($global:LastExitCode)] and message [$ChangeStartMode]."
            }

            Write-ADTLogEntry -Message "Successfully set service [$Name] startup mode to [$StartMode]."
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to set service [$Name] startup mode to [$StartMode]. `r`n$(Resolve-Error)" -Severity 3
            If (-not $ContinueOnError) {
                Throw "Failed to set service [$Name] startup mode to [$StartMode]: $($_.Exception.Message)"
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}

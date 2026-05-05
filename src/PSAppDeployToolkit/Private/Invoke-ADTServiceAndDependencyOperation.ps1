#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTServiceAndDependencyOperation
#
#-----------------------------------------------------------------------------

function Private:Invoke-ADTServiceAndDependencyOperation
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'SkipDependentServices', Justification = "This parameter is used within a child function that isn't immediately visible to PSScriptAnalyzer.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Name,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Start', 'Stop')]
        [System.String]$Operation,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipDependentServices,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateGreaterThanZero()]
        [System.TimeSpan]$PendingStatusWait = [System.TimeSpan]::FromSeconds(60),

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    # Internal worker function.
    function Invoke-ADTDependentServiceOperation
    {
        if ($SkipDependentServices)
        {
            return
        }

        # Discover all dependent services.
        Write-ADTLogEntry -Message "Discovering all dependent service(s) for service [$($Service.ServiceName)] which are not '$(($status = ('Stopped', 'Running')[$Operation -eq 'Start']))'."
        if (!($dependentServices = $Service.DependentServices | & { process { if ($_.Status -ne $status) { return $_ } } }))
        {
            Write-ADTLogEntry -Message "Dependent service(s) were not discovered for service [$($Service.ServiceName)]."
            return
        }

        # Action each found dependent service.
        foreach ($dependent in $dependentServices)
        {
            Write-ADTLogEntry -Message "$(('Stopping', 'Starting')[$Operation -eq 'Start']) dependent service [$($dependent.ServiceName)] with display name [$($dependent.DisplayName)] and a status of [$($dependent.Status)]."
            try
            {
                $dependent | & "$($Operation)-Service" -Force -WarningAction Ignore
            }
            catch
            {
                Write-ADTLogEntry -Message "Failed to $($Operation.ToLowerInvariant()) dependent service [$($dependent.ServiceName)] with display name [$($dependent.DisplayName)] and a status of [$($dependent.Status)]. Continue..." -Severity Warning
            }
        }
    }

    # Get the service object before continuing.
    $Service = Get-Service -Name $Name

    # Wait up to 60 seconds if service is in a pending state.
    if (($desiredStatus = @{ ContinuePending = [System.ServiceProcess.ServiceControllerStatus]::Running; PausePending = [System.ServiceProcess.ServiceControllerStatus]::Paused; StartPending = [System.ServiceProcess.ServiceControllerStatus]::Running; StopPending = [System.ServiceProcess.ServiceControllerStatus]::Stopped }[$Service.Status]))
    {
        Write-ADTLogEntry -Message "Waiting for up to [$($PendingStatusWait.TotalSeconds)] seconds to allow service pending status [$($Service.Status)] to reach desired status [$desiredStatus]."
        $Service.WaitForStatus($desiredStatus, $PendingStatusWait)
        $Service.Refresh()
    }

    # Discover if the service is currently running.
    Write-ADTLogEntry -Message "Service [$($Service.ServiceName)] with display name [$($Service.DisplayName)] has a status of [$($Service.Status)]."
    if (($Operation -eq 'Stop') -and ($Service.Status -ne [System.ServiceProcess.ServiceControllerStatus]::Stopped))
    {
        # Process all dependent services.
        Invoke-ADTDependentServiceOperation

        # Stop the parent service.
        Write-ADTLogEntry -Message "Stopping parent service [$($Service.ServiceName)] with display name [$($Service.DisplayName)]."
        $Service = $Service | Stop-Service -PassThru -WarningAction Ignore -Force
    }
    elseif (($Operation -eq 'Start') -and ($Service.Status -ne [System.ServiceProcess.ServiceControllerStatus]::Running))
    {
        # Start the parent service.
        Write-ADTLogEntry -Message "Starting parent service [$($Service.ServiceName)] with display name [$($Service.DisplayName)]."
        $Service = $Service | Start-Service -PassThru -WarningAction Ignore

        # Process all dependent services.
        Invoke-ADTDependentServiceOperation
    }

    # Return the service object if option selected.
    if ($PassThru)
    {
        return $Service
    }
}

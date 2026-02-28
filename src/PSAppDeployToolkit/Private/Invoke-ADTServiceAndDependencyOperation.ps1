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
        [PSAppDeployToolkit.Foundation.ValidateNotNullOrWhiteSpace()]
        [System.String]$Name,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Start', 'Stop')]
        [System.String]$Operation,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipDependentServices,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$PendingStatusWait = [System.TimeSpan]::FromSeconds(60),

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    # Internal worker function.
    function Invoke-ADTDependentServiceOperation
    {
        if (!$SkipDependentServices)
        {
            return
        }

        # Discover all dependent services.
        Write-ADTLogEntry -Message "Discovering all dependent service(s) for service [$Service] which are not '$(($status = ('Stopped', 'Running')[$Operation -eq 'Start']))'."
        if (!($dependentServices = Get-Service -Name $Service.get_ServiceName() -DependentServices | & { process { if ($_.get_Status() -ne $status) { return $_ } } }))
        {
            Write-ADTLogEntry -Message "Dependent service(s) were not discovered for service [$Service]."
            return
        }

        # Action each found dependent service.
        foreach ($dependent in $dependentServices)
        {
            Write-ADTLogEntry -Message "$(('Starting', 'Stopping')[$Operation -eq 'Start']) dependent service [$($dependent.get_ServiceName())] with display name [$($dependent.get_DisplayName())] and a status of [$($dependent.get_Status())]."
            try
            {
                $dependent | & "$($Operation)-Service" -Force -WarningAction Ignore
            }
            catch
            {
                Write-ADTLogEntry -Message "Failed to $($Operation.ToLowerInvariant()) dependent service [$($dependent.get_ServiceName())] with display name [$($dependent.get_DisplayName())] and a status of [$($dependent.get_Status())]. Continue..." -Severity Warning
            }
        }
    }

    # Get the service object before continuing.
    $Service = Get-Service -Name $Name

    # Wait up to 60 seconds if service is in a pending state.
    if (($desiredStatus = @{ ContinuePending = 'Running'; PausePending = 'Paused'; StartPending = 'Running'; StopPending = 'Stopped' }[$Service.get_Status()]))
    {
        Write-ADTLogEntry -Message "Waiting for up to [$($PendingStatusWait.TotalSeconds)] seconds to allow service pending status [$($Service.get_Status())] to reach desired status [$([System.ServiceProcess.ServiceControllerStatus]$desiredStatus)]."
        $Service.WaitForStatus($desiredStatus, $PendingStatusWait)
        $Service.Refresh()
    }

    # Discover if the service is currently running.
    Write-ADTLogEntry -Message "Service [$($Service.get_ServiceName())] with display name [$($Service.get_DisplayName())] has a status of [$($Service.get_Status())]."
    if (($Operation -eq 'Stop') -and ($Service.get_Status() -ne 'Stopped'))
    {
        # Process all dependent services.
        Invoke-ADTDependentServiceOperation

        # Stop the parent service.
        Write-ADTLogEntry -Message "Stopping parent service [$($Service.get_ServiceName())] with display name [$($Service.get_DisplayName())]."
        $Service = $Service | Stop-Service -PassThru -WarningAction Ignore -Force
    }
    elseif (($Operation -eq 'Start') -and ($Service.get_Status() -ne 'Running'))
    {
        # Start the parent service.
        Write-ADTLogEntry -Message "Starting parent service [$($Service.get_ServiceName())] with display name [$($Service.get_DisplayName())]."
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

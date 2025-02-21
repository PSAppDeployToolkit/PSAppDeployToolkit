#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTServiceAndDependencyOperation
#
#-----------------------------------------------------------------------------

function Invoke-ADTServiceAndDependencyOperation
{
    <#

    .SYNOPSIS
    Process Windows service and its dependencies.

    .DESCRIPTION
    Process Windows service and its dependencies.

    .PARAMETER Name
    Specify the name of the service.

    .PARAMETER SkipDependentServices
    Choose to skip checking for dependent services.

    .PARAMETER PendingStatusWait
    The amount of time to wait for a service to get out of a pending state before continuing. Default is 60 seconds.

    .PARAMETER PassThru
    Return the System.ServiceProcess.ServiceController service object.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.ServiceProcess.ServiceController. Returns the service object.

    .EXAMPLE
    Invoke-ADTServiceAndDependencyOperation -Name wuauserv -Operation Start

    .EXAMPLE
    Invoke-ADTServiceAndDependencyOperation -Name wuauserv -Operation Stop

    .LINK
    https://psappdeploytoolkit.com

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'SkipDependentServices', Justification = "This parameter is used within a child function that isn't immediately visible to PSScriptAnalyzer.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
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
        Write-ADTLogEntry -Message "Discovering all dependent service(s) for service [$Service] which are not '$(($status = if ($Operation -eq 'Start') {'Running'} else {'Stopped'}))'."
        if (!($dependentServices = Get-Service -Name $Service.ServiceName -DependentServices | & { process { if ($_.Status -ne $status) { return $_ } } }))
        {
            Write-ADTLogEntry -Message "Dependent service(s) were not discovered for service [$Service]."
            return
        }

        # Action each found dependent service.
        foreach ($dependent in $dependentServices)
        {
            Write-ADTLogEntry -Message "$(('Starting', 'Stopping')[$Operation -eq 'Start']) dependent service [$($dependent.ServiceName)] with display name [$($dependent.DisplayName)] and a status of [$($dependent.Status)]."
            try
            {
                $dependent | & "$($Operation)-Service" -Force -WarningAction Ignore
            }
            catch
            {
                Write-ADTLogEntry -Message "Failed to $($Operation.ToLower()) dependent service [$($dependent.ServiceName)] with display name [$($dependent.DisplayName)] and a status of [$($dependent.Status)]. Continue..." -Severity 2
            }
        }
    }

    # Get the service object before continuing.
    $Service = Get-Service -Name $Name

    # Wait up to 60 seconds if service is in a pending state.
    if (($desiredStatus = @{ ContinuePending = 'Running'; PausePending = 'Paused'; StartPending = 'Running'; StopPending = 'Stopped' }.($Service.Status)))
    {
        Write-ADTLogEntry -Message "Waiting for up to [$($PendingStatusWait.TotalSeconds)] seconds to allow service pending status [$($Service.Status)] to reach desired status [$([System.ServiceProcess.ServiceControllerStatus]$desiredStatus)]."
        $Service.WaitForStatus($desiredStatus, $PendingStatusWait)
        $Service.Refresh()
    }

    # Discover if the service is currently running.
    Write-ADTLogEntry -Message "Service [$($Service.ServiceName)] with display name [$($Service.DisplayName)] has a status of [$($Service.Status)]."
    if (($Operation -eq 'Stop') -and ($Service.Status -ne 'Stopped'))
    {
        # Process all dependent services.
        Invoke-ADTDependentServiceOperation

        # Stop the parent service.
        Write-ADTLogEntry -Message "Stopping parent service [$($Service.ServiceName)] with display name [$($Service.DisplayName)]."
        $Service = $Service | Stop-Service -PassThru -WarningAction Ignore -Force
    }
    elseif (($Operation -eq 'Start') -and ($Service.Status -ne 'Running'))
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

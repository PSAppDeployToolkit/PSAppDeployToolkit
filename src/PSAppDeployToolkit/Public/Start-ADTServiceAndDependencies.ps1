#-----------------------------------------------------------------------------
#
# MARK: Start-ADTServiceAndDependencies
#
#-----------------------------------------------------------------------------

function Start-ADTServiceAndDependencies
{
    <#
    .SYNOPSIS
        Start a Windows service and any service(s) that depend on it.

    .DESCRIPTION
        The `Start-ADTServiceAndDependencies` function starts a specified Windows service and any service(s) that depend on it. It provides options to skip starting dependent services, wait for a service to get out of a pending state, and return the service object.

    .PARAMETER Name
        Specifies the service name(s) of services to be stopped. Wildcards are permitted.

    .PARAMETER DisplayName
        Specifies the display name(s) of services to be stopped. Wildcards are permitted.

    .PARAMETER InputObject
        Specifies `ServiceController` object(s) representing the services to be started.

    .PARAMETER SkipDependentServices
        Specifies whether to skip checking for and starting dependent services.

    .PARAMETER PendingStatusWait
        The amount of time to wait for a service to get out of a pending state before continuing. Default is 60 seconds.

    .PARAMETER PassThru
        Return the `ServiceController` service object.

    .INPUTS
        System.ServiceProcess.ServiceController

        You can pipe `ServiceController` objects to this function.

    .OUTPUTS
        None

        By default, this function returns no output.

    .OUTPUTS
        System.ServiceProcess.ServiceController

        When the `-PassThru` parameter is provided, this function returns a ServiceController object representing the service that was started.

    .EXAMPLE
        Start-ADTServiceAndDependencies -Name 'wuauserv'

        Starts the Windows Update service and any service(s) that depend on it.

    .EXAMPLE
        Start-ADTServiceAndDependencies -DisplayName 'Windows Update'

        Starts the Windows Update service and any service(s) that depend on it.

    .EXAMPLE
        Start-ADTServiceAndDependencies -Name 'wuauserv' -PendingStatusWait 00:01:00

        Starts the Windows Update service and any service(s) that depend on it, waiting 1 minute for the serivce to start.

    .EXAMPLE
        Start-ADTServiceAndDependencies -Name 'wuauserv' -PendingStatusWait (New-TimeSpan -Minutes 1)

        Starts the Windows Update service and any service(s) that depend on it, waiting 1 minute for the serivce to start.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTServiceAndDependencies
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Name', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'DisplayName', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [CmdletBinding(SupportsShouldProcess = $true)]
    [OutputType([System.ServiceProcess.ServiceController])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [SupportsWildcards()]
        [Alias('Service')]
        [System.String[]]$Name,

        [Parameter(Mandatory = $true, ParameterSetName = 'DisplayName')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [SupportsWildcards()]
        [System.String[]]$DisplayName,

        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ParameterSetName = 'InputObject')]
        [ValidateScript({
                if ($null -eq $_)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName InputObject -ProvidedValue $_ -ExceptionMessage 'The provided input cannot be null.'))
                }
                if (!$_.ServiceName)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName InputObject -ProvidedValue $_ -ExceptionMessage 'The specified service does not exist.'))
                }
                return !!$_
            })]
        [System.ServiceProcess.ServiceController[]]$InputObject,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipDependentServices,

        [Parameter(Mandatory = $false)]
        [PSAppDeployToolkit.Attributes.ValidateGreaterThanZero()]
        [System.TimeSpan]$PendingStatusWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                $services = if ($PSCmdlet.ParameterSetName -ne 'InputObject')
                {
                    $gsParams = @{ $PSCmdlet.ParameterSetName = $PSBoundParameters.($PSCmdlet.ParameterSetName) }
                    Get-Service @gsParams
                }
                else
                {
                    $InputObject.Refresh()
                    $InputObject
                }

                foreach ($service in $services)
                {
                    try
                    {
                        try
                        {
                            if (($desiredStatus = $Script:ServiceStatusTable[$service.Status]))
                            {
                                Write-ADTLogEntry -Message "Waiting for up to [$($PendingStatusWait.TotalSeconds)] seconds to allow service pending status [$($service.Status)] to reach desired status [$desiredStatus]."
                                $service.WaitForStatus($desiredStatus, $PendingStatusWait)
                                $service.Refresh()
                            }

                            $dependentServices = if (!$SkipDependentServices)
                            {
                                $service.DependentServices | & { process { if (!$_.Status.Equals([System.ServiceProcess.ServiceControllerStatus]::Running)) { return $_ } } }
                            }

                            if ($service.Status.Equals([System.ServiceProcess.ServiceControllerStatus]::Running))
                            {
                                Write-ADTLogEntry -Message "Service [$($service.ServiceName)] with display name [$($service.DisplayName)] is already running."
                                if (!$dependentServices)
                                {
                                    if ($PassThru)
                                    {
                                        $PSCmdlet.WriteObject($service)
                                    }
                                    continue
                                }
                            }
                            else
                            {
                                Write-ADTLogEntry -Message "Service [$($service.ServiceName)] with display name [$($service.DisplayName)] has a status of [$($service.Status)]."
                            }

                            if (!$PSCmdlet.ShouldProcess($service.ServiceName, "Start service$(if (!$SkipDependentServices) { ' and dependencies' })"))
                            {
                                continue
                            }

                            if (!$service.Status.Equals([System.ServiceProcess.ServiceControllerStatus]::Running))
                            {
                                Write-ADTLogEntry -Message "Starting parent service [$($service.ServiceName)] with display name [$($service.DisplayName)]."
                                Start-Service -InputObject $service -PassThru:$PassThru -WarningAction Ignore
                            }

                            foreach ($dependent in $dependentServices)
                            {
                                Write-ADTLogEntry -Message "Starting dependent service [$($dependent.ServiceName)] with display name [$($dependent.DisplayName)] and a status of [$($dependent.Status)]."
                                try
                                {
                                    Start-Service -InputObject $dependent -WarningAction Ignore
                                }
                                catch
                                {
                                    Write-ADTLogEntry -Message "Failed to start dependent service [$($dependent.ServiceName)] with display name [$($dependent.DisplayName)] and a status of [$($dependent.Status)]. Continue..." -Severity Warning
                                }
                            }
                        }
                        catch
                        {
                            Write-Error -ErrorRecord $_
                        }
                    }
                    catch
                    {
                        Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to start the service [$($service.ServiceName)] with display name [$($service.DisplayName)]." -ErrorAction SilentlyContinue
                    }
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

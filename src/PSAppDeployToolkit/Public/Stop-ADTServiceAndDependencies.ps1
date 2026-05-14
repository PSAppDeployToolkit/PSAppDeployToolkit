#-----------------------------------------------------------------------------
#
# MARK: Stop-ADTServiceAndDependencies
#
#-----------------------------------------------------------------------------

function Stop-ADTServiceAndDependencies
{
    <#
    .SYNOPSIS
        Stop a Windows service and its dependencies.

    .DESCRIPTION
        The `Stop-ADTServiceAndDependencies` function stops a specified Windows service and its dependencies. It provides options to skip stopping dependent services, wait for a service to get out of a pending state, and return the service object.

    .PARAMETER Name
        Specifies the service name(s) of services to be stopped. Wildcards are permitted.

    .PARAMETER DisplayName
        Specifies the display name(s) of services to be stopped. Wildcards are permitted.

    .PARAMETER InputObject
        Specifies `ServiceController` object(s) representing the services to be stopped.

    .PARAMETER SkipDependentServices
        Specifies whether to skip checking for and stopping dependent services.

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

        When the `-PassThru` parameter is provided, this function returns a `ServiceController` object representing the service that was stopped.

    .EXAMPLE
        Stop-ADTServiceAndDependencies -Name 'wuauserv'

        Stops the Windows Update service and its dependencies.

    .EXAMPLE
        Stop-ADTServiceAndDependencies -DisplayName 'Windows Update'

        Stops the Windows Update service and its dependencies.

    .EXAMPLE
        Stop-ADTServiceAndDependencies -Name 'wuauserv' -PendingStatusWait 00:01:00

        Stops the Windows Update service and its dependencies, waiting 1 minute for the service to stop.

    .EXAMPLE
        Stop-ADTServiceAndDependencies -Name 'wuauserv' -PendingStatusWait (New-TimeSpan -Minutes 1)

        Stops the Windows Update service and its dependencies, waiting 1 minute for the service to stop.

    .NOTES
        An active ADT session is NOT required to use this function.

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Stop-ADTServiceAndDependencies
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Name', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'DisplayName', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'PendingStatusWait', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'PassThru', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
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
        $iasadoParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation -Exclude $PSCmdlet.ParameterSetName
    }

    process
    {
        try
        {
            try
            {
                $services = if ($PSCmdlet.ParameterSetName -eq 'InputObject')
                {
                    $InputObject.Refresh()
                    $InputObject
                }
                else
                {
                    $gsParams = @{ $PSCmdlet.ParameterSetName = Get-Variable -Name $PSCmdlet.ParameterSetName -ValueOnly }
                    Get-Service @gsParams
                }

                foreach ($service in $services)
                {
                    try
                    {
                        try
                        {
                            if ($PSCmdlet.ShouldProcess($service.ServiceName, "Stop service$(if (!$SkipDependentServices) { ' and dependencies' })"))
                            {
                                Invoke-ADTServiceAndDependencyOperation -Service $service -Operation Stop @iasadoParams
                            }
                        }
                        catch
                        {
                            Write-Error -ErrorRecord $_
                        }
                    }
                    catch
                    {
                        Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to stop the service [$($service.ServiceName)] with display name [$($service.DisplayName)]." -ErrorAction SilentlyContinue
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

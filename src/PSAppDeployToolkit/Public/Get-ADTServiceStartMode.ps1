#-----------------------------------------------------------------------------
#
# MARK: Get-ADTServiceStartMode
#
#-----------------------------------------------------------------------------

function Get-ADTServiceStartMode
{
    <#
    .SYNOPSIS
        Retrieves the startup mode of a specified service.

    .DESCRIPTION
        The `Get-ADTServiceStartMode` function retrieves the startup mode of a specified service. This function checks the service's start type and adjusts the result if the service is set to 'Automatic (Delayed Start)'.

    .PARAMETER Name
        Specifies the service name of the service to retrieve the start mode for.

    .PARAMETER DisplayName
        Specifies the display name of the service to retrieve the start mode for.

    .PARAMETER InputObject
        Specify the `ServiceController` object to retrieve the start mode for.

    .INPUTS
        System.ServiceProcess.ServiceController

        You can pipe `ServiceController` objects to this function.

    .OUTPUTS
        System.String

        Returns the startup mode of the specified service.

    .EXAMPLE
        Get-ADTServiceStartMode -InputObject (Get-Service -Name 'wuauserv')

        Retrieves the startup mode of the 'wuauserv' service.

    .EXAMPLE
        ```PowerShell
        if ((($service = Test-ADTServiceExists -Name 'ScreenConnect*' -PassThru) | Get-ADTServiceStartMode) -ne 'Automatic')
        {
            Set-ADTServiceStartMode -InputObject $service -StartMode 'Automatic'
        }
        ```

        Sets the ScreenConnect service start mode to automatic, if it exists and has its start mode is not automatic.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTServiceStartMode
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Name', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'DisplayName', Justification = "This parameter is accessed programmatically via the ParameterSet it's within, which PSScriptAnalyzer doesn't understand.")]
    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$Name,

        [Parameter(Mandatory = $true, ParameterSetName = 'DisplayName')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$DisplayName,

        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ParameterSetName = 'InputObject')]
        [ValidateScript({
                if (!$_.ServiceName)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Service -ProvidedValue $_ -ExceptionMessage 'The specified service does not exist.'))
                }
                return !!$_
            })]
        [Alias('Service')]
        [System.ServiceProcess.ServiceController]$InputObject
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
                if ($PSCmdlet.ParameterSetName -eq 'InputObject')
                {
                    $InputObject.Refresh()
                    $service = $InputObject
                }
                else
                {
                    $serviceName = $PSBoundParameters.($PSCmdlet.ParameterSetName)
                    if (!($service = Get-Service | & { process { if ($_.($PSCmdlet.ParameterSetName) -eq $serviceName) { return $_ } } }))
                    {
                        $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName $PSCmdlet.ParameterSetName -ProvidedValue $PSBoundParameters.($PSCmdlet.ParameterSetName) -ExceptionMessage 'The specified service does not exist.'))
                    }
                }

                Write-ADTLogEntry -Message "Getting startup mode for the service [$($service.ServiceName)] with display name [$($service.DisplayName)]."

                # Get the start mode and adjust it if the automatic type is delayed.
                if ((($serviceStartMode = $service.StartType) -eq 'Automatic') -and ((Get-ItemProperty -LiteralPath "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\$($service.ServiceName)" -ErrorAction Ignore | Select-Object -ExpandProperty DelayedAutoStart -ErrorAction Ignore) -eq 1))
                {
                    $serviceStartMode = 'Automatic (Delayed Start)'
                }

                # Return startup type to the caller.
                Write-ADTLogEntry -Message "Service [$($service.ServiceName)] startup mode is set to [$serviceStartMode]."
                return $serviceStartMode.ToString()
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

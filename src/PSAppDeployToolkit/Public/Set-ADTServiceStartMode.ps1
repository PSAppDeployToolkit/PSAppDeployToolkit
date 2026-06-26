#-----------------------------------------------------------------------------
#
# MARK: Set-ADTServiceStartMode
#
#-----------------------------------------------------------------------------

function Set-ADTServiceStartMode
{
    <#
    .SYNOPSIS
        Set the service startup mode.

    .DESCRIPTION
        The `Set-ADTServiceStartMode` function sets the service startup mode. This function allows you to configure the startup mode of a specified service. The startup modes available are: Automatic, Automatic (Delayed Start), Manual, Disabled, Boot, and System.

    .PARAMETER Name
        Specifies the service name(s) of the services to set the start mode for. Wildcards are permitted.

    .PARAMETER DisplayName
        Specifies the display name(s) of services to set the start mode for. Wildcards are permitted.

    .PARAMETER InputObject
        Specifies `ServiceController` object(s) representing the services to be started.

    .PARAMETER StartMode
        Specify startup mode for the service. Valid values for this parameter are: `Automatic`, `Automatic (Delayed Start)`, `Manual`, `Disabled`, `Boot`, `System`

    .PARAMETER PassThru
        Returns the `ServiceController` service object.

    .INPUTS
        System.ServiceProcess.ServiceController

        You can pipe `ServiceController` objects to this function.

    .OUTPUTS
        None

        By default, this function does not return any output.

    .OUTPUTS
        System.ServiceProcess.ServiceController

        When the `-PassThru` parameter is provided, this function returns a `ServiceController` object representing the service that was started.

    .EXAMPLE
        Set-ADTServiceStartMode -Name 'wuauserv' -StartMode 'Automatic (Delayed Start)'

        Sets the 'wuauserv' service to start automatically with a delayed start.

    .EXAMPLE
        Set-ADTServiceStartMode -DisplayName 'Windows Update' -StartMode 'Manual'

        Sets the Windows Update service to start manually.

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

        This function supports the `-WhatIf` and `-Confirm` parameters for testing changes before applying them.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Set-ADTServiceStartMode

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/src/PSAppDeployToolkit/Public/Set-ADTServiceStartMode.ps1
    #>

    [CmdletBinding(SupportsShouldProcess = $true)]
    [OutputType([System.ServiceProcess.ServiceController])]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [PSAppDeployToolkit.Attributes.ValidateUnique()]
        [SupportsWildcards()]
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
        [Alias('Service')]
        [System.ServiceProcess.ServiceController[]]$InputObject,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Automatic', 'Automatic (Delayed Start)', 'Manual', 'Disabled', 'Boot', 'System')]
        [System.String]$StartMode,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Re-write StartMode to suit sc.exe.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        New-Variable -Name StartMode -Force -Confirm:$false -Value $(switch ($StartMode)
            {
                'Automatic' { 'Auto'; break }
                'Automatic (Delayed Start)' { 'Delayed-Auto'; break }
                'Manual' { 'Demand'; break }
                default { $_; break }
            })
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
                    # Early return if the desired start mode is already set. Exclude automatic start modes since we aren't validating whether 'Automatic' means'Automatic' or 'Automatic (Delayed Start)'
                    if ((!$service.StartType.Equals([System.ServiceProcess.ServiceStartMode]::Automatic)) -and ($service.StartType -eq $PSBoundParameters.StartMode))
                    {
                        Write-ADTLogEntry -Message "The startup mode for the service [$($service.ServiceName)] with display name [$($service.DisplayName)] is already set to [$($service.StartType)]."
                        if ($PassThru)
                        {
                            $PSCmdlet.WriteObject($service)
                        }
                        continue
                    }

                    Write-ADTLogEntry -Message "$(($msg = "Setting service [$($service.ServiceName)] with display name [$($service.DisplayName)] startup mode to [$($PSBoundParameters.StartMode)]"))."
                    if (!$PSCmdlet.ShouldProcess($service.ServiceName, "Set service startup mode to [$($PSBoundParameters.StartMode)]"))
                    {
                        continue
                    }

                    try
                    {
                        try
                        {
                            # Set the start up mode using sc.exe. Note: we found that the ChangeStartMode method in the Win32_Service WMI class set services to 'Automatic (Delayed Start)' even when you specified 'Automatic' on Win7, Win8, and Win10.
                            $scResult = & "$([System.Environment]::SystemDirectory)\sc.exe" config $service.ServiceName start= $StartMode 2>&1
                            if ($Global:LASTEXITCODE)
                            {
                                # If we're here, we had a bad exit code.
                                Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$Global:LASTEXITCODE]: $scResult") -Severity Error
                                $naerParams = @{
                                    Exception = [System.Runtime.InteropServices.ExternalException]::new($msg, $Global:LASTEXITCODE)
                                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                                    ErrorId = 'ScConfigFailure'
                                    TargetObject = $scResult
                                    RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                                }
                                throw (New-ADTErrorRecord @naerParams)
                            }

                            Write-ADTLogEntry -Message "Successfully set service [$($service.ServiceName)] with display name [$($service.DisplayName)] startup mode to [$($PSBoundParameters.StartMode)]."
                            if ($PassThru)
                            {
                                $service.Refresh()
                                $PSCmdlet.WriteObject($service)
                            }
                        }
                        catch
                        {
                            Write-Error -ErrorRecord $_
                        }
                    }
                    catch
                    {
                        Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -ErrorAction SilentlyContinue
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

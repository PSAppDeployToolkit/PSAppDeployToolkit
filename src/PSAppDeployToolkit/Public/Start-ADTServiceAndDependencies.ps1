#-----------------------------------------------------------------------------
#
# MARK: Start-ADTServiceAndDependencies
#
#-----------------------------------------------------------------------------

function Start-ADTServiceAndDependencies
{
    <#
    .SYNOPSIS
        Start a Windows service and its dependencies.

    .DESCRIPTION
        This function starts a specified Windows service and its dependencies. It provides options to skip starting dependent services, wait for a service to get out of a pending state, and return the service object.

    .PARAMETER Name
        Specify the name of the service.

    .PARAMETER InputObject
        A ServiceController object to start.

    .PARAMETER SkipDependentServices
        Choose to skip checking for and starting dependent services.

    .PARAMETER PendingStatusWait
        The amount of time to wait for a service to get out of a pending state before continuing. Default is 60 seconds.

    .PARAMETER PassThru
        Return the System.ServiceProcess.ServiceController service object.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.ServiceProcess.ServiceController

        Returns the service object.

    .EXAMPLE
        Start-ADTServiceAndDependencies -Name 'wuauserv'

        Starts the Windows Update service and its dependencies.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Start-ADTServiceAndDependencies
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [ValidateNotNullOrEmpty()]
        [Alias('Service')]
        [System.String]$Name,

        [Parameter(Mandatory = $true, ParameterSetName = 'InputObject')]
        [ValidateScript({
                if (!$_.Name)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Service -ProvidedValue $_ -ExceptionMessage 'The specified service does not exist.'))
                }
                return !!$_
            })]
        [System.ServiceProcess.ServiceController]$InputObject,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipDependentServices,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$PendingStatusWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        if ($pipelining = $PSCmdlet.ParameterSetName.Equals('InputObject'))
        {
            $null = $PSBoundParameters.Remove('InputObject')
        }
    }

    process
    {
        try
        {
            try
            {
                if ($pipelining)
                {
                    $PSBoundParameters.Name = $InputObject.Name
                }
                Invoke-ADTServiceAndDependencyOperation -Operation Start @PSBoundParameters
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            $iafehParams = @{
                Cmdlet = $PSCmdlet
                SessionState = $ExecutionContext.SessionState
                ErrorRecord = $_
            }
            if ($pipelining)
            {
                $iafehParams.Add('LogMessage', "Failed to start the service [$($InputObject.Name)].")
            }
            else
            {
                $iafehParams.Add('LogMessage', "Failed to start the service [$($Name)].")
            }
            Invoke-ADTFunctionErrorHandler @iafehParams
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

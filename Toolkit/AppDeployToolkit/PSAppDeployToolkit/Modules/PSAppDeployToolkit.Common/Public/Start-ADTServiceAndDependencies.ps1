function Start-ADTServiceAndDependencies
{
    <#

    .SYNOPSIS
    Start Windows service and its dependencies.

    .DESCRIPTION
    Start Windows service and its dependencies.

    .PARAMETER Service
    Specify the name of the service.

    .PARAMETER SkipDependentServices
    Choose to skip checking for and starting dependent services. Default is: $false.

    .PARAMETER PendingStatusWait
    The amount of time to wait for a service to get out of a pending state before continuing. Default is 60 seconds.

    .PARAMETER PassThru
    Return the System.ServiceProcess.ServiceController service object.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.ServiceProcess.ServiceController. Returns the service object.

    .EXAMPLE
    Start-ADTServiceAndDependencies -Service 'wuauserv'

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
            if (!$_.Name)
            {
                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Service -ProvidedValue $_ -ExceptionMessage 'The specified service does not exist.'))
            }
            return !!$_
        })]
        [System.ServiceProcess.ServiceController]$Service,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SkipDependentServices,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$PendingStatusWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    # Dispatch this to the backend worker.
    Invoke-ADTServiceAndDependencyOperation -Operation Start @PSBoundParameters
}

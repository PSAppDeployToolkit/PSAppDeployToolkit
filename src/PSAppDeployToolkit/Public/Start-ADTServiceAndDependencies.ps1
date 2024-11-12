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

    .PARAMETER Service
        Specify the name of the service.

    .PARAMETER SkipDependentServices
        Choose to skip checking for and starting dependent services. Default is: $false.

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
        Start-ADTServiceAndDependencies -Service 'wuauserv'

        Starts the Windows Update service and its dependencies.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    param
    (
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
                Invoke-ADTServiceAndDependencyOperation -Operation Start @PSBoundParameters
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to start the service [$($Service.Name)]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

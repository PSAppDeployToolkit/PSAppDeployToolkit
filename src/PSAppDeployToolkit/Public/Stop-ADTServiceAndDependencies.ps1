﻿#-----------------------------------------------------------------------------
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
        This function stops a specified Windows service and its dependencies. It provides options to skip stopping dependent services, wait for a service to get out of a pending state, and return the service object.

    .PARAMETER Name
        Specify the name of the service.

    .PARAMETER SkipDependentServices
        Choose to skip checking for and stopping dependent services.

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
        Stop-ADTServiceAndDependencies -Name 'wuauserv'

        Stops the Windows Update service and its dependencies.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Stop-ADTServiceAndDependencies
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [Alias('Service')]
        [System.String]$Name,

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
                Invoke-ADTServiceAndDependencyOperation -Operation Stop @PSBoundParameters
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to stop the service [$($Name)]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

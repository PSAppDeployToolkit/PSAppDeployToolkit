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
        Retrieves the startup mode of a specified service. This function checks the service's start type and adjusts the result if the service is set to 'Automatic (Delayed Start)'.

    .PARAMETER Service
        Specify the service object to retrieve the startup mode for.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.String

        Returns the startup mode of the specified service.

    .EXAMPLE
        Get-ADTServiceStartMode -Service (Get-Service -Name 'wuauserv')

        Retrieves the startup mode of the 'wuauserv' service.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Get-ADTServiceStartMode
    #>

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
        [System.ServiceProcess.ServiceController]$Service
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        Write-ADTLogEntry -Message "Getting the service [$($Service.Name)] startup mode."
        try
        {
            try
            {
                # Get the start mode and adjust it if the automatic type is delayed.
                if ((($serviceStartMode = $Service.StartType) -eq 'Automatic') -and ((Get-ItemProperty -LiteralPath "Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\$($Service.Name)" -ErrorAction Ignore | Select-Object -ExpandProperty DelayedAutoStart -ErrorAction Ignore) -eq 1))
                {
                    $serviceStartMode = 'Automatic (Delayed Start)'
                }

                # Return startup type to the caller.
                Write-ADTLogEntry -Message "Service [$($Service.Name)] startup mode is set to [$serviceStartMode]."
                return $serviceStartMode
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

function Get-ADTServiceStartMode
{
    <#

    .SYNOPSIS
    Get the service startup mode.

    .DESCRIPTION
    Get the service startup mode.

    .PARAMETER Name
    Specify the name of the service.

    .PARAMETER ComputerName
    Specify the name of the computer. Default is: the local computer.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.ServiceProcess.ServiceController. Returns the service object.

    .EXAMPLE
    Get-ADTServiceStartMode -Name 'wuauserv'

    .LINK
    https://psappdeploytoolkit.com

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
        [System.ServiceProcess.ServiceController]$Service,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ComputerName
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        # Get the start mode and adjust it if the automatic type is delayed.
        Write-ADTLogEntry -Message "Getting the service [$($Service.Name)] startup mode."
        if ((($serviceStartMode = $Service.StartType) -eq 'Automatic') -and ((Get-ItemProperty -LiteralPath "Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\$Name" -ErrorAction Ignore | Select-Object -ExpandProperty DelayedAutoStart -ErrorAction Ignore) -eq 1))
        {
            $serviceStartMode = 'Automatic (Delayed Start)'
        }

        # Return startup type to the caller.
        Write-ADTLogEntry -Message "Service [$($Service.Name)] startup mode is set to [$serviceStartMode]."
        return $serviceStartMode
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

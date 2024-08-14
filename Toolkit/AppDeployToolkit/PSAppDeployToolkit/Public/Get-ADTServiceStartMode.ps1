#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Get-ADTServiceStartMode
{
    <#

    .SYNOPSIS
    Get the service startup mode.

    .DESCRIPTION
    Get the service startup mode.

    .PARAMETER Name
    Specify the name of the service.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.ServiceProcess.ServiceController. Returns the service object.

    .EXAMPLE
    Get-ADTServiceStartMode -Name 'wuauserv'

    .NOTES
    This function can be called without an active ADT session.

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
                if ((($serviceStartMode = $Service.StartType) -eq 'Automatic') -and ((& $Script:CommandTable.'Get-ItemProperty' -LiteralPath "Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\$Name" -ErrorAction Ignore | & $Script:CommandTable.'Select-Object' -ExpandProperty DelayedAutoStart -ErrorAction Ignore) -eq 1))
                {
                    $serviceStartMode = 'Automatic (Delayed Start)'
                }

                # Return startup type to the caller.
                Write-ADTLogEntry -Message "Service [$($Service.Name)] startup mode is set to [$serviceStartMode]."
                return $serviceStartMode
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
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

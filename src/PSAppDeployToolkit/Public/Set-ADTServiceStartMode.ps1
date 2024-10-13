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
        Set the service startup mode. This function allows you to configure the startup mode of a specified service. The startup modes available are: Automatic, Automatic (Delayed Start), Manual, Disabled, Boot, and System.

    .PARAMETER Service
        Specify the name of the service.

    .PARAMETER StartMode
        Specify startup mode for the service. Options: Automatic, Automatic (Delayed Start), Manual, Disabled, Boot, System.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Set-ADTServiceStartMode -Service 'wuauserv' -StartMode 'Automatic (Delayed Start)'

        Sets the 'wuauserv' service to start automatically with a delayed start.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

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

        [Parameter(Mandatory = $true)]
        [ValidateSet('Automatic', 'Automatic (Delayed Start)', 'Manual', 'Disabled', 'Boot', 'System')]
        [System.String]$StartMode
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue

        # Re-write StartMode to suit sc.exe.
        $StartMode = switch ($StartMode)
        {
            'Automatic' { 'Auto'; break }
            'Automatic (Delayed Start)' { 'Delayed-Auto'; break }
            'Manual' { 'Demand'; break }
            default { $_; break }
        }
    }

    process
    {
        Write-ADTLogEntry -Message "$(($msg = "Setting service [$($Service.Name)] startup mode to [$StartMode]"))."
        try
        {
            try
            {
                # Set the start up mode using sc.exe. Note: we found that the ChangeStartMode method in the Win32_Service WMI class set services to 'Automatic (Delayed Start)' even when you specified 'Automatic' on Win7, Win8, and Win10.
                $scResult = & "$([System.Environment]::SystemDirectory)\sc.exe" config $Service.Name start= $StartMode 2>&1
                if (!$LASTEXITCODE)
                {
                    Write-ADTLogEntry -Message "Successfully set service [($Service.Name)] startup mode to [$StartMode]."
                    return
                }

                # If we're here, we had a bad exit code.
                Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$LASTEXITCODE]: $scResult") -Severity 3
                $naerParams = @{
                    Exception = [System.ApplicationException]::new($msg)
                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                    ErrorId = 'ScConfigFailure'
                    TargetObject = $scResult
                    RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                }
                throw (New-ADTErrorRecord @naerParams)
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

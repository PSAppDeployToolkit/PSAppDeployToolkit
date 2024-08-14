function Set-ADTServiceStartMode
{
    <#

    .SYNOPSIS
    Set the service startup mode.

    .DESCRIPTION
    Set the service startup mode.

    .PARAMETER Name
    Specify the name of the service.

    .PARAMETER StartMode
    Specify startup mode for the service. Options: Automatic, Automatic (Delayed Start), Manual, Disabled, Boot, System.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .EXAMPLE
    Set-ADTServiceStartMode -Name 'wuauserv' -StartMode 'Automatic (Delayed Start)'

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Automatic', 'Automatic (Delayed Start)', 'Manual', 'Disabled', 'Boot', 'System')]
        [System.String]$StartMode
    )

    begin {
        # Make this function continue on error.
        if (!$PSBoundParameters.ContainsKey('ErrorAction'))
        {
            $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Continue
        }

        # Re-write StartMode to suit sc.exe.
        $StartMode = switch ($StartMode)
        {
            'Automatic' {
                'Auto'
                break
            }
            'Automatic (Delayed Start)' {
                'Delayed-Auto'
                break
            }
            'Manual' {
                'Demand'
                break
            }
            default {
                $_
                break
            }
        }
        Write-ADTDebugHeader
    }

    process {
        # Set the start up mode using sc.exe. Note: we found that the ChangeStartMode method in the Win32_Service WMI class set services to 'Automatic (Delayed Start)' even when you specified 'Automatic' on Win7, Win8, and Win10.
        Write-ADTLogEntry -Message "$(($msg = "Setting service [$Name] startup mode to [$StartMode]"))."
        $scResult = & "$env:WinDir\System32\sc.exe" config $Name start= $ScExeStartMode 2>&1
        if ($LASTEXITCODE)
        {
            Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$LASTEXITCODE]: $scResult") -Severity 3
            $naerParams = @{
                Exception = [System.ApplicationException]::new($msg)
                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId = 'ScConfigFailure'
                TargetObject = $scResult
                RecommendedAction = "Please review the result in this error's TargetObject property and try again."
            }
            $PSCmdlet.WriteError((New-ADTErrorRecord @naerParams))
        }
        else
        {
            Write-ADTLogEntry -Message "Successfully set service [$Name] startup mode to [$StartMode]."
        }
    }

    end {
        Write-ADTDebugFooter
    }
}

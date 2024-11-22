function Invoke-TerminalServerModeChange
{
    <#

    .SYNOPSIS
    Changes the mode for Remote Desktop Session Host/Citrix servers.

    .DESCRIPTION
    Changes the mode for Remote Desktop Session Host/Citrix servers.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .EXAMPLE
    Disable-TerminalServerInstallMode

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Install', 'Execute')]
        [System.String]$Mode
    )

    begin {
        # Make this function continue on error.
        $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
        if (!$PSBoundParameters.ContainsKey('ErrorAction'))
        {
            $PSBoundParameters.ErrorAction = [System.Management.Automation.ActionPreference]::Continue
        }
        Write-ADTDebugHeader
    }

    process {
        Write-ADTLogEntry -Message "$(($msg = "Changing terminal server into user $($Mode.ToLower()) mode"))."
        $terminalServerResult = & "$env:WinDir\System32\change.exe" User /$Mode
        if (!$LASTEXITCODE.Equals(1) -and ($PSBoundParameters.ErrorAction -notmatch '^(Ignore|SilentlyContinue)$'))
        {
            Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$LASTEXITCODE]: $terminalServerResult") -Severity 3
            if ($PSBoundParameters.ErrorAction.Equals([System.Management.Automation.ActionPreference]::Stop))
            {
                throw $msg
            }
        }
    }

    end {
        Write-ADTDebugFooter
    }
}

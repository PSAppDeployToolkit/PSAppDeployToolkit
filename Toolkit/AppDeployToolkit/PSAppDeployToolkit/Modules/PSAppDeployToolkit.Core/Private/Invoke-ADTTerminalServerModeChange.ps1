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

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Install', 'Execute')]
        [System.String]$Mode
    )

    begin {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -ErrorAction Continue
    }

    process {
        Write-ADTLogEntry -Message "$(($msg = "Changing terminal server into user $($Mode.ToLower()) mode"))."
        $terminalServerResult = & "$env:WinDir\System32\change.exe" User /$Mode 2>&1
        if (!$LASTEXITCODE.Equals(1))
        {
            Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$LASTEXITCODE]: $terminalServerResult") -Severity 3
            $naerParams = @{
                Exception = [System.ApplicationException]::new($msg)
                Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                ErrorId = 'RdsChangeUtilityFailure'
                TargetObject = $terminalServerResult
                RecommendedAction = "Please review the result in this error's TargetObject property and try again."
            }
            $PSCmdlet.WriteError((New-ADTErrorRecord @naerParams))
        }
    }

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}

#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTTerminalServerModeChange
#
#-----------------------------------------------------------------------------

function Private:Invoke-ADTTerminalServerModeChange
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Install', 'Execute')]
        [System.String]$Mode
    )

    # Change the terminal server mode. An exit code of 1 is considered successful.
    Write-ADTLogEntry -Message "$(($msg = "Changing terminal server into user $($Mode.ToLowerInvariant()) mode"))."
    $terminalServerResult = & "$([System.Environment]::SystemDirectory)\change.exe" User /$Mode 2>&1
    if ($Global:LASTEXITCODE.Equals(1))
    {
        return
    }

    # If we're here, we had a bad exit code.
    Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$Global:LASTEXITCODE]: $terminalServerResult") -Severity 3
    $naerParams = @{
        Exception = [System.Runtime.InteropServices.ExternalException]::new($msg, $Global:LASTEXITCODE)
        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
        ErrorId = 'RdsChangeUtilityFailure'
        TargetObject = $terminalServerResult
        RecommendedAction = "Please review the result in this error's TargetObject property and try again."
    }
    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
}

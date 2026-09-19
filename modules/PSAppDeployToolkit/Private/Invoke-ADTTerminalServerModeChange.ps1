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
    $terminalServerResult = Start-ADTProcess -FilePath "$([System.Environment]::SystemDirectory)\change.exe" -ArgumentList User, "/$Mode" -CreateNoWindow -PassThru -SuccessExitCodes 1 -InformationAction SilentlyContinue -ErrorAction Ignore -Confirm:$false
    if ($terminalServerResult.ExitCode.Equals(1))
    {
        return
    }

    # If we're here, we had a bad exit code.
    Write-ADTLogEntry -Message ($msg = "$msg failed with exit code [$($terminalServerResult.ExitCode)]: $($terminalServerResult.Interleaved)") -Severity Error
    $naerParams = @{
        Exception = [PSADT.ProcessManagement.ProcessException]::new($msg, $terminalServerResult)
        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
        ErrorId = 'RdsChangeUtilityFailure'
        TargetObject = $terminalServerResult
        RecommendedAction = "Please review the result in this error's TargetObject property and try again."
    }
    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
}

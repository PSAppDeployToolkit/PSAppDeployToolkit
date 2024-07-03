function Initialize-ADTFunction
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    # Write debug log messages.
    Write-ADTLogEntry -Message 'Function Start' -Source $Cmdlet.MyInvocation.MyCommand.Name -DebugMessage
    if ($CmdletBoundParameters = $Cmdlet.MyInvocation.BoundParameters | Format-Table -Property @{ Label = 'Parameter'; Expression = { "[-$($_.Key)]" } }, @{ Label = 'Value'; Expression = { $_.Value }; Alignment = 'Left' }, @{ Label = 'Type'; Expression = { $_.Value.GetType().Name }; Alignment = 'Left' } -AutoSize -Wrap | Out-String)
    {
        Write-ADTLogEntry -Message "Function invoked with bound parameter(s):`n$CmdletBoundParameters" -Source $Cmdlet.MyInvocation.MyCommand.Name -DebugMessage
    }
    else
    {
        Write-ADTLogEntry -Message 'Function invoked without any bound parameters.' -Source $Cmdlet.MyInvocation.MyCommand.Name -DebugMessage
    }

    # Amend the caller's $ErrorActionPreference to archive off their provided value so we can always stop on a dime.
    # For the caller-provided values, we deliberately use a string value to escape issues when 'Ignore' is passed.
    # https://github.com/PowerShell/PowerShell/issues/1759#issuecomment-442916350
    $Cmdlet.SessionState.PSVariable.Set('OriginalErrorAction', $(if ($Cmdlet.MyInvocation.BoundParameters.ContainsKey('ErrorAction'))
    {
        # Caller's value directly against the function.
        $Cmdlet.MyInvocation.BoundParameters.ErrorAction.ToString()
    }
    elseif ($PSBoundParameters.ContainsKey('ErrorAction'))
    {
        # A function's own specified override.
        $PSBoundParameters.ErrorAction.ToString()
    }
    else
    {
        # The module's default ErrorActionPreference.
        $Script:ErrorActionPreference
    }))
    $Cmdlet.SessionState.PSVariable.Set('ErrorActionPreference', $Script:ErrorActionPreference)
}
